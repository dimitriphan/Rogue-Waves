#if ROGUEWAVESMODE_GAPS
// Debug build only, the solo gaps build's part "review" (-rwonly review, Tools/pc/Run-TwoPlayers.ps1 -Extra): the code review's and the
// simplification build's solo PC checks in game (docs/reports/CODE_REVIEW_2026-11-04.md §6, SIMPLIFICATION_BUILD_2026-11-04.md §4 R3, G3).
// A finding's reading is a line "TEST INFO FINDING <id>: SEEN|not seen (...)": it says the same before and after a fix, and fails nothing.
public static partial class RWTest
{
    static readonly List<string> reviewLines = new List<string>();

    static void Finding(string id, bool seen, string detail) { Line("INFO FINDING " + id + ": " + (seen ? "SEEN" : "not seen") + " (" + detail + ")"); }

    static void ReviewLog(string text, string stack, LogType type)
    {
        if (text == null || text.IndexOf("TEST ") >= 0) return;
        if (text.Contains(" paid ") || text.Contains("VENDCINT landed") || text.Contains("posted:") || text.Contains("the vote to leave") || text.Contains("bonus Tide"))
            reviewLines.Add(text);
    }

    static int SeenLines(string part)
    {
        int n = 0;
        foreach (string l in reviewLines) if (l.Contains(part)) n++;
        return n;
    }

    static float UpgradePrice(Entity_Master m)
    {
        Link_Command c;
        return m != null && m.Commandlistserver != null && m.Commandlistserver.TryGetValue("Upgrade", out c) && c != null ? c.Goalvalue : -1f;
    }

    static string Holding(Entity_Master m, string part)
    {
        List<string> held = new List<string>();
        if (m != null && m.Upgradelistserver != null)
            foreach (string k in m.Upgradelistserver.Keys) if (k.Contains(part)) held.Add(k);
        return string.Join(",", held.ToArray());
    }

    static float StatOf(Entity_Master m, string name)
    {
        Link_Stats s;
        return m != null && m.Statsserver != null && m.Statsserver.TryGetValue(name, out s) ? s.Value : -1f;
    }

