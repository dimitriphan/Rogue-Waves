// Salvage, the pick between Tides (DESIGN "Salvage"). The host rolls each player's offers when a Salvage break starts,
// writes them into the SALVAGE command on that player's vessel (Goalstring = artifact indices, Goalvalue = the Tide), and
// holds the break's clock until everyone has picked or SalvageSeconds run out (then it picks at random for them). Every
// machine watches its own vessel's command: new offers open the game's choice dialog, and a pick goes back to the host as
// Command_ServerRpc("Riptide_Salvage", {"Pick":n}), which the game only lets the vessel's owner send.
public static class RiptideSalvage
{
    // Artifacts that may be offered: loaded, not held by the team, not on the never-offer list, and (once probe 3 and
    // a hand-played run have made it) on the whitelist. An empty whitelist means "every loaded artifact" for now.
    public static readonly List<string> Whitelist = new List<string>();

    // ---------------------------------------------------------------- the Salvage on the host

    public static bool Enabled = true;     // the self-test turns it off until its own Salvage check
    public static int Open;                // the Tide whose Salvage is open, 0 when none
    public static float OpenedAt, HoldWave;
    public static readonly Dictionary<long, List<string>> Pending = new Dictionary<long, List<string>>();   // session -> offers
    public static readonly Dictionary<long, string> Picks = new Dictionary<long, string>();                 // this Salvage's picks
    static System.Random rng = new System.Random(0);
    static readonly List<Entity_Master> listening = new List<Entity_Master>();
    static float nextEnsure;

    public static void BeginRun(int seed)
    {
        rng = new System.Random(seed ^ 0x5a17);
        Open = 0;
        Pending.Clear();
        Picks.Clear();
        listening.Clear();
    }

    // The break after Tide 'tide' has started: roll every player's offers (none offered twice) and hold the clock.
    public static void Begin(int tide)
    {
        if (!Enabled || Open != 0 || Blm_Server.Instance == null || Game_Mapvars.Instance == null)
            return;
        List<long> sessions = new List<long>(Blm_Server.Instance.Sessionidsplayer.Keys);
        sessions.Sort();
        if (sessions.Count == 0)
            return;
        int count = RiptideData.IsBreaker(tide) ? RiptideData.BreakerOffers : RiptideData.Offers;
        List<string> taken = new List<string>();
        Pending.Clear();
        Picks.Clear();
        List<string> lines = new List<string>();
        foreach (long session in sessions)
        {
            List<string> offers = Offers(rng, count, taken);
            taken.AddRange(offers);
            if (offers.Count < count)
                offers.Add(RiptideData.Driftwood);      // one Driftwood card fills the gap
            Pending[session] = offers;
            lines.Add(session + ": " + string.Join(", ", offers.ToArray()));
        }
        Open = tide;
        OpenedAt = Time.time;
        HoldWave = Game_Mapvars.Instance.Time_Wave.Value;
        Warn(RiptideData.SalvageWarning, "");
        nextEnsure = 0f;
        RiptideLog.Info("Salvage after Tide " + tide + " opens: " + string.Join(" | ", lines.ToArray()));
    }

    // Host, every frame of a run: the SALVAGE button on every vessel, the clock hold, the auto-pick.
    public static void Tick()
    {
        if (!RiptideUtil.InRun || !Game_Entity.Instance.IsServer || !RiptideDirector.Running)
            return;
        if (Time.time >= nextEnsure)
        {
            nextEnsure = Time.time + 0.5f;
            foreach (Entity_Master hero in RiptideUtil.Tagged("Hero"))
                Ensure(hero);
        }
        if (Open == 0)
            return;
        Game_Mapvars mv = Game_Mapvars.Instance;
        if (Time.time - OpenedAt < RiptideData.SalvageSeconds)
        {
            if (mv.Time_Wave.Value < HoldWave)
                mv.Time_Wave.Value = HoldWave;
        }
        else
        {
            List<long> left = new List<long>(Pending.Keys);
            left.Sort();
            foreach (long session in left)
                Take(session, rng.Next(Pending[session].Count), true);
        }
        if (Pending.Count == 0)
            Close();
    }

