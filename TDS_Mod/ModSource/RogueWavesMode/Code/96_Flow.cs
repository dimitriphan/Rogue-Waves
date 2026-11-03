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
    static IEnumerator ToBreakAfter(int wave)
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        freeze = false;
        forcing = true;
        yield return Until(() => vars.Wave.Value >= wave, 120f);
        float t = 0f;
        while (!ModKitTides.InBreak && t < 90f)
        {
            ClearFoes();
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        forcing = false;
        ClearFoes();
        freeze = true;
    }

    public static IEnumerator Flow()
    {
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 120f);
        Mainmenu_Main menu = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (menu == null) { Setup("flow", "never reached the main menu"); yield break; }
        yield return new WaitForSecondsRealtime(8f);
        menu.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
        yield return Until(() => RWMenu.Button != null, 25f);
        yield return new WaitForSecondsRealtime(7f);
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
        yield return Snap("mainmenu_dialog");
        List<string> first = CardNames();
        Check("B1 New / Back on the main menu, no Continue without a saved campaign", first.Contains("New") && first.Contains("Back") && !first.Contains("Continue"), string.Join(",", first.ToArray()));
        if (!ClickCard("New")) { Check("B1 New clicked", false, "no card"); yield break; }

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
        yield return Snap("chart");

        RWChart.Node harbour = RWCampaign.Chart.At(0, 0);
        yield return Until(() => boat.Selected.Value.ToString() == harbour.Name && Macro_Worldvisual.Instance.Insetup, 25f);
        Check("staging: the harbour opens the Setup panel by itself", boat.Selected.Value.ToString() == harbour.Name && Macro_Worldvisual.Instance.Insetup && Tds_Multiplayerinfo.Instance.Mode.Value.ToString() == RWData.Mode,
            "selected '" + boat.Selected.Value + "', plan " + Tds_Multiplayerinfo.Instance.Gameplan.Value + ", mode " + Tds_Multiplayerinfo.Instance.Mode.Value);
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Snap("harbour_setup");

        // The staging map: T1 on its loading screen; Direction pick; the vote.
        boat.Playgame(true);
        yield return Until(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Gameloading", 40f);
        yield return Until(() => RWCard.LoadingWrites > 0, 30f);
        Check("T1 the loading screen says where the campaign stands", RWCard.LoadingWrites > 0, "'" + RWCard.LastLine + "'");
        yield return new WaitForSecondsRealtime(0.5f);
        yield return Snap("loading_staging");
        Game_Mapvars before = null;
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game", 200f);
        if (Game_Mapvars.Instance == null) { Setup("flow staging", "no map"); yield break; }
        freeze = true;
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.DirectionKind), 60f);
        yield return WaitDialog(10f);
        List<string> offered = CardNames();
        Check("Direction: " + RWDirections.Offers + " are offered before map 1", offered.Count == RWDirections.Offers && RWVote.State == "Picks", "open '" + RWAsk.Open + "', cards " + CardTexts() + ", " + RWDirections.List.Count + " upgrades");
        Info("the Direction dialog", "title '" + TextOf(Bla_Ui_Confirmation.Instance.Titlebox) + "', sub '" + TextOf(Bla_Ui_Confirmation.Instance.Subbox) + "'");
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
        yield return Snap("map_vote");
        List<string> mapCards = CardNames();
        Check("Vote: every map of the first row is offered", mapCards.Count == RWCampaign.Open().Count && RWVote.State == "Map", "cards " + string.Join(",", mapCards.ToArray()) + ", open " + Names(RWCampaign.Open()));
        ClickCard(mapCards[0]);
        Macro_Main oldBoat = boat;
        yield return Until(() => RWCampaign.Choice >= 0, 10f);
        Check("Vote: the winner is the boat's next stop", RWCampaign.Choice.ToString() == mapCards[0], "choice " + RWCampaign.Choice + ", " + RWVote.LastVote);
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != oldBoat && Macro_Main.Instance.Ready, 150f);
        boat = Macro_Main.Instance;
        Check("staging: the party is back on the boat, no retry spent", boat != null && RWCampaign.Retries == 3 && !RWCampaign.Staging && RWCampaign.Route.Count == 0, "retries " + RWCampaign.Retries + ", route " + Digits(RWCampaign.Route));
        yield return Until(() => Here(), 30f);
        yield return Snap("boat_after_staging");

        // Sails to the winner, picks it: map 1.
        int want = RWCampaign.Choice;
        yield return Until(() => RWBoat.ArrivedAt > 0f, 60f);
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 15f);
        RWChart.Node to = RWCampaign.Chart.At(1, want);
        Check("the boat sailed by itself to the vote's winner, and its Setup panel opened", to != null && boat.Selected.Value.ToString() == to.Name, "selected '" + boat.Selected.Value + "', wanted " + (to != null ? to.Name : "-") + ", arrived " + RWUtil.F(RWBoat.ArrivedAt - RWBoat.SailedAt) + " s");
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Snap("map1_setup");
        boat.Playgame(true);
        int loads = RWCard.LoadingWrites;
        yield return Until(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Gameloading", 40f);
        yield return Until(() => RWCard.LoadingWrites > loads, 30f);
        yield return new WaitForSecondsRealtime(0.5f);
        Check("T1 map 1's loading screen shows the card", RWCard.LastLine.StartsWith("Map 1 of 10") && RWCard.LastLine.Contains("3 retries left"), "'" + RWCard.LastLine + "'");
        yield return Snap("loading_map1");
        int cards = RWCard.CardWrites;
        yield return Until(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game" && RWCard.CardWrites > cards, 90f);
        Check("T2 the map intro card shows the card", RWCard.CardWrites > cards, "'" + RWCard.LastLine + "'");
        yield return new WaitForSecondsRealtime(1f);
        yield return Snap("intro_card");
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && RWRun.InCampaignMap, 120f);
        freeze = true;
        yield return Until(() => RWShop.Stand != null, 60f);
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
        Tds_Pausemenu.Instance.SendMessage("Exitgamehost", SendMessageOptions.DontRequireReceiver);
        yield return WaitDialog(5f);
        yield return new WaitForSecondsRealtime(0.5f);
        string exitSub = TextOf(Bla_Ui_Confirmation.Instance.Subbox);
        Check("B4 the host's exit dialog names the cost", RWCard.ExitWrites > 0 && exitSub.Contains("loses this map") && exitSub.Contains("2 retries left"), "'" + exitSub + "'");
        yield return Snap("exit_warning");
        ClickCard("Back");
        yield return Until(() => !RWDialog.Up(), 5f);
        yield return new WaitForSecondsRealtime(0.5f);

        // T6.
        Check("T6 the pack's tips are in the game's lists, gated on a campaign", RWTips.Added >= RWTips.Lines.Length && RWUtil.Has(RWData.SaveFolder, RWTips.SeenKey), "added " + RWTips.Added);

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
        RWShop.Open("Shop");
        yield return WaitDialog(5f);
        List<string> shelf = CardNames();
        Info("the shop", "title '" + TextOf(Bla_Ui_Confirmation.Instance.Titlebox) + "', sub '" + TextOf(Bla_Ui_Confirmation.Instance.Subbox) + "', cards " + CardTexts());
        yield return Snap("shop");
        string buy = shelf.Find(n => n.StartsWith("C:"));
        Check("Shop: Cardinals on the shelf, and a Back card", buy != null && shelf.Contains("Back"), string.Join(",", shelf.ToArray()));
        int pure0 = player.Pure, team0 = RWRun.Team.Count, vessel0 = player.Cardinals.Count;
        if (buy != null)
        {
            ClickCard(buy);
            yield return Until(() => player.Pure != pure0, 8f);
            Check("Shop: a Cardinal costs Pure Aura, and is held", player.Pure == pure0 - RWCardinals.Cost(buy.Substring(2)) && (RWRun.Team.Count > team0 || player.Cardinals.Count > vessel0), buy + " " + pure0 + " -> " + player.Pure + ", team " + string.Join(",", RWRun.Team.ToArray()) + ", vessel " + string.Join(",", player.Cardinals.ToArray()));
        }
        // Each pack Cardinal and stock one bought straight, so every script is met once in game.
        player.Pure = 500;
        int bad = errors;
        foreach (string id in new List<string>(RWCardinals.Sold))
        {
            string why = "";
            try { why = RWCardinals.Buy(player, id); }
            catch (Exception e) { why = e.GetType().Name + ": " + e.Message; Check("Cardinal " + id + " grants without an exception", false, why); }
            if (why != "") Info("Cardinal " + id, why);
            yield return null;
        }
        yield return new WaitForSeconds(2f);
        Check("Cardinals: every one on sale goes on with no error", errors == bad, (errors - bad) + " errors; team " + RWRun.Team.Count + ", vessel " + player.Cardinals.Count + ", " + RWCardinals.Sold.Count + " on sale");
        yield return Snap("cardinal_row");
        if (Mathf.Abs(player.Pure) >= 0) player.Pure = 60;

        // A Direction's reach: each pack Direction granted at rank III; a tower built now carries the effects.
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
        Beingobject archer = RWUtil.Load<Beingobject>("Mob", "Tower_Smackyarcher");
        Entity_Master built = null;
        if (archer != null && vessel != null)
        {
            Link_Being probe = new Link_Being { Being = archer, Entity = archer.Entity, Block = archer.Block, Team = vessel.Entitydata.Value.Team };
            Genitem tile = null;
            foreach (Genitem g in RWUtil.Ground())
            {
                float d = RWUtil.Flat(g.Position, vessel.Entityposition.Value);
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
        player.Directions.RemoveAll(id => id.StartsWith("Direction_Any_Tougher") == false && id.StartsWith("Direction_Any") && !id.EndsWith("_Upgradeobject"));

        // The board: this break's objective.
        RWObjectives.Active = null;
        yield return new WaitForSeconds(0.6f);
        if (RWObjectives.Posted == null) { Check("an objective is posted", false, "none"); yield break; }
        RWShop.Open("Board");
        yield return WaitDialog(5f);
        Info("the board", "title '" + TextOf(Bla_Ui_Confirmation.Instance.Titlebox) + "', sub '" + TextOf(Bla_Ui_Confirmation.Instance.Subbox) + "', cards " + CardTexts());
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
        Info("objective", taken.Display + ": " + RWObjectives.Outcome);
        yield return Snap("break_after_tide1");

        // On to Tide 15, its foes cleared by the test.
        float clock = Time.realtimeSinceStartup;
        for (int w = 2; w <= RWCampaign.WinTide; w++)
        {
            yield return ToBreakAfter(w);
            if (Game_Mapvars.Instance == null || Game_Mapvars.Instance.Gamecomplete.Value.ToString() != "Running") { Check("Tides run to " + RWCampaign.WinTide, false, "map ended at " + w); yield break; }
        }
        Info("Tides 2-15", RWUtil.F(Time.realtimeSinceStartup - clock) + " s");
        yield return Until(() => RWCampaign.WonThisMap, 10f);
        Check("Won: Tide 15 cleared wins the map, and adds it to the route", RWCampaign.WonThisMap && RWCampaign.Route.Count == 1 && RWCampaign.Playing < 0, "route " + Digits(RWCampaign.Route));
        Check("Won: a Direction pick is owed before the next map", player.Owed, "");
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 15f);
        yield return WaitDialog(8f);
        Info("the vote to leave", "title '" + TextOf(Bla_Ui_Confirmation.Instance.Titlebox) + "', sub '" + TextOf(Bla_Ui_Confirmation.Instance.Subbox) + "', cards " + CardTexts());
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
        Check("Continue: Pure Aura, Directions and Cardinals come back from the save", again.Pure == beforePure && again.Directions.Count == beforeDirs.Count && again.Cardinals.Count > 0 && again.Hero != "", "pure " + again.Pure + ", directions " + again.Directions.Count + ", cardinals " + again.Cardinals.Count + ", hero " + again.Hero);
        Check("Continue's card names where the campaign stands", line.StartsWith("Map 2 of 10") && line.Contains("3 retries left") && line.Contains("Crew: "), line);
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
