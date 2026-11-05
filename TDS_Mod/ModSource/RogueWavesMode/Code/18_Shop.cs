// VENDCINT (docs/scope/06 [Shopkeeper], kit docs/15 §16; RogueLab V1 proved the pieces): a neutral unit that falls in near the
// vessels' spawn at the start of every break and jumps away at its end. Every player can select it (owner -1, every
// Sessionid a subowner); its three buttons open the choice dialog on the machine that pressed them, and a pick goes
// to the host through that player's own vessel (RW_Order), which is how the host knows who ordered:
//   SHOP    three Cardinals and Trinkets rolled for this break, and the next rank of each Direction held, for Pure Aura
//   FUND    base aura to a friend, up to 50 x the coming Tide per break, in any mix of colours (docs/scope/06 [Fund Transfer])
//   BOARD   the break's objective (RWObjectives)
// The fall and the jump are client code on the sprite shader's _Offset, as the vessels' landing is (Entity_Vis_Hero, kit
// docs/15 §16), started by every machine when it sees the entity, and by the host's Vis cue for the jump.
public static class RWShop
{
    public const float JumpBefore = 1.2f;    // seconds before the break ends
    public static Entity_Master Stand;       // host: the VENDCINT standing now
    public static int Landed;                // self-test
    public static string LastOrder = "";
    static bool jumped;
    static string asking = "";
    static readonly List<Entity_Master> fallen = new List<Entity_Master>();
    static float nextLook;

    // ---------------------------------------------------------------- the host

    public static void Land()
    {
        if (RWContent.Vendcint == null || Game_Entity.Instance == null)
            return;
        Leave();
        Entity_Master core = PlayerCore();
        if (core == null)
            return;
        Genitem tile = Tile(core);
        if (tile == null)
        {
            RWLog.Warn("VENDCINT: no tile near the Basin");
            return;
        }
        Link_Spawn spawn = new Link_Spawn();
        spawn.Being = RWContent.Vendcint;
        spawn.Genadd = tile;
        spawn.Skin = "";
        spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        spawn.Owner = -1;
        spawn.Team = core.Entitydata.Value.Team;
        foreach (long id in Blm_Server.Instance.Sessionids.Keys) spawn.Subowners.Add(id);
        Link_Being made = Helper_Entitytools.Spawn(spawn, tile.Position, null, false, null);
        Stand = made != null ? made.Worldobject : null;
        if (Stand == null)
        {
            RWLog.Warn("VENDCINT: spawn returned nothing");
            return;
        }
        jumped = false;
        Landed++;
        Helper_Entitytools.Createcommand(Stand, RWContent.ShopName, "RW_Shop", false, true);
        Helper_Entitytools.Createcommand(Stand, RWContent.FundName, "RW_Fund", false, true);
        Helper_Entitytools.Createcommand(Stand, RWContent.BoardName, "RW_Board", false, true);
        if (core.Radio != null)
            core.Radio.Uiwarning_ClientRpc("VENDCINT HAS LANDED", "RogueWaves_Shop", 3f);
        RWLog.Info("VENDCINT landed at " + RWUtil.V(tile.Position));
    }

    public static void Leave()
    {
        if (Stand != null && Stand.Beingserver != null && !Stand.Beingserver.Dead)
            Helper_Entitytools.Despawn(Stand.Beingserver, false, "RogueWaves");
        Stand = null;
    }

    static Entity_Master PlayerCore() { return RWUtil.PlayerCore(); }

    // Beside the Basin, not on the first tiles a vessel lands on, free and standable.
    static Genitem Tile(Entity_Master core)
    {
        Link_Being probe = new Link_Being { Being = RWContent.Vendcint, Entity = RWContent.Vendcint.Entity, Block = RWContent.Vendcint.Block, Team = core.Entitydata.Value.Team };
        Vector3 at = core.Entityposition.Value;
        Genitem best = null;
        float bestDistance = float.MaxValue;
        foreach (Genitem g in RWUtil.Ground())
        {
            float d = ModKitUtil.Flat(g.Position, at);
            if (d < 2f || d > 7f || d >= bestDistance)
                continue;
            Genitem stand = null;
            try { stand = Helper_Gridtools.Checkifstandable(g, probe); } catch (Exception) { }
            if (stand == null)
                continue;
            best = stand;
            bestDistance = d;
        }
        return best;
    }