    public static void Close()
    {
        if (Open == 0)
            return;
        List<string> got = new List<string>();
        foreach (KeyValuePair<long, string> kv in Picks)
            got.Add(kv.Key + ": " + kv.Value);
        RiptideLog.Info("Salvage after Tide " + Open + " closes after " + RiptideUtil.F(Time.time - OpenedAt) + " s: " + string.Join(", ", got.ToArray()));
        Open = 0;
        Pending.Clear();
        nextEnsure = 0f;
        // Grey every SALVAGE button now: Tick stops once the run is over, and a dialog still open closes with it.
        if (RiptideUtil.InMatch && Game_Entity.Instance.IsServer)
            foreach (Entity_Master hero in RiptideUtil.Tagged("Hero"))
                Ensure(hero);
    }

    // The vessel's SALVAGE command: created once per vessel (a respawned vessel is a new entity), showing its owner's
    // offers while they have some. Clients get it through the synced Commandlist.
    static void Ensure(Entity_Master hero)
    {
        // The game skips command updates on a dead entity, so a fallen vessel gets its offers when it stands again.
        if (hero == null || hero.Commandlistserver == null || hero.Beingserver == null || hero.Beingserver.Dead)
            return;
        Link_Command link;
        if (!hero.Commandlistserver.TryGetValue(RiptideData.SalvageLink, out link))
        {
            if (RiptideCommands.Salvage == null)
                return;
            link = Helper_Entitytools.Createcommand(hero, RiptideData.SalvageCommand, RiptideData.SalvageLink, false, true);
            if (link == null)
                return;
            link.Goalstring = "";
            link.Goalvalue = 0f;
            Helper_Entitytools.Updatecommand(hero, RiptideData.SalvageLink, "", true, true);
        }
        if (!listening.Contains(hero))
        {
            listening.RemoveAll(h => h == null);
            listening.Add(hero);
            Entity_Master me = hero;
            hero.Command += (command, json, server) => OnCommand(me, command, json, server);
        }
        List<string> offers;
        bool has = Open != 0 && Pending.TryGetValue(hero.Entitydata.Value.Owner, out offers);
        string code = has ? Encode(Pending[hero.Entitydata.Value.Owner]) : "";
        float value = has ? Open : 0f;
        if ((link.Goalstring ?? "") != code || link.Goalvalue != value)
        {
            link.Goalstring = code;
            link.Goalvalue = value;
            Helper_Entitytools.Updatecommand(hero, RiptideData.SalvageLink, "", false, false);
        }
    }

    // Runs on the host. A player's pick arrives through Command_ServerRpc, which checks the sender owns the vessel and
    // raises this with server = false (that flag means "the host issued it", not "running on the host").
    static void OnCommand(Entity_Master hero, string command, string json, bool server)
    {
        if (command != RiptideData.SalvageLink || Open == 0 || hero == null || Game_Entity.Instance == null || !Game_Entity.Instance.IsServer)
            return;
        int k = JsonInt(json, "Pick");
        if (k >= 0)
            Take(hero.Entitydata.Value.Owner, k, false);
    }

    static void Take(long session, int k, bool auto)
    {
        List<string> offers;
        if (!Pending.TryGetValue(session, out offers) || k < 0 || k >= offers.Count)
            return;
        string id = offers[k];
        Pending.Remove(session);
        string why = id == RiptideData.Driftwood ? GiveDriftwood(session) : RiptideArtifacts.Grant(id);
        if (why != "" && id != RiptideData.Driftwood)
        {
            RiptideLog.Warn("Salvage: " + id + " could not be granted (" + why + "); Driftwood instead");
            id = RiptideData.Driftwood;
            GiveDriftwood(session);
        }
        Picks[session] = id + (auto ? " (auto)" : "");
        nextEnsure = 0f;
    }

