#if ROGUEWAVESMODE_GAPS
// Debug build only (-Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_GAPS, with ROGUEWAVESMODE_SHOTS for captures): the features the
// campaign flow (96_Flow.cs) reaches but never measures (docs/reports/TEST_ROUND_2026-11-04.md §3), one launch, on a solo host:
//   rules    what a Direction roll may offer for each of the nine vessels; every Direction's, Cardinal's and Trinket's text and picture
//   effects  on a real map: what each Direction and Vampire Dentures do to the wallet and to health; each objective's failure
//   any      on a real map: each Direction any vessel can take (92_GapsAny.cs)
//   base     on five real maps: the base vessels' Directions no held stat carries (92_GapsBase.cs)
//   trinkets on a real map: each of Claude's Trinkets and Cardinals, bought at VENDCINT and measured (92_GapsTrinkets.cs)
//   scales   every dialog of the mode at each UI scale and three screen sizes
//   ten      a whole campaign on ten real maps, each voted, left and won, to the CAMPAIGN WON screen, with the difficulty measured
//   tour     every one of the 45 chart maps in Mode_RogueWave, each with another vessel: VENDCINT lands, both objective targets and
//            both weather objects find a tile, a Tide runs; Boutman's real death for the F key
//   light    only when named, its own launch: a map's lighting changed by the kit's ModKitLight, fog included (92_GapsLight.cs)
// "-rwonly <word,word>" on the command line picks parts (Tools/pc/Run-TwoPlayers.ps1 -Extra "..."; a solo build never starts the second copy).
public static partial class RWTest
{
    static readonly string[] AllHeroes = { "Hero_Smacky", "Hero_Buster", "Hero_Fie", "Hero_Boutman", "Hero_Snosno", "Hero_Yossele", "Hero_Spool", "Hero_Argo", "Hero_Uloe" };

    static bool Part(string name)
    {
        string[] all = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(all, "-rwonly");
        return i < 0 || i + 1 >= all.Length || Array.IndexOf(all[i + 1].Split(','), name) >= 0;
    }

    // Only when named: it needs a save made for it.
    static bool Asked(string name)
    {
        string[] all = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(all, "-rwonly");
        return i >= 0 && i + 1 < all.Length && Array.IndexOf(all[i + 1].Split(','), name) >= 0;
    }

    static IEnumerator Gaps()
    {
        if (Part("rules"))
        {
            try { RollChecks(); } catch (Exception e) { Check("the Direction rules ran", false, e.ToString()); }
            try { CatalogChecks(); } catch (Exception e) { Check("the catalogue checks ran", false, e.ToString()); }
            try { AnyRollChecks(); } catch (Exception e) { Check("the Any Direction rules ran", false, e.ToString()); }
            try { TrinketRuleChecks(); } catch (Exception e) { Check("the Trinket and Cardinal rules ran", false, e.ToString()); }
        }
        if (Asked("oldsave")) { yield return Stepped(GapsOldSave()); yield break; }
        yield return GapsMenu();
        if (Asked("a2"))
        {
            bool threw = Array.IndexOf(Environment.GetCommandLineArgs(), "-rwthrowicons") >= 0, up = Macro_Main.Instance != null && Macro_Main.Instance.Ready;
            if (!threw) Setup("MODE-A2", "launched without -rwthrowicons");
            else Finding("MODE-A2", !up, "this launch's RWIcons.Add threw on purpose: the Rogue Waves button took the party to the boat: " + up);
            yield break;
        }
        if (Macro_Main.Instance == null || !Macro_Main.Instance.Ready) { Setup("gaps", "never reached the boat"); yield break; }
        // Each in its own launch: a part that forces a map to end can leave the game's Lose menu up over the next (TEST_ROUND_2026-11-04 §6).
        if (Asked("throw")) { yield return Stepped(ReviewThrow()); yield break; }
        if (Asked("split")) { yield return Stepped(ReviewSplithead()); yield break; }
        if (Asked("artrio")) { yield return Stepped(ReviewArtrio()); yield break; }
        if (Asked("review")) { yield return Stepped(GapsReview()); yield break; }
        if (Asked("light")) { yield return Stepped(GapsLight()); yield break; }
        if (Part("effects") || Part("scales")) yield return Stepped(GapsEffects());
        if (Part("any")) yield return Stepped(GapsAny());
        if (Part("base")) yield return Stepped(GapsBase());
        if (Part("trinkets")) yield return Stepped(GapsTrinkets());
        if (Part("foes")) yield return Stepped(GapsFoes());
        if (Part("ten")) yield return Stepped(GapsTen());
        if (Part("tour")) yield return Stepped(GapsTour());
#if MODKIT_PLAY
        if (Part("blocked")) yield return Stepped(GapsBlocked());
        if (Part("hud"))
        {
            yield return Stepped(GapsHud("Hero_Uloe"));
            yield return Stepped(GapsHud("Hero_Argo"));
        }
#endif
    }

