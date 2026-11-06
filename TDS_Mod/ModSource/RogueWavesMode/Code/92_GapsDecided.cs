#if ROGUEWAVESMODE_GAPS
// The decided code fixes measured in real maps (docs/reports/DECIDED_CODE_FIXES_2026-11-05.md §4, items 1-5): part "decided"
// of the gaps build, its own launch ("-rwonly decided"), solo, from the boat. One calm map a vessel:
//   Spool    item 1: Made of Tougher Stuff III and Odd Socks III on a Crafted (2a: kit 1.14 ModKitUtil.Front, pitfall #168)
//   Yossele  item 1: Spring Tide III and Vine Blockade's barrier on a Henge (2a)
//   Uloe     item 2: a foe under Fog Horn and another slow in a Searchlight's ring (2b: ModKitUtil.SlowOn)
//   Argo     item 3: Cleave's second foe with Notched Oar, Fish Hook, Galley Kettle and Gutting Knife (2c: RWAny.ExtraHit);
//            item 4: Tar Bucket on a tower struck by hits that carry their own Damage (2f: RWUtil.Hit)
//   Boutman  item 5: a lobby Blurry Photo with Made of Tougher Stuff (2g: RWRun.IsCopy, pitfall #169)
// The other parts are reached through the game only (beings, upgrade ids, tags, stats); a number copied from one cites its
// line. An exception ends its own map's items, not the rest.
public static partial class RWTest
{
    static IEnumerator GapsDecided()
    {
        yield return DcStep("The Spool map ran to its end", DcSpool());
        yield return DcStep("The Yossele map ran to its end", DcYossele());
        yield return DcStep("The Uloe map ran to its end", DcUloe());
        yield return DcStep("The Argo map ran to its end", DcArgo());
        yield return DcStep("The Boutman map ran to its end", DcBoutman());
        RWAny.ForceSnag = false;
        dcSwinger = null;
        yield return BackOnBoat();
    }

    // Stepped's way with its own line, and the steps it yields stepped too: one map's exception ends that map only.
    static IEnumerator DcStep(string line, IEnumerator inner) { return DcStep(line, inner, new bool[1]); }

    static IEnumerator DcStep(string line, IEnumerator inner, bool[] threw)
    {
        while (!threw[0])
        {
            object step;
            try
            {
                if (!inner.MoveNext()) yield break;
                step = inner.Current;
            }
            catch (Exception e)
            {
                threw[0] = true;
                Check(line, false, e.ToString());
                yield break;
            }
            IEnumerator sub = step as IEnumerator;
            if (sub != null) yield return DcStep(line, sub, threw);
            else yield return step;
        }
    }

    // ---------------------------------------------------------------- helpers

    static long dcMe;
    static RWPlayer dcPlayer;
    static Entity_Master dcVessel;

    // GapsBase's way onto a calm map: back on the boat, a fresh run with this vessel. dcVessel stays null when it isn't there.
    static IEnumerator DcMap(string hero)
    {
        dcVessel = null;
        RWAny.ForceSnag = false;
        yield return BackOnBoat();
        RWRun.Clear();
        Macro_Main.Instance.Selectcharacter(hero);
        yield return new WaitForSecondsRealtime(1f);
        yield return ToMap(Seed, RWChart.Calm);
        if (Game_Mapvars.Instance == null || Game_Entity.Instance == null) yield break;
        dcMe = RWMine.Session;
        dcPlayer = RWRun.For(dcMe);
        Entity_Master v = RWRun.Vessel(dcMe);
        if (v == null || dcPlayer == null || v.Being.Value.ToString() != hero) yield break;
        dcVessel = v;
        Rich(dcMe, 5000f);
    }

    // Ranks 1 to 'upTo' of a Direction; the id that isn't loaded, or "".
    static string DcTake(string vessel, string name, int upTo)
    {
        for (int r = 1; r <= upTo; r++)
        {
            string id = RogueWavesModeKitDirections.Id(vessel, name + (r > 1 ? r.ToString() : ""));
            Upgradeproperties u = Bl_Address.Getload("Upgrade", id) as Upgradeproperties;
            if (u == null) return id;
            RWDirections.Take(dcPlayer, u);
        }
        return "";
    }