    static string GiveDriftwood(long session)
    {
        List<Link_Resourceset> aura = new List<Link_Resourceset>();
        foreach (string name in RiptideData.Auras)
        {
            Link_Resourceset r = new Link_Resourceset();
            r.Name = name;
            r.Amount = RiptideData.DriftwoodAura(Open);
            aura.Add(r);
        }
        Helper_Resources.Giveresources(aura, session, Helper_Resources.Styles.Default);
        Warn(RiptideData.DriftwoodName + RiptideData.SalvagedWarning, "");
        return "";
    }

    static void Warn(string text, string icon)
    {
        List<Entity_Master> cores = RiptideUtil.Tagged("Core");
        if (cores.Count > 0 && cores[0].Radio != null)
            cores[0].Radio.Uiwarning_ClientRpc(text, icon, 3f);
    }

    // Offers travel as indices into RiptideContent.Artifacts (built the same, sorted, on every machine) to fit the
    // command's 61-byte Goalstring: "12.30.4"; "d" is Driftwood.
    public static string Encode(List<string> offers)
    {
        List<string> parts = new List<string>();
        foreach (string id in offers)
            parts.Add(id == RiptideData.Driftwood ? "d" : RiptideContent.Artifacts.IndexOf(id).ToString());
        return string.Join(".", parts.ToArray());
    }

    public static List<string> Decode(string code)
    {
        List<string> offers = new List<string>();
        if (string.IsNullOrEmpty(code))
            return offers;
        foreach (string part in code.Split('.'))
        {
            int i;
            if (part == "d") offers.Add(RiptideData.Driftwood);
            else if (int.TryParse(part, out i) && i >= 0 && i < RiptideContent.Artifacts.Count) offers.Add(RiptideContent.Artifacts[i]);
        }
        return offers;
    }

    static int JsonInt(string json, string key)
    {
        string k = "\"" + key + "\":";
        int at = json != null ? json.IndexOf(k) : -1;
        if (at < 0)
            return -1;
        int start = at + k.Length, end = start;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
        int v;
        return int.TryParse(json.Substring(start, end - start), out v) ? v : -1;
    }

    // ---------------------------------------------------------------- the dialog, on every machine

    public static bool Asking;
    static bool abandon;
    static int askedFor, asks;
    static Bla_Ui_Confirmation.Entry firstCard;

    // This machine's vessel and its SALVAGE command as synced.
    public static Entity_Master Mine(out Link_Commandref link)
    {
        link = default(Link_Commandref);
        if (Game_Entity.Instance == null || Blm_Server.Instance == null || Blm_Server.Instance.Localplayer == null)
            return null;
        long me = Blm_Server.Instance.Localplayer.Sessionid.Value;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
        {
            if (e == null || e.Commandlist == null || e.Entitydata.Value.Owner != me)
                continue;
            foreach (Link_Commandref c in e.Commandlist)
                if (c.Name.ToString() == RiptideData.SalvageLink)
                {
                    link = c;
                    return e;
                }
        }
        return null;
    }

    static float nextLook;

    public static void ClientTick()
    {
        if (!RiptideUtil.InRun)
        {
            askedFor = 0;
            return;
        }
        if (Time.unscaledTime < nextLook)
            return;
        nextLook = Time.unscaledTime + 0.2f;
        Link_Commandref link;
        Entity_Master hero = Mine(out link);
        if (hero == null)
            return;
        string code = link.Goalstring.ToString();
        int tide = (int)link.Goalvalue;
        if (code == "")
        {
            askedFor = 0;       // nothing to pick: the next Salvage opens the dialog again
            if (Asking && !abandon)
                Abandon();      // the host picked for this player (time ran out)
        }
        else if (!Asking && tide != askedFor)
            Ask(hero, code, tide);
    }

    // The SALVAGE button: reopens the dialog while this player still has offers, also when another menu closed it.
    public static void Reopen()
    {
        Link_Commandref link;
        Entity_Master hero = Mine(out link);
        if (hero != null && (!Asking || !DialogOpen()) && link.Goalstring.ToString() != "")
            Ask(hero, link.Goalstring.ToString(), (int)link.Goalvalue);
    }

