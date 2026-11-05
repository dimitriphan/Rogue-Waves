#if ROGUEWAVESMODE_SELFTEST
// A campaign played the way a player meets it, clicked through the game's own dialogs on a solo host: the main-menu button,
// the staging map's Direction pick and vote, the boat's trip, VENDCINT and its shop, an objective, fifteen Tides (their
// foes cleared), the vote to leave, a bonus Tide, the next pick and vote, the end screen's title and the next map's
// Setup panel. With ROGUEWAVESMODE_SHOTS it captures every screen. Probes U1-U9 of docs/reports/UX_FEATURES_2026-11-03.md.
public static partial class RWTest
{
    static IEnumerator Snap(string name)
    {
#if ROGUEWAVESMODE_SHOTS
        yield return RWShots.Shot(name);
#else
        yield return null;
#endif
    }

    static Transform CardNamed(string name)
    {
        Bla_Ui_Confirmation d = Bla_Ui_Confirmation.Instance;
        if (d == null || d.List == null) return null;
        foreach (Transform c in d.List.transform)
            if (c.name == name && c.gameObject.activeInHierarchy) return c;
        return null;
    }

    static List<string> CardNames()
    {
        List<string> names = new List<string>();
        Bla_Ui_Confirmation d = Bla_Ui_Confirmation.Instance;
        if (d != null && d.List != null)
            foreach (Transform c in d.List.transform)
                if (c.gameObject.activeInHierarchy && c.GetComponent<Blu3_Button>() != null) names.Add(c.name);
        return names;
    }

    static string CardTexts()
    {
        List<string> all = new List<string>();
        Bla_Ui_Confirmation d = Bla_Ui_Confirmation.Instance;
        if (d != null && d.List != null)
            foreach (Transform c in d.List.transform)
                if (c.gameObject.activeInHierarchy && c.GetComponent<Blu3_Button>() != null)
                {
                    TMPro.TMP_Text t = c.GetComponentInChildren<TMPro.TMP_Text>(true);
                    all.Add(c.name + "='" + (t != null ? t.text.Replace("\n", " / ") : "") + "'");
                }
        return string.Join(", ", all.ToArray());
    }

    static bool ClickCard(string name)
    {
        Transform t = CardNamed(name);
        Blu3_Button b = t != null ? t.GetComponent<Blu3_Button>() : null;
        if (b == null || b.Buttonclick == null) return false;
        b.Buttonclick();
        return true;
    }

    // Waits for the dialog to show its cards, then says what it holds; true when it did.
    static IEnumerator WaitDialog(float timeout)
    {
        yield return Until(() => RWDialog.Up() && CardNames().Count > 0, timeout);
        yield return new WaitForSecondsRealtime(0.6f);
    }

    static string Rc(Rect r) { return "x " + Mathf.RoundToInt(r.xMin) + ".." + Mathf.RoundToInt(r.xMax) + " y " + Mathf.RoundToInt(r.yMin) + ".." + Mathf.RoundToInt(r.yMax); }

    static bool OnTheScreen(Rect r) { return r.xMin >= -1f && r.yMin >= -1f && r.xMax <= Screen.width + 1f && r.yMax <= Screen.height + 1f; }

    // The dialog as a player sees it once the game's slide-in has ended: every card, the title and every card's hover line
    // wholly on the screen, the cards in its middle, and Main (the dim and the dark edge) where the game puts it.
    static IEnumerator DialogFits(string what)
    {
        yield return new WaitForSecondsRealtime(1.6f);
        Bla_Ui_Confirmation d = Bla_Ui_Confirmation.Instance;
        if (d == null || !RWDialog.Up()) { Setup(what + ": on the screen", "no dialog up"); yield break; }
        List<string> off = new List<string>();
        List<RectTransform> cards = RWDialog.Cards(d);
        Rect all = new Rect(), words;
        for (int i = 0; i < cards.Count; i++)
        {
            Rect r = RWDialog.OnScreen(cards[i]);
            if (!OnTheScreen(r)) off.Add(cards[i].name + " " + Rc(r));
            Vector3[] corner = new Vector3[4];
            cards[i].GetWorldCorners(corner);
            Vector2 at = RectTransformUtility.WorldToScreenPoint(d.GetComponentInParent<Canvas>().worldCamera, corner[2]);
            if (Mathf.Abs(at.x - Mathf.Round(at.x)) > 0.05f || Mathf.Abs(at.y - Mathf.Round(at.y)) > 0.05f) off.Add(cards[i].name + " between pixels at " + RWUtil.F(at.x) + "," + RWUtil.F(at.y));
            all = i == 0 ? r : Rect.MinMaxRect(Mathf.Min(all.xMin, r.xMin), Mathf.Min(all.yMin, r.yMin), Mathf.Max(all.xMax, r.xMax), Mathf.Max(all.yMax, r.yMax));
        }
        string title = "-", line = "-";
        if (RWDialog.Words(d.Titlebox, out words))
        {
            title = Rc(words);
            if (!OnTheScreen(words)) off.Add("title " + title);
        }
        if (RWDialog.Words(d.Subbox, out words))
        {
            line = Rc(words);
            if (!OnTheScreen(words)) off.Add("sub " + line);
        }
        foreach (RectTransform c in cards)
        {
            Blu3_Button b = c.GetComponent<Blu3_Button>();
            if (b == null || b.Buttonenter == null) continue;
            b.Buttonenter();
            yield return null;
            if (RWDialog.Words(d.Subbox, out words))
            {
                line = Rc(words);
                if (!OnTheScreen(words)) off.Add("hover " + c.name + " " + line);
            }
            if (b.Buttonleave != null) b.Buttonleave();
        }
        RectTransform main = d.List.transform.parent as RectTransform;
        Rect stage = main != null ? RWDialog.OnScreen(main) : new Rect();
        bool centred = Mathf.Abs(all.center.x - Screen.width * 0.5f) <= 2f;
        bool stock = main != null && Mathf.Abs(stage.xMin) <= 2f && Mathf.Abs(stage.xMax - Screen.width) <= 2f;
        Check(what + ": every card and word on the screen on whole pixels, the cards in its middle", cards.Count > 0 && off.Count == 0 && centred && stock,
            Screen.width + "x" + Screen.height + ", " + cards.Count + " cards at " + Rc(all) + ", title " + title + ", last line " + line + ", Main " + Rc(stage) + (off.Count > 0 ? "; off: " + string.Join("; ", off.ToArray()) : ""));
    }

    static string TextOf(GameObject box)
    {
        TMPro.TMP_Text t = box != null ? box.GetComponentInChildren<TMPro.TMP_Text>() : null;
        return t != null ? t.text : "";
    }

    static string UiPlace(Transform t)
    {
        RectTransform r = t as RectTransform;
        return t.name + (r != null ? "(" + RWUtil.F(r.anchoredPosition.x) + "," + RWUtil.F(r.anchoredPosition.y) + ")" : "");
    }

    // Runs the current Tide table entries out until Tide 'wave' has started, then its foes are cleared and its break is up.
    static IEnumerator ToBreakAfter(int wave) { return ToBreakAfter(wave, false); }