    // ---------------------------------------------------------------- every machine

    public static void Update()
    {
        if (RWRun.InCampaignMap && ModKitUtil.IsServer && Stand != null && !jumped && Game_Mapvars.Instance != null && Game_Mapvars.Instance.Time_Wave.Value <= JumpBefore && ModKitTides.InBreak)
        {
            jumped = true;
            if (Stand.Radio != null)
                Stand.Radio.Vis_ClientRpc("RW_Jump", Stand.Entityposition.Value, 0f);
        }
        if (Time.unscaledTime < nextLook)
            return;
        nextLook = Time.unscaledTime + 0.1f;
        if (Game_Entity.Instance == null || Game_Entity.Instance.Entityclient == null)
            return;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && !fallen.Contains(e) && e.Being.Value.ToString() == RWContent.Vendor)
            {
                fallen.RemoveAll(x => x == null);
                fallen.Add(e);
                RogueWavesModeMod.Instance.StartCoroutine(Fall(e));
                Entity_Master me = e;
                if (e.Client != null)
                    e.Client.Vis += (type, position, number) => { if (type == "RW_Jump") RogueWavesModeMod.Instance.StartCoroutine(Jump(me)); };
            }
    }

    static List<Material> Materials(Entity_Master e)
    {
        List<Material> list = new List<Material>();
        if (e == null)
            return list;
        foreach (SpriteRenderer r in e.GetComponentsInChildren<SpriteRenderer>(true))
            foreach (Material m in r.materials)
                if (m != null && m.HasProperty("_Offset")) list.Add(m);
        return list;
    }

    static IEnumerator Fall(Entity_Master e)
    {
        // Its look is built a moment after the entity.
        List<Material> mats = new List<Material>();
        for (float w = 0f; w < 3f && mats.Count == 0 && e != null; w += Time.deltaTime)
        {
            mats = Materials(e);
            if (mats.Count == 0) yield return null;
        }
        Play(e, "Falling");
        const float seconds = 2.2f;
        for (float t = 0f; t < seconds && e != null; t += Time.deltaTime)
        {
            Vector4 v = Vector4.Lerp(new Vector4(-10f, 200f, -10f, 0f), Vector4.zero, Mathf.Pow(t / seconds, 2f));
            foreach (Material m in mats) if (m != null) m.SetVector("_Offset", v);
            yield return null;
        }
        foreach (Material m in mats) if (m != null) m.SetVector("_Offset", Vector4.zero);
        Play(e, "Land");
        if (e != null && e.Entityposition != null)
            Shake(e.Entityposition.Value);
    }

    static IEnumerator Jump(Entity_Master e)
    {
        List<Material> mats = Materials(e);
        for (float t = 0f; t < 1f && e != null; t += Time.deltaTime)
        {
            Vector4 v = Vector4.Lerp(Vector4.zero, new Vector4(10f, 200f, 10f, 0f), t * t);
            foreach (Material m in mats) if (m != null) m.SetVector("_Offset", v);
            yield return null;
        }
    }

    static void Play(Entity_Master e, string anim)
    {
        if (e == null) return;
        foreach (Entity_Vis_Animator_Base a in e.GetComponentsInChildren<Entity_Vis_Animator_Base>(true))
            a.Animplay(anim, Game_Sprite.Animationtype.Force);
    }

    // A landing mark: the ring the game draws where a building is placed (its heavier impacts aren't loaded on every map, seen on the PC).
    static void Shake(Vector3 at)
    {
        try { Helper_Vfxtools.Oneshot("Placementborder", at, Vector3.forward); }
        catch (Exception e) { RWLog.Once("landing", "VENDCINT's landing effect: " + e.Message); }
    }

    // ---------------------------------------------------------------- the buttons (the machine that pressed them)

    public static void Open(string what)
    {
        if (asking != "" || RogueWavesModeMod.Instance == null)
            return;
        asking = what;
        RogueWavesModeMod.Instance.StartCoroutine(Asking(what == "Shop" ? Shelf() : what == "Fund" ? Fund() : Board()));
    }

    // A dialog that throws still frees the buttons: SHOP, FUND and BOARD stayed dead for the launch (MODE-D10).
    static IEnumerator Asking(IEnumerator dialog)
    {
        yield return RWUtil.Guarded(dialog, "VENDCINT's " + asking + " dialog");
        asking = "";
    }

    static uint Seed(out int map, out int wave)
    {
        uint seed;
        List<int> route;
        map = wave = 0;
        if (Tds_Multiplayerinfo.Instance == null || !RWWorld.Parse(Tds_Multiplayerinfo.Instance.World.Value.ToString(), out seed, out route))
            return 0;
        map = route.Count + 1;
        wave = Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Wave.Value : 0;
        return seed;
    }

    static Bla_Ui_Confirmation.Entry Back()
    {
        Sprite s = null;
        try { s = Helper_Uitools.Geticon("Back"); } catch (Exception) { }
        return RWDialog.Card("Back", "Back", "", s, new Color(0.3568f, 0.3647059f, 0.4117647f, 1f));
    }

    static IEnumerator Shelf()
    {
        int map, wave;
        uint seed = Seed(out map, out wave);
        int pure = Mathf.RoundToInt(RWMine.Pure());
        List<Bla_Ui_Confirmation.Entry> cards = new List<Bla_Ui_Confirmation.Entry>();
        foreach (string id in RWCardinals.Shelf(seed, map, wave))
        {
            int cost = RWCardinals.Cost(id);
            cards.Add(RWDialog.Card("C:" + id, RWCardinals.Display(id), "Pure Aura " + cost + RWCard.Separator + (RWCardinals.IsTrinket(id) ? "Trinket" : "Cardinal") + RWCard.Separator + RWCardinals.Desc(id), RWCardinals.Picture(id), cost <= pure ? RWDialog.Teal : RWDialog.Swell));
        }
        Entity_Master v = RWMine.Vessel();
        List<string> held = new List<string>();
        if (v != null && v.Upgradelist != null)
            foreach (FixedString64Bytes u in v.Upgradelist)
                if (u.ToString().StartsWith(ModKitDirections.Prefix)) held.Add(u.ToString());
        foreach (Upgradeproperties u in RWDirections.NextRanks(held))
        {
            string vs, n;
            int r;
            ModKitDirections.Parse(u.Objectname, out vs, out n, out r);
            int cost = RWDirections.RankCost(r);
            cards.Add(RWDialog.Card("D:" + u.Objectname, u.Display_Name, "Pure Aura " + cost + RWCard.Separator + u.Display_Desc, RWDirections.Picture(u.Objectname), cost <= pure ? RWDialog.Coral : RWDialog.Swell));
        }
        cards.Add(Back());
        string pick = null;
        yield return RWDialog.Ask(cards, "VENDCINT", "Pure Aura: " + pure, 0f, n => pick = n);
        if (pick == null || pick == "Back")
            yield break;
        RWMine.Send("Buy", 0, pick);
    }

    // A card with no picture shows a blank white square (seen on a real second player's screen, 2026-11-04): each of Fund
    // Transfer's cards carries one. The colour on its tile says which aura; a friend's card shows their vessel; an amount, its number.
    static Bla_Ui_Confirmation.Entry Colour(string res, string display, float have, Color tint)
    {
        Sprite icon = null;
        try { icon = Helper_Uitools.Geticon(res); } catch (Exception) { }
        return RWDialog.Card(res, display, "You hold " + Mathf.FloorToInt(have), icon ?? RWAssets.Sprite("RogueWaves_Fund"), tint);
    }

    static Sprite FriendPicture(Blm_Player p)
    {
        Tds_Clientinfo info = p != null ? p.GetComponent<Tds_Clientinfo>() : null;
        Heroentryobject hero = info != null ? RWUtil.Item<Heroentryobject>("Hero", info.Hero.Value.ToString()) : null;
        return hero != null && hero.Icon != null ? hero.Icon : RWAssets.Sprite("RogueWaves_Fund");
    }

    static float Have(string res)
    {
        Tds_Clientinfo me = Blm_Server.Instance != null && Blm_Server.Instance.Localplayer != null ? Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>() : null;
        if (me == null) return 0f;
        foreach (Link_Resource r in me.Resources)
            if (r.Name.ToString() == res) return r.Amount;
        return 0f;
    }

    static IEnumerator Fund()
    {
        long me = RWMine.Session;
        List<Bla_Ui_Confirmation.Entry> friends = new List<Bla_Ui_Confirmation.Entry>();
        foreach (long s in Blm_Server.Instance.Sessionidsplayer.Keys)
            if (s != me)
                friends.Add(RWDialog.Card(s.ToString(), Blm_Server.Instance.Sessionidsplayer[s].Displayname.Value.ToString(), "Send base aura to this friend", FriendPicture(Blm_Server.Instance.Sessionidsplayer[s]), RWDialog.Teal));
        if (friends.Count == 0)
        {
            RWMine.Popup("NO ONE TO SEND TO", "RogueWaves_Fund");
            yield break;
        }
        friends.Add(Back());
        string to = null;
        yield return RWDialog.Ask(friends, "FUND TRANSFER", "Send to whom?", 0f, n => to = n);
        if (to == null || to == "Back") yield break;
        yield return new WaitForSecondsRealtime(0.3f);
        List<Bla_Ui_Confirmation.Entry> colours = new List<Bla_Ui_Confirmation.Entry>();
        colours.Add(Colour("Redaura", "Red", Have("Redaura"), new Color(0.82f, 0.29f, 0.27f, 1f)));
        colours.Add(Colour("Blueaura", "Blue", Have("Blueaura"), new Color(0.27f, 0.45f, 0.82f, 1f)));
        colours.Add(Colour("Yellowaura", "Yellow", Have("Yellowaura"), new Color(0.86f, 0.72f, 0.25f, 1f)));
        colours.Add(Back());
        string res = null;
        yield return RWDialog.Ask(colours, "FUND TRANSFER", "Send which aura?", 0f, n => res = n);
        if (res == null || res == "Back") yield break;
        yield return new WaitForSecondsRealtime(0.3f);
        int wave = Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Wave.Value : 0;
        int limit = RWFund.Limit(wave);
        List<Bla_Ui_Confirmation.Entry> amounts = new List<Bla_Ui_Confirmation.Entry>();
        foreach (int a in new[] { 10, 25, limit / 2, limit })
            if (a > 0 && !amounts.Exists(x => x.Name == a.ToString()))
                amounts.Add(RWDialog.Card(a.ToString(), a.ToString(), "Send " + a, RWDialog.Number(a), RWDialog.Teal));
        amounts.Add(Back());
        string amount = null;
        yield return RWDialog.Ask(amounts, "FUND TRANSFER", "This break's limit: " + limit, 0f, n => amount = n);
        if (amount == null || amount == "Back")
            yield break;
        RWMine.Send("Fund", int.Parse(amount), to + "/" + res);
    }

    static IEnumerator Board()
    {
        Entity_Master v = RWMine.Vessel();
        string posted = RWObjectives.PostedText(v);
        if (posted == "")
        {
            RWMine.Popup("NO OBJECTIVE THIS BREAK", "RogueWaves_Board");
            yield break;
        }
        string[] p = posted.Split('|');
        List<Bla_Ui_Confirmation.Entry> cards = new List<Bla_Ui_Confirmation.Entry>();
        cards.Add(RWDialog.Card("Take", "Take it", p[1], RWAssets.Sprite("RogueWaves_Board"), RWDialog.Teal));
        cards.Add(Back());
        string pick = null;
        yield return RWDialog.Ask(cards, p[0], p[1], 0f, n => pick = n);
        if (pick == "Take")
            RWMine.Send("Board", 0, "");
    }

    // ---------------------------------------------------------------- orders (host)

    public static void OnOrder(Entity_Master vessel, RWPlayer p, string json)
    {
        string kind = RWRun.JsonText(json, "Kind");
        if (kind != "Buy" && kind != "Fund" && kind != "Board")
            return;
        LastOrder = kind + " " + RWRun.JsonText(json, "Text");
        if (!ModKitTides.InBreak || Stand == null)
        {
            Refuse(vessel, "VENDCINT HAS LEFT");
            return;
        }
        string why = "";
        if (kind == "Buy") why = Buy(p, RWRun.JsonText(json, "Text"));
        else if (kind == "Fund") why = RWFund.Send(p, RWRun.JsonText(json, "Text"), RWRun.JsonInt(json, "Pick"));
        else why = RWObjectives.Take(p);
        if (why != "")
            Refuse(vessel, why.ToUpperInvariant());
        RWRun.Tick();
    }

    static string Buy(RWPlayer p, string item)
    {
        if (item.StartsWith("C:"))
        {
            string id = item.Substring(2);
            int map = RWCampaign.Route.Count + 1 - (RWCampaign.WonThisMap ? 1 : 0);
            if (!RWCardinals.Shelf(RWCampaign.Seed, map, Game_Mapvars.Instance.Wave.Value).Contains(id))
                return "Not on the shelf";
            return RWCardinals.Buy(p, id);
        }
        if (!item.StartsWith("D:"))
            return "";
        string name = item.Substring(2);
        string v, n;
        int r;
        Upgradeproperties u = Bl_Address.Getload("Upgrade", name) as Upgradeproperties;
        if (u == null || !ModKitDirections.Parse(name, out v, out n, out r) || r < 2 || RWDirections.Top(p, v, n) != r - 1)
            return "Not for sale";
        int cost = RWDirections.RankCost(r);
        if (p.Pure < cost)
            return "Not enough Pure Aura";
        p.Pure -= cost;
        RWDirections.Take(p, u);
        return "";
    }

    // A refusal reaches everyone as a chat line (the buttons already check what they can on the buyer's machine).
    static void Refuse(Entity_Master vessel, string text)
    {
        RWPlayer p = vessel != null ? RWRun.For(vessel.Entitydata.Value.Owner) : null;
        RWRejoin.Say("VENDCINT to " + (p != null ? p.Name : "a friend") + ": " + text.ToLowerInvariant() + ".");
    }
}