    static void Direction(RWPlayer player, string name)
    {
        RWDirections.Take(player, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id("Any", name)) as Upgradeproperties);
    }

    static IEnumerator ToBoat()
    {
        Macro_Main old = Macro_Main.Instance;
        freeze = false;
        forcing = false;
        Tds_Multiplayer_Remote.Instance.Quitmap_ServerRpc(default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        yield return new WaitForSecondsRealtime(3f);
    }

    static IEnumerator BackOnBoat()
    {
        if (Macro_Main.Instance == null || !Macro_Main.Instance.Ready) yield return Stepped(ToBoat());
    }

    static IEnumerator GapsReview()
    {
        Application.logMessageReceived += ReviewLog;
        yield return Stepped(ReviewCampaign());
        yield return BackOnBoat();
        yield return Stepped(ReviewBoutman());
        yield return BackOnBoat();
        yield return Stepped(ReviewFie(false));
        yield return BackOnBoat();
        yield return Stepped(ReviewFie(true));
        yield return BackOnBoat();
        yield return Stepped(ReviewVoice());
        Application.logMessageReceived -= ReviewLog;
    }

    // MODE-A1, C12, C1, C4, D1, D6, D2, then on map 2 D5 and the Cardinals' return.
    static IEnumerator ReviewCampaign()
    {
        Macro_Main boat = Macro_Main.Instance;
        RWCampaign.New(0);
        RWRun.Clear();
        boat.Selectcharacter("Hero_Smacky");
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
        yield return new WaitForSecondsRealtime(1f);
        string staged = RWUtil.ReadString(RWData.SaveFolder, RWKeys.Staged);
        Finding("MODE-A1", staged == "1", "after the staging map, on the boat: the save's " + RWKeys.Staged + " '" + staged + "', in memory " + RWCampaign.Staging);

        boat = Macro_Main.Instance;
        yield return Until(() => Here(), 40f);
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 90f);
        boat.Playgame(true);
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
        yield return Until(() => RWShop.Stand != null, 120f);
        freeze = true;
        yield return new WaitForSeconds(3f);
        Game_Mapvars vars = Game_Mapvars.Instance;
        RWPlayer player = RWRun.For(me);
        Entity_Master vessel = RWRun.Vessel(me);
        if (vessel == null) { Setup("the review's campaign", "no vessel"); yield break; }
        Rich(me, 8000f);
        player.Pure = 800;
        List<Vector3> used = new List<Vector3>();
        List<Entity_Master> known = new List<Entity_Master>();

        // MODE-C12: Voice's step is Rate_Base/100 * (float)(30 / Totalwaves), an integer division.
        int total = vars.Mode != null && vars.Mode.Enemies != null ? vars.Mode.Enemies.Totalwaves : -1;
        Finding("MODE-C12", total > 30, "Mode.Enemies.Totalwaves " + total + " on a campaign map, so Voice's factor is " + (total > 0 ? (30 / total).ToString() : "?"));

        // MODE-C1: a Cardinal bought mid-map.
        Entity_Master older = null, newer = null;
        yield return BuildOne(vessel, "Tower_Smackyarcher", used, known, (m, c) => older = m);
        float speed0 = StatOf(vessel, "Speed");
        string sport = RWCardinals.Buy(player, "Artifact_Caffeine"), pipe = RWCardinals.Buy(player, "Artifact_Rustypipe");
        yield return new WaitForSeconds(1.5f);
        yield return BuildOne(vessel, "Tower_Smackyarcher", used, known, (m, c) => newer = m);
        if (older == null) { Setup("MODE-C1", "no tower built before the purchase"); yield break; }
        string onVessel = Holding(vessel, "Caffeine"), onOlder = Holding(older, "Rustypipe"), onNewer = Holding(newer, "Rustypipe");
        Finding("MODE-C1", onVessel == "" || onOlder == "", "bought '" + sport + "' and '" + pipe + "' (empty is yes); vessel's Caffeine [" + onVessel + "], speed " + speed0 + " -> " + StatOf(vessel, "Speed")
            + "; the tower from before [" + onOlder + "], a tower built after [" + onNewer + "]");

        // MODE-C4: Voice alone for two Tides, then Cheap Refits.
        string voice = RWCardinals.Buy(player, "Artifact_Voice");
        Entity_Master voiced = null;
        yield return BuildOne(vessel, "Tower_Smackyarcher", used, known, (m, c) => voiced = m);
        yield return new WaitForSeconds(1f);
        if (voiced == null) { Setup("MODE-C4", "no tower built after Voice"); yield break; }
        float g0 = UpgradePrice(voiced);
        Info("MODE-C4", "bought Voice '" + voice + "'; the new tower holds [" + Holding(voiced, "Voice") + "], price scale " + g0);
        yield return ToBreakAfter(vars.Wave.Value + 1);
        yield return new WaitForSeconds(1f);
        yield return ToBreakAfter(vars.Wave.Value + 1);
        yield return new WaitForSeconds(1f);
        float g2 = UpgradePrice(voiced);
        Direction(player, "Refit");
        yield return new WaitForSeconds(1.5f);
        float g3 = UpgradePrice(voiced);
        Finding("MODE-C4", g2 < g0 - 0.001f && g3 >= g2 + 0.001f, "price scale " + g0 + " at the start, " + g2 + " after two Tides of Voice, " + g3 + " once Cheap Refits is held (fixed: lower than " + g2 + ")");
        Finding("MODE-C12 (the effect)", g2 >= g0 - 0.001f, "two Tides of Voice took the scale from " + g0 + " to " + g2);
        if (voiced != null)
        {
            voiced.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Upgrade", "{\"Upgrade\":\"Tower_Smackyarcher_Range2_Upgradeobject\",\"Upgrader\":" + me + "}", default(Unity.Netcode.ServerRpcParams));
            yield return new WaitForSeconds(1.5f);
            Info("MODE-C4 after an upgrade", "price scale " + UpgradePrice(voiced) + " (fixed: the Refits price)");
        }

        // MODE-D1: on to Tide 29's break, where the bonus rewind begins.
        vars.Entry.Value = 2 * 29 - 1;
        vars.Wave.Value = 28;
        yield return new WaitForSeconds(1f);
        ClearFoes();
        int pure0 = player.Pure, breaks0 = RWVote.Breaks;
        reviewLines.Clear();
        yield return ToBreakAfter(29);
        yield return new WaitForSeconds(3f);
        // The fixed build still sees Break(29) twice (the rewind's repeat is counted before its guard returns), so the reading
        // is the payout's (MAX KIT-1).
        Finding("MODE-D1", SeenLines("Tide 29 paid") >= 2 || SeenLines("VENDCINT landed") >= 2, "Tide 29's break: Break fired " + (RWVote.Breaks - breaks0) + " times, 'Tide 29 paid' " + SeenLines("Tide 29 paid") + " lines, 'VENDCINT landed' "
            + SeenLines("VENDCINT landed") + " lines, Pure +" + (player.Pure - pure0) + " (a Tide pays " + RWRun.PerTide + ")");
        // The vote to leave: stay.
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 20f);
        if (RWAsk.Open == RWVote.LeaveKind)
        {
            yield return WaitDialog(6f);
            ClickCard("Stay");
        }
        yield return Until(() => RWVote.State == "", 25f + RWVote.MapSeconds);    // the stay's vote on the next map too (B1)

        // MODE-D6: a Tide that runs out its clock with a foe left, which falls 45 s into the break; the vote takes its time.
        ClearFoes();
        int wave = vars.Wave.Value;
        freeze = false;
        forcing = false;
        vars.Time_Wave.Value = 0.1f;
        yield return Until(() => vars.Wave.Value > wave, 30f);
        yield return NewFoe();
        Entity_Master keeper = lastFoe;
        if (keeper != null) Helper_Entitytools.Addmod(keeper, "Speed", -0.9f, Helper_Entitytools.Modtypes.Multi, 90f);
        int breaks = RWVote.Breaks;
        forcing = true;
        float t = 0f;
        while (RWVote.Breaks == breaks && t < 60f)
        {
            ClearFoesKeeping(keeper);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        forcing = false;
        vars.Time_Wave.Value = RWMode.BreakSeconds;     // the forced -1 mustn't run this break out as well
        yield return new WaitForSeconds(0.5f);
        float left0 = vars.Time_Wave.Value;
        Info("MODE-D6", "the Tide ran out with " + RWVote.Foes() + " foes up, the break's clock at " + left0 + " s, Wave " + vars.Wave.Value);
        t = 0f;
        while (t < 45f && ModKitTides.InBreak)
        {
            ClearFoesKeeping(keeper);
            t += Time.deltaTime;
            yield return null;
        }
        ClearFoes();
        int waveAsked = vars.Wave.Value;
        string lastVote = RWVote.LastVote;
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 6f);
        bool asked = RWAsk.Open == RWVote.LeaveKind;
        float clockAsked = vars.Time_Wave.Value;
        yield return Until(() => vars.Wave.Value > waveAsked, 18f);       // the player reads the question for 18 s of its 20
        bool cut = vars.Wave.Value > waveAsked;
        string stateThen = RWVote.State, openThen = RWAsk.Open;
        // MODE-D2: the objective taken in the break where the party votes to leave, here if the vote is still up.
        RWObjectiveDef hands = Array.Find(RWObjectives.All, o => o.Name == "Hands");
        string took = "";
        if (!cut && RWDialog.Up())
        {
            RWObjectives.Posted = hands;
            RWObjectives.Active = null;
            took = RWObjectives.Take(player);
            ClickCard("Leave");
            yield return new WaitForSeconds(1f);
        }
        Finding("MODE-D6", cut && RWVote.LastVote == lastVote, "the vote opened " + asked + " with " + clockAsked + " s of break left; 18 s on: Wave " + waveAsked + " -> " + vars.Wave.Value + ", state '" + stateThen + "', question '" + openThen + "', last vote '" + RWVote.LastVote + "', departing " + RWVote.Departing);
        ClearFoes();
        yield return new WaitForSeconds(0.5f);

        if (!RWVote.Departing)
        {
            yield return ToBreakAfter(vars.Wave.Value + (cut ? 0 : 1));
            yield return new WaitForSeconds(1f);
            RWObjectives.Posted = hands;
            RWObjectives.Active = null;
            took = RWObjectives.Take(player);
            yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 25f);
            if (RWAsk.Open != RWVote.LeaveKind) { Setup("MODE-D2", "no vote to leave: state '" + RWVote.State + "', Wave " + vars.Wave.Value); yield break; }
            yield return WaitDialog(6f);
            ClickCard("Leave");
        }
        Info("MODE-D2", "took Mind the Vessels in the break of the vote to leave: '" + took + "' (empty is yes)");
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.DirectionKind) || RWCampaign.Choice >= 0, 20f);
        if (RWAsk.Open.StartsWith(RWVote.DirectionKind))
        {
            yield return WaitDialog(8f);
            ClickCard("0");
        }
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.MapKind) || RWCampaign.Choice >= 0, 20f);
        if (RWAsk.Open.StartsWith(RWVote.MapKind))
        {
            yield return WaitDialog(8f);
            List<string> next = CardNames();
            ClickCard(next[next.Count - 1]);
        }
        yield return Until(() => Game_Mapvars.Instance == null || Game_Mapvars.Instance.Gamecomplete.Value.ToString() == "Win", 30f);
        yield return new WaitForSeconds(7f);
        old = Macro_Main.Instance;
        Tds_Multiplayer_Remote.Instance.Returnmap_ServerRpc(true, default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        yield return new WaitForSecondsRealtime(1f);
        reviewLines.Clear();
        int pureBefore = player.Pure;
        yield return ToNextMap();
        yield return Until(() => SeenLines("posted:") > 0, 60f);
        player = RWRun.For(me);
        int paidAt = reviewLines.FindIndex(l => l.Contains("Mind the Vessels") && l.Contains("paid")), postedAt = reviewLines.FindIndex(l => l.Contains("posted:"));
        Finding("MODE-D2", paidAt >= 0 && (postedAt < 0 || paidAt < postedAt), "map 2's opening: " + string.Join(" | ", reviewLines.ToArray()) + "; Pure " + pureBefore + " -> " + player.Pure);

        // The Cardinals held on map 1, on map 2: the return PR #82 fixed.
        vars = Game_Mapvars.Instance;
        vessel = RWRun.Vessel(me);
        Check("A later map: the Cardinals held go back on its new core, and in the HUD's row", RWRun.Cardinals.Count >= 3 && RWCardinals.MissingOnCore() == 0, string.Join(",", RWRun.Cardinals.ToArray()) + ", missing " + RWCardinals.MissingOnCore() + ", row " + Tds_Multiplayerinfo.Instance.Artifacts.Count);
        Info("A later map: what the Cardinals put on the vessel", "[" + Holding(vessel, "Caffeine") + "]; a tower built now gets Rusty Pipe, as map 1's did after buying");

        // MODE-D5: map 2's plain foes.
        freeze = false;
        forcing = false;
        wave = vars.Wave.Value;
        vars.Time_Wave.Value = 0.1f;
        yield return Until(() => vars.Wave.Value > wave, 30f);
        t = 0f;
        string skins = "";
        int plainSkin = 0, healthSkin = 0, withHealth = 0, read = 0;
        List<Entity_Master> seen = new List<Entity_Master>();
        while (t < 20f && read < 8)
        {
            t += Time.unscaledDeltaTime;
            List<Entity_Master> all;
            if (Game_Entity.Instance.Entityclienttags.TryGetValue("Enemy", out all))
                foreach (Entity_Master e in new List<Entity_Master>(all))
                {
                    if (e == null || e.Beingserver == null || e.Beingserver.Dead || seen.Contains(e) || e.Taglistserver.Contains("Enemycore") || e.Taglistserver.Contains("Tower") || e.Upgradelistserver == null || e.Upgradelistserver.Count == 0) continue;
                    seen.Add(e);
                    read++;
                    string skin = e.Skin.Value.ToString();
                    if (skin == "") plainSkin++;
                    if (skin == "Health") healthSkin++;
                    if (Holding(e, "RW_Foe_Health") != "") withHealth++;
                    if (read <= 4) skins += e.Being.Value + "='" + skin + "' ";
                }
            yield return null;
        }
        yield return new WaitForSeconds(0.6f);
        string healths = "";
        foreach (Entity_Master e in seen)
        {
            if (e == null || e.Beingserver == null || e.Beingserver.Dead || e.Beingserver.Entity == null || e.Beingserver.Entity.Defaultstat == null) continue;
            float listed = 0f;
            foreach (Statproperties.Defaultstatgroup g in e.Beingserver.Entity.Defaultstat.Stats) if (g.Name == "Health") listed = g.Value;
            if (healths.Length < 120) healths += e.Being.Value + " " + HpMax(e) + "/" + listed + " ";
        }
        Finding("MODE-D5", healthSkin > 0, "map 2, Tide 1: " + read + " foes read, " + healthSkin + " wear 'Health', " + plainSkin + " none, " + withHealth + " hold the map's health upgrade: " + skins + "; max Health against the listed (fixed: x1.08): " + healths);
        ClearFoes();
        freeze = true;
        yield return ToBoat();
    }

    // MODE-D3: Mind the Vessels while a Boutbro falls and Boutman lives.
    static IEnumerator ReviewBoutman()
    {
        Macro_Main boat = Macro_Main.Instance;
        RWRun.Clear();
        boat.Selectcharacter("Hero_Boutman");
        yield return new WaitForSecondsRealtime(1f);
        yield return ToMap(Seed, RWChart.Calm);
        long me = RWMine.Session;
        RWPlayer player = RWRun.For(me);
        Entity_Master vessel = RWRun.Vessel(me);
        if (vessel == null || vessel.Being.Value.ToString() != "Hero_Boutman") { Setup("MODE-D3", "no Boutman"); yield break; }
        Rich(me, 3000f);
        List<Entity_Master> known = new List<Entity_Master>();
        Entity_Master house = null;
        yield return BuildOne(vessel, "Tower_Boutmanhouse", new List<Vector3>(), known, (m, c) => house = m);
        Entity_Master bro = null;
        yield return Until(() => (bro = MineLast("Mob_Boutmanbro", me)) != null, 60f);
        if (bro == null) { Setup("MODE-D3", "no Boutbro after 60 s"); yield break; }
        yield return new WaitForSeconds(1f);
        // MODE-C6: Cheap Refits reaches the Bouthouse.
        Direction(player, "Refit");
        yield return new WaitForSeconds(1.5f);
        Finding("MODE-C6 the Bouthouse", UpgradePrice(house) >= 0.999f, "its Upgrade price scale with Cheap Refits held: " + UpgradePrice(house));
        // FOES-2: a Dazzling Dagger beside the Bouthouse, Boutman walked away: does Illuminate reach him through the house?
        Beingobject walker = ModKitUtil.Load<Beingobject>("Mob", "Tower_Boutmanhouse");
        Vector3 away = walker != null && house != null ? RogueWavesModeKitTest.Spot(house.Entityposition.Value, walker, vessel.Entitydata.Value.Team, 6f, 12f, new List<Vector3>(), "away from the house") : new Vector3(float.NaN, 0f, 0f);
        if (!float.IsNaN(away.x))
        {
            ModKitUtil.MoveTo(vessel, null, away, 0f);
            yield return Until(() => vessel == null || ModKitUtil.Flat(vessel.Entityposition.Value, house.Entityposition.Value) >= 5.5f, 12f);
        }
        Upgradeproperties dazzling = Bl_Address.Getload("Upgrade", "RW_Modifier_Dazzling_Upgradeobject") as Upgradeproperties;
        Entity_Master dagger = house != null ? RogueWavesModeKitTest.SpawnFoe("Enemy_Dagger", house.Entityposition.Value, 1f, 2.5f, null) : null;
        if (dazzling == null || dagger == null) Setup("FOES-2", "no Dazzling (" + (dazzling != null) + ") or no Dagger (" + (dagger != null) + ")");
        else
        {
            yield return new WaitForSeconds(0.5f);
            Helper_Entitytools.Upgrade(dagger, dazzling, Helper_Entitytools.Upgradecheck.Forced, null);
            yield return new WaitForSeconds(2.5f);
            float gap = ModKitUtil.Flat(vessel.Entityposition.Value, house.Entityposition.Value);
            Finding("FOES-2", Holding(vessel, "Illuminate") != "", "Boutman " + RWUtil.F(gap) + " from the house holds [" + Holding(vessel, "Illuminate") + "], the house holds [" + Holding(house, "Illuminate") + "]");
            ClearFoes();
            yield return new WaitForSeconds(0.5f);
            Info("FOES-2 once the foe is gone", "Boutman holds [" + Holding(vessel, "Illuminate") + "]");
        }
        RWObjectiveDef hands = Array.Find(RWObjectives.All, o => o.Name == "Hands");
        RWObjectives.Posted = hands;
        RWObjectives.Active = null;
        string took = RWObjectives.Take(player);
        Game_Mapvars vars = Game_Mapvars.Instance;
        int wave = vars.Wave.Value, breaks = RWVote.Breaks;
        freeze = false;
        forcing = false;
        vars.Time_Wave.Value = 0.1f;
        yield return Until(() => vars.Wave.Value > wave, 40f);
        yield return new WaitForSeconds(0.5f);
        yield return NewFoe();
        bro = MineLast("Mob_Boutmanbro", me);
        if (lastFoe != null && bro != null) Hurt(lastFoe, bro, 100000f);
        yield return new WaitForSeconds(0.5f);
        forcing = true;
        float t = 0f;
        while (RWVote.Breaks == breaks && t < 90f)
        {
            ClearFoes();
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        forcing = false;
        ClearFoes();
        freeze = true;
        yield return new WaitForSeconds(0.8f);
        Entity_Master still = RWRun.Vessel(me);
        Finding("MODE-D3", RWObjectives.Outcome == "Hands failed", "took '" + took + "'; outcome '" + RWObjectives.Outcome + "' with Boutman alive " + (still != null) + ", the Boutbro felled");
        yield return ToBoat();
    }

    // R3 and MODE-C3: Spring Tide's Beacon against Fie's walls, on real hits.
    static IEnumerator ReviewFie(bool springFirst)
    {
        Macro_Main boat = Macro_Main.Instance;
        RWRun.Clear();
        boat.Selectcharacter("Hero_Fie");
        yield return new WaitForSecondsRealtime(1f);
        yield return ToMap(Seed, RWChart.Calm);
        long me = RWMine.Session;
        RWPlayer player = RWRun.For(me);
        Entity_Master vessel = RWRun.Vessel(me);
        if (vessel == null || vessel.Being.Value.ToString() != "Hero_Fie") { Setup("R3", "no Fie"); yield break; }
        Rich(me, 8000f);
        if (springFirst)
        {
            Direction(player, "Spring");
            yield return new WaitForSeconds(1.5f);
        }
        List<Vector3> used = new List<Vector3>();
        List<Entity_Master> known = new List<Entity_Master>();
        Entity_Master wall = null, beside = null, far = null;
        yield return BuildOne(vessel, "Tower_Fiewall", used, known, (m, c) => wall = m);
        if (wall == null) { Setup("R3", "no wall"); yield break; }
        Beingobject silo = ModKitUtil.Load<Beingobject>("Mob", "Tower_Fiesilo"), shop = ModKitUtil.Load<Beingobject>("Mob", "Tower_Fieshop");
        Vector3 at = silo != null ? RogueWavesModeKitTest.Spot(wall.Entityposition.Value, silo, wall.Entitydata.Value.Team, 1f, 1.6f, used, "beside the wall") : new Vector3(float.NaN, 0f, 0f);
        if (float.IsNaN(at.x)) { Setup("R3", "no tile beside the wall"); yield break; }
        vessel.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Build", "{\"Tower\":\"Tower_Fiesilo\",\"Position\":" + Pos(at) + ",\"Orientation\":0,\"Owner\":" + me + "}", default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => (beside = Mine("Tower_Fiesilo", me, known)) != null, 15f);
        if (beside != null) known.Add(beside);
        at = shop != null ? RogueWavesModeKitTest.Spot(vessel.Entityposition.Value, shop, wall.Entitydata.Value.Team, 5f, 9f, used, "away from the wall") : new Vector3(float.NaN, 0f, 0f);
        if (!float.IsNaN(at.x))
        {
            vessel.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Build", "{\"Tower\":\"Tower_Fieshop\",\"Position\":" + Pos(at) + ",\"Orientation\":0,\"Owner\":" + me + "}", default(Unity.Netcode.ServerRpcParams));
            yield return Until(() => (far = Mine("Tower_Fieshop", me, known)) != null, 15f);
        }
        if (beside == null) { Setup("R3", "no building beside the wall"); yield break; }
        yield return new WaitForSeconds(1f);
        foreach (string u in new[] { "Connect2", "Shield3", "Adjacent4" })
        {
            Upgradeproperties up = Bl_Address.Getload("Upgrade", "Tower_Fiewall_" + u + "_Upgradeobject") as Upgradeproperties;
            if (up != null) Helper_Entitytools.Upgrade(wall, up, Helper_Entitytools.Upgradecheck.Forced, null);
            yield return new WaitForSeconds(0.8f);
        }
        yield return new WaitForSeconds(1.5f);
        Info("R3 before Spring Tide", "wall holds [" + Holding(wall, "Effect") + "], Resistadd " + StatOf(wall, "Resistadd") + "; the building beside it [" + Holding(beside, "Effect") + "], Resistadd " + StatOf(beside, "Resistadd"));
        float[] before = new float[3], after = new float[3];
        yield return NewFoe();
        Entity_Master foe = lastFoe;
        if (foe == null) { Setup("R3", "no foe"); yield break; }
        Entity_Master[] targets = { beside, far, wall };
        for (int i = 0; i < 3; i++)
        {
            if (targets[i] == null) continue;
            float h0 = Hp(targets[i]);
            Hurt(foe, targets[i], 10f);
            yield return new WaitForSeconds(0.2f);
            before[i] = h0 - Hp(targets[i]);
        }
        if (!springFirst) Direction(player, "Spring");
        yield return new WaitForSeconds(2.5f);
        Info("R3 with Spring Tide", "wall [" + Holding(wall, "Effect") + "], Resistadd " + StatOf(wall, "Resistadd") + "; beside it [" + Holding(beside, "Effect") + "], Resistadd " + StatOf(beside, "Resistadd") + "; away [" + Holding(far, "Effect") + "], Resistadd " + StatOf(far, "Resistadd"));
        if (!ModKitUtil.Alive(foe)) { yield return NewFoe(); foe = lastFoe; }
        for (int i = 0; i < 3; i++)
        {
            if (targets[i] == null) continue;
            float h0 = Hp(targets[i]);
            Hurt(foe, targets[i], 10f);
            yield return new WaitForSeconds(0.2f);
            after[i] = h0 - Hp(targets[i]);
        }
        Finding(springFirst ? "R3, Spring Tide first" : "R3, the wall first", springFirst ? after[0] < 6.5f : before[0] > 0f && after[0] <= before[0] * 0.65f, "a 10 hit on the building beside the wall: " + before[0] + " before Spring Tide, " + after[0] + " with it; on one away from the wall " + before[1] + " then " + after[1]);
        Finding(springFirst ? "MODE-C3, Spring Tide first" : "MODE-C3, the wall first", springFirst ? after[2] < 6.5f : before[2] > 0f && after[2] <= before[2] * 0.65f, "a 10 hit on the Shield wall itself: " + before[2] + " before Spring Tide, " + after[2] + " with it, Resistadd " + StatOf(wall, "Resistadd"));
        // MODE-C6: Cut Costs on what a Fie wall builds.
        Direction(player, "Cost");
        yield return new WaitForSeconds(1.5f);
        string wallPrices = "";
        float lowest = 9f;
        foreach (KeyValuePair<string, Link_Command> kv in wall.Commandlistserver)
            if (kv.Key.StartsWith("Buildoption") && kv.Value != null) { wallPrices += kv.Key + "=" + kv.Value.Goalvalue + " "; lowest = Mathf.Min(lowest, kv.Value.Goalvalue); }
        Finding("MODE-C6 Fie's wall", wallPrices != "" && lowest >= 0.999f, "its build commands' price scales with Cut Costs held: " + (wallPrices != "" ? wallPrices : "none listed"));
        ClearFoes();
        yield return ToBoat();
    }

    // G3: Voice in a Bottle held, a pack building: it stays, its price falls, the next upgrade costs that.
    static IEnumerator ReviewVoice()
    {
        Macro_Main boat = Macro_Main.Instance;
        RWRun.Clear();
        boat.Selectcharacter("Hero_Yossele");
        yield return new WaitForSecondsRealtime(1f);
        yield return ToMap(Seed, RWChart.Calm);
        long me = RWMine.Session;
        RWPlayer player = RWRun.For(me);
        Entity_Master vessel = RWRun.Vessel(me);
        if (vessel == null || vessel.Being.Value.ToString() != "Hero_Yossele") { Setup("G3", "no Yossele"); yield break; }
        Rich(me, 8000f);
        player.Pure = 300;
        string bought = RWCardinals.Buy(player, "Artifact_Voice");
        yield return new WaitForSeconds(1f);
        Entity_Master cannon = null;
        yield return BuildOne(vessel, "Tower_Yosselecannon", new List<Vector3>(), new List<Entity_Master>(), (m, c) => cannon = m);
        if (cannon == null) { Setup("G3", "no Spore Cannon"); yield break; }
        yield return new WaitForSeconds(1f);
        Game_Mapvars vars = Game_Mapvars.Instance;
        float g0 = UpgradePrice(cannon);
        string held0 = Holding(cannon, "Voice");
        yield return ToBreakAfter(vars.Wave.Value + 1);
        yield return new WaitForSeconds(1f);
        yield return ToBreakAfter(vars.Wave.Value + 1);
        yield return new WaitForSeconds(1f);
        float g2 = UpgradePrice(cannon);
        Check("G3 Voice stays on a pack building, and its price falls with the Tides", cannon != null && Holding(cannon, "Voice") != "" && g2 < g0 - 0.001f, "bought '" + bought + "', holds [" + Holding(cannon, "Voice") + "] (at first [" + held0 + "]), price scale " + g0 + " -> " + g2);
        const string Sticky = "Tower_Yosselecannon_Sticky2_Upgradeobject";
        Upgradeproperties up = Bl_Address.Getload("Upgrade", Sticky) as Upgradeproperties;
        float price = 0f;
        if (up != null) foreach (Link_Resourceset c in up.Costs) price += c.Amount;
        float t0 = Total(me);
        cannon.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Upgrade", "{\"Upgrade\":\"" + Sticky + "\",\"Upgrader\":" + me + "}", default(Unity.Netcode.ServerRpcParams));
        yield return new WaitForSeconds(1f);
        float paid = t0 - Total(me);
        Check("G3 the next upgrade costs the fallen price", cannon.Upgradelistserver.ContainsKey(Sticky) && Mathf.Abs(paid - price * g2) <= 1f, "paid " + paid + " of " + price + " x " + g2);
        Info("G3 after the upgrade", "price scale " + UpgradePrice(cannon) + ", holds [" + Holding(cannon, "Voice") + "]");
        ClearFoes();
        // FOES-1: a vessel that dies in a Dazzling aura: does it come back Illuminated?
        Upgradeproperties dazzle = Bl_Address.Getload("Upgrade", "RW_Modifier_Dazzling_Upgradeobject") as Upgradeproperties;
        Entity_Master bright = dazzle != null ? RogueWavesModeKitTest.SpawnFoe("Enemy_Dagger", vessel.Entityposition.Value, 1f, 2.5f, null) : null;
        if (bright == null) Setup("FOES-1", "no Dazzling, or no Dagger");
        else
        {
            yield return new WaitForSeconds(0.5f);
            Helper_Entitytools.Upgrade(bright, dazzle, Helper_Entitytools.Upgradecheck.Forced, null);
            yield return new WaitForSeconds(2.5f);
            Info("FOES-1 before the fall", "the vessel holds [" + Holding(vessel, "Illuminate") + "]");
            Entity_Master was = vessel;
            Hurt(bright, vessel, 100000f);
            yield return new WaitForSeconds(0.3f);
            ClearFoes();
            freeze = false;
            yield return Until(() => { Entity_Master v = RWRun.Vessel(me); return v != null && v != was && v.Beingserver != null && !v.Beingserver.Dead; }, 60f);
            freeze = true;
            yield return new WaitForSeconds(1.5f);
            vessel = RWRun.Vessel(me);
            yield return NewFoe();
            float hp0 = Hp(vessel);
            if (lastFoe != null && vessel != null) Hurt(lastFoe, vessel, 10f);
            yield return new WaitForSeconds(0.3f);
            Finding("FOES-1", Holding(vessel, "Illuminate") != "", "the respawned vessel holds [" + Holding(vessel, "Illuminate") + "]; a hit of 10 takes " + (hp0 - Hp(vessel)));
            ClearFoes();
        }
        // FOES-3: a foe with Aging and Berserk, at half health, against a plain one.
        Upgradeproperties aging = Bl_Address.Getload("Upgrade", "RW_Modifier_Aging_Upgradeobject") as Upgradeproperties, berserk = Bl_Address.Getload("Upgrade", "RW_Modifier_Berserk_Upgradeobject") as Upgradeproperties;
        yield return NewFoe();
        Entity_Master plainFoe = lastFoe;
        yield return NewFoe();
        Entity_Master both = lastFoe;
        if (aging == null || berserk == null || plainFoe == null || both == null) Setup("FOES-3", "no modifier or foe");
        else
        {
            Helper_Entitytools.Upgrade(both, aging, Helper_Entitytools.Upgradecheck.Forced, null);
            Helper_Entitytools.Upgrade(both, berserk, Helper_Entitytools.Upgradecheck.Forced, null);
            yield return new WaitForSeconds(0.5f);
            Link_Stats hp;
            if (both.Statsserver.TryGetValue("Health", out hp)) hp.Value = hp.Value_Calc * 0.5f;
            yield return new WaitForSeconds(1.5f);
            float a = StatOf(both, "Damage"), p = StatOf(plainFoe, "Damage");
            Finding("FOES-3", a < p * 1.4f, "at half health: Damage " + a + " against a plain foe's " + p + " (fixed: at least 1.5x, with Aging's share on top)");
        }
        ClearFoes();
        yield return ToBoat();
    }

    // MODE-A3: a map with sub-cores: which being does the part take for the Basin?
    static IEnumerator ReviewArtrio()
    {
        Macro_Main boat = Macro_Main.Instance;
        RWRun.Clear();
        boat.Selectcharacter("Hero_Smacky");
        yield return new WaitForSecondsRealtime(1f);
        RWCampaign.New(Seed);
        RWCampaign.Staging = false;
        yield return Until(() => RWCampaign.Live && Here(), 40f);
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 60f);
        RWChart.Plan plan = RWMode.Pool.Find(p => p.Map == "Map_Artrio" || p.Name.Contains("Artrio"));
        if (plan == null) { Setup("MODE-A3", "no Artrio in the pool: " + RWMode.Pool.Count + " maps"); yield break; }
        RWChart.Node at = RWCampaign.Open()[0];
        at.Plan = plan.Name;
        at.Map = plan.Map;
        at.Label = plan.Label;
        at.Weather = RWChart.Calm;
        foreach (Macro_Maplink_Add l in Macro_Worldvisual.Instance.World.GetComponentsInChildren<Macro_Maplink_Add>(true))
            if (l.name == at.Name) l.Gameplan = RWMode.TwinPlan(plan.Name);
        Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(at.Name, false, default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => boat.Selected.Value.ToString() == at.Name && Macro_Worldvisual.Instance.Insetup && Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString() == RWMode.Twin(plan.Name), 60f);
        boat.Playgame(true);
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
        yield return Until(() => RWShop.Stand != null, 120f);
        freeze = true;
        yield return new WaitForSeconds(3f);
        Entity_Master first = RWUtil.PlayerCore();
        string all = "";
        foreach (Entity_Master c in RWUtil.PlayerCores()) all += c.Being.Value + " ";
        Finding("MODE-A3", first != null && first.Being.Value.ToString().ToLowerInvariant().Contains("sub"), plan.Name + ": PlayerCore() is " + (first != null ? first.Being.Value.ToString() : "none") + "; PlayerCores(): " + all);
        yield return ToBoat();
    }

    // MODE-A2's reading: this launch's RWIcons.Add threw on purpose (-rwthrowicons), and the mode still came up.
    public static bool ThrowShelf;

    // MODE-D10: SHOP's dialog throws once; the buttons must still work.
    static IEnumerator ReviewThrow()
    {
        Macro_Main boat = Macro_Main.Instance;
        RWRun.Clear();
        boat.Selectcharacter("Hero_Smacky");
        yield return new WaitForSecondsRealtime(1f);
        yield return ToMap(Seed, RWChart.Calm);
        ThrowShelf = true;
        RWShop.Open("Shop");
        yield return new WaitForSeconds(1.5f);
        bool threw = !ThrowShelf, upAfterThrow = RWDialog.Up();
        RWShop.Open("Shop");
        yield return WaitDialog(6f);
        bool opened = RWDialog.Up();
        Finding("MODE-D10", threw && !opened, "the dialog threw " + threw + ", nothing up after it " + !upAfterThrow + "; SHOP pressed again opens: " + opened);
        if (opened) ClickCard("Back");
        yield return ToBoat();
    }

    // MODE-D8: Keep the Basin Clean on a two-Basin map, a foe hitting only the second.
    static IEnumerator ReviewSplithead()
    {
        Macro_Main boat = Macro_Main.Instance;
        RWRun.Clear();
        boat.Selectcharacter("Hero_Smacky");
        yield return new WaitForSecondsRealtime(1f);
        RWCampaign.New(Seed);
        RWCampaign.Staging = false;
        yield return Until(() => RWCampaign.Live && Here(), 40f);
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 60f);      // the boat has docked: its own pick won't overwrite this one
        long me = RWMine.Session;
        RWChart.Plan plan = RWMode.Pool.Find(p => p.Map == "Map_Split" || p.Name.Contains("Split"));
        if (plan == null) { Setup("MODE-D8", "no Splithead in the pool"); yield break; }
        RWChart.Node at = RWCampaign.Open()[0];
        at.Plan = plan.Name;
        at.Map = plan.Map;
        at.Label = plan.Label;
        at.Weather = RWChart.Calm;
        foreach (Macro_Maplink_Add l in Macro_Worldvisual.Instance.World.GetComponentsInChildren<Macro_Maplink_Add>(true))
            if (l.name == at.Name) l.Gameplan = RWMode.TwinPlan(plan.Name);
        Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(at.Name, false, default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => boat.Selected.Value.ToString() == at.Name && Macro_Worldvisual.Instance.Insetup && Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString() == RWMode.Twin(plan.Name), 60f);
        boat.Playgame(true);
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
        yield return Until(() => RWShop.Stand != null, 120f);
        freeze = true;
        yield return new WaitForSeconds(3f);
        yield return Until(() => RWUtil.PlayerCores().Count >= 2, 30f);
        List<Entity_Master> cores = RWUtil.PlayerCores();
        string coreNames = "";
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Being.Value.ToString().ToLowerInvariant().Contains("core") && e.Entitydata.Value.Team != 0) coreNames += e.Being.Value + "[" + (e.Taglistserver != null ? string.Join(",", new List<string>(e.Taglistserver).ToArray()) : "") + "] ";
        Info("MODE-D8 the map", plan.Name + ", plan " + Tds_Multiplayerinfo.Instance.Gameplan.Value + ": " + cores.Count + " Basins by PlayerCores(); on the player's side: " + coreNames);
        if (cores.Count < 2) { Setup("MODE-D8", plan.Name + " has " + cores.Count + " Basin here"); yield return ToBoat(); yield break; }
        RWPlayer player = RWRun.For(me);
        RWObjectives.Posted = Array.Find(RWObjectives.All, o => o.Name == "Hull");
        RWObjectives.Active = null;
        string took = RWObjectives.Take(player);
        Game_Mapvars vars = Game_Mapvars.Instance;
        int wave = vars.Wave.Value, breaks = RWVote.Breaks;
        freeze = false;
        forcing = false;
        vars.Time_Wave.Value = 0.1f;
        yield return Until(() => vars.Wave.Value > wave, 40f);
        yield return new WaitForSeconds(0.5f);
        yield return NewFoe();
        if (lastFoe == null) { Setup("MODE-D8", "no foe to hit the second Basin"); yield return ToBoat(); yield break; }
        Hurt(lastFoe, cores[1], 1f);
        yield return new WaitForSeconds(0.3f);
        forcing = true;
        float t = 0f;
        while (RWVote.Breaks == breaks && t < 90f)
        {
            ClearFoes();
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        forcing = false;
        ClearFoes();
        freeze = true;
        yield return new WaitForSeconds(0.8f);
        Finding("MODE-D8", RWObjectives.Outcome == "Hull done", plan.Name + ", " + cores.Count + " Basins, took '" + took + "': a foe hit only the second; outcome '" + RWObjectives.Outcome + "' (fixed: Hull failed)");
        yield return ToBoat();
    }
}
#endif