    // With 'target', the objective's target falls as soon as it stands.
    static IEnumerator ToBreakAfter(int wave, bool target)
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        int breaks = RWVote.Breaks;
        freeze = false;
        forcing = !target;
        if (target) vars.Time_Wave.Value = 0.1f;
        yield return Until(() => vars.Wave.Value >= wave, 120f);
        float t = 0f;
        while (target && RWObjectives.Target == null && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
        if (target && RWObjectives.Target != null && RWObjectives.Target.Beingserver != null)
            Helper_Entitytools.Despawn(RWObjectives.Target.Beingserver, true, "RogueWaves");
        forcing = true;
        t = 0f;
        while (RWVote.Breaks == breaks && t < 90f)
        {
            ClearFoes();
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        forcing = false;
        ClearFoes();
        freeze = true;
    }

    // A Tide whose foes all fall at once, its clock left to run: Beat the Clock's way.
    static IEnumerator ClearedEarly(int wave)
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        int breaks = RWVote.Breaks;
        freeze = false;
        forcing = false;
        vars.Time_Wave.Value = 0.1f;
        yield return Until(() => vars.Wave.Value >= wave, 30f);
        float t = 0f, length = vars.Time_Wave.Value;
        while (t < 45f && (vars.Time_Wave.Value >= length - 0.5f || t < 3f))
        {
            ClearFoes();
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Info("Beat the Clock", "the Tide's clock began at " + RWUtil.F(length) + " s; " + RWUtil.F(t) + " s on it reads " + RWUtil.F(vars.Time_Wave.Value) + " s, foes " + RWVote.Foes());
        yield return new WaitForSeconds(0.5f);
        forcing = true;
        t = 0f;
        while (RWVote.Breaks == breaks && t < 60f)
        {
            ClearFoes();
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        forcing = false;
        ClearFoes();
        freeze = true;
    }

    // Home to the main menu through the pause menu's Leave, as a player on the boat goes.
    static IEnumerator ToMainMenu()
    {
        Tds_Pausemenu.Openpausemenu();
        yield return new WaitForSecondsRealtime(1.2f);
        GameObject leave;
        if (Blockage.Menu.Bl_Menus.Menus["Pause"].Items.TryGetValue("Leavegame", out leave) && leave != null && leave.GetComponent<Blu3_Button>() != null && leave.GetComponent<Blu3_Button>().Buttonclick != null)
            leave.GetComponent<Blu3_Button>().Buttonclick();
        yield return WaitDialog(5f);
        Info("the boat's exit dialog", "title '" + TextOf(Bla_Ui_Confirmation.Instance.Titlebox) + "', sub '" + TextOf(Bla_Ui_Confirmation.Instance.Subbox) + "', cards " + string.Join(",", CardNames().ToArray()));
        ClickCard("Exit");
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 90f);
    }

    public static IEnumerator Flow()
    {
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 120f);
        Mainmenu_Main menu = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (menu == null) { Setup("flow", "never reached the main menu"); yield break; }
        yield return new WaitForSecondsRealtime(8f);
        menu.Idlethreshold = 100000f;   // an unattended main menu goes to its idle view, which hides the buttons
        menu.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
        yield return Until(() => RWMenu.Button != null, 25f);
        yield return new WaitForSecondsRealtime(7f);
        Blockage.Menu.Bl_Menus.Changemenu("Main", true);   // the automated run never leaves the title by itself (RogueLab M4b)
        yield return new WaitForSecondsRealtime(10f);
        if (RWMenu.Button == null) { Setup("B1 the button", "no button: Tutorial_New_Game " + RWUtil.Has("Tutorial", "Tutorial_New_Game")); yield break; }
        Blockage.Menu.Blu2_Menu_Handler main = Blockage.Menu.Bl_Menus.Menus["Main"];
        GameObject play = main.Items["Play"];
        Transform parent = play.transform.parent;
        List<string> column = new List<string>();
        foreach (Transform c in parent) column.Add(UiPlace(c));
        TMPro.TMP_Text label = RWMenu.Button.GetComponentInChildren<TMPro.TMP_Text>(true);
        Check("B1 the Rogue Waves button stands beside Play", RWMenu.Button.gameObject.activeInHierarchy == play.activeInHierarchy && main.Items.ContainsKey(RWMenu.ItemName) && RWMenu.Button.transform.parent == parent,
            "shown " + RWMenu.Button.gameObject.activeInHierarchy + ", Play shown " + play.activeInHierarchy + ", label '" + (label != null ? label.text : "-") + "', column " + string.Join(" ", column.ToArray()));
        List<string> comps = new List<string>();
        foreach (Component c in RWMenu.Button.GetComponentsInChildren<Component>(true)) if (c != null) comps.Add(c.GetType().Name);
        Info("B1 the clone's components", string.Join(",", comps.ToArray()));
        yield return Snap("mainmenu");

        RWMenu.Clicked();
        yield return WaitDialog(10f);
        Info("B1 the dialog", "title '" + TextOf(Bla_Ui_Confirmation.Instance.Titlebox) + "', sub '" + TextOf(Bla_Ui_Confirmation.Instance.Subbox) + "', cards " + CardTexts());
        yield return DialogFits("The Rogue Waves button");
        yield return Snap("mainmenu_dialog");
        List<string> first = CardNames();
        // A run stopped half way can leave a campaign in the save: New over it asks first, which is checked too.
        bool hadSaved = first.Contains("Continue");
        Check("B1 New / Back on the main menu" + (hadSaved ? ", and Continue with the saved campaign" : ""), first.Contains("New") && first.Contains("Back"), string.Join(",", first.ToArray()));
        if (!ClickCard("New")) { Check("B1 New clicked", false, "no card"); yield break; }
        if (hadSaved)
        {
            yield return new WaitForSecondsRealtime(1f);
            yield return WaitDialog(8f);
            Check("B1c New over a saved campaign asks first", CardNamed("Sure") != null, CardTexts());
            yield return DialogFits("New campaign, over a saved one");
        yield return Snap("mainmenu_startover");
            ClickCard("Sure");
        }

        // The boat, the chart, the harbour's Setup panel.
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance.Ready, 120f);
        Macro_Main boat = Macro_Main.Instance;
        if (boat == null || !boat.Ready) { Setup("flow boat", "no boat"); yield break; }
        // A vessel every launch has: the save may name a vessel part that's disabled in this round.
        boat.Selectcharacter("Hero_Smacky");
        yield return new WaitForSecondsRealtime(1f);
        Info("flow vessel", Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>().Hero.Value.ToString());
        yield return Until(() => RWCampaign.Live && Here(), 40f);
        Check("B1 New embarked: a campaign is on and the party is on its chart", RWCampaign.Live && Here() && RWMenu.Embarking == "", "seed " + RWCampaign.Seed + ", world " + Tds_Multiplayerinfo.Instance.World.Value);
        Check("T3 the world card's line names the map and the retries", RWUtil.Item<Mapworlddatabaseobject>("World", RWCampaign.WorldName()) != null && RWUtil.Item<Mapworlddatabaseobject>("World", RWCampaign.WorldName()).Display_Desc.Contains("3 retries left"),
            RWUtil.Item<Mapworlddatabaseobject>("World", RWCampaign.WorldName()) != null ? RWUtil.Item<Mapworlddatabaseobject>("World", RWCampaign.WorldName()).Display_Desc : "no world");
        yield return Snap("world_card");
        // U7, U9
        Tds_Clientinfo myInfo = Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>();
        Check("U7 Tds_Clientinfo.Mods reads true for a player with mods on", myInfo.Mods.Value && RWLobby.Warned.Count == 0, "Mods " + myInfo.Mods.Value + ", warned " + RWLobby.Warned.Count);
        int billboards = 0;
        RWChart.Node hoverNode = null;
        foreach (Macro_Addon_Nodeshowbillboardlink a in Macro_Worldvisual.Instance.World.GetComponentsInChildren<Macro_Addon_Nodeshowbillboardlink>(true))
        {
            billboards++;
            if (hoverNode == null) hoverNode = RWCampaign.Chart.Find(a.name);
        }
        Info("U9 billboards on this chart", billboards + " nodes; weathered nodes " + Weathered());
        if (hoverNode != null)
        {
            foreach (Macro_Addon_Nodeshowbillboardlink a in Macro_Worldvisual.Instance.World.GetComponentsInChildren<Macro_Addon_Nodeshowbillboardlink>(true))
                if (a.name == hoverNode.Name) { try { a.Show(true); } catch (Exception e) { Info("U9 show", e.Message); } }
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Snap("chart_billboard");
            Check("U9 the weather's billboard shows over a chart node", GameObject.Find("Billboardnode(Clone)") != null || FindBillboard(), hoverNode.Name);
        }
        else Setup("U9 the weather's billboard", "no weather on this chart's nodes");
        yield return new WaitForSecondsRealtime(4f);
        yield return Snap("chart");
        Info("chart islands", RWWorld.IslandsCopied + " copied from the stock worlds");
        Mapworlddatabaseobject stockWorld = RWWorld.StockWorld();
        if (stockWorld != null && stockWorld.Worldobj != null)
        {
            List<string> kids = new List<string>();
            foreach (Transform c in stockWorld.Worldobj.transform) if (c.GetComponent<Macro_Link_Add_Base>() == null && kids.Count < 40) kids.Add(c.name);
            Info("stock world's other children", stockWorld.Objectname + ": " + string.Join(",", kids.ToArray()));
        }