    // A campaign saved before Team Cardinals became Cardinals and Vessel Cardinals Trinkets (2026-11-04), with one Vessel Cardinal,
    // two Team Cardinals and a Direction: needs that save on disk (handoff\parked\oldsave_20261104, made on the old code). Also the
    // renamed words a player reads.
    static IEnumerator GapsOldSave()
    {
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 120f);
        Mainmenu_Main menu = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (menu == null) { Setup("oldsave", "no main menu"); yield break; }
        yield return new WaitForSecondsRealtime(8f);
        menu.Idlethreshold = 100000f;
        menu.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
        yield return Until(() => RWMenu.Button != null, 25f);
        yield return new WaitForSecondsRealtime(7f);
        Blockage.Menu.Bl_Menus.Changemenu("Main", true);
        yield return new WaitForSecondsRealtime(6f);
        RWMenu.Clicked();
        yield return WaitDialog(10f);
        Check("Old save: the Rogue Waves button offers Continue", CardNames().Contains("Continue"), CardTexts() + "; saved line '" + RWMenu.SavedLine() + "'");
        ClickCard("Continue");
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance.Ready, 120f);
        Macro_Main boat = Macro_Main.Instance;
        yield return Until(() => RWCampaign.Live && Here(), 40f);
        long me = RWMine.Session;
        RWPlayer player = RWRun.For(me);
        Check("Old save: its Vessel Cardinal comes back as a Trinket", player.Trinkets.Contains("Cardinal_Dentures1") && RWCardinals.IsTrinket("Cardinal_Dentures"), string.Join(",", player.Trinkets.ToArray()));
        Check("Old save: its Team Cardinals come back as Cardinals", RWRun.Cardinals.Contains("Artifact_Badge#1") && RWRun.Cardinals.Contains("Artifact_Beastpelt#1"), string.Join(",", RWRun.Cardinals.ToArray()));
        Check("Old save: its Direction and Pure Aura come back", player.Directions.Contains("Direction_Any_Cost_Upgradeobject") && player.Pure == 455, string.Join(",", player.Directions.ToArray()) + ", Pure " + player.Pure);
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 60f);
        boat.Playgame(true);
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
        yield return Until(() => RWShop.Stand != null, 120f);
        freeze = true;
        yield return new WaitForSeconds(4f);
        Entity_Master vessel = RWRun.Vessel(me);
        bool direction = false;
        if (vessel != null)
            foreach (string u in vessel.Upgradelistserver.Keys)
                if (u == "Direction_Any_Cost_Upgradeobject") direction = true;
        Check("Old save: in the map the Direction is on the vessel", direction, vessel != null ? string.Join(",", new List<string>(vessel.Upgradelistserver.Keys).ToArray()) : "no vessel");
        // The Trinket has no upgrade of its own to look for: its heal, on a real hit.
        yield return NewFoe();
        if (lastFoe != null && vessel != null)
        {
            Hurt(lastFoe, vessel, 3f);
            yield return new WaitForSeconds(0.6f);
            ClearFoes();
            yield return NewFoe();
            float healed0 = RWCardinals.Healed;
            if (lastFoe != null) Hurt(vessel, lastFoe, 5f);
            yield return new WaitForSeconds(0.8f);
            Check("Old save: in the map the Trinket heals the vessel as it hits", RWCardinals.Healed > healed0, "healed " + (RWCardinals.Healed - healed0));
            ClearFoes();
        }
        else Setup("Old save's Trinket", "no foe");
        int listed = 0;
        foreach (FixedString64Bytes a in Tds_Multiplayerinfo.Instance.Artifacts) if (a.ToString() == "Artifact_Badge" || a.ToString() == "Artifact_Beastpelt") listed++;
        Check("Old save: in the map the Cardinals are on the core and in the HUD's row", listed == 2 && RWCardinals.MissingOnCore() == 0, listed + " of 2 in the row, " + RWCardinals.MissingOnCore() + " upgrades missing on the core");

        // The words, on a shelf holding a Trinket and two Cardinals.
        List<string> sold = new List<string>(RWCardinals.Sold);
        RWCardinals.Sold.Clear();
        RWCardinals.Sold.Add("Cardinal_Dentures");
        foreach (string id in sold) if (!RWCardinals.IsTrinket(id) && RWCardinals.Sold.Count < 3) RWCardinals.Sold.Add(id);
        player.Pure = Mathf.Max(player.Pure, 200);
        yield return new WaitForSeconds(0.6f);
        RWShop.Open("Shop");
        yield return WaitDialog(6f);
        yield return new WaitForSecondsRealtime(2f);
        // A card shows only its picture: its words are in the dialog's line while it is hovered.
        string cards = "";
        foreach (string card in CardNames())
        {
            Blu3_Button b = CardNamed(card) != null ? CardNamed(card).GetComponent<Blu3_Button>() : null;
            if (b == null || b.Buttonenter == null) continue;
            b.Buttonenter();
            yield return null;
            cards += card + ": " + TextOf(Bla_Ui_Confirmation.Instance.Descbox).Replace("\n", " / ") + " | ";
        }
        Check("The shop calls them Trinket and Cardinal, and nothing the old names", cards.Contains("Trinket") && cards.Contains("Cardinal") && !cards.Contains("Vessel Cardinal") && !cards.Contains("Team Cardinal"), cards);
        yield return DialogFits("The shop with a Trinket and two Cardinals");
        yield return Snap("rename_shop");
        ClickCard("Back");
        yield return Until(() => !RWDialog.Up(), 5f);
        RWCardinals.Sold.Clear();
        RWCardinals.Sold.AddRange(sold);
        string shopDesc = RWContent.ShopCommand != null ? RWContent.ShopCommand.Display_Desc : "";
        Check("The SHOP button says Cardinals, Trinkets", shopDesc.Contains("Cardinals, Trinkets"), shopDesc);
    }

    static IEnumerator GapsMenu()
    {
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 120f);
        Mainmenu_Main menu = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (menu == null) yield break;
        yield return new WaitForSecondsRealtime(8f);
        menu.Idlethreshold = 100000f;
        menu.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
        yield return Until(() => RWMenu.Button != null, 25f);
        yield return new WaitForSecondsRealtime(7f);
        Blockage.Menu.Bl_Menus.Changemenu("Main", true);
        yield return new WaitForSecondsRealtime(6f);
        RWMenu.Clicked();
        yield return WaitDialog(10f);
        bool saved = CardNames().Contains("Continue");
        ClickCard("New");
        if (saved)
        {
            yield return new WaitForSecondsRealtime(1f);
            yield return WaitDialog(8f);
            ClickCard("Sure");
        }
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance.Ready, 120f);
        yield return new WaitForSecondsRealtime(1f);
    }

    // ---------------------------------------------------------------- rules: no map needed

    static void RollChecks()
    {
        RWDirections.Refresh();
        foreach (string hero in AllHeroes)
        {
            string vessel = RWDirections.VesselName(hero);
            RWPlayer p = new RWPlayer { Key = "test" };
            List<string> seen = new List<string>();
            int wrongCount = 0, foreign = 0, notFirst = 0, repeats = 0;
            for (int seed = 1; seed <= 400; seed++)
            {
                List<int> offers = RWDirections.Roll(p, hero, seed);
                if (offers.Count != RWDirections.Offers) wrongCount++;
                List<int> once = new List<int>();
                foreach (int i in offers)
                {
                    if (once.Contains(i)) repeats++;
                    once.Add(i);
                    string v, n;
                    int r;
                    RogueWavesModeKitDirections.Parse(RWDirections.List[i].Objectname, out v, out n, out r);
                    if (v != "Any" && v != vessel) foreign++;
                    if (r != 1) notFirst++;
                    if (!seen.Contains(v + "/" + n)) seen.Add(v + "/" + n);
                }
            }
            seen.Sort(string.CompareOrdinal);
            bool own = true;
            foreach (Upgradeproperties u in RWDirections.List)
            {
                string v, n;
                int r;
                if (RogueWavesModeKitDirections.Parse(u.Objectname, out v, out n, out r) && v == vessel && r == 1 && !seen.Contains(v + "/" + n)) own = false;
            }
            Check("Roll: " + hero + " is offered 3 first ranks, only its own or any vessel's, all of its own in time", wrongCount == 0 && foreign == 0 && notFirst == 0 && repeats == 0 && own,
                "400 rolls: wrong count " + wrongCount + ", foreign " + foreign + ", not rank I " + notFirst + ", repeats " + repeats + "; seen " + string.Join(" ", seen.ToArray()));
        }

        // Next ranks, a full rank, Needs, clashes, a repeat of the same seed.
        RWPlayer holder = new RWPlayer { Key = "test" };
        holder.Directions.Add(RogueWavesModeKitDirections.Id("Any", "Cost"));
        bool saw2 = false, saw1 = false;
        for (int seed = 1; seed <= 400; seed++)
            foreach (int i in RWDirections.Roll(holder, "Hero_Smacky", seed))
            {
                string name = RWDirections.List[i].Objectname;
                if (name == RogueWavesModeKitDirections.Id("Any", "Cost2")) saw2 = true;
                if (name == RogueWavesModeKitDirections.Id("Any", "Cost")) saw1 = true;
            }
        Check("Roll: a Direction held comes back as its next rank, never itself", saw2 && !saw1, "rank II seen " + saw2 + ", rank I seen " + saw1);
        holder.Directions.Add(RogueWavesModeKitDirections.Id("Any", "Cost2"));
        holder.Directions.Add(RogueWavesModeKitDirections.Id("Any", "Cost3"));
        bool any = false;
        for (int seed = 1; seed <= 400; seed++)
            foreach (int i in RWDirections.Roll(holder, "Hero_Smacky", seed))
                if (RWDirections.List[i].Objectname.Contains("_Any_Cost")) any = true;
        Check("Roll: a Direction at rank III is offered no more", !any, "");
        List<int> a = RWDirections.Roll(holder, "Hero_Smacky", 77), b = RWDirections.Roll(holder, "Hero_Smacky", 77);
        Check("Roll: the same seed offers the same cards, so a retry of the pick can't reroll", string.Join(",", a.ToArray()) == string.Join(",", b.ToArray()), string.Join(",", a.ToArray()));
        List<Upgradeproperties> next = RWDirections.NextRanks(new List<string> { RogueWavesModeKitDirections.Id("Any", "Cost") });
        Check("The shop sells the next rank only, and nothing past III", next.Count == 1 && next[0].Objectname == RogueWavesModeKitDirections.Id("Any", "Cost2") && RWDirections.NextRanks(new List<string> { RogueWavesModeKitDirections.Id("Any", "Cost3") }).Count == 0,
            next.Count > 0 ? next[0].Objectname : "none");
        Check("A Direction's rank costs its scope's Pure Aura", RWDirections.RankCost(2) == 20 && RWDirections.RankCost(3) == 30, RWDirections.RankCost(2) + ", " + RWDirections.RankCost(3));

        // Which vessels the "minions" Direction reaches.
        List<string> tougher = new List<string>();
        foreach (string hero in AllHeroes)
        {
            RWPlayer p = new RWPlayer { Key = "test" };
            for (int seed = 1; seed <= 400; seed++)
                foreach (int i in RWDirections.Roll(p, hero, seed))
                    if (RWDirections.List[i].Objectname.Contains("_Any_Tougher") && !tougher.Contains(hero)) tougher.Add(hero);
        }
        Info("Made of Tougher Stuff is offered to", string.Join(" ", tougher.ToArray()));

        // Clashes: a vessel's Directions marked with the same Clash number lock each other out.
        foreach (string hero in AllHeroes)
        {
            string vessel = RWDirections.VesselName(hero);
            List<Upgradeproperties> clashing = new List<Upgradeproperties>();
            foreach (Upgradeproperties u in RWDirections.List)
            {
                string v, n;
                int r;
                float c;
                if (RogueWavesModeKitDirections.Parse(u.Objectname, out v, out n, out r) && v == vessel && r == 1 && u.Defaultparams != null && u.Defaultparams.TryGetValue("Clash", out c)) clashing.Add(u);
            }
            if (clashing.Count < 2) continue;
            RWPlayer p = new RWPlayer { Key = "test" };
            p.Directions.Add(clashing[0].Objectname);
            bool other = false, nextRank = false;
            string v0, n0;
            int r0;
            RogueWavesModeKitDirections.Parse(clashing[0].Objectname, out v0, out n0, out r0);
            for (int seed = 1; seed <= 400; seed++)
                foreach (int i in RWDirections.Roll(p, hero, seed))
                {
                    if (clashing.Exists(u => u != clashing[0] && u.Objectname == RWDirections.List[i].Objectname)) other = true;
                    if (RWDirections.List[i].Objectname == RogueWavesModeKitDirections.Id(v0, n0 + "2")) nextRank = true;
                }
            Check("Roll: " + hero + "'s clashing Directions lock each other out, and the one held still ranks up", !other && nextRank, "held " + clashing[0].Objectname + ", other offered " + other + ", its rank II " + nextRank);
        }
    }

    static void CatalogChecks()
    {
        int noName = 0, noDesc = 0, crest = 0, ranks = 0;
        List<string> crested = new List<string>(), bad = new List<string>();
        Sprite fallback = RWAssets.Sprite("RogueWaves_Crest");
        foreach (Upgradeproperties u in RWDirections.List)
        {
            string v, n;
            int r;
            if (!RogueWavesModeKitDirections.Parse(u.Objectname, out v, out n, out r)) { bad.Add(u.Objectname); continue; }
            ranks++;
            if (string.IsNullOrEmpty(u.Display_Name)) { noName++; bad.Add(u.Objectname + " has no name"); }
            if (string.IsNullOrEmpty(u.Display_Desc)) { noDesc++; bad.Add(u.Objectname + " has no line"); }
            Sprite s = RWDirections.Picture(u.Objectname);
            if (s == null || s == fallback) { crest++; if (r == 1) crested.Add(v + "/" + n); }
            string want = n.Length > 0 ? (r == 2 ? " II" : r == 3 ? " III" : "") : "";
            if (r > 1 && !u.Display_Name.EndsWith(want)) bad.Add(u.Objectname + " reads '" + u.Display_Name + "'");
            int has = RWDirections.Ranks(u.Objectname);
            for (int k = 2; k <= 3; k++)
                if (r == 1 && (Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id(v, n + k)) == null) != (k > has)) bad.Add(u.Objectname + (k > has ? " is a Duo with a rank " : " has no rank ") + k);
        }
        Check("Directions: every rank has a name and a line, ranks II and III exist and say so (a Duo has one rank)", bad.Count == 0 && ranks >= 60, ranks + " ranks; " + string.Join("; ", bad.ToArray()));
        Check("Directions: every one has a picture of its own, not the crest", crested.Count == 0, crest + " ranks on the crest: " + string.Join(" ", crested.ToArray()));

        List<string> wrong = new List<string>();
        foreach (string id in RWCardinals.Sold)
        {
            string name = RWCardinals.Display(id), desc = RWCardinals.Desc(id);
            if (string.IsNullOrEmpty(name) || name == id) wrong.Add(id + " has no name");
            if (string.IsNullOrEmpty(desc)) wrong.Add(id + " has no line");
            if (RWCardinals.Picture(id) == null) wrong.Add(id + " has no picture");
            if (RWCardinals.Cost(id) <= 0) wrong.Add(id + " is free");
        }
        Check("Cardinals and Trinkets: every one on sale has a name, a line, a picture and a price", wrong.Count == 0 && RWCardinals.Sold.Count > 0, RWCardinals.Sold.Count + " on sale; " + string.Join("; ", wrong.ToArray()));
        int shelves = 0, shelfBad = 0;
        List<string> sawOnShelf = new List<string>();
        for (uint seed = 1; seed <= 30; seed++)
            for (int map = 1; map <= 10; map++)
                for (int wave = 0; wave <= 16; wave++)
                {
                    List<string> s1 = RWCardinals.Shelf(seed, map, wave), s2 = RWCardinals.Shelf(seed, map, wave);
                    shelves++;
                    if (s1.Count != RWCardinals.OnShelf || string.Join(",", s1.ToArray()) != string.Join(",", s2.ToArray())) shelfBad++;
                    List<string> once = new List<string>();
                    foreach (string id in s1) { if (once.Contains(id)) shelfBad++; once.Add(id); if (!sawOnShelf.Contains(id)) sawOnShelf.Add(id); }
                }
        Check("The shelf: 3 different Cardinals or Trinkets, the same on every machine, each on sale coming up in time", shelfBad == 0 && sawOnShelf.Count == RWCardinals.Sold.Count, shelves + " shelves, " + shelfBad + " bad, " + sawOnShelf.Count + " of " + RWCardinals.Sold.Count + " seen");
        int trinketSlotBad = 0;
        bool anyTrinket = RWCardinals.Sold.Exists(RWCardinals.IsTrinket);
        for (uint seed = 1; seed <= 30; seed++)
            if (RWCardinals.Shelf(seed, 1, (int)seed % 16).FindAll(RWCardinals.IsTrinket).Count != (anyTrinket ? RWCardinals.TrinketSlots : 0)) trinketSlotBad++;
        Check("B4 the shelf keeps a slot for a Trinket", trinketSlotBad == 0, trinketSlotBad + " of 30 shelves without exactly " + RWCardinals.TrinketSlots);
        foreach (string w in new[] { RWChart.WeatherNames[RWChart.RisingWater], RWChart.WeatherNames[RWChart.QuickeningCurrent], RWChart.WeatherNames[RWChart.NestingGround] })
            Info("weather name", w);
    }

    // ---------------------------------------------------------------- helpers on a map

    static IEnumerator ToMap(uint seed, int weather)
    {
        RWCampaign.New(seed);
        RWCampaign.Staging = false;
        yield return Until(() => RWCampaign.Live && Here(), 40f);
        RWChart.Node to = RWCampaign.Open()[0];
        to.Weather = weather;
        RWCampaign.Choice = to.Col;
        Macro_Main boat = Macro_Main.Instance;
        yield return Until(() => boat.Selected.Value.ToString() == to.Name && Macro_Worldvisual.Instance.Insetup, 60f);
        if (weather != RWChart.Calm) { RWWeather.Preload(weather); yield return Until(() => RWWeather.Loaded(weather), 120f); }
        boat.Playgame(true);
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
        yield return Until(() => RWShop.Stand != null, 120f);
        freeze = true;
        yield return new WaitForSeconds(3f);
    }

    static float Total(long s) { return Purse2(s, "Redaura") + Purse2(s, "Blueaura") + Purse2(s, "Yellowaura"); }

    static float Purse2(long session, string res)
    {
        Tds_Clientinfo info;
        if (Game_Players.Instance == null || !Game_Players.Instance.Prevsessionlist.TryGetValue(session, out info) || info == null)
            return -1f;
        foreach (Link_Resource r in info.Resources)
            if (r.Name.ToString() == res) return r.Amount;
        return 0f;
    }

    static void Rich(long session, float each)
    {
        List<Link_Resourceset> pay = new List<Link_Resourceset>();
        foreach (string r in new[] { "Redaura", "Blueaura", "Yellowaura" }) pay.Add(new Link_Resourceset { Name = r, Amount = each });
        Game_Mapvars vars = Game_Mapvars.Instance;
        float gain = vars.Resourcegainmultiplier;
        vars.Resourcegainmultiplier = 1f;
        Helper_Resources.Giveresources(pay, session, Helper_Resources.Styles.Default);
        vars.Resourcegainmultiplier = gain;
    }

    static string Pos(Vector3 at) { return "{\"x\":" + RWUtil.F(at.x) + ",\"y\":" + RWUtil.F(at.y) + ",\"z\":" + RWUtil.F(at.z) + "}"; }

    static Entity_Master Mine(string being, long me, List<Entity_Master> skip)
    {
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Entitydata.Value.Owner == me && e.Being.Value.ToString() == being && (skip == null || !skip.Contains(e))) return e;
        return null;
    }

    // Builds by the order the HUD sends; what it cost, by the wallet.
    static IEnumerator BuildOne(Entity_Master vessel, string tower, List<Vector3> used, List<Entity_Master> known, Action<Entity_Master, float> done)
    {
        long me = vessel.Entitydata.Value.Owner;
        Beingobject being = ModKitUtil.Load<Beingobject>("Mob", tower);
        Vector3 at = being != null ? RogueWavesModeKitTest.Spot(vessel.Entityposition.Value, being, vessel.Entitydata.Value.Team, 3f, 8f, used, tower) : new Vector3(float.NaN, 0f, 0f);
        if (float.IsNaN(at.x)) { done(null, 0f); yield break; }
        float before = Total(me);
        vessel.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Build", "{\"Tower\":\"" + tower + "\",\"Position\":" + Pos(at) + ",\"Orientation\":0,\"Owner\":" + me + "}", default(Unity.Netcode.ServerRpcParams));
        Entity_Master made = null;
        yield return Until(() => (made = Mine(tower, me, known)) != null, 15f);
        yield return new WaitForSeconds(0.5f);
        if (made != null) known.Add(made);
        done(made, before - Total(me));
    }

    // A foe out of every tower's reach, by the enemy core, whose stats are up after a moment: fetched as lastFoe after yield return NewFoe().
    static Entity_Master lastFoe;

    static IEnumerator NewFoe()
    {
        List<Entity_Master> cores = RWUtil.EnemyCores();
        Vector3 near = cores.Count > 0 ? cores[0].Entityposition.Value : Vector3.zero;
        lastFoe = RogueWavesModeKitTest.SpawnFoe("Enemy_Dagger", near, 2f, 10f, null) ?? RogueWavesModeKitTest.SpawnFoe("Enemy_Spear", near, 2f, 10f, null) ?? RogueWavesModeKitTest.SpawnFoe("Enemy_Halberd", near, 2f, 10f, null);
        yield return new WaitForSeconds(0.4f);
        if (lastFoe != null && Hp(lastFoe) < 0f)
            Info("a test foe", lastFoe.Being.Value + " has no Health: " + string.Join(",", new List<string>(lastFoe.Statsserver.Keys).ToArray()));
    }

    static float Hp(Entity_Master e) { Link_Stats s; return e != null && e.Statsserver != null && e.Statsserver.TryGetValue("Health", out s) ? s.Value : -1f; }
    static float HpMax(Entity_Master e) { Link_Stats s; return e != null && e.Statsserver != null && e.Statsserver.TryGetValue("Health", out s) ? s.Value_Calc : -1f; }

    static void Hurt(Entity_Master from, Entity_Master to, float damage)
    {
        RogueWavesModeKitCombat.Hurt(from, to, damage);
    }

    // Used by 92_Review.cs too, with or without a play build (MAX P1-6).
    static IEnumerator ToNextMap()
    {
        Macro_Main boat = Macro_Main.Instance;
        yield return Until(() => Here(), 40f);
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 90f);
        yield return new WaitForSecondsRealtime(1f);
        freeze = false;
        forcing = false;
        boat.Playgame(true);
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
        yield return Until(() => RWShop.Stand != null, 120f);
        freeze = true;
        yield return new WaitForSeconds(3f);
    }

    static Entity_Master MineLast(string being, long me)
    {
        Entity_Master last = null;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Entitydata.Value.Owner == me && e.Being.Value.ToString() == being && e.Beingserver != null && !e.Beingserver.Dead) last = e;
        return last;
    }

