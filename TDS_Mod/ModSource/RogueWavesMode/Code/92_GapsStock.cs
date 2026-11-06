#if ROGUEWAVESMODE_GAPS
// The solo gaps build's part "stock" (-rwonly stock, its own launch): the stock Trinkets a buyer holds, measured in a map
// (docs/reports/STOCK_TRINKETS_2026-11-05.md §6 items 2-8; item 1 is part "rules", the old save part "oldsave").
//   map 1, Smacky   Paper Tower, Rusty Pipe, Frail Titan, Voice in a Bottle on two archers (one built before the buy),
//                   Sport Water and Beast Tassle on the vessel, Red Coin at the buy and two breaks, Frail Titan's burst,
//                   Aura Gavel on a miner, the vessel drowned and back
//   map 2, Yossele  Sport Water as a lobby pick, then Diorama
// The lobby's picks are the game's for everyone, and a pick can't be bought: map 1 takes the tested ones (and Fluffy
// Slippers, which moves Speed) off the lobby's list, map 2 adds Sport Water, and the list goes back as it was at the end.
// The game writes that list to the save as it changes (the PC scripts back the save up). Solo: what a teammate's things
// or a second player's wallet should not get is a SETUP line.
public static partial class RWTest
{
    static readonly string[] StockTested =
    {
        "Artifact_Caffeine", "Artifact_Beastpelt", "Artifact_Diorama", "Artifact_Paper", "Artifact_Rustypipe", "Artifact_Titan",
        "Artifact_Voice", "Artifact_Redcoin", "Artifact_Gavel", "Artifact_Slippers",
    };

    const string CaffeineUp = "Effect_Artifact_Caffeine_Upgradeproperties", VoiceUp = "Effect_Artifact_Voice_Upgradeproperties",
        TitanUp = "Effect_Artifact_Titan_Upgradeproperties", RustyUp = "Effect_Artifact_Rustypipe_Upgradeproperties",
        DioramaUp = "Effect_Artifact_Diorama_Upgradeproperties";

    sealed class StockRun
    {
        public long Me;
        public RWPlayer Player;
        public Entity_Master Vessel, Core, Before, After;
        public readonly List<Vector3> Used = new List<Vector3>();
        public readonly List<Entity_Master> Known = new List<Entity_Master>();
        public float Speed0 = -1f, PriceBefore = -1f, PriceAfter = -1f;
        public int VoiceWave = -1;
    }

    sealed class StockHit
    {
        public bool Self, Extra;
        public float Damage;
        public string From;
    }