        RWChart.Node harbour = RWCampaign.Chart.At(0, 0);
        yield return Until(() => boat.Selected.Value.ToString() == harbour.Name && Macro_Worldvisual.Instance.Insetup, 25f);
        Check("staging: the harbour opens the Setup panel by itself", boat.Selected.Value.ToString() == harbour.Name && Macro_Worldvisual.Instance.Insetup && Tds_Multiplayerinfo.Instance.Mode.Value.ToString() == RWData.Mode,
            "selected '" + boat.Selected.Value + "', plan " + Tds_Multiplayerinfo.Instance.Gameplan.Value + ", mode " + Tds_Multiplayerinfo.Instance.Mode.Value);
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Snap("harbour_setup");

        // The staging map: T1 on its loading screen; Direction pick; the vote.
        boat.Playgame(true);
        yield return Until(() => RWCard.LoadingWrites > 0, 70f);
        Check("T1 the loading screen says where the campaign stands", RWCard.LoadingWrites > 0, "'" + RWCard.LastLine + "'");
        yield return new WaitForSecondsRealtime(0.5f);
        yield return Snap("loading_staging");
        Game_Mapvars before = null;
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game", 200f);
        if (Game_Mapvars.Instance == null) { Setup("flow staging", "no map"); yield break; }
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.DirectionKind), 90f);
        yield return WaitDialog(10f);
        List<string> offered = CardNames();
        Check("Direction: " + RWDirections.Offers + " are offered before map 1", offered.Count == RWDirections.Offers && RWVote.State == "Picks", "open '" + RWAsk.Open + "', cards " + CardTexts() + ", " + RWDirections.List.Count + " upgrades");
        Info("the Direction dialog", "title '" + TextOf(Bla_Ui_Confirmation.Instance.Titlebox) + "', sub '" + TextOf(Bla_Ui_Confirmation.Instance.Subbox) + "'");
        yield return DialogFits("The Direction pick");
        yield return Snap("direction_pick");
        long me = RWMine.Session;
        RWPlayer player = RWRun.For(me);
        int heldBefore = player.Directions.Count;
        ClickCard("0");
        yield return Until(() => player.Directions.Count > heldBefore, 10f);
        Check("Direction: the pick reaches the host through the vessel's order", player.Directions.Count == heldBefore + 1 && !player.Owed, "held " + string.Join(",", player.Directions.ToArray()));
        Entity_Master vessel = RWRun.Vessel(me);
        yield return new WaitForSeconds(1.5f);
        Check("Direction: it sits on the player's vessel", vessel != null && player.Directions.Count > 0 && vessel.Upgradelistserver.ContainsKey(player.Directions[0]), vessel != null ? "vessel " + vessel.Being.Value : "no vessel");
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.MapKind), 15f);
        yield return WaitDialog(10f);
        Info("the map vote", "title '" + TextOf(Bla_Ui_Confirmation.Instance.Titlebox) + "', cards " + CardTexts());
        yield return DialogFits("The map vote");
        yield return Snap("map_vote");
        List<string> mapCards = CardNames();
        Check("Vote: every map of the first row is offered", mapCards.Count == RWCampaign.Open().Count && RWVote.State == "Map", "cards " + string.Join(",", mapCards.ToArray()) + ", open " + Names(RWCampaign.Open()));
        if (mapCards.Count == 0) yield break;
        ClickCard(mapCards[0]);
        Macro_Main oldBoat = boat;
        yield return Until(() => RWCampaign.Choice >= 0, 10f);
        Check("Vote: the winner is the boat's next stop", RWCampaign.Choice.ToString() == mapCards[0], "choice " + RWCampaign.Choice + ", " + RWVote.LastVote);
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != oldBoat && Macro_Main.Instance.Ready, 150f);
        boat = Macro_Main.Instance;
        Check("staging: the party is back on the boat, no retry spent", boat != null && RWCampaign.Retries == 3 && !RWCampaign.Staging && RWCampaign.Route.Count == 0, "retries " + RWCampaign.Retries + ", route " + Digits(RWCampaign.Route));
        yield return Until(() => Here(), 30f);
        yield return new WaitForSecondsRealtime(5f);
        yield return Snap("boat_after_staging");

        // Sails to the winner, picks it: map 1.
        int want = RWCampaign.Choice;
        yield return Until(() => RWBoat.ArrivedAt > 0f, 60f);
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 15f);
        RWChart.Node to = RWCampaign.Chart.At(1, want);
        Check("the boat sailed by itself to the vote's winner, and its Setup panel opened", to != null && boat.Selected.Value.ToString() == to.Name, "selected '" + boat.Selected.Value + "', wanted " + (to != null ? to.Name : "-") + ", arrived " + RWUtil.F(RWBoat.ArrivedAt - RWBoat.SailedAt) + " s");
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Snap("map1_setup");
        int loads = RWCard.LoadingWrites;
        boat.Playgame(true);
        yield return Until(() => RWCard.LoadingWrites > loads, 70f);
        yield return new WaitForSecondsRealtime(0.5f);
        Check("T1 map 1's loading screen shows the card", RWCard.LastLine.StartsWith("Map 1 of 10") && RWCard.LastLine.Contains("3 retries left"), "'" + RWCard.LastLine + "'");
        yield return Snap("loading_map1");
        yield return Until(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game", 90f);
        yield return new WaitForSecondsRealtime(4f);
        Check("U2 T2 the map intro card shows the card", RWCard.CardWrites > 0 && RWCard.LastLine.StartsWith("Map 1 of 10"), "'" + RWCard.LastLine + "', writes " + RWCard.CardWrites);
        try
        {
            GameObject alt = Blockage.Menu.Bl_Menus.Menus["Cine"].Items["Maptitlealt"];
            List<string> texts = new List<string>();
            foreach (TMPro.TMP_Text x in alt.GetComponentsInChildren<TMPro.TMP_Text>(true)) texts.Add(x.name + "='" + x.text + "'");
            Info("the intro card's texts", "Cine active " + Blockage.Menu.Bl_Menus.Menus["Cine"].Active + ": " + string.Join(", ", texts.ToArray()));
        }
        catch (Exception e) { Info("the intro card's texts", e.Message); }
        yield return new WaitForSecondsRealtime(1f);
        yield return Snap("intro_card");
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && RWRun.InCampaignMap, 120f);
        yield return Until(() => RWShop.Stand != null, 90f);
        freeze = true;   // only now: the vessels' own start runs on the Tide clock
        Check("VENDCINT lands in the opening break", RWShop.Stand != null, RWShop.Stand != null ? "at " + RWUtil.V(RWShop.Stand.Entityposition.Value) + ", owner " + RWShop.Stand.Entitydata.Value.Owner : "none");
        if (RWShop.Stand == null) yield break;
        yield return new WaitForSeconds(0.8f);
        yield return Snap("vendcint_falling");
        yield return new WaitForSeconds(4f);
        yield return Snap("vendcint_landed");
        Entity_Master stand = RWShop.Stand;
        long[] subs = stand.Entitydata.Value.Subowner;
        Check("VENDCINT: nobody's, every player a subowner, not a target", stand.Entitydata.Value.Owner == -1 && subs != null && Array.IndexOf(subs, me) >= 0 && stand.Beingserver.Entity.Targetable == false, "subowners " + (subs != null ? subs.Length : 0));

        // The Exit Map warning (B4).
        Tds_Pausemenu.Openpausemenu();
        yield return new WaitForSecondsRealtime(1.2f);
        yield return Snap("pause_menu");
        Blu3_Button exitButton = null;
        GameObject exitItem;
        if (Blockage.Menu.Bl_Menus.Menus["Pause"].Items.TryGetValue("Exit", out exitItem) && exitItem != null) exitButton = exitItem.GetComponent<Blu3_Button>();
        if (exitButton != null && exitButton.Buttonclick != null) exitButton.Buttonclick();
        yield return WaitDialog(5f);
        yield return new WaitForSecondsRealtime(0.5f);
        string exitSub = TextOf(Bla_Ui_Confirmation.Instance.Subbox);
        Check("B4 the host's exit dialog names the cost", RWCard.ExitWrites > 0 && exitSub.Contains("loses this map") && exitSub.Contains("2 retries left"), "'" + exitSub + "'");
        yield return Snap("exit_warning");
        {
            Bla_Ui_Confirmation stock = Bla_Ui_Confirmation.Instance;
            RectTransform stockList = stock.List.transform as RectTransform;
            TMPro.TMP_Text stockSub = stock.Subbox.GetComponent<TMPro.TMP_Text>();
            Check("The stock Exit prompt keeps its own corner after the mode's dialogs", Mathf.Approximately(stockList.anchorMin.x, 1f) && Mathf.Approximately(stockList.anchorMin.y, 0.2f) && Mathf.Approximately(stockList.pivot.x, 1f) && stockSub.alignment == TMPro.TextAlignmentOptions.TopRight && stockSub.enableAutoSizing,
                "list anchors " + RWUtil.V(stockList.anchorMin) + ", pivot " + RWUtil.V(stockList.pivot) + ", sub " + stockSub.alignment + ", autosize " + stockSub.enableAutoSizing);
        }
        ClickCard("Back");
        yield return Until(() => !RWDialog.Up(), 5f);
        yield return new WaitForSecondsRealtime(0.5f);
        Tds_Pausemenu.Instance.SendMessage("Pauseclose", SendMessageOptions.DontRequireReceiver);
        yield return new WaitForSecondsRealtime(1f);

        // T6.
        Check("T6 the pack's tips are in the game's lists, gated on a campaign", RWTips.Added >= RWTips.Lines.Length && RWUtil.Has(RWData.SaveFolder, RWTips.SeenKey), "added " + RWTips.Added);
        Factobject.Entry packTip = null;
        if (Tds_Pausemenu.Instance != null && Tds_Pausemenu.Instance.Funfacts != null && Tds_Pausemenu.Instance.Funfacts.Data != null) Tds_Pausemenu.Instance.Funfacts.Data.TryGetValue("RW_Tip_0", out packTip);
        if (packTip == null) Check("U4 a pack tip is in the pause menu's list", false, "none");
        else
        {
            bool withKey = Blockage.Tags.Bla_Data.Playerdata.Comparedata(packTip.Unlockconditions);
            RWUtil.Remove(RWData.SaveFolder, RWTips.SeenKey);
            bool withoutKey = Blockage.Tags.Bla_Data.Playerdata.Comparedata(packTip.Unlockconditions);
            RWTips.MarkSailed();
            Check("U4 a pack tip can show with the campaign key, never without it", withKey && !withoutKey, "with " + withKey + ", without " + withoutKey);
        }
        Info("U4 the pause menu's blurb", "'" + TextOf(Blockage.Menu.Bl_Menus.Menus["Pause"].Items["Blurb"]) + "'");

        // The panel and the shop.
        Game_Selection.Instance.Selected = stand;
        Game_Selection.Instance.Select();
        yield return new WaitForSeconds(1.2f);
        Blockage.Menu.Blu2_Menu_Handler hud = Blockage.Menu.Bl_Menus.Menus["Main"];
        List<string> onBar = new List<string>();
        foreach (Transform t in hud.GetComponentsInChildren<Transform>(true))
            if (t.gameObject.activeInHierarchy && t.name.StartsWith("RW_")) onBar.Add(t.name);
        Check("VENDCINT's three buttons show for a player who selects it", onBar.Exists(n => n.StartsWith("RW_Shop")) && onBar.Exists(n => n.StartsWith("RW_Fund")) && onBar.Exists(n => n.StartsWith("RW_Board")), string.Join(",", onBar.ToArray()));
        yield return Snap("vendcint_panel");
        Info("run line", "'" + RWRunLabel.Shown + "'");
        player.Pure = 60;
        yield return new WaitForSeconds(1.2f);
        Check("Pure Aura: the vessel's counter and the run line follow the host", Mathf.RoundToInt(RWMine.Pure()) == 60 && RWRunLabel.Shown.Contains("Pure Aura 60"), "counter " + RWMine.Pure() + ", line '" + RWRunLabel.Shown + "'");