    // Whether the game's choice dialog is the menu on screen (a closed menu keeps its cards, with its canvas off).
    public static bool DialogOpen()
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        Blu2_Menu_Handler menu;
        return dialog != null && Bl_Menus.Menus != null && Bl_Menus.Menus.TryGetValue(dialog.gameObject.name, out menu) && menu != null && menu.Active;
    }

    static void Ask(Entity_Master hero, string code, int tide)
    {
        askedFor = tide;
        if (RiptideMod.Instance != null)
            RiptideMod.Instance.StartCoroutine(AskRoutine(hero, Decode(code)));
    }

    static IEnumerator AskRoutine(Entity_Master hero, List<string> offers)
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        if (dialog == null || offers.Count == 0)
            yield break;
        int ask = ++asks;       // a newer Ask (the SALVAGE button) takes over the dialog from this one
        Asking = true;
        abandon = false;
        List<Bla_Ui_Confirmation.Entry> cards = new List<Bla_Ui_Confirmation.Entry>();
        foreach (string id in offers)
            cards.Add(Card(id));
        firstCard = cards[0];
        Cysharp.Threading.Tasks.UniTask<Bla_Ui_Confirmation.Entry> task;
        try { task = dialog.Runprompt(cards, RiptideData.SalvageTitle, RiptideData.SalvageSub, true); }
        catch (Exception e)
        {
            RiptideLog.Warn("Salvage dialog: " + e.Message);
            Asking = false;
            yield break;
        }
        // Centred as soon as the cards exist, and again once the game's layout and text have settled.
        Place(dialog, offers.Count);
        yield return null;
        yield return null;
        Place(dialog, offers.Count);
        float settle = Time.unscaledTime + 0.5f;
        while (task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Pending && ask == asks && !(abandon && !DialogOpen()))
        {
            if (settle > 0f && Time.unscaledTime >= settle)
            {
                settle = 0f;
                Place(dialog, offers.Count);
                if (!placedLogged)
                {
                    placedLogged = true;
                    try { RiptideLog.Info("Salvage dialog: " + Layout(dialog)); } catch (Exception) { }
                }
            }
            yield return null;
        }
        if (ask != asks)
            yield break;
        Unfit(dialog);
        Asking = false;
        if (abandon || task.Status != Cysharp.Threading.Tasks.UniTaskStatus.Succeeded || hero == null)
            yield break;
        Bla_Ui_Confirmation.Entry chosen = task.GetAwaiter().GetResult();
        int k = chosen != null ? offers.IndexOf(chosen.Name) : -1;
        Entity_Master_Remote remote = hero.GetComponent<Entity_Master_Remote>();
        if (k >= 0 && remote != null)
            remote.Command_ServerRpc(RiptideData.SalvageLink, "{\"Pick\":" + k + "}");
    }

    static bool placedLogged;

    static void Place(Bla_Ui_Confirmation dialog, int cards)
    {
        try { Centre(dialog, cards); }
        catch (Exception e) { RiptideLog.Warn("Salvage dialog layout: " + e.Message); }
    }

    // Closes an open Salvage dialog without sending a pick: clicks its first card, which ends the game's prompt cleanly,
    // or closes the menu outright if the card is gone.
    static void Abandon()
    {
        abandon = true;
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        if (dialog == null || dialog.List == null)
            return;
        Transform card = firstCard != null ? dialog.List.transform.Find(firstCard.Name) : null;
        Blu3_Button button = card != null ? card.GetComponent<Blu3_Button>() : null;
        bool clicked = button != null && button.Buttonclick != null;
        if (clicked)
            button.Buttonclick();
        else if (DialogOpen())
            Bl_Menus.Closemenu(dialog.gameObject, true);
        RiptideLog.Info("Salvage dialog closed: the host picked for this player" + (clicked ? "" : " (menu closed directly)"));
    }

    public static List<string> Pool()
    {
        List<string> pool = new List<string>();
        foreach (string id in RiptideContent.Artifacts)
        {
            if (System.Array.IndexOf(RiptideData.NeverOffer, id) >= 0 || RiptideArtifacts.Held(id))
                continue;
            if (Whitelist.Count > 0 && !Whitelist.Contains(id))
                continue;
            pool.Add(id);
        }
        return pool;
    }

    // One player's offers: 'count' distinct artifacts from the pool, none of them in 'taken' (offered to someone else
    // in the same Salvage). Fewer than asked when the pool runs dry; the empty slots become Driftwood.
    public static List<string> Offers(System.Random rng, int count, List<string> taken)
    {
        List<string> pool = Pool();
        if (taken != null)
            pool.RemoveAll(id => taken.Contains(id));
        List<string> offers = new List<string>();
        while (offers.Count < count && pool.Count > 0)
        {
            int i = rng.Next(pool.Count);
            offers.Add(pool[i]);
            pool.RemoveAt(i);
        }
        return offers;
    }

    // A card for the game's choice dialog (Bla_Ui_Confirmation), from an artifact's own entry. The game's cards show only
    // an icon on a coloured tile (the pause menu's Exit/Back), and hovering one puts its Display_Desc in the dialog's
    // sub line: so the tile takes the artifact's slot colour and the hover line names it, the way the unlock popup does
    // ("AURA GAVEL ▪ RED").
    public static Bla_Ui_Confirmation.Entry Card(string id)
    {
        Bla_Ui_Confirmation.Entry card = new Bla_Ui_Confirmation.Entry();
        card.Name = id;
        if (id == RiptideData.Driftwood)
        {
            card.Display_Name = RiptideData.DriftwoodName;
            card.Display_Desc = RiptideData.DriftwoodName + " ▪ " + RiptideData.DriftwoodDesc;
            card.Display_Image = RiptideCommands.IconSprite;
            card.Display_Color = RiptideData.Sea;
            return card;
        }
        Artifactentryobject e = RiptideArtifacts.Entry(id);
        if (e == null)
        {
            card.Display_Name = id;
            card.Display_Desc = id;
            return card;
        }
        string name = Bla_Localization.GetText(e.Display_Name).ToUpper();
        card.Display_Name = name;
        card.Display_Desc = name + " ▪ " + Bla_Localization.GetText(e.Display_Desc);
        card.Display_Image = e.Display_Icon;
        try
        {
            Iconobject.Icon slot = Helper_Uitools.Geticondetail(e.Type);
            if (slot != null)
                card.Display_Color = slot.Color;
        }
        catch (Exception) { }
        return card;
    }

    // ---------------------------------------------------------------- placing the dialog

    // The game shows this dialog for the pause menu's two-card prompts: in the bottom-right corner, its cards in a grid of
    // two columns, so a Salvage's three cards sat far off centre as two plus one (Dimitri, 2026-10-05). For a Salvage,
    // Centre lays the cards out in one row and moves the dialog's title, cards and hover line together to the middle of
    // the screen. Unfit puts it all back before the pause menu uses the dialog again.
    public const int MaxRow = 4;
    static readonly List<RectTransform> movedParts = new List<RectTransform>();
    static readonly List<Vector2> movedFrom = new List<Vector2>();
    static UnityEngine.UI.GridLayoutGroup grid;
    static UnityEngine.UI.GridLayoutGroup.Constraint gridConstraint;
    static int gridCount;
    static Vector2 listSize;

    // The cards on show: the list's active buttons (the list also keeps the subtitle, which ignores its layout).
    static List<RectTransform> Cards(Bla_Ui_Confirmation dialog)
    {
        List<RectTransform> cards = new List<RectTransform>();
        if (dialog != null && dialog.List != null)
            foreach (Transform c in dialog.List.transform)
                if (c.gameObject.activeInHierarchy && c.GetComponent<Blu3_Button>() != null && c is RectTransform)
                    cards.Add((RectTransform)c);
        return cards;
    }

    // The dialog's children that hold its title, subtitle, hover line and cards: what Centre moves.
    static List<RectTransform> Parts(Bla_Ui_Confirmation dialog)
    {
        List<RectTransform> parts = new List<RectTransform>();
        foreach (GameObject piece in new GameObject[] { dialog.List, dialog.Titlebox, dialog.Subbox, dialog.Descbox })
        {
            if (piece == null || piece == dialog.gameObject)
                continue;
            Transform p = piece.transform;
            while (p.parent != null && p.parent != dialog.transform)
                p = p.parent;
            RectTransform part = (p.parent == dialog.transform ? p : piece.transform) as RectTransform;
            if (part != null && !parts.Contains(part))
                parts.Add(part);
        }
        return parts;
    }

    public static Rect OnScreen(RectTransform rt, out Canvas canvas)
    {
        canvas = rt.GetComponentInParent<Canvas>();
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return OnScreen(corners, canvas);
    }

    static Camera Cam(Canvas canvas)
    {
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }

    static Rect OnScreen(Vector3[] corners, Canvas canvas)
    {
        Camera cam = Cam(canvas);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 c in corners)
        {
            Vector2 s = RectTransformUtility.WorldToScreenPoint(cam, c);
            min = Vector2.Min(min, s);
            max = Vector2.Max(max, s);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // Where a text box's words are drawn (not its whole box, which may be wider than the words), or false when it's empty.
    static bool Words(GameObject box, out Rect r)
    {
        r = new Rect();
        TMPro.TMP_Text text = box != null && box.activeInHierarchy ? box.GetComponentInChildren<TMPro.TMP_Text>() : null;
        if (text == null || string.IsNullOrEmpty(text.text) || text.text.Trim() == "")
            return false;
        Bounds b = text.textBounds;
        if (b.size.x <= 0f || b.size.y <= 0f)
            return false;
        Transform t = text.transform;
        Vector3[] corners = { t.TransformPoint(new Vector3(b.min.x, b.min.y, 0f)), t.TransformPoint(new Vector3(b.min.x, b.max.y, 0f)),
            t.TransformPoint(new Vector3(b.max.x, b.max.y, 0f)), t.TransformPoint(new Vector3(b.max.x, b.min.y, 0f)) };
        r = OnScreen(corners, text.GetComponentInParent<Canvas>());
        return true;
    }

    // The screen rect around the cards and the title and subtitle's words.
    static Rect Block(Bla_Ui_Confirmation dialog, List<RectTransform> cards, out Canvas canvas)
    {
        canvas = null;
        bool any = false;
        Rect block = new Rect();
        foreach (RectTransform card in cards)
        {
            Rect r = OnScreen(card, out canvas);
            block = any ? Union(block, r) : r;
            any = true;
        }
        foreach (GameObject box in new GameObject[] { dialog.Titlebox, dialog.Subbox })
        {
            Rect r;
            if (Words(box, out r))
            {
                block = any ? Union(block, r) : r;
                any = true;
            }
        }
        return block;
    }

    static Rect Union(Rect a, Rect b)
    {
        return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
    }

    // One row of cards (up to MaxRow), in a list widened to hold the row, so the subtitle stays centred over the cards.
    // (The game's compiler has no TextAnchor, so the grid's alignment can't be changed instead.)
    static void Row(Bla_Ui_Confirmation dialog, int n)
    {
        UnityEngine.UI.GridLayoutGroup g = dialog.List.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        RectTransform list = dialog.List.transform as RectTransform;
        if (g == null || list == null)
            return;
        if (grid != g)
        {
            Unrow();
            grid = g;
            gridConstraint = g.constraint;
            gridCount = g.constraintCount;
            listSize = list.sizeDelta;
        }
        int columns = Mathf.Clamp(n, 1, MaxRow);
        g.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = columns;
        float width = g.padding.left + g.padding.right + columns * g.cellSize.x + (columns - 1) * g.spacing.x;
        if (list.rect.width < width - 0.5f)
            list.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    static void Unrow()
    {
        if (grid != null)
        {
            grid.constraint = gridConstraint;
            grid.constraintCount = gridCount;
            RectTransform list = grid.transform as RectTransform;
            if (list != null)
                list.sizeDelta = listSize;
        }
        grid = null;
    }

    // Lays the cards out in a row and moves the dialog so the block sits in the middle of the screen. Safe to call again:
    // it only moves what is off centre, and remembers where each part started. With fresh > 0, only the list's last fresh
    // buttons are this prompt's cards: the ones before them are the last prompt's, which the game destroys at the end of
    // the frame, so they are hidden now rather than taking the first cells of the row.
    public static void Centre(Bla_Ui_Confirmation dialog, int fresh = 0)
    {
        if (fresh > 0)
        {
            List<RectTransform> all = Cards(dialog);
            for (int i = 0; i < all.Count - fresh; i++)
                all[i].gameObject.SetActive(false);
        }
        List<RectTransform> cards = Cards(dialog);
        if (cards.Count == 0)
            return;
        Row(dialog, cards.Count);
        Canvas.ForceUpdateCanvases();
        RectTransform root = dialog.transform as RectTransform;
        if (root != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        Canvas canvas;
        Rect r = Block(dialog, cards, out canvas);
        Vector2 middle = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if ((middle - r.center).magnitude <= 2f)
            return;
        Camera cam = Cam(canvas);
        foreach (RectTransform part in Parts(dialog))
        {
            RectTransform parent = part.parent as RectTransform;
            Vector3 from, to;
            if (parent == null || !RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, r.center, cam, out from)
                || !RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, middle, cam, out to))
                continue;
            if (!movedParts.Contains(part))
            {
                movedParts.Add(part);
                movedFrom.Add(part.anchoredPosition);
            }
            part.position += to - from;
        }
    }

    // Centres the dialog, then says whether it sits in the middle of the screen with its cards in one row.
    public static bool Fit(Bla_Ui_Confirmation dialog)
    {
        Centre(dialog);
        return Centred(dialog);
    }

    // Whether the dialog sits in the middle of the screen, all on it, with its cards in one row.
    public static bool Centred(Bla_Ui_Confirmation dialog)
    {
        List<RectTransform> cards = Cards(dialog);
        if (cards.Count == 0)
            return false;
        Canvas canvas;
        Rect r = Block(dialog, cards, out canvas);
        Vector2 middle = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        bool onScreen = r.yMin >= 0f && r.yMax <= Screen.height && r.xMin >= 0f && r.xMax <= Screen.width;
        return onScreen && (middle - r.center).magnitude <= 0.03f * Screen.height && (cards.Count > MaxRow || OneRow(cards));
    }

    static bool OneRow(List<RectTransform> cards)
    {
        Canvas canvas;
        Rect first = OnScreen(cards[0], out canvas);
        foreach (RectTransform card in cards)
            if (Mathf.Abs(OnScreen(card, out canvas).center.y - first.center.y) > 0.25f * first.height)
                return false;
        return true;
    }

    public static void Unfit(Bla_Ui_Confirmation dialog)
    {
        for (int i = 0; i < movedParts.Count; i++)
            if (movedParts[i] != null)
                movedParts[i].anchoredPosition = movedFrom[i];
        movedParts.Clear();
        movedFrom.Clear();
        Unrow();
    }

    // Where the dialog's parts are and how its list lays out, to check Unfit puts the game's dialog back as it was.
    public static string Placement(Bla_Ui_Confirmation dialog)
    {
        List<string> parts = new List<string>();
        foreach (RectTransform part in Parts(dialog))
            parts.Add(part.name + " " + part.anchoredPosition);
        UnityEngine.UI.GridLayoutGroup g = dialog.List != null ? dialog.List.GetComponent<UnityEngine.UI.GridLayoutGroup>() : null;
        RectTransform list = dialog.List != null ? dialog.List.transform as RectTransform : null;
        return string.Join(", ", parts.ToArray()) + (g != null ? ", grid " + g.constraint + " " + g.constraintCount : "") + (list != null ? ", list size " + list.sizeDelta : "");
    }

    // For the debug log: where the dialog's pieces sit on the screen and what lays them out.
    public static string Layout(Bla_Ui_Confirmation dialog)
    {
        if (dialog == null || dialog.List == null)
            return "no dialog";
        Canvas canvas;
        List<RectTransform> cards = Cards(dialog);
        Rect block = Block(dialog, cards, out canvas);
        List<string> shown = new List<string>();
        foreach (RectTransform c in cards)
            shown.Add(c.name + " " + Pixels(OnScreen(c, out canvas)));
        Rect words;
        List<string> tree = new List<string>();
        foreach (Transform c in dialog.transform)
            tree.Add(Describe(c, true));
        return "screen " + Screen.width + "x" + Screen.height + ", canvas " + (canvas != null ? canvas.renderMode + " x" + RiptideUtil.F(canvas.scaleFactor) : "none")
            + ", block " + (cards.Count > 0 ? Pixels(block) + " centre off by " + RiptideUtil.F(block.center.x - Screen.width * 0.5f) + ", "
                + RiptideUtil.F(block.center.y - Screen.height * 0.5f) : "-")
            + ", cards [" + string.Join("; ", shown.ToArray()) + "]"
            + ", title words " + (Words(dialog.Titlebox, out words) ? Pixels(words) : "-") + ", sub words " + (Words(dialog.Subbox, out words) ? Pixels(words) : "-")
            + ", list " + Describe(dialog.List.transform, false) + ", tree [" + string.Join("; ", tree.ToArray()) + "]";
    }

    static string Pixels(Rect r)
    {
        return "x " + RiptideUtil.F(r.xMin) + "-" + RiptideUtil.F(r.xMax) + " y " + RiptideUtil.F(r.yMin) + "-" + RiptideUtil.F(r.yMax);
    }

    // A transform's name, place, look and layout, and (one level down) its children's, for the layout log.
    static string Describe(Transform t, bool children)
    {
        RectTransform rt = t as RectTransform;
        Canvas canvas;
        string s = t.name + (t.gameObject.activeSelf ? "" : " (off)");
        if (rt != null)
            s += " at " + rt.anchoredPosition + " size " + rt.rect.size + " " + Pixels(OnScreen(rt, out canvas));
        UnityEngine.UI.Image image = t.GetComponent<UnityEngine.UI.Image>();
        if (image != null && image.enabled)
            s += ", image " + (image.sprite != null ? image.sprite.name : "plain") + " alpha " + RiptideUtil.F(image.color.a);
        TMPro.TMP_Text text = t.GetComponent<TMPro.TMP_Text>();
        if (text != null)
            s += ", text '" + text.text + "'";
        UnityEngine.UI.GridLayoutGroup g = t.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (g != null)
            s += ", grid " + g.constraint + " " + g.constraintCount + " cell " + g.cellSize + " gap " + g.spacing + " pad " + g.padding.left + "/" + g.padding.right + "/" + g.padding.top + "/" + g.padding.bottom;
        UnityEngine.UI.HorizontalOrVerticalLayoutGroup line = t.GetComponent<UnityEngine.UI.HorizontalOrVerticalLayoutGroup>();
        if (line != null)
            s += ", " + (line is UnityEngine.UI.VerticalLayoutGroup ? "column" : "row") + " gap " + RiptideUtil.F(line.spacing);
        UnityEngine.UI.ContentSizeFitter fitter = t.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (fitter != null)
            s += ", fits " + fitter.horizontalFit + "/" + fitter.verticalFit;
        if (children && t.childCount > 0)
        {
            List<string> inner = new List<string>();
            foreach (Transform c in t)
                inner.Add(Describe(c, false));
            s += " {" + string.Join(", ", inner.ToArray()) + "}";
        }
        return s;
    }
}