    static string DcF(float v) { return v.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture); }

    static string DcList(List<float> l)
    {
        List<string> s = new List<string>();
        foreach (float x in l) s.Add(DcF(x));
        return "[" + string.Join(", ", s.ToArray()) + "]";
    }

    static float DcFlat(Entity_Master a, Entity_Master b) { return a != null && b != null ? ModKitUtil.Flat(a.Entityposition.Value, b.Entityposition.Value) : -1f; }

    static void DcFill(Entity_Master e) { Link_Stats s; if (e != null && e.Statsserver != null && e.Statsserver.TryGetValue("Health", out s)) s.Value = s.Value_Calc; }

    static void DcSetHp(Entity_Master e, float hp) { Link_Stats s; if (e != null && e.Statsserver != null && e.Statsserver.TryGetValue("Health", out s)) s.Value = hp; }

    // +10000 Health, filled (Uloe's self-test Armour, R4 §8): nothing round a test foe fells it before its reading.
    static void DcArmour(Entity_Master e)
    {
        Link_Stats s;
        if (e == null || e.Statsserver == null || !e.Statsserver.TryGetValue("Health", out s) || s == null) return;
        Helper_Entitytools.Addmod(e, "Health", 10000f, Helper_Entitytools.Modtypes.Add, 0f);
        s.Value = s.Value_Calc;
    }

    // Held nearly still by a Speed Pmulti, which scales the base only: the Multi sum every slow reads is left as it is.
    static void DcHold(Entity_Master e)
    {
        if (ModKitUtil.Speed(e) != null) Helper_Entitytools.Addmod(e, "Speed", -0.95f, Helper_Entitytools.Modtypes.Pmulti, 0f);
    }

    // What one blow took from its health.
    static float DcBlow(Entity_Master from, Entity_Master to, float damage)
    {
        float hp = Hp(to);
        Hurt(from, to, damage);
        return hp - Hp(to);
    }

    // A hit that carries its own Damage, as a Sno Box's or a wall's blast: the game's Interact without ModKitCombat.Mark, so
    // Lasthittime doesn't move (kit docs/17 §2.6).
    static void DcOwnHit(Entity_Master from, Entity_Master to, float damage)
    {
        Helper_Entitytools.Interact(from, to, "Hurt", new Dictionary<string, float> { { "Damage", damage }, { "Damagemulti", 1f } });
    }

    // A no-op at the end of a hit chain: Helper_Entitytools.Updateinteract keeps only the game's own handlers, so its going
    // shows the chain was rebuilt.
    static readonly Entity_Master.Oninteracts dcMarker = hit => { };

    static void DcMark(Entity_Master m)
    {
        if (m != null) m.Oninteract = (Entity_Master.Oninteracts)Delegate.Combine(Delegate.Remove(m.Oninteract, dcMarker), dcMarker);
    }

    static bool DcMarked(Entity_Master m) { return m != null && m.Oninteract != null && Array.IndexOf(m.Oninteract.GetInvocationList(), dcMarker) >= 0; }

    static bool DcHas(Entity_Master m, string id) { return m != null && m.Upgradelistserver != null && m.Upgradelistserver.ContainsKey(id); }

    // An upgrade by the order the HUD sends ("ordered"); not taken in 4 s, landed the way the game lands a forced one ("landed").
    static IEnumerator DcUpgrade(Entity_Master m, string id, Action<string> done)
    {
        Upgradeproperties u = Bl_Address.Getload("Upgrade", id) as Upgradeproperties;
        if (u == null || !ModKitUtil.Alive(m)) { done(u == null ? id + " isn't loaded" : "the building is gone"); yield break; }
        long me = m.Entitydata.Value.Owner;
        m.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Upgrade", "{\"Upgrade\":\"" + id + "\",\"Upgrader\":" + me + "}", default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => !ModKitUtil.Alive(m) || DcHas(m, id), 4f);
        if (DcHas(m, id)) { done("ordered"); yield break; }
        if (!ModKitUtil.Alive(m)) { done("the building is gone"); yield break; }
        Helper_Entitytools.Upgrade(m, u, Helper_Entitytools.Upgradecheck.Forced, null);
        yield return new WaitForSeconds(0.3f);
        if (DcHas(m, id)) Info("an upgrade landed by the test", id + " wasn't taken by the order the HUD sends within 4 s");
        done(DcHas(m, id) ? "landed" : "not taken, by order or by force");
    }

    // BuildOne at a tile of the test's choosing: the order the HUD sends; the vessel walks there to build.
    static IEnumerator DcBuildAt(Entity_Master vessel, string tower, Vector3 at, Action<Entity_Master> done)
    {
        long me = vessel.Entitydata.Value.Owner;
        List<Entity_Master> had = new List<Entity_Master>();
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Entitydata.Value.Owner == me && e.Being.Value.ToString() == tower) had.Add(e);
        vessel.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Build", "{\"Tower\":\"" + tower + "\",\"Position\":" + Pos(at) + ",\"Orientation\":0,\"Owner\":" + me + "}", default(Unity.Netcode.ServerRpcParams));
        Entity_Master made = null;
        yield return Until(() => (made = Mine(tower, me, had)) != null, 45f);
        yield return new WaitForSeconds(0.5f);
        done(made);
    }

    // Spawned as the vessel's child, the way a build is (96_Flow's PlaceArcher): for a tile the order didn't build on.
    static Entity_Master DcSpawnTower(Entity_Master vessel, Beingobject being, Vector3 at)
    {
        Genitem g = RogueWavesModeKitTest.Surface(at);
        if (g == null || being == null) return null;
        Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(vessel.Beingserver);
        spawn.Being = being; spawn.Genadd = g; spawn.Skin = ""; spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        Link_Being made = Helper_Entitytools.Spawn(spawn, g.Position, vessel.Beingserver, true, null);
        return made != null ? made.Worldobject : null;
    }

    // One of the vessel's units, min to max tiles from 'near', spawned as its child (96_Flow's minion): its part's brain
    // takes it by its being's name, as one its building makes.
    static Entity_Master DcUnit(Entity_Master vessel, string being, Vector3 near, float min, float max)
    {
        Beingobject b = ModKitUtil.Load<Beingobject>("Mob", being);
        if (b == null || vessel == null) return null;
        Link_Being probe = new Link_Being { Being = b, Entity = b.Entity, Block = b.Block, Team = vessel.Entitydata.Value.Team };
        foreach (Genitem g in RogueWavesModeKitTest.Ground(near, max, 1, 1))
        {
            float dn = ModKitUtil.Flat(g.Position, near);
            if (dn < min || dn > max) continue;
            Genitem spot = null;
            try { spot = Helper_Gridtools.Checkifstandable(g, probe); } catch (Exception) { }
            if (spot == null) continue;
            Link_Spawn ms = Helper_Entitytools.Createlinkspawnparent(vessel.Beingserver);
            ms.Being = b; ms.Genadd = spot; ms.Skin = ""; ms.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
            Link_Being got = Helper_Entitytools.Spawn(ms, spot.Position, vessel.Beingserver, true, null);
            return got != null ? got.Worldobject : null;
        }
        return null;
    }

    // A Dagger (else a Spear) as the enemy core's child (SpawnFoe's way), on the first standable tile within 'reach' of
    // 'near' that 'fits'.
    static Entity_Master DcFoeWhere(Vector3 near, float reach, Func<Vector3, bool> fits)
    {
        Beingobject b = ModKitUtil.Load<Beingobject>("Mob", "Enemy_Dagger") ?? ModKitUtil.Load<Beingobject>("Mob", "Enemy_Spear");
        Entity_Master core = RogueWavesModeKitTest.Core();
        if (b == null || core == null) return null;
        Link_Being probe = new Link_Being { Being = b, Entity = b.Entity, Block = b.Block, Team = core.Entitydata.Value.Team };
        foreach (Genitem g in RogueWavesModeKitTest.Ground(near, reach, 1, 1))
        {
            if (!fits(g.Position)) continue;
            Genitem st = null;
            try { st = Helper_Gridtools.Checkifstandable(g, probe); } catch (Exception) { }
            if (st == null) continue;
            Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(core.Beingserver);
            spawn.Being = b; spawn.Genadd = g; spawn.Skin = ""; spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
            Link_Being lb = Helper_Entitytools.Spawn(spawn, g.Position, core.Beingserver, true, null);
            return lb != null ? lb.Worldobject : null;
        }
        return null;
    }

    // An aura node as Uloe's lamps find one (UloeUtil.IsNode: tagged Resource, with Healthresource; a den has none).
    static Entity_Master DcNode(Vector3 at, float within)
    {
        Entity_Master best = null;
        float bestD = within;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (ModKitUtil.Alive(e) && e.Taglistserver != null && e.Taglistserver.Contains("Resource") && e.Statsserver != null && e.Statsserver.ContainsKey("Healthresource"))
            {
                float d = ModKitUtil.Flat(e.Entityposition.Value, at);
                if (d < bestD) { bestD = d; best = e; }
            }
        return best;
    }

    // ---------------------------------------------------------------- item 1: two parts' hit hooks on one thing (2a)

    // Odd Socks III beside another kind: +25% Damage, and x(1 - 0.1) on each hit taken (Spool/Code/02_Data.cs:174-175).
    const float DcSocksBonus = 0.25f, DcSocksGuard = 0.1f;
    const string DcTougherFirst = "Made of Tougher Stuff's hook first", DcSocksFirst = "Odd Socks' hook first";

    static string DcOrder(float share, float tougherFirst, float socksFirst, float tougherAlone)
    {
        if (Mathf.Abs(share - tougherFirst) <= 0.003f) return DcTougherFirst;
        if (Mathf.Abs(share - socksFirst) <= 0.003f) return DcSocksFirst;
        if (Mathf.Abs(share - tougherAlone) <= 0.003f) return "Made of Tougher Stuff's hook alone";
        return "neither order";
    }

    static IEnumerator DcSpool()
    {
        const string small = "Made of Tougher Stuff III and Odd Socks III both soften a small blow on a Crafted";
        const string steady = "Made of Tougher Stuff III and Odd Socks III keep one order on a Crafted between upgrades";
        yield return DcMap("Hero_Spool");
        Entity_Master vessel = dcVessel;
        if (vessel == null) { Setup(small, "no Spool on the map"); Setup(steady, "no Spool on the map"); yield break; }
        string missing = DcTake("Any", "Tougher", 3);
        if (missing == "") missing = DcTake("Spool", "Oddsocks", 3);
        if (missing != "") { Setup(small, missing + " isn't loaded"); Setup(steady, missing + " isn't loaded"); yield break; }
        yield return new WaitForSeconds(1.5f);
        Entity_Master stitched = DcUnit(vessel, "Mob_Spoolstitched", vessel.Entityposition.Value, 2f, 4f);
        yield return new WaitForSeconds(0.6f);
        float d0 = Multi(stitched, "Damage");
        Entity_Master dart = stitched != null ? DcUnit(vessel, "Mob_Spooldart", stitched.Entityposition.Value, 0.8f, 1.5f) : null;
        if (stitched == null || dart == null)
        {
            string why = (stitched == null ? "no Stitched" : "no Dart") + " could be spawned";
            Setup(small, why); Setup(steady, why); yield break;
        }
        yield return new WaitForSeconds(0.4f);
        DcHold(stitched);
        DcHold(dart);
        yield return Until(() => !ModKitUtil.Alive(stitched) || Multi(stitched, "Damage") >= d0 + DcSocksBonus - 0.001f, 8f);
        if (!ModKitUtil.Alive(stitched) || Multi(stitched, "Damage") < d0 + DcSocksBonus - 0.001f)
        {
            string why = "Odd Socks III never reached the Stitched: Damage Multi " + DcF(d0) + " -> " + DcF(Multi(stitched, "Damage")) + ", " + DcF(DcFlat(stitched, dart)) + " tiles from the Dart";
            Setup(small, why); Setup(steady, why); yield break;
        }
        yield return NewFoe();
        Entity_Master foe = lastFoe;
        if (foe == null) { Setup(small, "no foe to strike with"); Setup(steady, "no foe to strike with"); yield break; }

        // A small blow: neither cap bites, so both hooks show whichever goes first.
        float tr = RWDirections.TougherResist[3], cap = RWDirections.TougherMaxShare, keep = 1f - DcSocksGuard;
        DcFill(stitched);
        int soft0 = RWHits.Softened;
        float lost = DcBlow(foe, stitched, 2f), want = 2f * (1f - tr) * keep;
        int soft = RWHits.Softened - soft0;
        DcFill(stitched);
        Check(small, soft == 1 && Mathf.Abs(lost - want) < 0.01f, "a blow of 2 took " + DcF(lost) + " (want " + DcF(want) + " = 2 x " + DcF(1f - tr) + " x " + DcF(keep) + "); Made of Tougher Stuff's hook ran " + soft + " time(s)");

        // A blow of 25% of max over k, k between both cuts (0.585) and Tougher's alone (0.65): Tougher's cap bites only when its
        // hook sees the blow before Odd Socks' guard, so each order leaves its own share of max health (RWHits.OnHurt).
        float max = HpMax(stitched), k = ((1f - tr) * keep + (1f - tr)) / 2f, big = cap * max / k;
        float tougherFirst = cap * keep, socksFirst = (1f - tr) * keep * big / max, tougherAlone = cap;
        Upgradeproperties nudge = Bl_Address.Getload("Upgrade", AnyId("Compass", 1)) as Upgradeproperties;
        if (nudge == null) { Setup(steady, "no upgrade to land on the Crafted (" + AnyId("Compass", 1) + ")"); yield break; }
        List<string> rows = new List<string>();
        int calm = 0, even = 0, ordered = 0, rebuilt = 0;
        for (int s = 0; s < 3 && ModKitUtil.Alive(stitched); s++)
        {
            if (s > 0)
            {
                // An upgrade landing and leaving rebuilds the hit chain (Helper_Entitytools.Updateinteract): the hooks line up again.
                DcMark(stitched);
                Helper_Entitytools.Upgrade(stitched, nudge, Helper_Entitytools.Upgradecheck.Forced, null);
                yield return new WaitForSeconds(0.3f);
                if (DcHas(stitched, nudge.Objectname)) Helper_Entitytools.Removeupgrade(stitched, nudge);
                if (!DcMarked(stitched)) rebuilt++;
                yield return new WaitForSeconds(0.5f);
            }
            DcMark(stitched);
            List<float> shares = new List<float>();
            for (int i = 0; i < 8 && ModKitUtil.Alive(stitched); i++)
            {
                DcFill(stitched);
                shares.Add(DcBlow(foe, stitched, big) / max);
                DcFill(stitched);
                yield return new WaitForSeconds(0.15f);
            }
            float lo = 9f, hi = -9f, sum = 0f;
            foreach (float x in shares) { lo = Mathf.Min(lo, x); hi = Mathf.Max(hi, x); sum += x; }
            string order = shares.Count > 0 ? DcOrder(sum / shares.Count, tougherFirst, socksFirst, tougherAlone) : "no blows";
            bool quiet = DcMarked(stitched);
            if (quiet)
            {
                calm++;
                if (shares.Count == 8 && hi - lo <= 0.002f) even++;
                if (order == DcTougherFirst || order == DcSocksFirst) ordered++;
            }
            rows.Add("stretch " + (s + 1) + (s > 0 ? " after an upgrade landed and left" : "") + ": " + shares.Count + " blows took " + DcF(lo) + " to " + DcF(hi) + " of max, " + order + (quiet ? "" : " (the chain was rebuilt inside it)"));
            Info("item 1 the order on the Crafted in stretch " + (s + 1), order);
        }
        bool socksStill = Multi(stitched, "Damage") >= d0 + DcSocksBonus - 0.001f;
        string detail = string.Join("; ", rows.ToArray()) + "; want " + DcF(tougherFirst) + " (" + DcTougherFirst + ") or " + DcF(socksFirst) + " (" + DcSocksFirst + "), " + DcF(tougherAlone)
            + " is Made of Tougher Stuff's alone; blows of " + DcF(big) + " on " + DcF(max) + " health; the upgrade rebuilt the chain " + rebuilt + " of 2 times; Odd Socks still on: " + socksStill;
        if (rebuilt == 0) Setup(steady, "landing and taking off an upgrade never rebuilt the Crafted's hit chain: " + detail);
        else Check(steady, calm >= 2 && even == calm && ordered == calm && socksStill, detail);
        ClearFoes();
        yield return ToBoat();
    }

    // Vine Blockade's barrier: 30% of the Henge's max health as each Tide begins (Yossele/Code/02_Data.cs:330, 10_Henge.cs:111).
    const float DcBlockadeShare = 0.3f;

    // Blows of a 6.5th of the barrier. The Henge's own hook (Yossele's: armour, then the barrier) and Spring Tide's (x0.9) each
    // see the blow in their order, so a full blow drains d x (1 - armour) with the Henge's first and 0.9 of that with Spring
    // Tide's. From the blow that breaks it (n, losing L) and the free blows after (each losing f x d):
    //   Spring Tide first: barrier = n f d - L;   the Henge's own first: barrier = (n f d - L) / 0.9.
    static IEnumerator DcYossele()
    {
        const string line = "Spring Tide III and Vine Blockade keep one order on a Henge for every blow of a break";
        yield return DcMap("Hero_Yossele");
        Entity_Master vessel = dcVessel;
        if (vessel == null) { Setup(line, "no Yossele on the map"); yield break; }
        string missing = DcTake("Any", "Spring", 3);
        if (missing != "") { Setup(line, missing + " isn't loaded"); yield break; }
        yield return new WaitForSeconds(0.6f);
        Entity_Master henge = null;
        yield return BuildOne(vessel, "Tower_Yosselehenge", new List<Vector3>(), new List<Entity_Master>(), (m, c) => henge = m);
        if (henge == null) { Setup(line, "no Henge built"); yield break; }
        string how = "";
        yield return DcUpgrade(henge, "Tower_Yosselehenge_Blockade2_Upgradeobject", r => how = r);
        if (how != "ordered" && how != "landed") { Setup(line, "Vine Blockade: " + how); yield break; }
        yield return new WaitForSeconds(0.5f);
        float max0 = HpMax(henge), barrier = DcBlockadeShare * max0;
        yield return ToBreakAfter(1);
        if (!ModKitUtil.Alive(henge)) { Setup(line, "the Henge fell in Tide 1"); yield break; }
        yield return NewFoe();
        Entity_Master foe = lastFoe;
        if (foe == null) { Setup(line, "no foe to strike with"); yield break; }
        float d = barrier / 6.5f, sp = 1f - RWDirections.SpringShrug;
        DcMark(henge);
        List<float> losses = new List<float>();
        int broke = 0, spring = 0;
        for (int i = 0; i < 14 && ModKitUtil.Alive(henge) && (broke == 0 || losses.Count < broke + 2); i++)
        {
            int s0 = RWHits.Softened;
            float loss = DcBlow(foe, henge, d);
            if (RWHits.Softened - s0 == 1) spring++;
            losses.Add(loss);
            if (broke == 0 && loss > 0.0005f) broke = losses.Count;
            yield return new WaitForSeconds(0.15f);
        }
        bool quiet = DcMarked(henge);
        string list = DcList(losses);
        if (broke == 0) Setup(line, "the barrier held through " + losses.Count + " blows of " + DcF(d) + " (" + DcF(barrier) + " expected): " + list);
        else if (broke == 1) Setup(line, "the Henge had no barrier at the break: the first blow of " + DcF(d) + " took " + DcF(losses[0]));
        else if (losses.Count < broke + 2) Setup(line, "the Henge fell before two free blows: " + list);
        else
        {
            float f = (losses[broke] + losses[broke + 1]) / (2f * d), x = broke * f * d - losses[broke - 1], tol = 0.01f * barrier + 0.01f;
            bool springFirst = Mathf.Abs(x - barrier) <= tol, hengeFirst = Mathf.Abs(x / sp - barrier) <= tol;
            bool freeEven = Mathf.Abs(losses[broke] - losses[broke + 1]) <= 0.002f * d + 0.0005f;
            string order = springFirst ? "Spring Tide's hook first" : hengeFirst ? "the Henge's own hook first" : "neither order";
            string detail = "barrier " + DcF(DcBlockadeShare) + " x " + DcF(max0) + " = " + DcF(barrier) + "; blows of " + DcF(d) + " took " + list + "; it broke on blow " + broke
                + ", then each blow took " + DcF(f) + " of itself; the barrier they imply is " + DcF(x) + " with Spring Tide's hook first, " + DcF(x / sp) + " with the Henge's own first: "
                + order + "; Spring Tide's hook ran on " + spring + " of " + losses.Count + " blows";
            if (!quiet) Setup(line, "the Henge's hit chain was rebuilt during the blows: " + detail);
            else Check(line, (springFirst || hengeFirst) && freeEven && spring == losses.Count, detail);
            Info("item 1 the order on the Henge", order);
        }
        ClearFoes();
        yield return ToBoat();
    }

    // ---------------------------------------------------------------- item 2: a Searchlight under Fog Horn (2b)

    // Searchlight's slow and Wide Flood's ring (Uloe/Code/02_Data.cs:300, 290); the floor of every slow (ModKitUtil.SlowOn).
    const float DcSearchSlow = 0.2f, DcRing = 3.5f, DcFloor = 0.1f;

    static IEnumerator DcUloe()
    {
        const string floor = "A foe under Fog Horn and another slow stops at 10% of its speed in a Searchlight";
        const string again = "Out of Fog Horn's reach, the Searchlight slows the foe the full 20% again";
        yield return DcMap("Hero_Uloe");
        Entity_Master vessel = dcVessel;
        if (vessel == null) { Setup(floor, "no Uloe on the map"); Setup(again, "no Uloe on the map"); yield break; }
        int team = vessel.Entitydata.Value.Team;
        Beingobject lamp = ModKitUtil.Load<Beingobject>("Mob", "Tower_Uloeflood");
        Entity_Master node = DcNode(vessel.Entityposition.Value, 80f);
        List<Vector3> used = new List<Vector3>();
        // Within the lamp's Range of the node (2.5, Uloe/Code/07_Building.cs:285).
        Vector3 at = lamp != null && node != null ? RogueWavesModeKitTest.Spot(node.Entityposition.Value, lamp, team, 1.6f, 2.3f, used, "a Flood Light by a node") : new Vector3(float.NaN, 0f, 0f);
        if (float.IsNaN(at.x))
        {
            string why = lamp == null ? "no Flood Light being" : node == null ? "no aura node within 80 tiles" : "no free tile within 2.3 of the node";
            Setup(floor, why); Setup(again, why); yield break;
        }
        Entity_Master flood = null;
        yield return DcBuildAt(vessel, "Tower_Uloeflood", at, m => flood = m);
        if (flood == null)
        {
            flood = DcSpawnTower(vessel, lamp, at);
            if (flood != null) Info("item 2 the Flood Light", "the build order didn't build it in 45 s: spawned as a build is");
        }
        if (flood == null) { Setup(floor, "no Flood Light by the node"); Setup(again, "no Flood Light by the node"); yield break; }
        yield return Until(() => !ModKitUtil.Alive(flood) || (flood.Commandlistserver != null && flood.Commandlistserver.ContainsKey("Upgrade")), 5f);
        DcArmour(flood);
        string how = "";
        foreach (string key in new[] { "Wide2", "Halogen3" })
        {
            yield return DcUpgrade(flood, "Tower_Uloeflood_" + key + "_Upgradeobject", r => how = r);
            if (how != "ordered" && how != "landed") break;
        }
        if (how != "ordered" && how != "landed") { Setup(floor, "the Flood Light's upgrades: " + how); Setup(again, "the Flood Light's upgrades: " + how); yield break; }
        Vector3 fp = flood.Entityposition.Value;
        Vector3 standAt = RogueWavesModeKitTest.Spot(fp, lamp, team, 1.8f, 2.6f, used, "a stand for Uloe by the Flood Light");
        if (float.IsNaN(standAt.x)) { Setup(floor, "no ground for Uloe by the Flood Light"); Setup(again, "no ground for Uloe by the Flood Light"); yield break; }
        ModKitUtil.MoveTo(vessel, null, standAt, 0f);
        yield return Until(() => !ModKitUtil.Alive(vessel) || ModKitUtil.Flat(vessel.Entityposition.Value, standAt) < 0.6f, 25f);
        if (!ModKitUtil.Alive(vessel)) { Setup(floor, "Uloe sank"); Setup(again, "Uloe sank"); yield break; }
        Vector3 vp = vessel.Entityposition.Value;
        float reach = RWAny.FogReach[3];
        Entity_Master foe = DcFoeWhere((vp + fp) * 0.5f, 2.2f, p => ModKitUtil.Flat(p, fp) >= 0.7f && ModKitUtil.Flat(p, fp) <= DcRing - 1f && ModKitUtil.Flat(p, vp) >= 0.7f && ModKitUtil.Flat(p, vp) <= reach - 0.8f);
        if (foe == null)
        {
            string why = "no foe could stand within " + DcF(DcRing - 1f) + " of the Flood Light and " + DcF(reach - 0.8f) + " of Uloe (" + DcF(ModKitUtil.Flat(vp, fp)) + " apart)";
            Setup(floor, why); Setup(again, why); yield break;
        }
        yield return new WaitForSeconds(0.4f);
        DcHold(foe);
        DcArmour(foe);
        // Fog Horn III (25%) and the Searchlight (20%) alone leave a foe at 55%: another slow goes on first, as Sap Bucket or
        // Yossele's vines add, so Fog Horn leaves it at 25% and the Searchlight's 20% meets the floor.
        float m0 = Multi(foe, "Speed"), other = m0 - RWAny.FogSlow[3] - 0.25f;
        Link_Modifier otherMod = other > 0.01f ? ModKitUtil.SlowOn(foe, other) : null;
        if (otherMod == null) { Setup(floor, "the foe's Speed Multi is " + DcF(m0) + ": no room for the other slow"); Setup(again, "no other slow"); yield break; }
        Info("item 2 the other slow", "a stand-in slow of " + DcF(other) + " before Fog Horn: Fog Horn III and the Searchlight alone never reach the 10% floor");
        string missing = DcTake("Any", "Fog", 3);
        if (missing != "") { Setup(floor, missing + " isn't loaded"); Setup(again, missing + " isn't loaded"); ModKitUtil.Unmod(otherMod); yield break; }
        yield return Until(() => !ModKitUtil.Alive(foe) || RWAny.FogOn(foe) > 0f, 3f);
        float m1 = Multi(foe, "Speed"), want1 = m0 - other - RWAny.FogSlow[3];
        string where = "the foe " + DcF(DcFlat(foe, flood)) + " from the Flood Light (its ring " + DcF(DcRing) + "), " + DcF(DcFlat(foe, vessel)) + " from Uloe (Fog Horn's reach " + DcF(reach) + "), the node " + DcF(DcFlat(node, flood)) + " from the lamp";
        if (RWAny.FogOn(foe) <= 0f) { Setup(floor, "Fog Horn never reached the foe: " + where); Setup(again, "Fog Horn never reached the foe"); ModKitUtil.Unmod(otherMod); yield break; }
        yield return DcUpgrade(flood, "Tower_Uloeflood_Searchlight4_Upgradeobject", r => how = r);
        if (how != "ordered" && how != "landed") { Setup(floor, "Searchlight: " + how); Setup(again, "Searchlight: " + how); ModKitUtil.Unmod(otherMod); yield break; }
        // Searchlight looks once a second (UloeData.LightTick) once it has a node.
        yield return Until(() => !ModKitUtil.Alive(foe) || Multi(foe, "Speed") < m1 - 0.005f, 3.5f);
        yield return new WaitForSeconds(0.2f);
        float m2 = Multi(foe, "Speed"), want2 = Mathf.Max(DcFloor, m1 - DcSearchSlow);
        if (m2 >= m1 - 0.005f)
        {
            string why = "the Searchlight never slowed the foe: Speed Multi " + DcF(m1) + "; " + where;
            Setup(floor, why); Setup(again, why); ModKitUtil.Unmod(otherMod); yield break;
        }
        Check(floor, Mathf.Abs(m1 - want1) < 0.003f && Mathf.Abs(m2 - want2) < 0.003f, "Speed Multi " + DcF(m0) + " as it came, " + DcF(m1) + " under the other slow and Fog Horn (want " + DcF(want1) + "), "
            + DcF(m2) + " in the Searchlight (want " + DcF(want2) + ": its " + DcF(DcSearchSlow) + " cut to " + DcF(m1 - want2) + "); " + where);

        // Out of Fog Horn's reach: Uloe walks away, or, with no ground to walk to, the player lets Fog Horn go.
        Vector3 away = RogueWavesModeKitTest.Spot(foe.Entityposition.Value, lamp, team, reach + 3f, reach + 7f, used, "a walk out of Fog Horn's reach");
        bool walked = false;
        if (!float.IsNaN(away.x))
        {
            ModKitUtil.MoveTo(vessel, null, away, 0f);
            yield return Until(() => !ModKitUtil.Alive(vessel) || !ModKitUtil.Alive(foe) || DcFlat(vessel, foe) > reach + 0.5f, 15f);
            walked = ModKitUtil.Alive(vessel) && ModKitUtil.Alive(foe) && DcFlat(vessel, foe) > reach + 0.5f;
        }
        if (!walked) DropAny(dcPlayer, "Fog");
        yield return Until(() => !ModKitUtil.Alive(foe) || RWAny.FogOn(foe) <= 0f, 2f);
        float freed = Multi(foe, "Speed"), want3 = m0 - other - DcSearchSlow;
        yield return Until(() => !ModKitUtil.Alive(foe) || Mathf.Abs(Multi(foe, "Speed") - want3) < 0.003f, 3.5f);
        yield return new WaitForSeconds(0.2f);
        float m3 = Multi(foe, "Speed");
        Check(again, ModKitUtil.Alive(foe) && RWAny.FogOn(foe) <= 0f && Mathf.Abs(m3 - want3) < 0.003f, "Speed Multi " + DcF(m2) + " under both, " + DcF(freed) + " as Fog Horn let go, " + DcF(m3)
            + " a search later (want " + DcF(want3) + "); " + (walked ? "Uloe walked " + DcF(DcFlat(vessel, foe)) + " tiles from the foe" : "Uloe had no ground to walk to, so Fog Horn was let go instead")
            + "; the foe " + DcF(DcFlat(foe, flood)) + " from the Flood Light");
        ModKitUtil.Unmod(otherMod);
        DropAny(dcPlayer, "Fog");
        ClearFoes();
        yield return ToBoat();
    }

    // ---------------------------------------------------------------- items 3 and 4: Argo's map

    static IEnumerator DcArgo()
    {
        yield return DcMap("Hero_Argo");
        Entity_Master vessel = dcVessel;
        if (vessel == null)
        {
            foreach (string l in new[] { DcShareLine, DcSnagLine, DcOnceLine, DcAfterLine, DcMaskedLine }) Setup(l, "no Argo on the map");
            yield break;
        }
        yield return DcStep("The Cleave items ran to their end", DcCleave(vessel));
        RWAny.ForceSnag = false;
        dcSwinger = null;
        ClearFoes();
        yield return DcStep("The Tar Bucket items ran to their end", DcTar(vessel));
        ClearFoes();
        yield return ToBoat();
    }

    const string DcShareLine = "Cleave's second foe takes neither Notched Oar's doubling nor Gutting Knife's bonus";
    const string DcSnagLine = "Fish Hook snags the swing's first foe only, never Cleave's second";
    const string DcOnceLine = "Each swing counts once for Notched Oar and Galley Kettle, and Cleave not at all";
    const string DcAfterLine = "Tar Bucket waits 4 s after the last of the hits that carry their own Damage";
    const string DcMaskedLine = "A hit healed in the same moment still holds Tar Bucket back 4 s";

    // Argo's Damage, Cleave's reach from the first foe and its share of the swing (Argo/Code/02_Data.cs:128, 131, 132).
    const float DcSwing = 4f, DcCleaveReach = 1.25f, DcCleaveShare = 0.6f;

    sealed class DcHit { public Entity_Master To; public float Damage, Multi; public bool Cleave, Extra; }
    static readonly List<DcHit> dcHits = new List<DcHit>();
    static Entity_Master dcSwinger;
    static int dcSwingFrame = -1, dcNatural;

    // At the very front of a foe's chain: each hit as the attacker's side left it (Notched Oar's and Gutting Knife's
    // Damagemulti), and Argo's own swings outside the test's frames.
    static readonly Entity_Master.Oninteracts dcRecord = hit =>
    {
        if (hit == null || hit.Type != "Hurt" || hit.Data == null || hit.To == null) return;
        float dmg, mul;
        if (!hit.Data.TryGetValue("Damage", out dmg)) dmg = -1f;
        if (!hit.Data.TryGetValue("Damagemulti", out mul)) mul = -1f;
        DcHit h = new DcHit { To = hit.To, Damage = dmg, Multi = mul, Cleave = hit.Data.ContainsKey(RWAny.ArgoCleave), Extra = hit.Data.ContainsKey(RWAny.Extra) };
        if (dcSwinger != null && hit.From == dcSwinger && !h.Cleave && !h.Extra && Time.frameCount != dcSwingFrame) dcNatural++;
        dcHits.Add(h);
    };

    // Five swings of Argo's, one a frame, as ModKitCombat deals them (GapsAny's Notched Oar), with two foes held where his
    // Cleave reaches the second; Fish Hook made to snag every hit (RWAny.ForceSnag, its self-test switch).
    static IEnumerator DcCleave(Entity_Master vessel)
    {
        const string share = DcShareLine, snag = DcSnagLine, once = DcOnceLine;
        string missing = DcTake("Any", "Oar", 3);
        if (missing == "") missing = DcTake("Any", "Hook", 3);
        if (missing == "") missing = DcTake("Any", "Kettle", 1);
        string knife = missing == "" ? BuyTrinket(dcPlayer, "Knife", 2) : "";
        if (missing != "" || knife != "")
        {
            string why = missing != "" ? missing + " isn't loaded" : "Gutting Knife wasn't bought: " + knife;
            Setup(share, why); Setup(snag, why); Setup(once, why); yield break;
        }
        yield return new WaitForSeconds(1f);
        RWAny.Crew crew = RWAny.CrewOf(dcMe);
        if (crew == null || crew.Vessel != vessel || crew.Oar != 3 || crew.Hook != 3 || crew.Kettle != 1)
        {
            string why = "RWAny keeps no crew for Argo with these ranks" + (crew != null ? " (Oar " + crew.Oar + ", Hook " + crew.Hook + ", Kettle " + crew.Kettle + ")" : "");
            Setup(share, why); Setup(snag, why); Setup(once, why); yield break;
        }
        float range = ModKitUtil.Stat(vessel, "Range", 1.75f), reach = range + 1f;
        Vector3 ap = vessel.Entityposition.Value;
        Entity_Master a = DcFoeWhere(ap, reach, p => ModKitUtil.Flat(p, ap) >= range + 0.15f && ModKitUtil.Flat(p, ap) <= reach - 0.35f);
        Entity_Master b = null;
        if (a != null)
        {
            Vector3 at = a.Entityposition.Value;
            b = DcFoeWhere(at, 1.3f, p => ModKitUtil.Flat(p, at) >= 0.6f && ModKitUtil.Flat(p, at) <= DcCleaveReach - 0.15f && ModKitUtil.Flat(p, ap) <= reach - 0.3f);
        }
        if (a == null || b == null)
        {
            string why = "no two foes could stand by Argo for a Cleave (" + (a == null ? "first" : "second") + " foe)";
            Setup(share, why); Setup(snag, why); Setup(once, why); yield break;
        }
        yield return new WaitForSeconds(0.4f);
        DcHold(a); DcHold(b);
        DcArmour(a); DcArmour(b);
        // Argo's own swings held back by an Attackspeed Multi of -0.95, taken off after: the five here are his only ones.
        Link_Modifier held = Helper_Entitytools.Addmod(vessel, "Attackspeed", -0.95f, Helper_Entitytools.Modtypes.Multi, 0f);
        RWAny.ForceSnag = true;
        dcSwinger = vessel;
        dcNatural = 0;
        // A modifier landing may rebuild the vessel's chains: Argo's Cleave handler and the mode's settle back within 0.35 s.
        yield return new WaitForSeconds(0.6f);
        float ab = DcFlat(a, b), bv = DcFlat(b, vessel);
        string place = "the second foe " + DcF(ab) + " from the first (Cleave's reach " + DcF(DcCleaveReach) + "), " + DcF(bv) + " from Argo (Range + 1 = " + DcF(reach) + "); the first tagged Mob: " + ModKitUtil.HasTag(a, "Mob");
        List<string> rows = new List<string>();
        int cleaves = 0, sharesOk = 0, firstOk = 0, due = 0, snagged = 0, counted = 0;
        if (ab <= DcCleaveReach - 0.05f && bv <= reach - 0.05f)
            for (int i = 0; i < 5 && ModKitUtil.Alive(vessel) && ModKitUtil.Alive(a) && ModKitUtil.Alive(b); i++)
            {
                ModKitUtil.Unfront(a, dcRecord); ModKitUtil.Front(a, dcRecord);
                ModKitUtil.Unfront(b, dcRecord); ModKitUtil.Front(b, dcRecord);
                dcHits.Clear();
                int oar0 = crew.OarCount, kettle0 = crew.KettleHits, snags0 = RWAny.Snags, doubled0 = RWAny.Doubled;
                float hpB = Hp(b);
                bool doubles = oar0 + 1 >= RWAny.OarEvery[3];
                dcSwingFrame = Time.frameCount;
                Hurt(vessel, a, DcSwing);
                DcHit first = dcHits.Find(h => h.To == a && !h.Cleave && !h.Extra), cleave = dcHits.Find(h => h.To == b && h.Cleave);
                int onB = dcHits.FindAll(h => h.To == b).Count, snags = RWAny.Snags - snags0, doubled = RWAny.Doubled - doubled0, kettle = crew.KettleHits - kettle0;
                float wantFirst = (doubles ? 2f : 1f) * (1f + RWTrinkets.KnifeEach * 2f), lostB = hpB - Hp(b);
                int wantOar = doubles ? 0 : oar0 + 1;
                if (cleave != null) cleaves++;
                if (first != null && Mathf.Abs(first.Multi - wantFirst) < 0.001f) firstOk++;
                if (cleave != null && onB == 1 && Mathf.Abs(cleave.Multi - 1f) < 0.001f && Mathf.Abs(cleave.Damage * cleave.Multi - DcSwing * DcCleaveShare) < 0.001f) sharesOk++;
                if (doubles) due++;
                if (snags == 1) snagged++;
                if (crew.OarCount == wantOar && kettle == 1 && doubled == (doubles ? 1 : 0)) counted++;
                rows.Add("swing " + (i + 1) + ": first foe x" + (first != null ? DcF(first.Multi) : "none") + " (want x" + DcF(wantFirst) + "), second " + (cleave != null ? DcF(cleave.Damage) + " x" + DcF(cleave.Multi) : "not struck")
                    + " (" + onB + " hit, lost " + DcF(lostB) + "), snags +" + snags + ", doubled +" + doubled + ", Oar count " + oar0 + " -> " + crew.OarCount + ", Kettle hits +" + kettle);
                yield return null;
            }
        // The bleeds Fish Hook started run 3 s (RWAny.HookLasts): their ticks carry RW_Extra.
        int ticksA = 0, ticksB = 0;
        dcHits.Clear();
        float t = 0f, until = Time.time + RWAny.HookLasts + 0.6f;
        while (rows.Count > 0 && Time.time < until && t < 8f)
        {
            if (ModKitUtil.Alive(a)) ModKitUtil.Front(a, dcRecord);
            if (ModKitUtil.Alive(b)) ModKitUtil.Front(b, dcRecord);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        foreach (DcHit h in dcHits)
            if (h.Extra) { if (h.To == a) ticksA++; else if (h.To == b) ticksB++; }
        ModKitUtil.Unmod(held);
        RWAny.ForceSnag = false;
        ModKitUtil.Unfront(a, dcRecord);
        ModKitUtil.Unfront(b, dcRecord);
        dcSwinger = null;
        DropAny(dcPlayer, "Oar");
        DropAny(dcPlayer, "Hook");
        DropAny(dcPlayer, "Kettle");
        int n = rows.Count;
        string detail = string.Join("; ", rows.ToArray()) + "; then bleed ticks on the first foe " + ticksA + ", on the second " + ticksB + "; Argo's own swings in between " + dcNatural + "; " + place;
        if (cleaves == 0)
        {
            string why = "no swing cleaved: " + detail;
            Setup(share, why); Setup(snag, why); Setup(once, why); yield break;
        }
        Check(share, n == 5 && cleaves == n && sharesOk == n && firstOk == n && due == 1, detail);
        if (dcNatural > 0) Setup(snag, "Argo swung by himself " + dcNatural + " time(s), so a bleed on either foe can be his: " + detail);
        else Check(snag, n == 5 && snagged == n && ticksA > 0 && ticksB == 0, detail);
        Check(once, n == 5 && counted == n, detail);
    }

    // Tar Bucket x2 on a Scatter Cannon: a hit holds the mend back 4 s (RWTrinkets.TarAfter); it looks four times a second.
    static IEnumerator DcTar(Entity_Master vessel)
    {
        const string after = DcAfterLine, masked = DcMaskedLine;
        string why = BuyTrinket(dcPlayer, "Tar", 2);
        if (why != "") { Setup(after, "Tar Bucket wasn't bought: " + why); Setup(masked, "Tar Bucket wasn't bought: " + why); yield break; }
        Entity_Master tower = null;
        yield return BuildOne(vessel, "Tower_Argoscatter", new List<Vector3>(), new List<Entity_Master>(), (m, c) => tower = m);
        if (tower == null) { Setup(after, "no Scatter Cannon built"); Setup(masked, "no Scatter Cannon built"); yield break; }
        yield return NewFoe();
        Entity_Master foe = lastFoe;
        if (foe == null) { Setup(after, "no foe"); Setup(masked, "no foe"); yield break; }
        yield return new WaitForSeconds(0.6f);
        float max = HpMax(tower), mended0 = RWTrinkets.TarMended, lo = RWTrinkets.TarAfter - 0.05f, hi = RWTrinkets.TarAfter + 0.6f;
        Hurt(foe, tower, max * 0.4f);
        yield return Until(() => !ModKitUtil.Alive(tower) || RWTrinkets.TarMended > mended0, RWTrinkets.TarAfter + 3f);
        if (RWTrinkets.TarMended <= mended0)
        {
            string w = "Tar Bucket never mended the Scatter Cannon after a hit: health " + DcF(Hp(tower)) + " of " + DcF(max);
            Setup(after, w); Setup(masked, w); yield break;
        }
        yield return new WaitForSeconds(1.2f);

        // Three hits 0.8 s apart that carry their own Damage and never move Lasthittime.
        float lht0 = tower.Lasthittime, start = RWTrinkets.TarMended, last = 0f;
        for (int i = 0; i < 3; i++)
        {
            DcOwnHit(foe, tower, max * 0.03f);
            last = Time.time;
            if (i < 2) yield return new WaitForSeconds(0.8f);
        }
        float lht1 = tower.Lasthittime, between = RWTrinkets.TarMended - start, m1 = RWTrinkets.TarMended;
        yield return Until(() => !ModKitUtil.Alive(tower) || RWTrinkets.TarMended > m1 + 0.0001f, RWTrinkets.TarAfter + 4f);
        float first = RWTrinkets.TarMended > m1 + 0.0001f ? Time.time - last : -1f;
        Check(after, between < 0.0001f && first >= lo && first <= hi, "the first mend " + DcF(first) + " s after the last hit (want " + DcF(RWTrinkets.TarAfter) + " to " + DcF(RWTrinkets.TarAfter + 0.25f)
            + ": a look each 0.25 s), mended " + DcF(between) + " during the hits; Lasthittime " + DcF(lht0) + " -> " + DcF(lht1) + (Mathf.Approximately(lht0, lht1) ? " (unmoved)" : " (moved: these hits set it)"));

        // One hit whose loss is put back in the same frame: only the hit hook (RWUtil.Hit) sees it, not the drop in health.
        yield return new WaitForSeconds(1.6f);
        float m2 = RWTrinkets.TarMended, hp0 = Hp(tower);
        DcOwnHit(foe, tower, max * 0.03f);
        float took = hp0 - Hp(tower);
        DcSetHp(tower, hp0);
        float hitAt = Time.time;
        yield return Until(() => !ModKitUtil.Alive(tower) || RWTrinkets.TarMended > m2 + 0.0001f, RWTrinkets.TarAfter + 4f);
        float next = RWTrinkets.TarMended > m2 + 0.0001f ? Time.time - hitAt : -1f;
        if (took <= 0.0001f) Setup(masked, "the hit took nothing from the Scatter Cannon");
        else Check(masked, next >= lo && next <= hi, "the hit took " + DcF(took) + ", put back at once; the next mend came " + DcF(next) + " s later (want " + DcF(RWTrinkets.TarAfter) + " to "
            + DcF(RWTrinkets.TarAfter + 0.25f) + "; the mends ran once a second before it)");
    }

    // ---------------------------------------------------------------- item 5: a lobby Blurry Photo (2g)

    // A copy on the player's team: the game's own tag on the thing, which RWRun.IsCopy reads (pitfall #169).
    static int DcCopies(List<Entity_Master> into, int team)
    {
        into.Clear();
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (ModKitUtil.Alive(e) && e.Entitydata.Value.Team == team && ModKitUtil.HasTag(e, "Clone")) into.Add(e);
        return into.Count;
    }

    static Entity_Master DcBro(long me)
    {
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (ModKitUtil.Alive(e) && e.Entitydata.Value.Owner == me && e.Being.Value.ToString() == "Mob_Boutmanbro" && !RWRun.IsCopy(e)) return e;
        return null;
    }

    // A tenth of its health from a foe, put back after: the share of the blow it took, and how often Made of Tougher Stuff's
    // hook ran (RWHits.Softened).
    static float DcSoft(Entity_Master foe, Entity_Master to, out int ran)
    {
        float d = 0.1f * HpMax(to), hp = Hp(to);
        int s0 = RWHits.Softened;
        Hurt(foe, to, d);
        ran = RWHits.Softened - s0;
        float took = d > 0f ? (hp - Hp(to)) / d : -1f;
        DcSetHp(to, hp);
        return took;
    }

    static IEnumerator DcBoutman()
    {
        const string copyLine = "A lobby Blurry Photo's copy of the vessel doesn't take Made of Tougher Stuff";
        const string broLine = "Boutbro takes Made of Tougher Stuff beside a Blurry Photo copy";
        yield return DcMap("Hero_Boutman");
        Entity_Master vessel = dcVessel;
        if (vessel == null) { Setup(copyLine, "no Boutman on the map"); Setup(broLine, "no Boutman on the map"); yield break; }
        string missing = DcTake("Any", "Tougher", 3);
        if (missing != "") { Setup(copyLine, missing + " isn't loaded"); Setup(broLine, missing + " isn't loaded"); yield break; }
        yield return new WaitForSeconds(0.6f);
        Entity_Master house = null, bro = null;
        yield return BuildOne(vessel, "Tower_Boutmanhouse", new List<Vector3>(), new List<Entity_Master>(), (m, c) => house = m);
        yield return Until(() => (bro = DcBro(dcMe)) != null, 60f);
        if (bro == null) { string why = house == null ? "no Bouthouse built" : "no Boutbro after 60 s"; Setup(copyLine, why); Setup(broLine, why); yield break; }
        yield return new WaitForSeconds(1f);

        // Not sold (RWCardinals.NotSold): the boat strips it from the lobby (LockLobby), and boot doesn't load its group.
        Artifactentryobject entry = RWUtil.Item<Artifactentryobject>("Artifact", "Artifact_Blurryphoto");
        Bl_Address.Loadgroup group = null;
        if (entry != null && entry.Artifactloads != null)
        {
            try { group = Bl_Address.Load(entry.Artifactloads.Objectname); }
            catch (Exception e) { Info("item 5 the photo's group", e.Message); }
        }
        yield return Until(() => group == null || group.Loaded, 60f);
        Artifactobject art = entry != null && entry.Artifactloads != null ? ModKitUtil.Load<Artifactobject>("Artifact", entry.Artifactloads.Objectname) : null;
        Entity_Master core = RWUtil.PlayerCore();
        if (art == null || art.Coreupgrades == null || core == null || core.Upgradelistserver == null)
        {
            string why = art == null ? "Artifact_Blurryphoto didn't load" : "no Basin";
            Setup(copyLine, why); Setup(broLine, why); yield break;
        }
        Info("item 5 the lobby pick", "the boat strips a lobby Blurry Photo (RWCardinals.NotSold), so it goes on the Basin here as the game puts on a lobby pick: its core upgrades with the photo passed, then RWCardinals.CatchUp for what already stands");
        foreach (Upgradeproperties u in art.Coreupgrades)
            if (u != null && !core.Upgradelistserver.ContainsKey(u.Objectname))
                Helper_Entitytools.Upgrade(core, u, Helper_Entitytools.Upgradecheck.Forced, art);
        int reached = RWCardinals.CatchUp(art, core), team = vessel.Entitydata.Value.Team;
        List<Entity_Master> copies = new List<Entity_Master>();
        yield return Until(() => DcCopies(copies, team) > 0, 8f);
        bool sank = false;
        if (copies.Count == 0 && ModKitUtil.Alive(vessel))
        {
            // The game's own Heroapply runs as a vessel spawns: Boutman sinks and comes back from the Basin with the photo on him.
            Info("item 5 no copy yet", "CatchUp reached " + reached + " things and no copy stood after 8 s: Boutman sinks and comes back");
            Entity_Master old = vessel;
            Helper_Entitytools.Despawn(old.Beingserver, true, "RogueWaves");
            sank = true;
            yield return Until(() => { Entity_Master back = RWRun.Vessel(dcMe); return back != null && back != old && ModKitUtil.Alive(back); }, 75f);
            Entity_Master now = RWRun.Vessel(dcMe);
            if (now != null && now != old) vessel = now;
            yield return Until(() => DcCopies(copies, team) > 0, 12f);
        }
        yield return NewFoe();
        Entity_Master foe = lastFoe;
        if (foe == null) { Setup(copyLine, "no foe to strike with"); Setup(broLine, "no foe to strike with"); yield break; }
        // A Hero-tagged spawn is reached once it has its stats (RWDirections.ReachLater): a second covers it.
        yield return new WaitForSeconds(1f);
        bro = DcBro(dcMe) ?? bro;
        List<string> copyRows = new List<string>(), broRows = new List<string>();
        int copyRan = 0, copyHits = 0, broRan = 0, broHits = 0;
        for (int pass = 0; pass < 2; pass++)
        {
            if (pass == 1) { RWDirections.Sweep(dcPlayer); yield return new WaitForSeconds(0.3f); }
            string when = pass == 0 ? "at first" : "after a sweep of the player's things";
            foreach (Entity_Master c in copies)
            {
                if (!ModKitUtil.Alive(c)) continue;
                int ran;
                float took = DcSoft(foe, c, out ran);
                copyRan += ran;
                copyHits++;
                copyRows.Add(when + ", " + c.Being.Value + " took " + DcF(took) + " of a blow, the hook ran " + ran + " time(s)");
            }
            if (ModKitUtil.Alive(bro))
            {
                int ran;
                float took = DcSoft(foe, bro, out ran);
                broRan += ran;
                broHits++;
                broRows.Add(when + " he took " + DcF(took) + " of a blow, the hook ran " + ran + " time(s)");
            }
        }
        string tags = "";
        foreach (Entity_Master c in copies)
            if (c != null && c.Beingserver != null && c.Beingserver.Entity != null)
                tags += "; " + c.Being.Value + " (owner " + (c.Entitydata.Value.Owner == dcMe ? "the player" : c.Entitydata.Value.Owner.ToString()) + ") has Mob among its being's tags: "
                    + c.Beingserver.Entity.Tags.Contains("Mob") + ", Clone " + ModKitUtil.HasTag(c, "Clone") + ", Prespawn " + ModKitUtil.HasTag(c, "Prespawn");
        if (copyHits == 0) Setup(copyLine, "no Blurry Photo copy stood (CatchUp reached " + reached + (sank ? "; Boutman sank and came back" : "") + ")");
        else Check(copyLine, copyRan == 0, string.Join("; ", copyRows.ToArray()) + tags);
        if (broHits == 0) Setup(broLine, "Boutbro was gone");
        else Check(broLine, broRan == broHits, string.Join("; ", broRows.ToArray()) + " (want the hook once a blow, " + DcF(1f - RWDirections.TougherResist[3]) + " of it with nothing else on him)"
            + (copyHits == 0 ? "; no copy stood beside him" : ""));
        Entity_Master pick = RWRun.Vessel(dcMe);
        Finding("2g RWRun.Vessel picks a Blurry Photo copy", pick != null && RWRun.IsCopy(pick), "RWRun.Vessel gives " + (pick == null ? "none" : pick.Being.Value + (RWRun.IsCopy(pick) ? ", a copy" : ", the vessel"))
            + "; " + copies.Count + " copies stand; " + (sank ? "Boutman sank and came back before the copy" : "Boutman never sank, so he stands before the copy in the Hero list"));
        ClearFoes();
        yield return ToBoat();
    }
}
#endif