#if ROGUEWAVESMODE_FEELMAP
        yield return SmoothInMap();
        yield break;
#endif
        RWShop.Open("Shop");
        yield return WaitDialog(5f);
        List<string> shelf = CardNames();
        Info("the shop", "title '" + TextOf(Bla_Ui_Confirmation.Instance.Titlebox) + "', sub '" + TextOf(Bla_Ui_Confirmation.Instance.Subbox) + "', cards " + CardTexts());
        yield return DialogFits("VENDCINT");
        yield return Snap("shop");
        string buy = shelf.Find(n => n.StartsWith("C:"));
        Check("Shop: Cardinals or Trinkets on the shelf, and a Back card", buy != null && shelf.Contains("Back"), string.Join(",", shelf.ToArray()));
        int pure0 = player.Pure, cardinals0 = RWRun.Cardinals.Count, trinkets0 = player.Trinkets.Count;
        if (buy != null)
        {
            ClickCard(buy);
            yield return Until(() => player.Pure != pure0, 8f);
            Check("Shop: a Cardinal or Trinket costs Pure Aura, and is held", player.Pure == pure0 - RWCardinals.Cost(buy.Substring(2)) && (RWRun.Cardinals.Count > cardinals0 || player.Trinkets.Count > trinkets0), buy + " " + pure0 + " -> " + player.Pure + ", cardinals " + string.Join(",", RWRun.Cardinals.ToArray()) + ", trinkets " + string.Join(",", player.Trinkets.ToArray()));
        }
        // Each pack Trinket and stock Cardinal bought straight, so every script is met once in game.
        player.Pure = 500;
        int bad = errors;
        foreach (string id in new List<string>(RWCardinals.Sold))
        {
            string why = "", what = (RWCardinals.IsTrinket(id) ? "Trinket " : "Cardinal ") + id;
            float gainBefore = Game_Mapvars.Instance.Resourcegainmultiplier;
            try { why = RWCardinals.Buy(player, id); }
            catch (Exception e) { why = e.GetType().Name + ": " + e.Message; Check(what + " grants without an exception", false, why); }
            if (why != "") Info(what, why);
            yield return null;
            if (!Mathf.Approximately(gainBefore, Game_Mapvars.Instance.Resourcegainmultiplier)) Info(what, "changes all aura pay: " + gainBefore + " -> " + Game_Mapvars.Instance.Resourcegainmultiplier);
        }
        yield return new WaitForSeconds(2f);
        Check("Cardinals and Trinkets: every one on sale goes on with no error", errors == bad, (errors - bad) + " errors; cardinals " + RWRun.Cardinals.Count + ", trinkets " + player.Trinkets.Count + ", " + RWCardinals.Sold.Count + " on sale");
        yield return Snap("cardinal_row");
        if (Mathf.Abs(player.Pure) >= 0) player.Pure = 60;

        // A Direction's reach: each pack Direction granted at rank III; a tower built now carries the effects.
        vessel = RWRun.Vessel(me);
        RWDirections.Refresh();
        foreach (RWDirectionDef d in RWDirections.Mine)
            if (d.Vessel == "Any")
                foreach (string n in new[] { d.Name, d.Name + "2", d.Name + "3" })
                {
                    Upgradeproperties u = Bl_Address.Getload("Upgrade", ModKitDirections.Id("Any", n)) as Upgradeproperties;
                    if (u != null) RWDirections.Take(player, u);
                }
        yield return new WaitForSeconds(1f);
        bad = errors;
        Beingobject archer = ModKitUtil.Load<Beingobject>("Mob", "Tower_Smackyarcher");
        Entity_Master built = null;
        if (archer != null && vessel != null)
        {
            Link_Being probe = new Link_Being { Being = archer, Entity = archer.Entity, Block = archer.Block, Team = vessel.Entitydata.Value.Team };
            Genitem tile = null;
            foreach (Genitem g in RWUtil.Ground())
            {
                float d = ModKitUtil.Flat(g.Position, vessel.Entityposition.Value);
                if (d < 2f || d > 7f) continue;
                bool ok = false;
                try { ok = Helper_Gridtools.Checkifplaceable(g, archer, 0, vessel.Entitydata.Value.Team); } catch (Exception) { }
                if (ok) { tile = g; break; }
            }
            if (tile != null)
            {
                Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(vessel.Beingserver);
                spawn.Being = archer;
                spawn.Genadd = tile;
                spawn.Skin = "";
                spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
                Link_Being made = Helper_Entitytools.Spawn(spawn, tile.Position, vessel.Beingserver, true, null);
                built = made != null ? made.Worldobject : null;
            }
        }
        yield return new WaitForSeconds(1f);
        if (built == null) Setup("Directions reach a player's buildings", "no tower could be placed");
        else
        {
            List<string> ups = new List<string>(built.Upgradelistserver.Keys);
            Check("Directions reach a player's new buildings at their top rank", ups.Contains("RW_Effect_Barnacle3_Upgradeobject") && ups.Contains("RW_Effect_Mend3_Upgradeobject") && ups.Contains("RW_Effect_Spring3_Upgradeobject"), string.Join(",", ups.ToArray()));
            Check("no error from the effects on a new tower", errors == bad, (errors - bad) + " errors");
        }
        Check("Cut Costs: the vessel's build scale follows the rank", BuildScale(vessel) <= RWDirections.CostScale[3] + 0.001f, "scale " + RWUtil.F(BuildScale(vessel)));
        yield return new WaitForSeconds(1f);
        Link_Command refit;
        if (built == null || !built.Commandlistserver.TryGetValue("Upgrade", out refit) || refit == null) Setup("Cheap Refits", "the tower has no Upgrade command");
        else Check("Cheap Refits: the tower's upgrade price follows the rank", Mathf.Approximately(refit.Goalvalue, RWDirections.RefitScale[3]), "scale " + RWUtil.F(refit.Goalvalue));
        Tds_Clientinfo purse = Game_Players.Instance.Prevsessionlist[me];
        float due = 0f, had = 0f;
        foreach (Link_Resource r in purse.Resources) { had += r.Amount; due += Mathf.Min(RWDirections.InterestCap, Mathf.Floor(r.Amount * RWDirections.InterestShare[3])); }
        due *= Game_Mapvars.Instance.Resourcegainmultiplier;    // what the Cardinals bought above make of all aura pay
        RWDirections.Interest(1);
        float has = 0f;
        foreach (Link_Resource r in purse.Resources) has += r.Amount;
        Check("Compound Interest pays its share of the unspent aura", Mathf.Abs(has - had - due) < 0.01f, had + " -> " + has + ", due " + due + ", gain multiplier " + Game_Mapvars.Instance.Resourcegainmultiplier);
        float hit0 = vessel != null && vessel.Statsserver.ContainsKey("Damage") ? vessel.Statsserver["Damage"].Value_Calc : 0f;
        Upgradeproperties skipping = Bl_Address.Getload("Upgrade", ModKitDirections.Id("Smacky", "Skipping3")) as Upgradeproperties;
        if (skipping != null) RWDirections.Take(player, skipping);
        yield return new WaitForSeconds(1.5f);
        float hit1 = vessel != null && vessel.Statsserver.ContainsKey("Damage") ? vessel.Statsserver["Damage"].Value_Calc : 0f;
        Check("A base vessel's Direction holds its stat: Skipping Stones on Smacky", skipping != null && hit0 > 0f && hit1 >= hit0 * 1.25f, hit0 + " -> " + hit1 + " (rank I may already be held: its first pick)");
        // Made of Tougher Stuff: a minion of this player, hit once.
        Entity_Master minion = null;
        Beingobject miner = ModKitUtil.Load<Beingobject>("Mob", "Mob_Smackyminer");
        if (miner != null && vessel != null)
            foreach (Genitem g in RWUtil.Ground())
            {
                float d = ModKitUtil.Flat(g.Position, vessel.Entityposition.Value);
                if (d < 2f || d > 8f) continue;
                Link_Being mb = new Link_Being { Being = miner, Entity = miner.Entity, Block = miner.Block, Team = vessel.Entitydata.Value.Team };
                Genitem spot = null;
                try { spot = Helper_Gridtools.Checkifstandable(g, mb); } catch (Exception) { }
                if (spot == null) continue;
                try
                {
                    Link_Spawn ms = Helper_Entitytools.Createlinkspawnparent(vessel.Beingserver);
                    ms.Being = miner;
                    ms.Genadd = spot;
                    ms.Skin = "";
                    ms.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
                    Link_Being got = Helper_Entitytools.Spawn(ms, spot.Position, vessel.Beingserver, true, null);
                    minion = got != null ? got.Worldobject : null;
                }
                catch (Exception e) { Info("the minion", e.Message); }
                break;
            }
        yield return new WaitForSeconds(0.6f);
        if (minion == null) Setup("Made of Tougher Stuff", "no minion could be spawned");
        else
        {
            int soft0 = RWHits.Softened;
            Link_Interact blow = new Link_Interact();
            blow.Type = "Hurt";
            blow.To = minion;
            blow.Data = new Dictionary<string, float> { { "Damagemulti", 1f }, { "Damage", 1f } };
            // The hook is first in the chain; the stock's own handlers after it want a whole hit, which this isn't.
            try { minion.Oninteract(blow); } catch (Exception) { }
            float multi;
            bool saw = blow.Data.TryGetValue("Damagemulti", out multi);
            Check("Made of Tougher Stuff softens a hit on a player's minion", RWHits.Softened == soft0 + 1 && saw && multi <= 1f - RWDirections.TougherResist[3] + 0.001f, "softened " + (RWHits.Softened - soft0) + ", multi " + multi);
            Helper_Entitytools.Despawn(minion.Beingserver, false, "RogueWaves");
        }
        float tower0 = built != null && built.Statsserver.ContainsKey("Health") ? built.Statsserver["Health"].Value_Calc : 0f;
        player.Directions.RemoveAll(id => id.StartsWith("Direction_Any_Tougher") == false && id.StartsWith("Direction_Any") && !id.EndsWith("_Upgradeobject"));

        // The board: this break's objective.
        RWObjectives.Active = null;
        yield return new WaitForSeconds(0.6f);
        if (RWObjectives.Posted == null) { Check("an objective is posted", false, "none"); yield break; }
        RWShop.Open("Board");
        yield return WaitDialog(5f);
        Info("the board", "title '" + TextOf(Bla_Ui_Confirmation.Instance.Titlebox) + "', sub '" + TextOf(Bla_Ui_Confirmation.Instance.Subbox) + "', cards " + CardTexts());
        yield return DialogFits("The Board");
        yield return Snap("board");
        ClickCard("Take");
        yield return Until(() => RWObjectives.Active != null, 8f);
        Check("Objective: a player takes the posted one for the team", RWObjectives.Active != null, RWObjectives.Active != null ? RWObjectives.Active.Display : "none");
        RWObjectiveDef taken = RWObjectives.Active;

        // Fund Transfer (solo: the limit and the refusal; F1 proved the moves).
        RWShop.Open("Fund");
        yield return new WaitForSeconds(0.5f);
        Check("Fund Transfer: the limit is 50 x the coming Tide", RWFund.Limit(0) == 50 && RWFund.Limit(4) == 250, "before Tide 1 " + RWFund.Limit(0) + ", before Tide 5 " + RWFund.Limit(4));

        // Tide 1 and its break: paid, the shop back, the objective read.
        int paid0 = player.Pure;
        Game_Selection.Instance.Selected = null;
        yield return ToBreakAfter(1);
        yield return new WaitForSeconds(1f);
        Check("Pure Aura: Tide 1 pays 3", player.Pure == paid0 + RWRun.PerTide + (RWWeather.Current != RWChart.Calm ? RWWeather.PureAuraPerTide : 0) + (RWObjectives.Outcome.EndsWith("done") ? taken.Pays : 0), paid0 + " -> " + player.Pure + ", weather " + RWWeather.Current + ", objective " + RWObjectives.Outcome);
        Check("VENDCINT lands again in the break", RWShop.Stand != null && RWShop.Landed >= 2, "landed " + RWShop.Landed);
        Check("a new objective is posted", RWObjectives.Posted != null, RWObjectives.Posted != null ? RWObjectives.Posted.Display : "-");
        float tower1 = built != null && built.Beingserver != null && built.Statsserver.ContainsKey("Health") ? built.Statsserver["Health"].Value_Calc : 0f;
        Check("Spring Tide: the tower gained health in the Tide", tower0 > 0f && tower1 > tower0, tower0 + " -> " + tower1);
        Info("objective", taken.Display + ": " + RWObjectives.Outcome);
        yield return Snap("break_after_tide1");

        // On to Tide 15, its foes cleared by the test.
        float clock = Time.realtimeSinceStartup;
        for (int w = 2; w <= RWCampaign.WinTide; w++)
        {
            // Each objective once, on Tides 2 to 6, the way a player meets it.
            RWObjectiveDef goal = w - 2 < RWObjectives.All.Length ? RWObjectives.All[w - 2] : null;
            int pureGoal = player.Pure;
            if (goal != null)
            {
                RWObjectives.Posted = goal;
                RWObjectives.Active = null;
                string took = RWObjectives.Take(player);
                if (took != "") Info("objective " + goal.Display, took);
            }
            if (goal != null && goal.Name == "Clock") yield return ClearedEarly(w);
            else if (goal != null && (goal.Name == "Meteor" || goal.Name == "Nest")) yield return ToBreakAfter(w, true);
            else yield return ToBreakAfter(w);
            if (Game_Mapvars.Instance == null || Game_Mapvars.Instance.Gamecomplete.Value.ToString() != "Running") { Check("Tides run to " + RWCampaign.WinTide, false, "map ended at " + w); yield break; }
            if (goal != null)
            {
                yield return new WaitForSeconds(0.5f);
                Check("Objective " + goal.Display + " done pays everyone in full", RWObjectives.Outcome == goal.Name + " done" && player.Pure >= pureGoal + goal.Pays, "outcome '" + RWObjectives.Outcome + "', Pure " + pureGoal + " -> " + player.Pure);
                if (goal.Name == "Nest") Check("A razed nest's Tide hook goes with it, so the Tides run on", RWWeather.HooksDropped > 0, RWWeather.HooksDropped + " dropped");
            }
        }
        Info("Tides 2-15", RWUtil.F(Time.realtimeSinceStartup - clock) + " s");
        yield return Until(() => RWCampaign.WonThisMap, 10f);
        Check("Won: Tide 15 cleared wins the map, and adds it to the route", RWCampaign.WonThisMap && RWCampaign.Route.Count == 1 && RWCampaign.Playing < 0, "route " + Digits(RWCampaign.Route));
        Check("Won: a Direction pick is owed before the next map", player.Owed, "");
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 15f);
        yield return WaitDialog(8f);
        Info("the vote to leave", "title '" + TextOf(Bla_Ui_Confirmation.Instance.Titlebox) + "', sub '" + TextOf(Bla_Ui_Confirmation.Instance.Subbox) + "', cards " + CardTexts());
        yield return DialogFits("The vote to leave");
        yield return Snap("leave_vote");
        ClickCard("Stay");
        yield return Until(() => RWVote.State == "", 8f);
        Check("Leave: staying keeps the party on the map", !RWVote.Departing && RWVote.LastVote == "0 of 1 to leave", RWVote.LastVote);
        int pure16 = player.Pure;
        yield return ToBreakAfter(RWCampaign.WinTide + 1);
        yield return new WaitForSeconds(1f);
        Check("Pure Aura: a bonus Tide pays 2", player.Pure - pure16 >= RWRun.PerBonusTide && player.Pure - pure16 <= RWRun.PerBonusTide + 6, pure16 + " -> " + player.Pure + ", objective " + RWObjectives.Outcome);
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 15f);
        yield return WaitDialog(8f);
        // Past the table's end: on to Tide 28's break, then Tides 29 and 30 and the row they replay.
        ClickCard("Stay");
        yield return Until(() => RWVote.State == "", 8f);
        Game_Mapvars tideVars = Game_Mapvars.Instance;
        List<Wavesobject.Waves> tideTable = tideVars.Mode.Enemies.Wave;
        int lastTide = tideTable.Count - 3;     // Tide N is row 2N - 1: Tide 29's; the row after it is the table's last break
        Func<int> rowSize = () => { int n = 0; foreach (Wavesobject.Wavecontent c in tideTable[lastTide].Content) n += c.Count; return n; };
        tideVars.Entry.Value = lastTide;        // Tide 28's break, as if the party had got here
        tideVars.Wave.Value = 28;
        Info("the table's end", "row " + lastTide + " is a Tide " + tideTable[lastTide].Wave + ", the last row is a Tide " + tideTable[tideTable.Count - 1].Wave + ", " + tideTable.Count + " rows");
        int size29 = rowSize();
        yield return new WaitForSeconds(0.5f);   // the jump itself reads as a break
        yield return ToBreakAfter(29);
        yield return new WaitForSeconds(0.5f);
        Check("Bonus Tides: past Tide 29 the last break rewinds onto its row, one step harder", tideVars.Entry.Value == lastTide && rowSize() > size29 && tideVars.Gamecomplete.Value.ToString() == "Running", "Entry " + tideVars.Entry.Value + ", Wave " + tideVars.Wave.Value + ", foes in the row " + size29 + " -> " + rowSize());
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 15f);
        yield return WaitDialog(8f);
        ClickCard("Stay");
        yield return Until(() => RWVote.State == "", 8f);
        int size30 = rowSize();
        yield return ToBreakAfter(30);
        yield return new WaitForSeconds(0.5f);
        Check("Bonus Tides: Tide 30 plays the row again, harder still, and the map runs on", tideVars.Wave.Value == 30 && tideVars.Entry.Value == lastTide && rowSize() > size30 && tideVars.Gamecomplete.Value.ToString() == "Running", "Entry " + tideVars.Entry.Value + ", Wave " + tideVars.Wave.Value + ", foes in the row " + size30 + " -> " + rowSize());
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 15f);
        yield return WaitDialog(8f);
        ClickCard("Leave");
        yield return Until(() => RWVote.Departing, 8f);
        Check("Leave: a majority leaves", RWVote.Departing, RWVote.LastVote);
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.DirectionKind), 12f);
        yield return WaitDialog(8f);
        Info("the second pick", CardTexts());
        yield return Snap("direction_pick_2");
        ClickCard("0");
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.MapKind) || RWCampaign.Choice >= 0, 12f);
        string secondWin = "";
        if (RWAsk.Open.StartsWith(RWVote.MapKind))
        {
            yield return WaitDialog(8f);
            List<string> second = CardNames();
            Info("the second vote", CardTexts());
            secondWin = second.Count > 0 ? second[second.Count - 1] : "";
            ClickCard(secondWin);
        }
        yield return Until(() => Game_Mapvars.Instance == null || Game_Mapvars.Instance.Gamecomplete.Value.ToString() == "Win", 15f);
        Check("Leave: the party leaves by the stock win path", Game_Mapvars.Instance != null && Game_Mapvars.Instance.Gamecomplete.Value.ToString() == "Win", RWVote.LastVote);
        yield return new WaitForSeconds(0.5f);
        Check("T4 the end screen's title is the campaign's", Game_Mapvars.Instance.Display.Value.Display_Endgame.ToString() == "MAP 1 OF 10 WON", "'" + Game_Mapvars.Instance.Display.Value.Display_Endgame + "', last " + RWCard.LastEnd);
        yield return new WaitForSeconds(7f);
        GameObject title = null;
        Blockage.Menu.Blu2_Menu_Handler win;
        if (Blockage.Menu.Bl_Menus.Menus.TryGetValue("Win", out win) && win != null) win.Items.TryGetValue("Title", out title);
        Check("U3 the Win screen reads the title, its score line hidden", title != null && TextOf(title).ToUpperInvariant().Contains("MAP 1 OF 10 WON"), "'" + TextOf(title) + "'");
        yield return Snap("end_screen");

        // Back on the boat, one row on; the second map's Setup panel.
        Macro_Main old = Macro_Main.Instance;
        Tds_Multiplayer_Remote.Instance.Returnmap_ServerRpc(true, default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        boat = Macro_Main.Instance;
        yield return Until(() => Here(), 30f);
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 60f);
        Check("the boat sails on to the next vote's winner", boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup && RWCampaign.Route.Count == 1, "selected '" + boat.Selected.Value + "', route " + Digits(RWCampaign.Route));
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Snap("map2_setup");

        // The save holds it all, and Continue gives it back.
        RWRun.Save();
        string line = RWMenu.SavedLine();
        Info("Continue's card", line);
        int beforePure = player.Pure;
        List<string> beforeDirs = new List<string>(player.Directions);
        RWRun.Load();
        RWPlayer again = RWRun.For(me);
        Check("Continue: Pure Aura, Directions and Trinkets come back from the save", again.Pure == beforePure && again.Directions.Count == beforeDirs.Count && again.Trinkets.Count > 0 && again.Hero != "", "pure " + again.Pure + ", directions " + again.Directions.Count + ", trinkets " + again.Trinkets.Count + ", hero " + again.Hero);
        Check("Continue's card names where the campaign stands", line.StartsWith("Map 2 of 10") && line.Contains("3 retries left") && line.Contains("Crew: "), line);

        // Map 2 lost: the title, a retry spent, what it earned gone, the boat sails back to it by itself.
        int lostCol = RWCampaign.Next != null ? RWCampaign.Next.Col : -1;
        int pureSaved = RWRun.For(me).Pure;
        boat.Playgame(true);
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 200f);
        yield return Until(() => RWShop.Stand != null, 90f);
        freeze = true;
        // What was bought on map 1 goes back on this map's new core, and in the HUD's row (missing until 2026-11-04).
        yield return new WaitForSeconds(1f);
        Check("A later map: the Cardinals held go back on its new core, and in the HUD's row", RWRun.Cardinals.Count > 0 && RWCardinals.MissingOnCore() == 0 && Tds_Multiplayerinfo.Instance.Artifacts.Count > 0, RWRun.Cardinals.Count + " held, " + RWCardinals.MissingOnCore() + " missing on the core, " + Tds_Multiplayerinfo.Instance.Artifacts.Count + " in the row");
        RWRun.For(me).Pure += 9;
        ClearFoes();
        Mode_Normal lostMode = Game_Rounds.Instance.GetComponent<Mode_Normal>();
        if (lostMode != null) lostMode.Finished = true;
        Tds_Multiplayerinfo.Instance.Gamestate.Value = new FixedString64Bytes("Lose");
        Game_Mapvars.Instance.Gamecomplete.Value = new FixedString64Bytes("Lose");
        Game_Rounds.Instance.Gamecompleted();
        freeze = false;
        yield return new WaitForSeconds(0.8f);
        Check("T4 a lost map's end screen title names the retries", Game_Mapvars.Instance.Display.Value.Display_Endgame.ToString() == "MAP LOST" + RWCard.Separator + "2 RETRIES LEFT", "'" + Game_Mapvars.Instance.Display.Value.Display_Endgame + "'");
        Check("Retries: a lost map spends one and offers only itself", RWCampaign.Retries == 2 && RWCampaign.Replay == lostCol && RWCampaign.Open().Count == 1 && RWCampaign.Open()[0].Col == lostCol, "retries " + RWCampaign.Retries + ", replay " + RWCampaign.Replay + ", open " + Names(RWCampaign.Open()));
        Check("Retries: what the lost map earned goes", RWRun.For(me).Pure == pureSaved, pureSaved + " saved, " + RWRun.For(me).Pure + " now");
        yield return new WaitForSeconds(7f);
        yield return Snap("lose_screen");
        old = Macro_Main.Instance;
        Tds_Multiplayer_Remote.Instance.Returnmap_ServerRpc(true, default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        boat = Macro_Main.Instance;
        yield return Until(() => Here(), 30f);
        Check("Retries: the chart's card says so", Macro_Worldvisual.Instance.World != null && Macro_Worldvisual.Instance.World.name.EndsWith("r2") && RWUtil.Item<Mapworlddatabaseobject>("World", RWCampaign.WorldName()).Display_Desc.Contains("2 retries left"), Macro_Worldvisual.Instance.World != null ? Macro_Worldvisual.Instance.World.name : "-");
        yield return new WaitForSecondsRealtime(3f);
        yield return Snap("boat_after_loss");
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 60f);
        yield return new WaitForSecondsRealtime(1f);
        Check("Retries: the boat sails back to the lost map by itself", boat.Selected.Value.ToString() != "" && RWCampaign.Next != null && RWCampaign.Next.Col == lostCol, "selected '" + boat.Selected.Value + "', next " + (RWCampaign.Next != null ? RWCampaign.Next.Name : "-"));

        // Home to the main menu through the pause menu's Exit, and back by the Rogue Waves button's Continue.
        yield return ToMainMenu();
        Mainmenu_Main menu2 = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (menu2 == null) { Setup("Continue from the main menu", "the boat's Exit never reached it"); }
        else
        {
            yield return new WaitForSecondsRealtime(8f);
            menu2.Idlethreshold = 100000f;
            menu2.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
            yield return Until(() => RWMenu.Button != null, 25f);
            yield return new WaitForSecondsRealtime(7f);
            Blockage.Menu.Bl_Menus.Changemenu("Main", true);
            yield return new WaitForSecondsRealtime(8f);
            RWMenu.Clicked();
            yield return WaitDialog(10f);
            List<string> again2 = CardNames();
            Check("Continue is on the button's dialog, with a saved campaign", again2.Contains("Continue") && again2.Contains("New"), string.Join(",", again2.ToArray()) + "; " + RWMenu.SavedLine());
            Check("Continue's card counts the retry", RWMenu.SavedLine().Contains("2 retries left"), RWMenu.SavedLine());
            yield return DialogFits("The Rogue Waves button, with a saved campaign");
        yield return Snap("mainmenu_continue");
            // U10, B1c: New over a saved campaign asks first; the second dialog opens only after the first has closed.
            ClickCard("New");
            yield return new WaitForSecondsRealtime(1f);
            yield return WaitDialog(8f);
            Check("B1c U10 New over a saved campaign asks first, the second dialog after the first", CardNamed("Sure") != null && CardNamed("Keep") != null, CardTexts());
            yield return DialogFits("New campaign, over a saved one");
        yield return Snap("mainmenu_startover");
            ClickCard("Keep");
            yield return new WaitForSecondsRealtime(1.5f);
            RWMenu.Clicked();
            yield return WaitDialog(10f);
            ClickCard("Continue");
            yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance.Ready, 120f);
            boat = Macro_Main.Instance;
            yield return Until(() => RWCampaign.Live && Here(), 40f);
            Check("Continue: the campaign is back on its chart, the retry still spent", RWCampaign.Live && Here() && RWCampaign.Retries == 2 && RWCampaign.Route.Count == 1, "retries " + RWCampaign.Retries + ", route " + Digits(RWCampaign.Route) + ", replay " + RWCampaign.Replay);
            yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 60f);
            yield return new WaitForSecondsRealtime(1f);
            Check("Continue: the boat sails to the lost map by itself", RWCampaign.Next != null && RWCampaign.Next.Col == lostCol, "selected '" + boat.Selected.Value + "'");
            yield return Snap("continue_setup");
        }
        boat.Deselectcurrentmap();
        boat.Selected.Value = new FixedString64Bytes("");
        RWCampaign.Stop();
        RWRun.Clear();
        yield return Until(() => !RWBoat.OnChart, 40f);
        yield return new WaitForSecondsRealtime(2f);
        RWLobby.InMapAsClient = true;
        Blm_Multiplayer mp = Blm_Multiplayer.Instance;
        mp.Multiplayererror.Add("test drop");
        RWLobby.AtMainMenu();
        Check("U8 a friend dropped from a map gets the way back in as the error box's last line", mp.Multiplayererror[mp.Multiplayererror.Count - 1] == RWLobby.RejoinLine, mp.Multiplayererror[mp.Multiplayererror.Count - 1]);
        mp.Multiplayererror.RemoveAll(s => s == "test drop" || s == RWLobby.RejoinLine);
        try
        {
            Blm_Multiplayer.Instance.Kickclient(987654321UL);
            Check("U10 Kickclient with a fake id throws nothing", true, "");
        }
        catch (Exception e) { Check("U10 Kickclient with a fake id throws nothing", false, e.Message); }
        // The older probes start from a boat of their own.
        yield return ToMainMenu();
        Info("difficulty", "map 5 for 2 players: foes x" + RWUtil.F((1f + RWDifficulty.CountPerMap * 4f) * (1f + RWDifficulty.CountPerPlayer)) + ", pool " + string.Join(", ", RWDifficulty.CampaignPool(Seed).ToArray()));
    }

    static bool FindBillboard()
    {
        foreach (GameObject g in Resources.FindObjectsOfTypeAll<GameObject>())
            if (g.name.StartsWith("Billboardnode") && g.scene.IsValid()) return true;
        return false;
    }

    static string Weathered()
    {
        int n = 0;
        foreach (List<RWChart.Node> row in RWCampaign.Chart.Rows)
            foreach (RWChart.Node x in row)
                if (x.Weather != RWChart.Calm) n++;
        return n.ToString();
    }

    static float BuildScale(Entity_Master vessel)
    {
        float s = 1f;
        if (vessel == null) return s;
        foreach (Link_Command c in vessel.Commandlistserver.Values)
            if (c != null && c.Name != null && c.Name.StartsWith("Buildoption"))
                s = c.Goalvalue;
        return s;
    }
}
#endif