// Fund Transfer (docs/scope/06; RogueLab F1 proved the moves solo).
public static class RWFund
{
    public const int PerTide = 50;
    public static float Moved;      // self-test

    public static int Limit(int wave) { return PerTide * (wave + 1); }

    public static string Send(RWPlayer from, string what, int amount)
    {
        string[] p = what.Split('/');
        long to;
        if (p.Length != 2 || !long.TryParse(p[0], out to) || amount <= 0 || to == from.Session || Array.IndexOf(new[] { "Redaura", "Blueaura", "Yellowaura" }, p[1]) < 0)
            return "No such transfer";
        Tds_Clientinfo info;
        if (Game_Players.Instance == null || !Game_Players.Instance.Prevsessionlist.TryGetValue(to, out info) || info == null)
            return "No such friend";
        int wave = Game_Mapvars.Instance.Wave.Value;
        float room = Limit(wave) - from.Sent;
        if (room <= 0f)
            return "This break's limit is spent";
        float send = Mathf.Min(amount, room);
        Game_Mapvars vars = Game_Mapvars.Instance;
        float gain = vars.Resourcegainmultiplier, loss = vars.Resourcelossmultiplier;
        vars.Resourcegainmultiplier = 1f;
        vars.Resourcelossmultiplier = 1f;
        try
        {
            List<Link_Resourceset> cost = new List<Link_Resourceset> { new Link_Resourceset { Name = p[1], Amount = send } };
            if (!Helper_Resources.Checkresources(cost, from.Session, 1f) || !Helper_Resources.Useresources(cost, from.Session, 1f))
                return "Not enough aura";
            Helper_Resources.Giveresources(cost, to, Helper_Resources.Styles.Default);
        }
        finally
        {
            vars.Resourcegainmultiplier = gain;
            vars.Resourcelossmultiplier = loss;
        }
        from.Sent += send;
        Moved += send;
        RWLog.Info(from.Name + " sent " + send + " " + p[1] + " to " + to + "; " + from.Sent + " of " + Limit(wave) + " this break");
        return "";
    }
}