#if MODKIT_PLAY
    // ---------------------------------------------------------------- the HUD by a virtual hand, for the vessels no play build covers

    static List<string> TitlesOf(Entity_Master m)
    {
        List<string> all = new List<string>();
        foreach (Transform t in RogueWavesModeKitPlay.Named(RogueWavesModeKitPlay.HudRoot(), ""))
            if (t.gameObject.activeInHierarchy && t.GetComponent<Blu3_Button>() != null && t.name != "") all.Add(t.name + "='" + RogueWavesModeKitPlay.Texts(t).Replace("\n", " ") + "'");
        return all;
    }

    // Does the HUD still answer a hand after each way into a map? (An earlier run found the end screen's menu up over a map, eating every click.)
    static bool playInit;

    static IEnumerator HudOk(string what, long me)
    {
        Entity_Master v = RWRun.Vessel(me);
        if (v == null) { Setup("HUD after " + what, "no vessel"); yield break; }
        if (!playInit)
        {
            playInit = true;
            RogueWavesModeKitPlay.Log = m => Info("hud", m);
            RogueWavesModeKitPlay.Host = RogueWavesModeMod.Instance;
            RogueWavesModeKitPlay.Init();
        }
        RogueWavesModeKitPlay.Reset(v);
        yield return RogueWavesModeKitPlay.WaitForWorld(20f);
        List<string> menus = new List<string>();
        foreach (KeyValuePair<string, Blockage.Menu.Blu2_Menu_Handler> kv in Blockage.Menu.Bl_Menus.Menus) if (kv.Value != null && kv.Value.Active) menus.Add(kv.Key);
        string middle = RogueWavesModeKitPlay.UiAt(new Vector2(Screen.width / 2f, Screen.height / 2f));
        yield return RogueWavesModeKitPlay.SelectVessel();
        bool ok = RogueWavesModeKitPlay.IsSelected(v);
        Check("After " + what + ": the map's HUD answers a hand, no end screen over it", ok && !menus.Contains("Lose") && !menus.Contains("Win"), "selected " + RogueWavesModeKitPlay.Selected() + ", menus up " + string.Join(",", menus.ToArray()) + ", UI at the middle: " + middle);
        yield return Snap("hud_after_" + what.Replace(" ", "_").Replace("'", ""));
        yield return RogueWavesModeKitPlay.Cancel();
    }


    static IEnumerator GapsBlocked()
    {
        Macro_Main boat = Macro_Main.Instance;
        boat.Selectcharacter("Hero_Smacky");
        yield return Until(() => RWCampaign.Live && Here(), 40f);
        long me = RWMine.Session;
        yield return Until(() => boat.Selected.Value.ToString() == RWCampaign.Chart.At(0, 0).Name && Macro_Worldvisual.Instance.Insetup, 40f);
        boat.Playgame(true);
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.DirectionKind), 240f);
        yield return WaitDialog(10f);
        ClickCard("0");
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.MapKind) || RWCampaign.Choice >= 0, 20f);
        if (RWAsk.Open.StartsWith(RWVote.MapKind)) { yield return WaitDialog(10f); ClickCard(CardNames()[0]); }
        Macro_Main old = boat;
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 200f);
        yield return ToNextMap();
        yield return HudOk("the staging map and the boat's trip", me);

        // A Tide and its break.
        yield return ToBreakAfter(1);
        yield return new WaitForSeconds(1f);
        yield return HudOk("a Tide", me);

        // The vessel's death.
        Entity_Master was = RWRun.Vessel(me);
        yield return NewFoe();
        if (lastFoe != null && was != null) Hurt(lastFoe, was, 100000f);
        ClearFoes();
        freeze = false;
        yield return new WaitForSeconds(1.5f);
        {
            List<string> menus = new List<string>();
            foreach (KeyValuePair<string, Blockage.Menu.Blu2_Menu_Handler> kv in Blockage.Menu.Bl_Menus.Menus) if (kv.Value != null && kv.Value.Active) menus.Add(kv.Key);
            Info("while the vessel is down", "menus up " + string.Join(",", menus.ToArray()));
            yield return Snap("vessel_down");
        }
        yield return Until(() => { Entity_Master v = RWRun.Vessel(me); return v != null && v != was && v.Beingserver != null && !v.Beingserver.Dead; }, 180f);
        freeze = true;
        yield return new WaitForSeconds(2f);
        yield return HudOk("the vessel's death and return", me);

        // A map lost, and its replay.
        ClearFoes();
        Mode_Normal lost = Game_Rounds.Instance.GetComponent<Mode_Normal>();
        if (lost != null) lost.Finished = true;
        Tds_Multiplayerinfo.Instance.Gamestate.Value = new FixedString64Bytes("Lose");
        Game_Mapvars.Instance.Gamecomplete.Value = new FixedString64Bytes("Lose");
        Game_Rounds.Instance.Gamecompleted();
        freeze = false;
        yield return new WaitForSeconds(9f);
        old = Macro_Main.Instance;
        Tds_Multiplayer_Remote.Instance.Returnmap_ServerRpc(true, default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        yield return ToNextMap();
        yield return HudOk("a lost map and its replay", me);

        // The host's Exit from a running map, and the replay after it.
        Tds_Pausemenu.Openpausemenu();
        yield return new WaitForSecondsRealtime(1.2f);
        GameObject exitItem;
        if (Blockage.Menu.Bl_Menus.Menus["Pause"].Items.TryGetValue("Exit", out exitItem) && exitItem != null && exitItem.GetComponent<Blu3_Button>() != null && exitItem.GetComponent<Blu3_Button>().Buttonclick != null) exitItem.GetComponent<Blu3_Button>().Buttonclick();
        yield return WaitDialog(5f);
        old = Macro_Main.Instance;
        ClickCard("Exit");
        freeze = false;
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        yield return ToNextMap();
        yield return HudOk("the host's Exit and the replay", me);

        // A map won and left by the vote.
        freeze = false;
        forcing = false;
        Game_Mapvars v2 = Game_Mapvars.Instance;
        v2.Entry.Value = 2 * RWCampaign.WinTide - 1;
        v2.Wave.Value = RWCampaign.WinTide - 1;
        yield return new WaitForSeconds(0.5f);
        yield return ToBreakAfter(RWCampaign.WinTide);
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 20f);
        yield return WaitDialog(8f);
        ClickCard("Leave");
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.DirectionKind) || RWCampaign.Choice >= 0, 20f);
        if (RWAsk.Open.StartsWith(RWVote.DirectionKind)) { yield return WaitDialog(8f); ClickCard("0"); }
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.MapKind) || RWCampaign.Choice >= 0, 20f);
        if (RWAsk.Open.StartsWith(RWVote.MapKind)) { yield return WaitDialog(8f); ClickCard(CardNames()[0]); }
        yield return Until(() => Game_Mapvars.Instance == null || Game_Mapvars.Instance.Gamecomplete.Value.ToString() == "Win", 30f);
        yield return new WaitForSeconds(7f);
        old = Macro_Main.Instance;
        Tds_Multiplayer_Remote.Instance.Returnmap_ServerRpc(true, default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        yield return ToNextMap();
        yield return HudOk("a map won and left", me);

        // Test-only exits next: Quitmap with nothing in play, as the staging map leaves.
        RWCampaign.Playing = -1;
        RWCampaign.WonThisMap = false;
        old = Macro_Main.Instance;
        freeze = false;
        Tds_Multiplayer_Remote.Instance.Quitmap_ServerRpc(default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        yield return new WaitForSecondsRealtime(3f);
    }

    static IEnumerator GapsHud(string hero)
    {
        Macro_Main boat = Macro_Main.Instance;
        RWRun.Clear();      // or the last campaign's lock puts the old vessel back
        boat.Selectcharacter(hero);
        yield return new WaitForSecondsRealtime(1f);
        yield return ToMap(Seed, RWChart.Calm);
        long me = RWMine.Session;
        Entity_Master vessel = RWRun.Vessel(me);
        if (vessel == null || vessel.Being.Value.ToString() != hero) { Setup("HUD " + hero, "no such vessel: " + (vessel != null ? vessel.Being.Value.ToString() : "none")); yield break; }
        RogueWavesModeKitPlay.Log = m => Info("hud", m);
        RogueWavesModeKitPlay.Host = RogueWavesModeMod.Instance;
#if ROGUEWAVESMODE_SHOTS
        RogueWavesModeKitPlay.Shot = RWShots.Shot;
#endif
        RogueWavesModeKitPlay.Init();
        RogueWavesModeKitPlay.Reset(vessel);
        Rich(me, 8000f);
        yield return RogueWavesModeKitPlay.WaitForWorld(30f);
        {
            Vector2 mid = new Vector2(Screen.width / 2f, Screen.height / 2f);
            List<string> menus = new List<string>();
            foreach (KeyValuePair<string, Blockage.Menu.Blu2_Menu_Handler> kv in Blockage.Menu.Bl_Menus.Menus) if (kv.Value != null && kv.Value.Active) menus.Add(kv.Key);
            Info("hud " + hero + " at the start", "menus up: " + string.Join(",", menus.ToArray()) + "; UI at the middle: " + RogueWavesModeKitPlay.UiAt(mid) + "; time scale " + Time.timeScale);
        }
        yield return RogueWavesModeKitPlay.SelectVessel();
        Check("HUD " + hero + ": F or a click selects the vessel", RogueWavesModeKitPlay.IsSelected(vessel), RogueWavesModeKitPlay.Selected());
        Info("hud " + hero, "the build bar:" + RogueWavesModeKitPlay.BuildBar() + "; buttons:" + RogueWavesModeKitPlay.CommandTitles());
        yield return RogueWavesModeKitPlay.Capture(hero + "_hud");

        Heroentryobject entry = RWUtil.Item<Heroentryobject>("Hero", hero);
        List<Vector3> used = new List<Vector3>();
        List<Entity_Master> things = new List<Entity_Master>();
        int slot = 0, built = 0;
        foreach (string tower in new List<string>(entry.Towerdisplay))
        {
            Beingobject being = ModKitUtil.Load<Beingobject>("Mob", tower);
            if (being == null) { Info("hud " + hero, tower + " isn't loaded"); slot++; continue; }
            Vector3 at = RogueWavesModeKitTest.Spot(vessel.Entityposition.Value, being, vessel.Entitydata.Value.Team, 3f, 8f, used, tower);
            if (float.IsNaN(at.x)) { Setup("HUD " + hero + " " + tower, "no tile"); slot++; continue; }
            int before = MineCount(tower, me);
            string name = tower;
            yield return RogueWavesModeKitPlay.Build(slot, tower, at, 0, () => MineCount(name, me) > before, "the HUD walk");
            bool hand = RogueWavesModeKitPlay.LastOk;
            Entity_Master made = MineLast(tower, me);
            Info("hud " + hero, tower + (hand ? " stands" : " wasn't built") + ", " + RogueWavesModeKitPlay.Counts());
            if (made != null) { things.Add(made); built++; }
            slot++;
        }
        Check("HUD " + hero + ": the build bar builds every tower by hand, no fallback", built == entry.Towerdisplay.Count && RogueWavesModeKitPlay.Fallbacks == 0, built + " of " + entry.Towerdisplay.Count + "; " + RogueWavesModeKitPlay.Counts());
        yield return RogueWavesModeKitPlay.Capture(hero + "_built");

        // The pickers first: a Flood Light's first entry, Wide Flood, takes its AIM away.
        yield return HudVesselCommands(hero, vessel, things, me);
        // Each building: selected by a click, its first upgrade bought by its own entry, every other button pressed and cancelled.
        foreach (Entity_Master thing in new List<Entity_Master>(things))
        {
            if (thing == null) continue;
            string what = thing.Being.Value.ToString();
            yield return RogueWavesModeKitPlay.Select(thing, what);
            bool selected = RogueWavesModeKitPlay.IsSelected(thing);
            Check("HUD " + hero + ": a click selects the " + what, selected, RogueWavesModeKitPlay.Selected());
            Info("hud " + what, "entries:" + RogueWavesModeKitPlay.UpgradeEntries() + "; buttons:" + RogueWavesModeKitPlay.CommandTitles());
            yield return RogueWavesModeKitPlay.Capture(hero + "_" + what);
            if (!selected) continue;
            List<Transform> ups = RogueWavesModeKitPlay.Named(RogueWavesModeKitPlay.HudRoot(), "Upgradeoption");
            Transform entry1 = ups.Find(u => u.name.StartsWith("Upgradeoption") && u.gameObject.activeInHierarchy);
            if (entry1 != null)
            {
                int ups0 = thing.Upgradelistserver.Count;
                yield return RogueWavesModeKitPlay.ClickUi(entry1, "the first upgrade of the " + what);
                yield return Until(() => thing.Upgradelistserver.Count > ups0, 2f);
                Check("HUD " + hero + ": the " + what + "'s first upgrade entry buys it", thing.Upgradelistserver.Count > ups0, "upgrades " + ups0 + " -> " + thing.Upgradelistserver.Count + ": " + string.Join(",", new List<string>(thing.Upgradelistserver.Keys).ToArray()));
            }
        }
        // Sold by hand.
        Entity_Master sale = things.Find(x => x != null && x.Beingserver != null && !x.Beingserver.Dead);
        if (sale != null)
        {
            yield return RogueWavesModeKitPlay.Sell(sale, sale.Being.Value.ToString());
            Check("HUD " + hero + ": SELL and a click sell a building", RogueWavesModeKitPlay.LastOk, RogueWavesModeKitPlay.Counts());
        }
        RWCampaign.Playing = -1;
        RWCampaign.WonThisMap = false;
        Macro_Main old = Macro_Main.Instance;
        freeze = false;
        forcing = false;
        Tds_Multiplayer_Remote.Instance.Quitmap_ServerRpc(default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        yield return new WaitForSecondsRealtime(3f);
    }

    static int MineCount(string being, long me)
    {
        int n = 0;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Entitydata.Value.Owner == me && e.Being.Value.ToString() == being && e.Beingserver != null && !e.Beingserver.Dead) n++;
        return n;
    }


    static Entity_Master NodeNear(Vector3 at, float within)
    {
        Entity_Master best = null;
        float bestD = within;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Resource") && e.Beingserver != null && !e.Beingserver.Dead)
            {
                float d = ModKitUtil.Flat(e.Entityposition.Value, at);
                if (d < bestD) { bestD = d; best = e; }
            }
        return best;
    }

    // The vessel's own pickers: Uloe's PLUG IN and a Flood Light's AIM; Argo's crew buttons, SONAR and the Drill Rig's AIM.
    static IEnumerator HudVesselCommands(string hero, Entity_Master vessel, List<Entity_Master> things, long me)
    {
        if (hero == "Hero_Uloe")
        {
            Entity_Master bank = things.Find(t => t != null && t.Being.Value.ToString() == "Tower_Uloebank");
            Entity_Master flood = things.Find(t => t != null && t.Being.Value.ToString() == "Tower_Uloeflood");
            Entity_Master glass = things.Find(t => t != null && t.Being.Value.ToString() == "Tower_Uloeglass");
            Link_Command plug;
            Func<string> state = () => vessel.Commandlistserver.TryGetValue("Plug", out plug) && plug != null ? plug.Goalstring ?? "" : "-";
            string s0 = state();
            Entity_Master node = NodeNear(vessel.Entityposition.Value, 60f);
            Info("hud PLUG IN", "state '" + s0 + "', bank " + (bank != null) + ", nearest node " + (node != null ? node.Being.Value.ToString() + " " + RWUtil.F(ModKitUtil.Flat(node.Entityposition.Value, vessel.Entityposition.Value)) + " tiles" : "none"));
            if (bank != null)
            {
                yield return RogueWavesModeKitPlay.Command(vessel, "vessel", "PLUG IN", true, Vector3.zero, bank, () => state() == "On");
                Check("HUD Uloe: PLUG IN arms a picker, and a click on a Power Bank plugs him in", RogueWavesModeKitPlay.LastOk, "state '" + s0 + "' -> '" + state() + "'");
                yield return RogueWavesModeKitPlay.Capture("uloe_plugged");
                yield return RogueWavesModeKitPlay.Command(vessel, "vessel", "UNPLUG", false, Vector3.zero, null, () => state() != "On");
                Check("HUD Uloe: the same button reads UNPLUG, and unplugs", RogueWavesModeKitPlay.LastOk, "state '" + state() + "'; buttons:" + RogueWavesModeKitPlay.CommandTitles());
            }
            if (flood != null && glass != null)
            {
                // A Flood Light keeps its pick in its own brain, out of a mod's reach; what the player gets told is the game's warning line, in capitals (Game_Ui.Warning).
                Func<string> aimed = () => { foreach (TMPro.TMP_Text t in UnityEngine.Object.FindObjectsOfType<TMPro.TMP_Text>()) if (t != null && t.text != null && (t.text.ToUpper().Contains("FLOOD LIGHT AIMED") || t.text.ToUpper().Contains("AIM AT"))) return t.text.ToUpper(); return ""; };
                string a0 = aimed();
                yield return RogueWavesModeKitPlay.Command(flood, "Flood Light", "AIM", true, Vector3.zero, glass, () => aimed().Contains("FLOOD LIGHT AIMED"));
                Check("HUD Uloe: a Flood Light's AIM arms a picker, and a click on a building aims it", RogueWavesModeKitPlay.LastOk, "'" + a0 + "' -> '" + aimed() + "'");
                yield return RogueWavesModeKitPlay.Capture("uloe_aimed");
            }
        }
        if (hero == "Hero_Argo")
        {
            Entity_Master drill = things.Find(t => t != null && t.Being.Value.ToString() == "Tower_Argodrill");
            Entity_Master bell = things.Find(t => t != null && t.Being.Value.ToString() == "Tower_Argobell");
            Entity_Master scatter = things.Find(t => t != null && t.Being.Value.ToString() == "Tower_Argoscatter");
            if (drill != null)
            {
                Link_Command aim;
                Func<string> aimed = () => drill.Commandlistserver.TryGetValue("Aim", out aim) && aim != null ? (aim.Goalposition + "") : "-";
                string a0 = aimed();
                Vector3 spot = drill.Entityposition.Value + new Vector3(2f, 0f, 2f);
                foreach (Genitem g in RWUtil.Ground())
                    if (ModKitUtil.Flat(g.Position, drill.Entityposition.Value) > 1.5f && ModKitUtil.Flat(g.Position, drill.Entityposition.Value) < 3.5f && Mathf.Abs(g.Position.y - drill.Entityposition.Value.y) < 1.1f) { spot = g.Position; break; }
                yield return RogueWavesModeKitPlay.Command(drill, "Drill Rig", "AIM", true, spot, null, () => aimed() != a0);
                Check("HUD Argo: the Drill Rig's AIM arms a picker, and a click on the ground moves its Tremor", RogueWavesModeKitPlay.LastOk, "'" + a0 + "' -> '" + aimed() + "'");
                yield return RogueWavesModeKitPlay.Capture("argo_aim");
            }
            yield return RogueWavesModeKitPlay.SelectVessel();
            Info("hud Argo vessel buttons", RogueWavesModeKitPlay.CommandTitles());
            if (bell != null)
            {
                yield return RogueWavesModeKitPlay.Select(bell, "Miner's Base");
                Info("hud Argo Miner's Base buttons", RogueWavesModeKitPlay.CommandTitles() + "; entries:" + RogueWavesModeKitPlay.UpgradeEntries());
                yield return RogueWavesModeKitPlay.Capture("argo_bell");
                if (scatter != null)
                    foreach (string crew in new[] { "PORT", "STARBOARD", "BOSUN" })
                    {
                        Transform b = RogueWavesModeKitPlay.CommandButton(crew);
                        if (b == null) { Info("hud Argo", "no " + crew + " button"); continue; }
                        yield return RogueWavesModeKitPlay.Command(bell, "Miner's Base", crew, true, Vector3.zero, scatter, () => true);
                        yield return new WaitForSeconds(0.5f);
                        Info("hud Argo crew", crew + " sent to the Scatter: " + RogueWavesModeKitPlay.CommandState(bell));
                    }
            }
            yield return RogueWavesModeKitPlay.SelectVessel();
            Transform sonar = RogueWavesModeKitPlay.CommandButton("SONAR");
            if (sonar != null)
            {
                int sonarErrors = hardErrors;
                yield return RogueWavesModeKitPlay.ClickUi(sonar, "SONAR");
                yield return new WaitForSeconds(1f);
                yield return RogueWavesModeKitPlay.Capture("argo_sonar");
                Check("HUD Argo: SONAR presses with no error", hardErrors == sonarErrors, (hardErrors - sonarErrors) + " errors; " + RogueWavesModeKitPlay.CommandState(vessel));   // was Check(true) (MAX KIT-6)
            }
            else Info("hud Argo", "no SONAR on the vessel: " + RogueWavesModeKitPlay.CommandTitles());
        }
    }
#endif

    // ---------------------------------------------------------------- effects

    static IEnumerator GapsEffects()
    {
        Macro_Main boat = Macro_Main.Instance;
        boat.Selectcharacter("Hero_Smacky");
        yield return new WaitForSecondsRealtime(1f);
        yield return ToMap(Seed, RWChart.Calm);
        if (Game_Mapvars.Instance == null) { Setup("effects", "no map"); yield break; }
        long me = RWMine.Session;
        RWPlayer player = RWRun.For(me);
        Entity_Master vessel = RWRun.Vessel(me);
        if (vessel == null) { Setup("effects", "no vessel"); yield break; }
        Rich(me, 5000f);
        List<Vector3> used = new List<Vector3>();
        List<Entity_Master> known = new List<Entity_Master>();
        const string Archer = "Tower_Smackyarcher";
        Entity_Master plain = null, cheap = null;
        float full = 0f, paid = 0f;

        if (Part("effects"))
        {
            // Cut Costs: what a building really costs at each rank.
            yield return BuildOne(vessel, Archer, used, known, (m, c) => { plain = m; full = c; });
            Info("an archer's price", "rank 0: " + full);
            float[] paidAt = new float[4];
            paidAt[0] = full;
            for (int rank = 1; rank <= 3; rank++)
            {
                RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Cost" + (rank > 1 ? rank.ToString() : ""))) as Upgradeproperties);
                yield return new WaitForSeconds(1.2f);
                Entity_Master got = null;
                yield return BuildOne(vessel, Archer, used, known, (m, c) => { got = m; paidAt[rank] = c; });
                if (rank == 3) cheap = got;
            }
            Check("Cut Costs: a building really costs 10%, 20%, 30% less", full > 0f && Mathf.Abs(paidAt[1] - full * 0.9f) <= 1f && Mathf.Abs(paidAt[2] - full * 0.8f) <= 1f && Mathf.Abs(paidAt[3] - full * 0.7f) <= 1f,
                "paid " + paidAt[0] + ", " + paidAt[1] + ", " + paidAt[2] + ", " + paidAt[3]);

            // Cheap Refits: a real upgrade through the building's Upgrade order; the same upgrade before and after.
            const string Range2 = "Tower_Smackyarcher_Range2_Upgradeobject";
            yield return new WaitForSeconds(0.5f);
            float t0 = Total(me);
            if (plain != null) plain.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Upgrade", "{\"Upgrade\":\"" + Range2 + "\",\"Upgrader\":" + me + "}", default(Unity.Netcode.ServerRpcParams));
            yield return new WaitForSeconds(1f);
            float upFull = t0 - Total(me);
            bool had = plain != null && plain.Upgradelistserver.ContainsKey(Range2);
            RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Refit")) as Upgradeproperties);
            RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Refit2")) as Upgradeproperties);
            RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Refit3")) as Upgradeproperties);
            yield return new WaitForSeconds(1.5f);
            t0 = Total(me);
            if (cheap != null) cheap.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Upgrade", "{\"Upgrade\":\"" + Range2 + "\",\"Upgrader\":" + me + "}", default(Unity.Netcode.ServerRpcParams));
            yield return new WaitForSeconds(1f);
            float upCheap = t0 - Total(me);
            bool had2 = cheap != null && cheap.Upgradelistserver.ContainsKey(Range2);
            Check("Cheap Refits: an upgrade really costs 30% less at rank III", had && had2 && upFull > 0f && Mathf.Abs(upCheap - upFull * 0.7f) <= 1f, "full " + upFull + " (bought " + had + "), rank III " + upCheap + " (bought " + had2 + ")");

            // The tower-reaching Directions, one at a time, measured by a real hit.
            Entity_Master tower = cheap ?? plain;
            RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Barnacle")) as Upgradeproperties);
            yield return new WaitForSeconds(1.5f);
            yield return NewFoe();
            Entity_Master foe = lastFoe;
            if (foe == null) Setup("Barnacle Coat", "no foe could be spawned");
            else
            {
                // Read in one frame: the archers around it shoot it dead within moments.
                float f0 = Hp(foe);
                Hurt(foe, tower, 1f);
                Check("Barnacle Coat rank I: a foe that hits the tower is pricked for 2", f0 > 0f && Mathf.Abs(f0 - Hp(foe) - 2f) < 0.01f, f0 + " -> " + Hp(foe) + ", tower holds " + string.Join(",", new List<string>(tower.Upgradelistserver.Keys).ToArray()));
                ClearFoes();
                RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Barnacle2")) as Upgradeproperties);
                RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Barnacle3")) as Upgradeproperties);
                yield return new WaitForSeconds(1.5f);
                bool swapped = tower.Upgradelistserver.ContainsKey("RW_Effect_Barnacle3_Upgradeobject") && !tower.Upgradelistserver.ContainsKey("RW_Effect_Barnacle1_Upgradeobject");
                yield return NewFoe();
                foe = lastFoe;
                f0 = Hp(foe);
                if (foe != null && f0 > 0f)
                {
                    Hurt(foe, tower, 1f);
                    Check("Barnacle Coat rank III: the old rank gives way on a tower that stands, and pricks for 7, once", swapped && Mathf.Abs(f0 - Hp(foe) - 7f) < 0.01f, f0 + " -> " + Hp(foe) + ", swapped " + swapped);
                }
                ClearFoes();
            }

            // Spring Tide at rank III shrugs off 10%; first the ranks in order.
            RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Spring")) as Upgradeproperties);
            yield return new WaitForSeconds(1.5f);
            yield return NewFoe();
            Entity_Master striker = lastFoe;
            float h0 = Hp(tower);
            if (striker != null) Hurt(striker, tower, 10f);
            float lost1 = h0 - Hp(tower);
            ClearFoes();
            RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Spring2")) as Upgradeproperties);
            RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Spring3")) as Upgradeproperties);
            yield return new WaitForSeconds(1.5f);
            yield return NewFoe();
            striker = lastFoe;
            h0 = Hp(tower);
            if (striker != null) Hurt(striker, tower, 10f);
            float lost3 = h0 - Hp(tower);
            Info("Spring Tide's hits", "rank I lost " + lost1 + ", rank III lost " + lost3 + " of a 10 damage hit; tower holds " + string.Join(",", new List<string>(tower.Upgradelistserver.Keys).ToArray()));
            Check("Spring Tide: rank I leaves a hit as it was", Mathf.Abs(lost1 - 10f) < 0.05f, "lost " + lost1 + " of 10");
            Check("Spring Tide: rank III takes about a tenth off a hit", Mathf.Abs(lost3 - 9f) < 0.2f, "lost " + lost3 + " of 10");
            ClearFoes();

            // Tidal Mend: the tower beside it, wounded, at the next Tide.
            RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Mend")) as Upgradeproperties);
            yield return new WaitForSeconds(1.5f);
            Entity_Master wounded = plain != null && plain != tower ? plain : known.Find(k => k != null && k != tower);
            string menders = "Tidal Mend rank " + RWDirections.Top(player, "Any", "Mend");
            if (wounded == null) Setup("Tidal Mend", "one tower only");
            else
            {
                yield return NewFoe();
                Entity_Master blow = lastFoe;
                if (blow != null) Hurt(blow, wounded, 12f);
                yield return new WaitForSeconds(0.3f);
                ClearFoes();
                float m0 = Hp(wounded), mMax = HpMax(wounded), mended0 = RWDirections.Mended;
                yield return ToBreakAfter(1);
                yield return new WaitForSeconds(0.6f);
                // Spring Tide, held too, raises max health with the share kept, so the mend is read from its own count as well (B7).
                Check("Tidal Mend: towers mend what stands around them at the next Tide", Hp(wounded) > m0 && RWDirections.Mended > mended0, m0 + " -> " + Hp(wounded) + " of " + mMax + ", mended " + (RWDirections.Mended - mended0) + ", " + RWUtil.F(ModKitUtil.Flat(wounded.Entityposition.Value, tower.Entityposition.Value)) + " tiles from another; " + menders);
            }

            // Made of Tougher Stuff at rank III: a big hit takes at most a quarter of a minion's health.
            foreach (string n in new[] { "Tougher", "Tougher2", "Tougher3" }) RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", n)) as Upgradeproperties);
            yield return new WaitForSeconds(1f);
            Beingobject miner = ModKitUtil.Load<Beingobject>("Mob", "Mob_Smackyminer");
            Entity_Master minion = null;
            if (miner != null)
                foreach (Genitem g in RWUtil.Ground())
                {
                    float d = ModKitUtil.Flat(g.Position, vessel.Entityposition.Value);
                    if (d < 2f || d > 8f) continue;
                    Genitem spot = null;
                    try { spot = Helper_Gridtools.Checkifstandable(g, new Link_Being { Being = miner, Entity = miner.Entity, Block = miner.Block, Team = vessel.Entitydata.Value.Team }); } catch (Exception) { }
                    if (spot == null) continue;
                    Link_Spawn ms = Helper_Entitytools.Createlinkspawnparent(vessel.Beingserver);
                    ms.Being = miner; ms.Genadd = spot; ms.Skin = ""; ms.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
                    Link_Being got = Helper_Entitytools.Spawn(ms, spot.Position, vessel.Beingserver, true, null);
                    minion = got != null ? got.Worldobject : null;
                    break;
                }
            yield return new WaitForSeconds(0.6f);
            if (minion == null) Setup("Made of Tougher Stuff", "no minion");
            else
            {
                float max = HpMax(minion);
                yield return NewFoe();
                Entity_Master hitter = lastFoe;
                float before = Hp(minion);
                if (hitter != null) Hurt(hitter, minion, max * 3f);
                yield return new WaitForSeconds(0.3f);
                bool alive = minion != null && minion.Beingserver != null && !minion.Beingserver.Dead;
                float lost = before - Hp(minion);
                Check("Made of Tougher Stuff rank III: one huge hit takes at most a quarter of a minion's health", alive && lost <= max * RWDirections.TougherMaxShare + 0.01f, "max " + max + ", hit " + (max * 3f) + ", lost " + lost + ", alive " + alive);
                if (minion != null && hitter != null && alive)
                {
                    before = Hp(minion);
                    Hurt(hitter, minion, 1f);
                    yield return new WaitForSeconds(0.3f);
                    Check("Made of Tougher Stuff rank III: a small hit is 35% softer", Mathf.Abs(before - Hp(minion) - 0.65f) < 0.11f, before + " -> " + Hp(minion));
                }
                ClearFoes();
            }

            // Compound Interest's cap.
            RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Interest")) as Upgradeproperties);
            RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Interest2")) as Upgradeproperties);
            RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", "Interest3")) as Upgradeproperties);
            float gain = Game_Mapvars.Instance.Resourcegainmultiplier;
            float i0 = Purse2(me, "Redaura");
            RWDirections.Interest(1);
            float i1 = Purse2(me, "Redaura");
            Check("Compound Interest: never more than 40 of a colour a Tide, however rich", Mathf.Abs(i1 - i0 - RWDirections.InterestCap * gain) < 0.01f, i0 + " -> " + i1 + " at gain " + gain);

            // Vampire Dentures, real damage, then two stacks. Only the Trinket's own part of the hit counts.
            player.Pure = 200;
            string why = RWCardinals.Buy(player, "Cardinal_Dentures");
            yield return new WaitForSeconds(1f);
            yield return NewFoe();
            Entity_Master bite = lastFoe;
            if (why != "" || bite == null) Setup("Vampire Dentures", "buy '" + why + "', foe " + (bite != null));
            else
            {
                Hurt(bite, vessel, 3f);
                yield return new WaitForSeconds(0.6f);
                ClearFoes();
                yield return NewFoe();
                bite = lastFoe;
                float hv0 = Hp(vessel), healed0 = RWCardinals.Healed, dealt0 = vessel.Damagedone;
                if (bite != null) Hurt(vessel, bite, 5f);
                yield return new WaitForSeconds(0.8f);
                float gained = RWCardinals.Healed - healed0;
                Info("Vampire Dentures", "Damagedone " + dealt0 + " -> " + vessel.Damagedone + ", healed " + gained);
                Check("Vampire Dentures: the vessel heals a fifth of the damage it deals", gained > 0f && Hp(vessel) > hv0 && Mathf.Abs(gained - (vessel.Damagedone - dealt0) * RWCardinals.DenturesHeal) < 0.3f, "health " + hv0 + " -> " + Hp(vessel) + ", healed " + gained + ", Damagedone " + vessel.Damagedone);
                string why2 = RWCardinals.Buy(player, "Cardinal_Dentures");
                yield return new WaitForSeconds(0.6f);
                healed0 = RWCardinals.Healed;
                ClearFoes();
                yield return NewFoe();
                bite = lastFoe;
                if (bite != null) Hurt(bite, vessel, 3f);
                yield return new WaitForSeconds(0.4f);
                dealt0 = vessel.Damagedone;
                if (bite != null) Hurt(vessel, bite, 2f);
                yield return new WaitForSeconds(0.8f);
                Check("Vampire Dentures stack: two heal twice as much", why2 == "" && player.Trinkets.Count == 2 && Mathf.Abs(RWCardinals.Healed - healed0 - (vessel.Damagedone - dealt0) * RWCardinals.DenturesHeal * 2f) < 0.3f, "healed " + (RWCardinals.Healed - healed0) + " for a 2 damage hit; held " + string.Join(",", player.Trinkets.ToArray()));
                ClearFoes();
            }

            // The shop's refusals, on the host's own order path.
            player.Pure = 0;
            Check("Trinkets: refused with too little Pure Aura", RWCardinals.Buy(player, "Cardinal_Dentures") == "Not enough Pure Aura", "");
            player.Pure = 200;
            string stockCard = RWCardinals.Sold.Find(s => !RWCardinals.IsMine(s));
            string first = RWCardinals.Buy(player, stockCard), second = RWCardinals.Buy(player, stockCard);
            Check("Cardinals: a stock one can be held only once, and the refusal takes nothing", first == "" && second == "Already held" && player.Pure == 200 - RWCardinals.Cost(stockCard), stockCard + ": '" + first + "', '" + second + "', Pure " + player.Pure);
            // B3, B4: a Trinket stacks to 6 and then leaves the shelf; each stack shows on the vessel's panel; a Cardinal the
            // row has no room for is counted on the run line.
            player.Pure = 500;
            int dentures0 = player.Trinkets.Count;
            string refused = "";
            for (int k = 0; k < RWCardinals.Stacks + 1 && refused == ""; k++) refused = RWCardinals.Buy(player, "Cardinal_Dentures");
            yield return new WaitForSeconds(1.2f);
            int onPanel = 0;
            for (int k = 1; k <= RWCardinals.Stacks; k++)
                if (vessel != null && vessel.Upgradelistserver != null && vessel.Upgradelistserver.ContainsKey("Cardinal_Dentures" + k + "_Upgradeobject")) onPanel++;
            Check("B3 a Trinket stacks to 6 copies and the 7th is refused", player.Trinkets.Count == RWCardinals.Stacks && refused != "", "held " + player.Trinkets.Count + ", refusal '" + refused + "'");
            Check("B4 every held Trinket stack is on the vessel's panel", onPanel == RWCardinals.Stacks, onPanel + " of " + RWCardinals.Stacks + " on the vessel");
            List<string> maxedShelf = RWCardinals.Shelf(RWCampaign.Seed, 1, 3, c => RWCardinals.MaxedFor(player, c));
            Check("B3 a maxed one leaves the shelf, which still holds 3", !maxedShelf.Contains("Cardinal_Dentures") && !maxedShelf.Contains(stockCard) && maxedShelf.Count == RWCardinals.OnShelf, string.Join(",", maxedShelf.ToArray()));
            List<string> rolled = RWCardinals.ShelfFor("RWTest", RWCampaign.Seed, 1, 5, c => false), afterBuy = RWCardinals.ShelfFor("RWTest", RWCampaign.Seed, 1, 5, c => true);
            Check("A buy leaves the break's shelf as it was rolled (no next card pulled in)", string.Join(",", rolled.ToArray()) == string.Join(",", afterBuy.ToArray()) && rolled.Count == RWCardinals.OnShelf, string.Join(",", rolled.ToArray()) + " / " + string.Join(",", afterBuy.ToArray()));
            while (player.Trinkets.Count > dentures0) player.Trinkets.RemoveAt(player.Trinkets.Count - 1);
            int over0 = RWCardinals.Overflow();
            RWRun.Cardinals.Add("Cardinal_RWTestOverflow#1");
            yield return new WaitForSeconds(1.2f);
            string runShown = RWRunLabel.Shown;
            RWRun.Cardinals.Remove("Cardinal_RWTestOverflow#1");
            Check("B4 a Cardinal the row has no room for shows as +N on the run line", runShown.Contains("+" + (over0 + 1) + (over0 == 0 ? " Cardinal" : " Cardinals")), "'" + runShown + "', overflow before " + over0);
            // Only a run holding more than 8 can show the cap (MAX KIT-3: one Cardinal held passed it every time).
            if (RWRun.Cardinals.Count > 8)
                Check("The HUD's Cardinal row keeps to 8, the rest in the run's count", Tds_Multiplayerinfo.Instance.Artifacts.Count <= 8, "row " + Tds_Multiplayerinfo.Instance.Artifacts.Count + ", cardinals " + RWRun.Cardinals.Count);
            else
                Setup("The HUD's Cardinal row keeps to 8", "the run holds " + RWRun.Cardinals.Count + " Cardinals, 9 needed to test it");

            // Each objective failing pays nothing, and the map goes on.
            yield return ObjectiveFails(player, vessel, me);

            // A felled vessel comes back carrying everything.
            int trinkets0 = player.Trinkets.Count;
            vessel = RWRun.Vessel(me);
            float dmg0 = vessel.Statsserver.ContainsKey("Damage") ? vessel.Statsserver["Damage"].Value_Calc : -1f;
            RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Smacky", "Skipping")) as Upgradeproperties);
            yield return new WaitForSeconds(1.2f);
            float dmg1 = vessel.Statsserver.ContainsKey("Damage") ? vessel.Statsserver["Damage"].Value_Calc : -1f;
            yield return NewFoe();
            Entity_Master killer = lastFoe;
            Entity_Master was = vessel;
            if (killer != null) Hurt(killer, vessel, 100000f);
            ClearFoes();
            yield return new WaitForSeconds(1f);
            freeze = false;
            yield return Until(() => { Entity_Master v = RWRun.Vessel(me); return v != null && v != was && v.Beingserver != null && !v.Beingserver.Dead && v.Commandlistserver != null && v.Commandlistserver.ContainsKey(RWRun.OrderLink); }, 150f);
            freeze = true;
            yield return new WaitForSeconds(1.5f);
            vessel = RWRun.Vessel(me);
            bool newOne = vessel != null && vessel != was && !vessel.Beingserver.Dead;
            float dmg2 = newOne && vessel.Statsserver.ContainsKey("Damage") ? vessel.Statsserver["Damage"].Value_Calc : -1f;
            bool ups = newOne && player.Directions.TrueForAll(d => vessel.Upgradelistserver.ContainsKey(d));
            Check("A felled vessel comes back with its Directions, its stat and its counter", newOne && ups && Mathf.Abs(dmg2 - dmg1) < 0.01f && Mathf.RoundToInt(RogueWavesModeKitCounter.Get(vessel, RWRun.PureStat)) == player.Pure, "new " + newOne + ", all Directions on it " + ups + ", Damage " + dmg0 + " -> " + dmg1 + " -> " + dmg2 + ", counter " + (newOne ? RogueWavesModeKitCounter.Get(vessel, RWRun.PureStat) : -1f) + " of " + player.Pure);
        }

        if (Part("scales")) yield return UiScales(player);

        // Off the map, a retry spent; a fresh campaign follows.
        RWCampaign.Playing = -1;
        Macro_Main old = Macro_Main.Instance;
        freeze = false;
        forcing = false;
        Tds_Multiplayer_Remote.Instance.Quitmap_ServerRpc(default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        yield return new WaitForSecondsRealtime(3f);
    }

    static IEnumerator ObjectiveFails(RWPlayer player, Entity_Master vessel, long me)
    {
        foreach (string name in new[] { "Hull", "Clock", "Hands", "Meteor", "Nest" })
        {
            RWObjectiveDef goal = Array.Find(RWObjectives.All, o => o.Name == name);
            RWObjectives.Posted = goal;
            RWObjectives.Active = null;
            Check("Objective " + goal.Display + ": taken once", RWObjectives.Take(player) == "" && RWObjectives.Take(player) == "Already taken", "");
            int pure0 = player.Pure;
            int wave = Game_Mapvars.Instance.Wave.Value + 1;
            int breaks = RWVote.Breaks;
            freeze = false;
            forcing = false;
            Game_Mapvars.Instance.Time_Wave.Value = 0.1f;
            yield return Until(() => Game_Mapvars.Instance.Wave.Value >= wave, 40f);
            yield return new WaitForSeconds(0.5f);
            if (name == "Hull")
            {
                Entity_Master core = RWUtil.PlayerCore();
                yield return NewFoe();
                Entity_Master foe = lastFoe;
                if (foe != null) Hurt(foe, core, 1f);
                yield return new WaitForSeconds(0.2f);
                Helper_Entitytools.Interact(core, core, "Heal", new Dictionary<string, float> { { "Damage", 1f }, { "Damagemulti", 1f } });
            }
            if (name == "Hands")
            {
                Entity_Master v = RWRun.Vessel(me);
                yield return NewFoe();
                Entity_Master foe = lastFoe;
                if (foe != null) Hurt(foe, v, 100000f);
            }
            if (name == "Meteor" || name == "Nest")
                Check("Objective " + goal.Display + ": its target stands", RWObjectives.Target != null, RWObjectives.Target != null ? RWUtil.V(RWObjectives.Target.Entityposition.Value) : "none");
            if (name == "Meteor")
            {
                // The white crystal from the part's model library (open decisions 11-05b item 10); look at it by eye too.
                GameObject vis = RWObjectives.Target != null && RWObjectives.Target.Client != null ? RWObjectives.Target.Client.Currentvis : null;
                Check("Objective " + goal.Display + ": the node wears the white crystal " + RWModels.Meteor + ", lit like a stock ore", vis != null && vis.name.StartsWith(RWModels.Meteor) && RWModels.IsLit(vis),
                    vis != null ? vis.name + (RWModels.IsLit(vis) ? ", tower shader" : ", NOT on the tower shader (no hit flash)") : "no look");
            }
            Entity_Master target = RWObjectives.Target;
            forcing = true;
            float t = 0f;
            while (RWVote.Breaks == breaks && t < 90f)
            {
                ClearFoesKeeping(target);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            forcing = false;
            ClearFoes();
            freeze = true;
            yield return new WaitForSeconds(0.8f);
            int paidTide = player.Pure - pure0;
            Check("Objective " + goal.Display + " failed pays nothing, says so, and the map runs on", RWObjectives.Outcome == name + " failed" && paidTide == RWRun.PerTide && Game_Mapvars.Instance.Gamecomplete.Value.ToString() == "Running",
                "outcome '" + RWObjectives.Outcome + "', Pure +" + paidTide);
            if (name == "Meteor" || name == "Nest") Check("Objective " + goal.Display + ": its target goes at the Tide's end", target == null || target.Beingserver == null || target.Beingserver.Dead, "");
            if (name == "Hands")
            {
                freeze = false;
                yield return Until(() => { Entity_Master v = RWRun.Vessel(me); return v != null && v.Beingserver != null && !v.Beingserver.Dead && v.Commandlistserver != null && v.Commandlistserver.ContainsKey(RWRun.OrderLink); }, 150f);
                freeze = true;
                yield return new WaitForSeconds(1f);
            }
        }
    }

    static void ClearFoesKeeping(Entity_Master keep)
    {
        List<Entity_Master> foes;
        if (Game_Entity.Instance == null || !Game_Entity.Instance.Entityclienttags.TryGetValue("Enemy", out foes)) return;
        foreach (Entity_Master e in new List<Entity_Master>(foes))
            if (e != null && e != keep && e.Beingserver != null && !e.Beingserver.Dead && e.Entitydata.Value.Team == 0 && !e.Taglistserver.Contains("Enemycore") && e != RWWeather.Nest && e != RWWeather.Flood)
                Game_Entity.Instance.Despawn(e.Beingserver, false, "RogueWaves");
    }

    // ---------------------------------------------------------------- the dialogs at each UI scale and three screen sizes

    static void SetUiScale(float s)
    {
        foreach (Blockage.Menu.Blu1_Ui_Handler h in UnityEngine.Object.FindObjectsOfType<Blockage.Menu.Blu1_Ui_Handler>(true))
            h.Updatescale(s);
    }

    static IEnumerator UiScales(RWPlayer player)
    {
        int width0 = Screen.width, height0 = Screen.height;
        FullScreenMode mode0 = Screen.fullScreenMode;
        float scale0 = Blockage.Menu.Bl_Menus.Ui != null ? Blockage.Menu.Bl_Menus.Ui.GetComponent<UnityEngine.UI.CanvasScaler>().scaleFactor : 3f;
        Info("the UI scale in use", scale0 + " at " + width0 + "x" + height0 + ", settings Uiscale '" + RWUtil.ReadString("Settings", "Uiscale") + "'");
        player.Pure = 60;
        int[][] sizes = { new[] { 2560, 1440 }, new[] { 1920, 1080 }, new[] { 1280, 720 } };
        foreach (int[] size in sizes)
        {
            Screen.SetResolution(size[0], size[1], FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(1.5f);
            float most = size[0] >= 2560 ? 4f : size[0] >= 1920 ? 3f : 2f;
            for (float s = 1f; s <= most; s += 1f)
            {
                SetUiScale(s);
                yield return new WaitForSecondsRealtime(0.5f);
                string at = Screen.width + "x" + Screen.height + " at UI scale " + s;
                RWShop.Open("Shop");
                yield return WaitDialog(6f);
                yield return DialogFits("VENDCINT's shop, " + at);
                if (s == 1f || s == most) yield return Snap("scale_shop_" + Screen.width + "_" + s);
                ClickCard("Back");
                yield return Until(() => !RWDialog.Up(), 5f);
                yield return new WaitForSecondsRealtime(0.8f);

                RWObjectives.Active = null;
                RWObjectives.Posted = RWObjectives.All[0];
                RWRun.RunLine(RWMine.Session, "Posted: " + RWObjectives.All[0].Display);
                yield return new WaitForSeconds(0.5f);
                RWShop.Open("Board");
                yield return WaitDialog(6f);
                yield return DialogFits("The Board, " + at);
                ClickCard("Back");
                yield return Until(() => !RWDialog.Up(), 5f);
                yield return new WaitForSecondsRealtime(0.8f);

                // The questions' own cards, the map vote at its most: four maps with the longest line.
                List<Bla_Ui_Confirmation.Entry> cards = new List<Bla_Ui_Confirmation.Entry>();
                foreach (RWChart.Node n in RWCampaign.Chart.Rows[1].Count >= 4 ? RWCampaign.Chart.Rows[1] : RWCampaign.Chart.Rows[2])
                    cards.Add(RWDialog.Card(n.Col.ToString(), RWVote.MapName(n), RWWeather.Display(RWChart.QuickeningCurrent) + RWCard.Separator + "Danger " + n.Label, RWVote.MapPicture(n), RWDialog.Teal));
                string pick = null;
                RogueWavesModeMod.Instance.StartCoroutine(RWDialog.Ask(cards, "WHERE NEXT?", "The party sails to the map most of you pick", 0f, x => pick = x));
                yield return WaitDialog(6f);
                yield return DialogFits("The map vote, " + at);
                if (s == 1f || s == most) yield return Snap("scale_vote_" + Screen.width + "_" + s);
                RWDialog.Cancel();
                yield return Until(() => !RWDialog.Up(), 5f);
                yield return new WaitForSecondsRealtime(0.8f);

                pick = null;
                List<Bla_Ui_Confirmation.Entry> leave = new List<Bla_Ui_Confirmation.Entry>
                {
                    RWDialog.Card("Stay", "Stay", "Another bonus Tide: more Pure Aura, and harder", RWAssets.Sprite("RogueWaves_Stay"), RWDialog.Teal),
                    RWDialog.Card("Leave", "Leave", "Leave this map", RWAssets.Sprite("RogueWaves_Leave"), RWDialog.Coral),
                };
                RogueWavesModeMod.Instance.StartCoroutine(RWDialog.Ask(leave, "TIDE CLEARED", "A majority leaves. Nobody leaves right before their demise", 0f, x => pick = x));
                yield return WaitDialog(6f);
                yield return DialogFits("The vote to leave, " + at);
                RWDialog.Cancel();
                yield return Until(() => !RWDialog.Up(), 5f);
                yield return new WaitForSecondsRealtime(0.8f);
            }
        }
        SetUiScale(scale0);
        Screen.SetResolution(width0, height0, mode0);
        yield return new WaitForSecondsRealtime(1.5f);
        Info("the screen put back", Screen.width + "x" + Screen.height + " at UI scale " + scale0);
    }

    // ---------------------------------------------------------------- ten real maps

    static IEnumerator GapsTen()
    {
        Macro_Main boat = Macro_Main.Instance;
        RWCampaign.New(0);
        RWRun.Clear();
        boat.Selectcharacter("Hero_Yossele");
        yield return Until(() => RWCampaign.Live && Here(), 40f);
        long me = RWMine.Session;
        yield return Until(() => boat.Selected.Value.ToString() == RWCampaign.Chart.At(0, 0).Name && Macro_Worldvisual.Instance.Insetup, 40f);
        boat.Playgame(true);
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.DirectionKind), 240f);
        yield return WaitDialog(10f);
        ClickCard("0");
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.MapKind) || RWCampaign.Choice >= 0, 20f);
        if (RWAsk.Open.StartsWith(RWVote.MapKind))
        {
            yield return WaitDialog(10f);
            ClickCard(CardNames()[0]);
        }
        Macro_Main old = boat;
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 200f);
        string pool = string.Join(", ", RWDifficulty.CampaignPool(RWCampaign.Seed).ToArray());
        Info("the campaign's modifiers", pool);
        float started = Time.realtimeSinceStartup;
        for (int map = 1; map <= RWChart.Rows; map++)
        {
            boat = Macro_Main.Instance;
            yield return Until(() => Here(), 40f);
            yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 90f);
            string plan = RWCampaign.Next != null ? RWCampaign.Next.Plan : "-";
            int weather = RWCampaign.Next != null ? RWCampaign.Next.Weather : -1;
            int loads = RWCard.LoadingWrites;
            boat.Playgame(true);
            yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
            if (RWCampaign.Playing < 0) { Check("map " + map + " loads", false, plan); yield break; }
            yield return Until(() => RWShop.Stand != null, 120f);
            freeze = true;
            RWPlayer player = RWRun.For(me);
            yield return new WaitForSeconds(1f);
            float wantScale = 1f + RWDifficulty.CountPerMap * (map - 1), wantHealth = RWDifficulty.HealthPerMap * (map - 1);
            int wantMods = map < 3 ? 0 : Mathf.Min(4, 1 + (map - 3) / 2);
            int mods = RWDifficulty.LastModifiers == "" ? 0 : RWDifficulty.LastModifiers.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Length;
            Check("Map " + map + " of 10 (" + plan + ", " + RWWeather.Display(weather) + "): harder as written, every modifier loaded", Mathf.Abs(RWDifficulty.LastScale - wantScale) < 0.001f && Mathf.Abs(RWDifficulty.LastHealth - wantHealth) < 0.001f && mods == wantMods,
                "foes x" + RWUtil.F(RWDifficulty.LastScale) + ", health +" + RWUtil.F(RWDifficulty.LastHealth * 100f) + "%, modifiers " + mods + " of " + wantMods + " [" + RWDifficulty.LastModifiers + "]; " + RWUtil.F(Time.realtimeSinceStartup - started) + " s in");

            // Each pass of a Tide: a real Tide's foes carry the map's health and modifiers.
            if (map >= 3 || map == 2)
            {
                freeze = false;
                forcing = false;
                Game_Mapvars vars = Game_Mapvars.Instance;
                vars.Entry.Value = 15;     // the break before Tide 8 (its row is 2N - 1, and the row in play is Entry - 1), whose rows are big enough for a marked share
                vars.Wave.Value = 7;
                yield return new WaitForSeconds(0.5f);
                vars.Time_Wave.Value = 0.1f;
                yield return Until(() => vars.Wave.Value >= 8, 30f);
                string foes = "";
                int withHealth = 0, withMod = 0, total = 0;
                float t = 0f;
                List<string> names = new List<string>();
                // Every foe the Tide sends in its first seconds is read as it comes and taken off, so none reaches the Basin.
                List<Entity_Master> seen = new List<Entity_Master>();
                while (t < 24f)
                {
                    t += Time.unscaledDeltaTime;
                    List<Entity_Master> all;
                    if (Game_Entity.Instance.Entityclienttags.TryGetValue("Enemy", out all))
                        foreach (Entity_Master e in new List<Entity_Master>(all))
                        {
                            if (e == null || e.Beingserver == null || e.Beingserver.Dead || seen.Contains(e) || e.Taglistserver.Contains("Enemycore") || e.Taglistserver.Contains("Tower") || e.Entitydata.Value.Team != 0) continue;
                            if (e.Upgradelistserver == null || e.Upgradelistserver.Count == 0) continue;     // its upgrades arrive a frame after it
                            seen.Add(e);
                            total++;
                            bool h = false, m = false;
                            foreach (string u in e.Upgradelistserver.Keys)
                            {
                                if (u.StartsWith("RW_Foe_Health")) h = true;
                                if (RWDifficulty.LastModifiers.Contains(u)) { m = true; if (!names.Contains(u)) names.Add(u); }
                            }
                            if (h) withHealth++;
                            if (m) withMod++;
                            Game_Entity.Instance.Despawn(e.Beingserver, false, "RogueWaves");
                        }
                    yield return null;
                }
                foes = total + " foes up: " + withHealth + " with the map's health, " + withMod + " with a modifier (" + string.Join(", ", names.ToArray()) + ")";
                if (total == 0) Setup("Map " + map + "'s foes", "none up to read");
                else Check("Map " + map + ": a Tide's foes carry the map's health" + (wantMods > 0 ? " and a share its modifiers" : ""), withHealth == total && (wantMods == 0 || withMod > 0), foes);
                ClearFoes();
            }

            // Straight to Tide 15's break: its row is Entry 29.
            freeze = false;
            forcing = false;
            int pure0 = player.Pure;
            Game_Mapvars v2 = Game_Mapvars.Instance;
            v2.Entry.Value = 2 * RWCampaign.WinTide - 1;
            v2.Wave.Value = RWCampaign.WinTide - 1;
            yield return new WaitForSeconds(0.5f);
            yield return ToBreakAfter(RWCampaign.WinTide);
            yield return Until(() => RWCampaign.WonThisMap, 15f);
            Check("Map " + map + ": Tide 15 cleared wins it", RWCampaign.WonThisMap && RWCampaign.Route.Count == map, "route " + Digits(RWCampaign.Route) + ", Pure " + player.Pure);
            yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 20f);
            yield return WaitDialog(8f);
            ClickCard("Leave");
            if (map < RWChart.Rows)
            {
                yield return Until(() => RWAsk.Open.StartsWith(RWVote.DirectionKind) || RWCampaign.Choice >= 0, 20f);
                if (RWAsk.Open.StartsWith(RWVote.DirectionKind))
                {
                    yield return WaitDialog(8f);
                    Info("map " + map + "'s next pick", CardTexts());
                    ClickCard("0");
                }
                yield return Until(() => RWAsk.Open.StartsWith(RWVote.MapKind) || RWCampaign.Choice >= 0, 20f);
                if (RWAsk.Open.StartsWith(RWVote.MapKind))
                {
                    yield return WaitDialog(8f);
                    List<string> next = CardNames();
                    ClickCard(next[next.Count - 1]);
                }
            }
            yield return Until(() => Game_Mapvars.Instance == null || Game_Mapvars.Instance.Gamecomplete.Value.ToString() == "Win", 30f);
            yield return new WaitForSeconds(7f);
            GameObject title = null;
            Blockage.Menu.Blu2_Menu_Handler win;
            if (Blockage.Menu.Bl_Menus.Menus.TryGetValue("Win", out win) && win != null) win.Items.TryGetValue("Title", out title);
            string wantTitle = map < RWChart.Rows ? "MAP " + map + " OF 10 WON" : "CAMPAIGN WON";
            Check("Map " + map + "'s end screen reads " + wantTitle, title != null && TextOf(title).ToUpperInvariant().Contains(wantTitle), "'" + TextOf(title) + "', state: active " + RWCampaign.Active + ", ended '" + RWCampaign.Ended + "'");
            if (map == RWChart.Rows) yield return Snap("campaign_won");
            old = Macro_Main.Instance;
            Tds_Multiplayer_Remote.Instance.Returnmap_ServerRpc(true, default(Unity.Netcode.ServerRpcParams));
            yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
            yield return new WaitForSecondsRealtime(1f);
        }
        uint seed;
        List<int> saved;
        Check("Ten maps won: the campaign is over, retries unspent, the save is clean", !RWCampaign.Active && RWCampaign.Ended == "won" && !RWCampaign.Saved(out seed, out saved) && RWUtil.ReadInt(RWData.SaveFolder, RWRun.CountKey, 0) == 0, "ended '" + RWCampaign.Ended + "', " + RWUtil.F(Time.realtimeSinceStartup - started) + " s for ten maps");
        yield return Until(() => !RWBoat.OnChart && Macro_Worldvisual.Instance.World != null, 40f);
        Check("Ten maps won: the party is taken home to the stock world", !RWBoat.OnChart, Macro_Worldvisual.Instance.World != null ? Macro_Worldvisual.Instance.World.name : "-");
        yield return new WaitForSecondsRealtime(3f);
        yield return Snap("home_after_campaign");
    }

    // The difficulty on real foes: a map far enough along for three modifiers, Tide 8 read as it comes.
    static IEnumerator GapsFoes()
    {
        Macro_Main boat = Macro_Main.Instance;
        boat.Selectcharacter("Hero_Smacky");
        yield return new WaitForSecondsRealtime(1f);
        RWCampaign.New(Seed);
        RWCampaign.Staging = false;
        yield return Until(() => RWCampaign.Live && Here(), 40f);
        RWChart.Node at = RWCampaign.Chart.At(0, 0);
        for (int m = 0; m < 6; m++) { RWCampaign.Route.Add(at.Links[0]); at = RWCampaign.Here; }
        yield return Until(() => Here(), 40f);
        RWChart.Node to = RWCampaign.Open()[0];
        to.Weather = RWChart.Calm;
        RWCampaign.Choice = to.Col;
        yield return Until(() => boat.Selected.Value.ToString() == to.Name && Macro_Worldvisual.Instance.Insetup, 90f);
        boat.Playgame(true);
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
        yield return Until(() => RWShop.Stand != null, 120f);
        int map = RWCampaign.Route.Count + 1;
        Game_Mapvars vars = Game_Mapvars.Instance;
        freeze = false;
        forcing = false;
        vars.Entry.Value = 15;
        vars.Wave.Value = 7;
        yield return new WaitForSeconds(0.5f);
        vars.Time_Wave.Value = 0.1f;
        yield return Until(() => vars.Wave.Value >= 8, 30f);
        int withHealth = 0, withMod = 0, total = 0;
        List<string> names = new List<string>(), kinds = new List<string>();
        float t = 0f, hpFirst = -1f;
        List<Entity_Master> seen = new List<Entity_Master>();
        while (t < 30f)
        {
            t += Time.unscaledDeltaTime;
            List<Entity_Master> all;
            if (Game_Entity.Instance.Entityclienttags.TryGetValue("Enemy", out all))
                foreach (Entity_Master e in new List<Entity_Master>(all))
                {
                    if (e == null || e.Beingserver == null || e.Beingserver.Dead || seen.Contains(e) || e.Taglistserver.Contains("Enemycore") || e.Taglistserver.Contains("Tower") || e.Entitydata.Value.Team != 0) continue;
                    if (e.Upgradelistserver == null || e.Upgradelistserver.Count == 0) continue;
                    seen.Add(e);
                    total++;
                    bool h = false, m = false;
                    foreach (string u in e.Upgradelistserver.Keys)
                    {
                        if (u.StartsWith("RW_Foe_Health")) h = true;
                        if (RWDifficulty.LastModifiers.Contains(u)) { m = true; if (!names.Contains(u)) names.Add(u); }
                    }
                    if (h) withHealth++;
                    if (m) withMod++;
                    if (!kinds.Contains(e.Being.Value.ToString())) kinds.Add(e.Being.Value.ToString());
                    if (hpFirst < 0f) hpFirst = HpMax(e);
                    Game_Entity.Instance.Despawn(e.Beingserver, false, "RogueWaves");
                }
            yield return null;
        }
        Info("map " + map + "'s Tide 8", total + " foes of " + string.Join(",", kinds.ToArray()) + ", first one's health " + hpFirst + ", modifiers on: [" + RWDifficulty.LastModifiers + "]");
        if (total == 0) Setup("real foes", "none came");
        else
        {
            Check("Map " + map + ": every foe of a real Tide carries the map's health", withHealth == total, withHealth + " of " + total);
            Check("Map " + map + ": a share of them, about one in five, carries the campaign's modifiers, every one of them in turn", withMod >= total / 10 && withMod <= total / 2 && names.Count >= 1, withMod + " of " + total + ": " + string.Join(", ", names.ToArray()));
        }
        ClearFoes();
        RWCampaign.Playing = -1;
        Macro_Main old = Macro_Main.Instance;
        Tds_Multiplayer_Remote.Instance.Quitmap_ServerRpc(default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        yield return new WaitForSecondsRealtime(3f);
    }

    // ---------------------------------------------------------------- every chart map, a different vessel each

    static IEnumerator GapsTour()
    {
        Macro_Main boat = Macro_Main.Instance;
        foreach (int w in new[] { RWChart.RisingWater, RWChart.NestingGround })
        {
            RWWeather.Preload(w);
            yield return Until(() => RWWeather.Loaded(w), 150f);
        }
        RWCampaign.New(Seed);
        RWCampaign.Staging = false;
        yield return Until(() => RWCampaign.Live && Here(), 40f);
        long me = RWMine.Session;
        RWChart.Node at = RWCampaign.Open()[0];
        List<RWChart.Plan> plans = new List<RWChart.Plan>(RWMode.Pool);
        Info("the tour", plans.Count + " maps");
        bool boutmanDone = false;
        List<string> stockStats = new List<string>();
        noNest.Clear();
        freeze = false;
        forcing = false;
        string[] args = Environment.GetCommandLineArgs();
        int onlyAt = Array.IndexOf(args, "-rwmaps");
        string[] onlyMaps = onlyAt >= 0 && onlyAt + 1 < args.Length ? args[onlyAt + 1].Split((char)44) : null;
        float started = Time.realtimeSinceStartup;
        for (int i = 0; i < plans.Count; i++)
        {
            RWChart.Plan plan = plans[i];
            if (onlyMaps != null && Array.IndexOf(onlyMaps, plan.Name) < 0) continue;
            string hero = AllHeroes[i % AllHeroes.Length];
            RWPlayer player = RWRun.For(me);
            player.Hero = "";
            player.Directions.Clear();
            player.Trinkets.Clear();
            RWRun.Cardinals.Clear();
            boat = Macro_Main.Instance;
            boat.Selectcharacter(hero);
            yield return new WaitForSecondsRealtime(0.8f);
            at.Plan = plan.Name;
            at.Map = plan.Map;
            at.Label = plan.Label;
            at.Weather = RWChart.Calm;
            RWCampaign.Replay = -1;
            RWCampaign.Retries = RWCampaign.RetriesPerCampaign;
            foreach (Macro_Maplink_Add l in Macro_Worldvisual.Instance.World.GetComponentsInChildren<Macro_Maplink_Add>(true))
                if (l.name == at.Name) l.Gameplan = RWMode.TwinPlan(plan.Name);
            Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(at.Name, false, default(Unity.Netcode.ServerRpcParams));
            yield return Until(() => boat.Selected.Value.ToString() == at.Name && Macro_Worldvisual.Instance.Insetup && Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString() == RWMode.Twin(plan.Name), 40f);
            int e0 = hardErrors;
            float clock = Time.realtimeSinceStartup;
            boat.Playgame(true);
            Game_Mapvars vars0 = Game_Mapvars.Instance;
            yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance != vars0 && Game_Mapvars.Instance.Maploaded.Value && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
            string name = plan.Name + " as " + hero;
            if (Game_Mapvars.Instance == null || !Game_Mapvars.Instance.Maploaded.Value || !RWRun.InCampaignMap) { Check(name + " loads in the mode", false, "no map after 240 s, Gameplan " + Tds_Multiplayerinfo.Instance.Gameplan.Value); yield break; }
            yield return Until(() => RWShop.Stand != null, 90f);
            freeze = true;
            Entity_Master stand = RWShop.Stand;
            Entity_Master vessel = RWRun.Vessel(me);
            string loadNote = RWUtil.F(Time.realtimeSinceStartup - clock) + " s";
            yield return new WaitForSeconds(1f);
            Entity_Master core = RWUtil.PlayerCore();
            Check(name + ": the vessel stands, VENDCINT lands near the Basin, an objective is posted", vessel != null && vessel.Being.Value.ToString() == hero && stand != null && core != null && ModKitUtil.Flat(stand.Entityposition.Value, core.Entityposition.Value) <= 8f && RWObjectives.Posted != null,
                "vessel " + (vessel != null ? vessel.Being.Value.ToString() : "-") + ", VENDCINT " + (stand != null ? RWUtil.F(ModKitUtil.Flat(stand.Entityposition.Value, core != null ? core.Entityposition.Value : Vector3.zero)) + " from the Basin" : "none") + ", posted " + (RWObjectives.Posted != null ? RWObjectives.Posted.Display : "-") + ", " + loadNote);

            // Both weather objects find a tile on this map.
            Entity_Master flood = RWWeather.TestFlood();
            Check(name + ": Rising Water can stand here", flood != null, "flood " + (flood != null));
            yield return new WaitForSeconds(0.3f);
            if (flood != null && flood.Beingserver != null) Helper_Entitytools.Despawn(flood.Beingserver, false, "RogueWaves");
            if (!RWWeather.CanNest()) noNest.Add(plan.Name);

            // A base vessel's Direction: its stat is there to hold, at each rank.
            foreach (RWDirectionDef d in RWDirections.Mine)
                if (d.Vessel == RWDirections.VesselName(hero) && d.Stat != "" && !stockStats.Contains(d.Name))
                {
                    stockStats.Add(d.Name);
                    string how = "";
                    bool all = vessel.Statsserver.ContainsKey(d.Stat);
                    float b0 = all ? vessel.Statsserver[d.Stat].Value_Calc : -1f;
                    how = d.Stat + " " + b0;
                    for (int r = 1; r <= 3; r++)
                    {
                        RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id(d.Vessel, d.Name + (r > 1 ? r.ToString() : ""))) as Upgradeproperties);
                        yield return new WaitForSeconds(1.1f);
                        float now = vessel.Statsserver.ContainsKey(d.Stat) ? vessel.Statsserver[d.Stat].Value_Calc : -1f;
                        float want = d.Plain ? b0 + d.Amounts[r - 1] : b0 * (1f + d.Amounts[r - 1]);
                        how += " -> " + now;
                        if (Mathf.Abs(now - want) > 0.02f) all = false;
                    }
                    Check(d.Display + " holds " + d.Stat + " on " + hero + " at each rank", all, how);
                }

            // Two objectives' targets find a tile, in real Tides; the first Tide pays.
            yield return Tour2(i, name, me, hero, player);
            if (hero == "Hero_Boutman" && !boutmanDone)
            {
                boutmanDone = true;
                yield return FKey(me);
            }
            Check(name + ": no error from loading to here", hardErrors == e0, (hardErrors - e0) + " errors; " + RWUtil.F(Time.realtimeSinceStartup - started) + " s into the tour");

            // Back to the boat, nothing counted.
            RWCampaign.Playing = -1;
            RWCampaign.WonThisMap = false;
            Macro_Main old = Macro_Main.Instance;
            freeze = false;
            forcing = false;
            Tds_Multiplayer_Remote.Instance.Quitmap_ServerRpc(default(Unity.Netcode.ServerRpcParams));
            yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
            yield return Until(() => Here(), 40f);
            yield return new WaitForSecondsRealtime(1.5f);
        }
        Info("the tour", "done in " + RWUtil.F(Time.realtimeSinceStartup - started) + " s; no tile for a nest on: " + string.Join(", ", noNest.ToArray()));
    }

    static readonly List<string> noNest = new List<string>();

    static IEnumerator Tour2(int i, string name, long me, string hero, RWPlayer player)
    {
        RWObjectiveDef[] goals = { Array.Find(RWObjectives.All, o => o.Name == "Meteor"), Array.Find(RWObjectives.All, o => o.Name == "Nest"), Array.Find(RWObjectives.All, o => o.Name == "Nest") };
        int round = 0;
        foreach (RWObjectiveDef goal in goals)
        {
            round++;
            if (goal.Name == "Nest" && !RWWeather.CanNest())
            {
                // Where no nest can land, the board must not offer Raze the Nest.
                bool offered = false;
                for (int w = 1; w <= 30; w++)
                {
                    RWObjectives.Post(w);
                    if (RWObjectives.Posted != null && RWObjectives.Posted.Name == "Nest") offered = true;
                }
                Check(name + ": no tile for a nest, so the board never posts Raze the Nest" + (round == 3 ? " a second time" : ""), !offered, offered ? "posted" : "never in 30 rolls");
                continue;
            }
            RWObjectives.Posted = goal;
            RWObjectives.Active = null;
            RWObjectives.Take(player);
            int pure0 = player.Pure, room0 = RWObjectives.NoRoom;
            int wave = Game_Mapvars.Instance.Wave.Value + 1;
            yield return ToBreakAfter(wave, true);
            yield return new WaitForSeconds(0.3f);
            if (RWObjectives.NoRoom > room0)
                Check(name + ": " + goal.Display + " found no room as its Tide began: the team is told, and the Tide pays as it does", player.Pure - pure0 == RWRun.PerTide, "Pure +" + (player.Pure - pure0));
            else
                Check(name + ": " + goal.Display + "'s target finds a tile, falls, and pays", RWObjectives.Outcome == goal.Name + " done" && player.Pure - pure0 == RWRun.PerTide + goal.Pays, "'" + RWObjectives.Outcome + "', Pure +" + (player.Pure - pure0));
            if (Game_Mapvars.Instance == null || Game_Mapvars.Instance.Gamecomplete.Value.ToString() != "Running") { Check(name + ": the map runs on", false, "ended"); yield break; }
        }
    }

    // The F key with a real Boutbro: the Bouthouse's own, his death, his respawn, the Focus action as a player presses it.
    static IEnumerator FKey(long me)
    {
        Entity_Master vessel = RWRun.Vessel(me);
        Rich(me, 3000f);
        List<Entity_Master> known = new List<Entity_Master>();
        Entity_Master house = null;
        yield return BuildOne(vessel, "Tower_Boutmanhouse", new List<Vector3>(), known, (m, c) => house = m);
        if (house == null) { Setup("F key", "no Bouthouse"); yield break; }
        Func<List<string>> heroes = () =>
        {
            List<string> names = new List<string>();
            List<Entity_Master> list;
            if (Game_Entity.Instance.Entityclienttags.TryGetValue("Hero", out list))
                foreach (Entity_Master e in list) if (e != null) names.Add(e.Being.Value.ToString() + (e.Beingserver != null && e.Beingserver.Dead ? "(dead)" : ""));
            return names;
        };
        yield return Until(() => heroes().Count >= 2, 60f);
        Info("F key: the Hero tag list before", string.Join(",", heroes().ToArray()));
        if (heroes().Count < 2) { Setup("F key", "no Boutbro tagged Hero after 60 s"); yield break; }
        Entity_Master was = vessel;
        yield return NewFoe();
        Entity_Master foe = lastFoe;
        if (foe != null) Hurt(foe, vessel, 100000f);
        ClearFoes();
        freeze = false;
        // Read once he is down: in the killing hit's own frame the vessel was still his (MAX KIT-2).
        yield return Until(() => was == null || was.Beingserver == null || was.Beingserver.Dead, 5f);
        yield return null;
        Check("F key: while Boutman is down, his Boutbro isn't taken for his vessel", RWRun.Vessel(me) == null || RWRun.Vessel(me) == was, RWRun.Vessel(me) != null ? RWRun.Vessel(me).Being.Value.ToString() : "none");
        yield return Until(() => { Entity_Master v = RWRun.Vessel(me); return v != null && v != was && v.Being.Value.ToString() == "Hero_Boutman" && v.Beingserver != null && !v.Beingserver.Dead; }, 180f);
        freeze = true;
        yield return new WaitForSeconds(2f);
        vessel = RWRun.Vessel(me);
        Info("F key: the Hero tag list after his respawn", string.Join(",", heroes().ToArray()) + "; a new Entity_Master " + (vessel != was));
        if (vessel == null) { Setup("F key", "Boutman never came back"); yield break; }
        Blockage.Input focus = Blockage.Bl_Inputs.Actions["Focus"];
        // The stock handler alone first: does the bug show?
        Delegate[] chain = focus.Function.GetInvocationList();
        Info("F key: handlers on Focus", chain.Length.ToString());
        Game_Selection.Instance.Selected = null;
        ((Blockage.Input.Pressfunct)chain[0])("Onpressed");
        yield return null;
        Entity_Master stockPick = Game_Selection.Instance.Selected;
        Info("F key: the stock handler alone selects", stockPick != null ? stockPick.Being.Value.ToString() : "nothing");
        Game_Selection.Instance.Selected = null;
        focus.Function("Onpressed");
        yield return null;
        Entity_Master pick = Game_Selection.Instance.Selected;
        Check("F key: after Boutman's real death with a real Boutbro up, F selects Boutman", pick != null && pick == vessel && pick.Being.Value.ToString() == "Hero_Boutman", "selected " + (pick != null ? pick.Being.Value.ToString() : "nothing") + ", stock alone " + (stockPick != null ? stockPick.Being.Value.ToString() : "nothing"));
        Game_Selection.Instance.Selected = null;
        focus.Function("Onpressed");
        yield return null;
        Entity_Master again = Game_Selection.Instance.Selected;
        Info("F key: a second press selects", again != null ? again.Being.Value.ToString() : "nothing");
    }
}
#endif