    // Every Hurt a foe takes: Frail Titan's burst comes from the target itself, Aura Gavel's bolt carries RWAny.Extra.
    static readonly List<StockHit> stockHits = new List<StockHit>();
    static readonly Entity_Master.Oninteracts recordStock = hit =>
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.Data == null) return;
            float d;
            hit.Data.TryGetValue("Damage", out d);
            stockHits.Add(new StockHit { Self = hit.From != null && ReferenceEquals(hit.From, hit.To), Extra = RWAny.ExtraHit(hit.Data), Damage = d, From = hit.From != null ? ModKitUtil.BeingName(hit.From) : "none" });
        }
        catch (Exception) { }
    };

    // ---------------------------------------------------------------- helpers

    static List<string> LobbyIds()
    {
        List<string> ids = new List<string>();
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        if (info != null)
            foreach (FixedString64Bytes a in info.Artifacts) ids.Add(a.ToString());
        return ids;
    }

    static string LobbyMixed() { return Tds_Multiplayerinfo.Instance != null ? Tds_Multiplayerinfo.Instance.Mixedart.Value.ToString() : ""; }

    // The lobby's list as given; the campaign's record of it goes first, or the boat's LockLobby puts the record back.
    static void SetLobby(List<string> ids, string mixed)
    {
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        if (info == null || !info.IsServer) return;
        RWUtil.Remove(RWData.SaveFolder, RWKeys.Lobby);
        RWUtil.Flush();
        foreach (string id in LobbyIds())
            if (!ids.Contains(id)) info.Artifacts.Remove(new FixedString64Bytes(id));
        foreach (string id in ids)
            if (!info.Artifacts.Contains(new FixedString64Bytes(id))) info.Artifacts.Add(new FixedString64Bytes(id));
        if (info.Mixedart.Value.ToString() != mixed) info.Mixedart.Value = new FixedString64Bytes(mixed);
    }

    static List<string> Without(List<string> ids, string[] drop)
    {
        List<string> kept = new List<string>();
        foreach (string id in ids) if (Array.IndexOf(drop, id) < 0) kept.Add(id);
        return kept;
    }

    static string Ids(List<string> ids) { return ids.Count > 0 ? string.Join(",", ids.ToArray()) : "none"; }

    // One of a stock effect upgrade's own numbers (its Defaultparams), 0 when it has none or isn't loaded.
    static float EffectParam(string upgrade, string key)
    {
        Upgradeproperties u = ModKitUtil.Load<Upgradeproperties>("Upgrade", upgrade);
        float v;
        return u != null && u.Defaultparams != null && u.Defaultparams.TryGetValue(key, out v) ? v : 0f;
    }

    // A held stock Trinket's panel line on its buyer's vessel (RWCardinals.Build names it by its place in the fixed list).
    static string StockMark(string id) { return "RW_StockTrinket" + Array.IndexOf(RWCardinals.StockTrinketIds, id) + "_Upgradeobject"; }

    // In the synced list the vessel panel reads.
    static bool Synced(Entity_Master m, string upgrade) { return m != null && m.Upgradelist != null && m.Upgradelist.Contains(new FixedString64Bytes(upgrade)); }

    static string LineName(Entity_Master m, string upgrade)
    {
        Upgradeproperties u;
        return m != null && m.Upgradelistserver != null && m.Upgradelistserver.TryGetValue(upgrade, out u) && u != null ? u.Display_Name : "";
    }

    static string EntryName(string id)
    {
        Artifactentryobject e = RWUtil.Item<Artifactentryobject>("Artifact", id);
        return e != null ? e.Display_Name : "?";
    }

    static float BaseOf(Entity_Master e, string stat) { Link_Stats s; return e != null && e.Statsserver != null && e.Statsserver.TryGetValue(stat, out s) ? s.Value_Base : -1f; }

    static int StockFactor()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        int total = vars != null && vars.Mode != null && vars.Mode.Enemies != null ? vars.Mode.Enemies.Totalwaves : 0;
        return total > 0 ? 30 / total : 0;
    }

    static bool SkinHas(Visualproperties vp, string skin)
    {
        if (vp == null) return false;
        if (vp.Skinoverrides != null && vp.Skinoverrides.ContainsKey(skin)) return true;
        if (vp.Skinoverride != null)
            foreach (Visualproperties.Param p in vp.Skinoverride) if (p.Name == skin) return true;
        return false;
    }

    // ---------------------------------------------------------------- the part

    static IEnumerator GapsStock()
    {
        yield return BackOnBoat();
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        bool lobby = info != null && info.IsServer;
        List<string> lobby0 = LobbyIds();
        string mixed0 = LobbyMixed();
        Info("stock: the lobby's picks at the start", Ids(lobby0) + ", mixed '" + mixed0 + "'");
        if (!lobby) Setup("stock: the lobby's picks", "no lobby list on this host, so they stay as they are: a tested one picked there can't be bought, and the lobby pick of Sport Water is skipped");
        yield return Stepped(StockSmacky(lobby, lobby0, mixed0));
        yield return BackOnBoat();
        yield return Stepped(StockYossele(lobby, lobby0, mixed0));
        yield return BackOnBoat();
        if (!lobby) yield break;
        try { SetLobby(lobby0, mixed0); }
        catch (Exception e) { Info("stock: the lobby's picks put back", "threw: " + e.Message); }
        yield return new WaitForSecondsRealtime(1.5f);
        List<string> now = LobbyIds();
        bool same = now.Count == lobby0.Count && Without(now, lobby0.ToArray()).Count == 0 && LobbyMixed() == mixed0;
        Check("The test put the lobby's picks back as they were", same, Ids(now) + ", mixed '" + LobbyMixed() + "' (at the start " + Ids(lobby0) + ", mixed '" + mixed0 + "')");
    }

    static IEnumerator StockSmacky(bool lobby, List<string> lobby0, string mixed0)
    {
        RWRun.Clear();
        if (lobby) SetLobby(Without(lobby0, StockTested), Array.IndexOf(StockTested, mixed0) >= 0 ? "" : mixed0);
        Macro_Main.Instance.Selectcharacter("Hero_Smacky");
        yield return new WaitForSecondsRealtime(1f);
        yield return ToMap(Seed, RWChart.Calm);
        StockRun r = new StockRun();
        r.Me = RWMine.Session;
        r.Player = RWRun.For(r.Me);
        r.Vessel = RWRun.Vessel(r.Me);
        r.Core = RWUtil.PlayerCore();
        if (r.Player == null || !ModKitUtil.Alive(r.Vessel) || r.Core == null) { Setup("stock on Smacky", "no player, vessel or Basin"); yield return ToBoat(); yield break; }
        Info("stock: the lobby's picks on Smacky's map", Ids(LobbyIds()) + ", mixed '" + LobbyMixed() + "'; Tides " + (Game_Mapvars.Instance.Mode != null && Game_Mapvars.Instance.Mode.Enemies != null ? Game_Mapvars.Instance.Mode.Enemies.Totalwaves : -1) + ", factor " + StockFactor());
        Rich(r.Me, 3000f);
        yield return Stepped(StockTowers(r));
        yield return Stepped(StockVessel(r));
        yield return Stepped(StockTides(r));
        yield return Stepped(StockTitan(r));
        yield return Stepped(StockGavel(r));
        yield return Stepped(StockRespawn(r));
        ClearFoes();
        yield return ToBoat();
    }

    // Items 3 (Paper Tower), 4 (Rusty Pipe, Frail Titan on standing and new towers) and the start of 5 (Voice in a Bottle).
    static IEnumerator StockTowers(StockRun r)
    {
        string paper = BuyAt(r.Player, "Artifact_Paper");
        yield return Until(() => Holding(r.Vessel, "Effect_Artifact_Paper") != "", 4f);
        yield return new WaitForSeconds(0.5f);
        string onVessel = Holding(r.Vessel, "Effect_Artifact_Paper");
        Entity_Master before = null, after = null;
        yield return BuildOne(r.Vessel, "Tower_Smackyarcher", r.Used, r.Known, (m, c) => before = m);
        string pipe = BuyAt(r.Player, "Artifact_Rustypipe"), titan = BuyAt(r.Player, "Artifact_Titan"), voice = BuyAt(r.Player, "Artifact_Voice");
        r.VoiceWave = Game_Mapvars.Instance.Wave.Value;
        yield return new WaitForSeconds(1f);
        yield return BuildOne(r.Vessel, "Tower_Smackyarcher", r.Used, r.Known, (m, c) => after = m);
        yield return new WaitForSeconds(1f);
        r.Before = before;
        r.After = after;
        if (before == null || after == null) { Setup("stock towers", "an archer wasn't built (before the buy " + (before != null) + ", after " + (after != null) + ")"); yield break; }

        Check("Paper Tower: the first archer the buyer builds turns paper, and the next one doesn't", paper == "" && onVessel != "" && Holding(before, "Paper_Tower") != "" && Holding(after, "Paper_Tower") == "",
            "bought '" + paper + "', vessel [" + onVessel + "], first archer [" + Holding(before, "Paper") + "], second [" + Holding(after, "Paper") + "]");
        string pb = Holding(before, "Effect_Artifact_Rustypipe"), pa = Holding(after, "Effect_Artifact_Rustypipe"), tb = Holding(before, "Effect_Artifact_Titan"), ta = Holding(after, "Effect_Artifact_Titan");
        Check("Rusty Pipe and Frail Titan: the buyer's towers built before and after the buy hold both, and the vessel holds neither",
            pipe == "" && titan == "" && pb != "" && pa != "" && tb != "" && ta != "" && Holding(r.Vessel, "Rustypipe") == "" && Holding(r.Vessel, "Effect_Artifact_Titan") == "",
            "bought '" + pipe + "', '" + titan + "'; archer before [" + pb + "; " + tb + "], after [" + pa + "; " + ta + "], vessel [" + Holding(r.Vessel, "Rustypipe") + Holding(r.Vessel, "Effect_Artifact_Titan") + "]; Rusty Pipe ripens after Wave_Base " + EffectParam(RustyUp, "Wave_Base"));
        Setup("Rusty Pipe and Frail Titan on a teammate's towers", "solo launch: no teammate to build one");
        r.PriceBefore = UpgradePrice(before);
        r.PriceAfter = UpgradePrice(after);
        Info("Voice in a Bottle at the buy", "bought '" + voice + "' at Tide " + r.VoiceWave + "; archer before [" + Holding(before, "Voice") + "] price scale " + r.PriceBefore + ", after [" + Holding(after, "Voice") + "] " + r.PriceAfter);
    }

    // Item 2 on the vessel (its panel lines, untargetable) and item 6 at the buy.
    static IEnumerator StockVessel(StockRun r)
    {
        Entity_Master v = r.Vessel;
        r.Speed0 = BaseOf(v, "Speed");
        bool targetable0 = v.Option_Targetable;
        string sport = BuyAt(r.Player, "Artifact_Caffeine"), pelt = BuyAt(r.Player, "Artifact_Beastpelt");
        string ms = StockMark("Artifact_Caffeine"), mp = StockMark("Artifact_Beastpelt");
        float t0 = Time.unscaledTime;
        yield return Until(() => Synced(v, ms) && Synced(v, mp) && Holding(v, "Effect_Artifact_Caffeine") != "" && !v.Option_Targetable, 5f);
        float took = Time.unscaledTime - t0;
        yield return new WaitForSeconds(0.3f);
        string ns = LineName(v, ms), np = LineName(v, mp);
        Check("Sport Water and Beast Tassle: the vessel's panel shows both lines, by the game's own names",
            sport == "" && pelt == "" && Synced(v, ms) && Synced(v, mp) && ns == EntryName("Artifact_Caffeine") && np == EntryName("Artifact_Beastpelt"),
            "bought '" + sport + "', '" + pelt + "'; lines " + ms + " '" + Bla_Localization.GetText(ns) + "', " + mp + " '" + Bla_Localization.GetText(np) + "', in " + RWUtil.F(took) + " s");
        Check("Beast Tassle: the buyer's vessel can't be targeted", pelt == "" && Holding(v, "Effect_Artifact_Beastpelt") != "" && !v.Option_Targetable,
            "targetable " + targetable0 + " -> " + v.Option_Targetable + ", holds [" + Holding(v, "Beastpelt") + "]");
        Info("Sport Water at the buy", "Speed base " + r.Speed0 + " -> " + BaseOf(v, "Speed") + " at Tide " + Game_Mapvars.Instance.Wave.Value + "; Speed_Base " + EffectParam(CaffeineUp, "Speed_Base") + ", holds [" + Holding(v, "Caffeine") + "]");
        Setup("Sport Water and Beast Tassle on a teammate's vessel", "solo launch: no teammate's vessel to read");

        int players = Mathf.Max(1, RWRun.Sessions().Count);
        float paid0 = RWTrinkets.CoinPaid, red0 = Purse2(r.Me, "Redaura"), gain = ModKitUtil.Gain();
        string coin = BuyAt(r.Player, "Artifact_Redcoin");
        yield return Until(() => RWTrinkets.CoinPaid > paid0 + 0.001f, 4f);
        yield return new WaitForSeconds(0.3f);
        float want = RWCardinals.StockParam("Artifact_Redcoin", "Red_Base") / players, got = RWTrinkets.CoinPaid - paid0, rose = Purse2(r.Me, "Redaura") - red0;
        Check("Red Coin: the buyer's Red rises by its start sum at the buy", coin == "" && want > 0f && Mathf.Abs(got - want) < 0.01f && Mathf.Abs(rose - want * gain) < 0.5f,
            "bought '" + coin + "'; CoinPaid +" + RWUtil.F(got) + " (want Red_Base " + RWCardinals.StockParam("Artifact_Redcoin", "Red_Base") + " / " + players + " players = " + RWUtil.F(want) + "), wallet Red " + RWUtil.F(red0) + " -> " + RWUtil.F(red0 + rose) + " (gain x" + RWUtil.F(gain) + ")");
        Setup("Red Coin on another player's wallet", "solo launch: no second player whose Red should stay");
    }

    // Item 2 (Speed grows with the Tides), 5 (the price falls over two Tides) and 6 (each break's pay).
    static IEnumerator StockTides(StockRun r)
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        int factor = StockFactor(), players = Mathf.Max(1, RWRun.Sessions().Count);
        float speedRate = EffectParam(CaffeineUp, "Speed_Base"), step = EffectParam(VoiceUp, "Rate_Base") / 100f * factor, add = RWCardinals.StockParam("Artifact_Redcoin", "Red_Add"), gain = ModKitUtil.Gain();
        bool speedOk = speedRate > 0f, priceOk = step > 0f && r.PriceBefore > 0f && r.PriceAfter > 0f, coinOk = add > 0f, walletOk = true;
        string speeds = "", prices = "", coins = "";
        float lastSpeed = BaseOf(r.Vessel, "Speed");
        for (int i = 0; i < 2; i++)
        {
            float paid0 = RWTrinkets.CoinPaid, red0 = Purse2(r.Me, "Redaura");
            int breaks0 = RWVote.Breaks;
            yield return ToBreakAfter(vars.Wave.Value + 1);
            yield return new WaitForSeconds(1f);
            int w = vars.Wave.Value;
            if (RWVote.Breaks == breaks0) { Setup("stock over the Tides", "Tide " + w + "'s break never came"); yield break; }
            Entity_Master v = RWRun.Vessel(r.Me);
            float sp = BaseOf(v, "Speed"), wantSp = r.Speed0 + speedRate * w * factor;
            speedOk &= Mathf.Abs(sp - wantSp) < 0.005f && sp > lastSpeed;
            speeds += "Tide " + w + ": " + RWUtil.F(sp) + " (want " + RWUtil.F(wantSp) + ", level " + (v != null ? v.Entitydata.Value.Level : -1) + "); ";
            lastSpeed = sp;
            int changes = w - r.VoiceWave;
            float gb = UpgradePrice(r.Before), ga = UpgradePrice(r.After), wantB = Mathf.Max(0f, r.PriceBefore - step * changes), wantA = Mathf.Max(0f, r.PriceAfter - step * changes);
            priceOk &= Mathf.Abs(gb - wantB) < 0.005f && Mathf.Abs(ga - wantA) < 0.005f;
            prices += "Tide " + w + ": " + RWUtil.F(gb) + ", " + RWUtil.F(ga) + " (want " + RWUtil.F(wantB) + ", " + RWUtil.F(wantA) + "); ";
            float coinWant = add * w * factor / players, coinGot = RWTrinkets.CoinPaid - paid0, redRose = Purse2(r.Me, "Redaura") - red0;
            coinOk &= Mathf.Abs(coinGot - coinWant) < 0.01f;
            walletOk &= redRose >= coinWant * gain - 0.5f;
            coins += "Tide " + w + ": CoinPaid +" + RWUtil.F(coinGot) + " (want " + RWUtil.F(coinWant) + "), wallet Red +" + RWUtil.F(redRose) + "; ";
        }
        Check("Sport Water: the vessel's Speed grows by its rate at each Tide", speedOk, "Speed base " + RWUtil.F(r.Speed0) + " before the buy, Speed_Base " + speedRate + " x Tide x factor " + factor + "; " + speeds);
        Check("Voice in a Bottle: the buyer's towers' upgrade price falls by its rate at each of two Tides", priceOk,
            "step Rate_Base " + EffectParam(VoiceUp, "Rate_Base") + "/100 x factor " + factor + " = " + RWUtil.F(step) + " from " + RWUtil.F(r.PriceBefore) + " (built before) and " + RWUtil.F(r.PriceAfter) + " (after) at Tide " + r.VoiceWave + "; " + prices
            + "Rusty Pipe now [" + Holding(r.Before, "Rustypipe") + "]");
        Setup("Voice in a Bottle on a teammate's tower", "solo launch: no teammate to build one");
        Check("Red Coin: each break pays the buyer Add x Tide x factor in Red", coinOk && walletOk, "Red_Add " + add + ", factor " + factor + ", " + players + " players, gain x" + RWUtil.F(gain) + " (the wallet takes the Tide's own pay too); " + coins);
    }

    // Item 4: a Frail Titan tower bursts when it drowns.
    static IEnumerator StockTitan(StockRun r)
    {
        Entity_Master tower = r.After;
        if (!ModKitUtil.Alive(tower) || Holding(tower, "Effect_Artifact_Titan") == "") { Setup("Frail Titan's burst", "no standing archer holding Frail Titan"); yield break; }
        float range = EffectParam(TitanUp, "Range_Base");
        // Armoured (+10000 Health, filled, R4 §8) and the foe held still, so neither fells the other before the drowning:
        // Frail Titan's Health_Pmulti can leave the archer at 0 health (pitfall #171). The burst reads its level, not its health.
        float towerHp = Hp(tower), towerMax = HpMax(tower);
        DcArmour(tower);
        ClearFoes();
        Entity_Master foe = FoeNear(ModKitUtil.Pos(tower), 0.5f, Mathf.Clamp(range * 0.7f, 0.8f, 1.8f), null);
        if (foe == null) { Setup("Frail Titan's burst", "no foe could stand beside the archer"); yield break; }
        yield return Until(() => !ModKitUtil.Alive(foe) || Hp(foe) >= 0f, 1f);
        DcArmour(foe);
        DcHold(foe);
        yield return new WaitForSeconds(0.4f);
        if (!ModKitUtil.Alive(foe) || !ModKitUtil.Alive(tower)) { Setup("Frail Titan's burst", "armoured, still fell before the test drowned the archer: foe up " + ModKitUtil.Alive(foe) + ", archer up " + ModKitUtil.Alive(tower)); ClearFoes(); yield break; }
        stockHits.Clear();
        ModKitUtil.Front(foe, recordStock);
        int level = Mathf.Clamp(tower.Entitydata.Value.Level, 1, 5);
        float levelStat = Calc(tower, "Level"), dist = ModKitUtil.Flat(ModKitUtil.Pos(foe), ModKitUtil.Pos(tower)), hp0 = Hp(foe);
        Hurt(foe, tower, Mathf.Max(0f, Hp(tower)) + 1000f);
        yield return Until(() => !ModKitUtil.Alive(tower), 1f);
        string how = "hurt by the foe";
        if (ModKitUtil.Alive(tower)) { Helper_Entitytools.Despawn(tower.Beingserver, true, "RWTest"); how = "despawned as killed (a hurt left it standing)"; }
        float t = 0f;
        while (t < 1.5f && !stockHits.Exists(h => h.Self))
        {
            if (ModKitUtil.Alive(foe)) ModKitUtil.Front(foe, recordStock);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        StockHit burst = stockHits.Find(h => h.Self);
        float want = EffectParam(TitanUp, level == 1 ? "Damage_Base" : "Damage" + level + "_Base");
        Check("Frail Titan: a tower holding it bursts when it drowns, and hurts a foe beside it", burst != null && burst.Damage > 0f,
            "archer " + how + " (health " + RWUtil.F(towerHp) + " of " + RWUtil.F(towerMax) + " before the test's armour), foe " + RWUtil.F(dist) + " tiles off (Range_Base " + range + "); burst " + (burst != null ? RWUtil.F(burst.Damage) : "not seen") + ", want " + want + " for level " + level + " (Level stat " + levelStat + "); foe health " + RWUtil.F(hp0) + " -> " + RWUtil.F(Hp(foe)) + ", " + stockHits.Count + " hits seen");
        if (ModKitUtil.Alive(foe)) ModKitUtil.Unfront(foe, recordStock);
        ClearFoes();
    }

    // Item 7: one of the buyer's units drowns beside a foe, and the Gavel's bolt flies.
    static IEnumerator StockGavel(StockRun r)
    {
        Entity_Master hut = null, miner = null;
        yield return BuildOne(r.Vessel, "Tower_Smackyhut", r.Used, r.Known, (m, c) => hut = m);
        if (hut == null) { Setup("Aura Gavel", "no Mining Hut built"); yield break; }
        yield return Until(() => (miner = MineLast("Mob_Smackyminer", r.Me)) != null, 20f);
        if (miner == null) { Setup("Aura Gavel", "no miner came out of the hut in 20 s"); yield break; }
        string gavel = BuyAt(r.Player, "Artifact_Gavel");
        yield return new WaitForSeconds(0.6f);
        miner = MineLast("Mob_Smackyminer", r.Me);
        if (miner == null) { Setup("Aura Gavel", "the miner was gone after the buy"); yield break; }
        // Both armoured (+10000 Health, filled, R4 §8) and held still, so neither fells the other before the drowning; the bolt
        // reads the unit's level, not its health.
        DcArmour(miner);
        DcHold(miner);
        ClearFoes();
        Entity_Master foe = FoeNear(ModKitUtil.Pos(miner), 1f, 3f, null);
        if (foe == null) { Setup("Aura Gavel", "no foe could stand by the miner"); yield break; }
        yield return Until(() => !ModKitUtil.Alive(foe) || Hp(foe) >= 0f, 1f);
        DcArmour(foe);
        DcHold(foe);
        yield return new WaitForSeconds(0.4f);
        if (!ModKitUtil.Alive(miner) || !ModKitUtil.Alive(foe)) { Setup("Aura Gavel", "armoured, still fell before the test drowned the miner: miner up " + ModKitUtil.Alive(miner) + ", foe up " + ModKitUtil.Alive(foe)); ClearFoes(); yield break; }
        stockHits.Clear();
        ModKitUtil.Front(foe, recordStock);
        int gavels0 = RWTrinkets.Gavels;
        Link_Stats lv;
        int level = Mathf.Clamp(Mathf.RoundToInt(miner.Statsserver != null && miner.Statsserver.TryGetValue("Level", out lv) ? lv.Value_Calc : 1f), 1, 5);
        float want = RWCardinals.StockParam("Artifact_Gavel", level == 1 ? "Damage_Base" : "Damage" + level + "_Base"), reach = RWCardinals.StockParam("Artifact_Gavel", "Range_Base");
        float dist = ModKitUtil.Flat(ModKitUtil.Pos(foe), ModKitUtil.Pos(miner)), hp0 = Hp(foe);
        Hurt(foe, miner, Mathf.Max(0f, Hp(miner)) + 1000f);
        yield return Until(() => !ModKitUtil.Alive(miner), 1f);
        string how = "hurt by the foe";
        if (ModKitUtil.Alive(miner)) { Helper_Entitytools.Despawn(miner.Beingserver, true, "RWTest"); how = "despawned as killed (a hurt left it standing)"; }
        int fired = RWTrinkets.Gavels - gavels0;     // at the drown: a later death (the foe on another miner) is another bolt
        float t = 0f;
        while (t < 4f && !stockHits.Exists(h => h.Extra))
        {
            if (ModKitUtil.Alive(foe)) ModKitUtil.Front(foe, recordStock);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        StockHit bolt = stockHits.Find(h => h.Extra);
        Check("Aura Gavel: one of the buyer's units drowning beside a foe fires one bolt", gavel == "" && fired == 1,
            "bought '" + gavel + "'; miner " + how + ", Gavels " + gavels0 + " -> " + (gavels0 + fired) + " at the drown (" + RWTrinkets.Gavels + " after the bolt's flight); foe " + RWUtil.F(dist) + " tiles off (Range_Base " + reach + "); bolt landed "
            + (bolt != null ? RWUtil.F(bolt.Damage) + " from " + bolt.From : "not seen" + (ModKitUtil.Alive(foe) ? "" : " (the foe fell)")) + ", want " + want + " for the unit's level " + level + "; foe health " + RWUtil.F(hp0) + " -> " + RWUtil.F(Hp(foe)));
        Setup("Aura Gavel on a teammate's unit", "solo launch: no teammate's unit to drown");
        if (ModKitUtil.Alive(foe)) ModKitUtil.Unfront(foe, recordStock);
        ClearFoes();
    }

    // Item 2: the vessel drowns and comes back with both.
    static IEnumerator StockRespawn(StockRun r)
    {
        ClearFoes();
        Entity_Master old = RWRun.Vessel(r.Me), back = null;
        if (!ModKitUtil.Alive(old)) { Setup("Sport Water and Beast Tassle after a drown", "no vessel standing"); yield break; }
        Helper_Entitytools.Despawn(old.Beingserver, true, "RWTest");
        yield return Until(() => (back = RWRun.Vessel(r.Me)) != null && !ReferenceEquals(back, old) && ModKitUtil.Alive(back), 60f);
        if (back == null || ReferenceEquals(back, old) || !ModKitUtil.Alive(back)) { Setup("Sport Water and Beast Tassle after a drown", "the vessel didn't come back in 60 s"); yield break; }
        string ms = StockMark("Artifact_Caffeine"), mp = StockMark("Artifact_Beastpelt");
        Entity_Master v = back;
        float t0 = Time.unscaledTime;
        yield return Until(() => Synced(v, ms) && Synced(v, mp) && Holding(v, "Effect_Artifact_Caffeine") != "" && Holding(v, "Effect_Artifact_Beastpelt") != "" && !v.Option_Targetable, 6f);
        float took = Time.unscaledTime - t0;
        yield return new WaitForSeconds(0.5f);
        r.Vessel = v;
        int w = Game_Mapvars.Instance.Wave.Value, factor = StockFactor();
        float rate = EffectParam(CaffeineUp, "Speed_Base"), sp = BaseOf(v, "Speed"), atStart = r.Speed0 + rate * w, atChange = r.Speed0 + rate * w * factor;
        Check("Sport Water and Beast Tassle stay on a vessel that drowns and comes back",
            Synced(v, ms) && Synced(v, mp) && Holding(v, "Effect_Artifact_Caffeine") != "" && Holding(v, "Effect_Artifact_Beastpelt") != "" && !v.Option_Targetable && (Mathf.Abs(sp - atStart) < 0.005f || Mathf.Abs(sp - atChange) < 0.005f),
            "lines " + Synced(v, ms) + "/" + Synced(v, mp) + ", effects [" + Holding(v, "Caffeine") + "; " + Holding(v, "Beastpelt") + "] in " + RWUtil.F(took) + " s, targetable " + v.Option_Targetable
            + "; Speed base " + RWUtil.F(sp) + " at Tide " + w + " (want " + RWUtil.F(atStart) + (factor != 1 ? " or " + RWUtil.F(atChange) : "") + ")");
    }

    // ---------------------------------------------------------------- map 2: Yossele

    static IEnumerator StockYossele(bool lobby, List<string> lobby0, string mixed0)
    {
        RWRun.Clear();
        bool pick = false;
        if (lobby)
        {
            List<string> ids = Without(lobby0, StockTested);
            ids.Add("Artifact_Caffeine");
            SetLobby(ids, Array.IndexOf(StockTested, mixed0) >= 0 ? "" : mixed0);
            pick = LobbyIds().Contains("Artifact_Caffeine");
            if (!pick) Setup("A lobby pick of Sport Water", "the lobby's list didn't take it (" + Ids(LobbyIds()) + ")");
        }
        Macro_Main.Instance.Selectcharacter("Hero_Yossele");
        yield return new WaitForSecondsRealtime(1f);
        yield return ToMap(Seed, RWChart.Calm);
        StockRun r = new StockRun();
        r.Me = RWMine.Session;
        r.Player = RWRun.For(r.Me);
        r.Vessel = RWRun.Vessel(r.Me);
        r.Core = RWUtil.PlayerCore();
        if (r.Player == null || !ModKitUtil.Alive(r.Vessel) || r.Core == null || r.Vessel.Being.Value.ToString() != "Hero_Yossele") { Setup("stock on Yossele", "no player, Basin or Yossele"); yield return ToBoat(); yield break; }
        Info("stock: the lobby's picks on Yossele's map", Ids(LobbyIds()) + ", mixed '" + LobbyMixed() + "'");
        yield return Stepped(StockLobbyPick(r, pick));
        yield return Stepped(StockDiorama(r));
        ClearFoes();
        yield return ToBoat();
    }

    // Item 8: Sport Water picked in the lobby works as the game gives it, and the shop never offers it.
    static IEnumerator StockLobbyPick(StockRun r, bool pick)
    {
        if (!pick) yield break;
        const string id = "Artifact_Caffeine";
        Entity_Master v = r.Vessel;
        yield return Until(() => Holding(v, "Effect_Artifact_Caffeine") != "", 6f);
        string effect = Holding(v, "Effect_Artifact_Caffeine"), onCore = Holding(r.Core, id);
        bool line = Synced(v, StockMark(id)), held = r.Player.Trinkets.Exists(h => h.StartsWith(id, StringComparison.Ordinal));
        Check("A lobby pick of Sport Water: the vessel holds its effect as the game gives it, with no Trinket line", effect != "" && onCore != "" && !line && !held,
            "vessel [" + effect + "], Basin [" + onCore + "], Trinket line " + line + ", held as a Trinket " + held);
        Setup("A lobby pick of Sport Water on a teammate's vessel", "solo launch: no teammate's vessel to read");

        r.Player.Pure = 200;
        string why = RWCardinals.Buy(r.Player, id);
        bool paidNothing = r.Player.Pure == 200;
        held = r.Player.Trinkets.Exists(h => h.StartsWith(id, StringComparison.Ordinal));
        Func<string, bool> here = RWCardinals.MaxedHere();
        bool maxedHost = RWCardinals.MaxedFor(r.Player, id), maxedHere = here(id);
        int onHost = 0, onMachine = 0, unfiltered = 0;
        for (uint seed = 1; seed <= 300; seed++)
        {
            int map = 1 + (int)(seed % 10), wave = (int)(seed % 16);
            if (RWCardinals.Shelf(seed, map, wave, c => RWCardinals.MaxedFor(r.Player, c), 6).Contains(id)) onHost++;
            if (RWCardinals.Shelf(seed, map, wave, here, 6).Contains(id)) onMachine++;
            if (RWCardinals.Shelf(seed, map, wave, c => false, 6).Contains(id)) unfiltered++;
        }
        Check("A lobby pick of Sport Water: the shop refuses it and never offers it, on the host or on this machine",
            why == "Already held" && paidNothing && !held && maxedHost && maxedHere && onHost == 0 && onMachine == 0 && unfiltered > 0,
            "'" + why + "', Pure " + r.Player.Pure + " of 200, held " + held + ", maxed for the host " + maxedHost + " and here " + maxedHere + "; on " + onHost + " and " + onMachine + " of 300 shelves (" + unfiltered + " with nothing maxed)");
    }

    // Item 3: Diorama on a pack vessel: its stats at once, and the tiny look.
    static IEnumerator StockDiorama(StockRun r)
    {
        Entity_Master v = r.Vessel;
        string[] stats = { "Speed", "Damage", "Attackspeed", "Health" };
        float[] p0 = new float[stats.Length];
        for (int i = 0; i < stats.Length; i++) p0[i] = Pmulti(v, stats[i]);
        string skin0 = v.Skin.Value.ToString();
        float t0 = Time.unscaledTime;
        string why = BuyAt(r.Player, "Artifact_Diorama");
        yield return Until(() => Holding(v, "Effect_Artifact_Diorama") != "", 6f);
        float took = Time.unscaledTime - t0;
        yield return new WaitForSeconds(0.6f);
        bool ok = why == "" && Holding(v, "Effect_Artifact_Diorama") != "" && took < 2.5f;
        string d = "";
        for (int i = 0; i < stats.Length; i++)
        {
            float want = EffectParam(DioramaUp, stats[i] + "_Pmulti"), got = Pmulti(v, stats[i]) - p0[i];
            ok &= Mathf.Abs(got - want) < 0.001f;
            d += stats[i] + " Pmulti " + RWUtil.F(p0[i]) + " -> " + RWUtil.F(Pmulti(v, stats[i])) + " (want +" + RWUtil.F(want) + "); ";
        }
        Check("Diorama: Yossele takes its four stats at once, with no five-second wait", ok, "bought '" + why + "', held in " + RWUtil.F(took) + " s; " + d);
        string skin = v.Skin.Value.ToString(), being = ModKitUtil.BeingName(v);
        Visualproperties own = v.Beingserver != null && v.Beingserver.Entity != null ? v.Beingserver.Entity.Defaultvisual : null, shown = v.Client != null ? v.Client.Visprop : null;
        bool look = SkinHas(own, being) || SkinHas(shown, being);
        Check("Diorama: Yossele's look turns tiny, by a skin named after its being", skin == being && look,
            "skin '" + skin0 + "' -> '" + skin + "', being " + being + ", a skin of that name on its visual " + SkinHas(own, being) + ", on the shown one " + SkinHas(shown, being)
            + " (" + (own != null && own.Skinoverrides != null ? own.Skinoverrides.Count : -1) + " skins)");
#if MODKIT_PLAY
        RogueWavesModeKitPlay.Log = m => Info("Diorama camera", m);
        yield return RogueWavesModeKitPlay.Look(ModKitUtil.Pos(v));
        yield return new WaitForSecondsRealtime(1f);
#endif
        yield return Snap("stock_diorama_yossele");
    }
}
#endif
