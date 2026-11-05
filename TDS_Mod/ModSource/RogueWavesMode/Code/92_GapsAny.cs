#if ROGUEWAVESMODE_GAPS
// The Directions any vessel can take (22_AnyDirections.cs, docs/scope/11 (Any)), part "any" of the gaps build, one launch,
// solo, Smacky on a calm map: each Direction taken alone, measured on a real archer, Mining Hut, foe or the vessel, then
// let go again (RWAny reads the ranks the player holds, so taking the ids back stops it). The rules part checks the offers.
public static partial class RWTest
{
    static string AnyId(string name, int rank) { return RogueWavesModeKitDirections.Id("Any", name + (rank > 1 ? rank.ToString() : "")); }

    static void TakeAny(RWPlayer p, string name, int upTo) { TakeRanks(p, "Any", name, upTo); }

    static void TakeRanks(RWPlayer p, string vessel, string name, int upTo)
    {
        for (int r = 1; r <= upTo; r++)
            RWDirections.Take(p, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id(vessel, name + (r > 1 ? r.ToString() : ""))) as Upgradeproperties);
    }

    static void DropAny(RWPlayer p, string name) { p.Directions.RemoveAll(d => d == AnyId(name, 1) || d == AnyId(name, 2) || d == AnyId(name, 3)); }

    static float Multi(Entity_Master e, string stat) { Link_Stats s; return e != null && e.Statsserver != null && e.Statsserver.TryGetValue(stat, out s) ? s.Value_Multi : -1f; }

    static float Pmulti(Entity_Master e, string stat) { Link_Stats s; return e != null && e.Statsserver != null && e.Statsserver.TryGetValue(stat, out s) ? s.Value_Pmulti : -1f; }

    static Entity_Master FoeNear(Vector3 at, float min, float max, List<Vector3> skip)
    {
        return RogueWavesModeKitTest.SpawnFoe("Enemy_Dagger", at, min, max, skip) ?? RogueWavesModeKitTest.SpawnFoe("Enemy_Spear", at, min, max, skip);
    }

    // ---------------------------------------------------------------- rules (no map)

    static void AnyRollChecks()
    {
        RWDirections.Refresh();
        Func<RWPlayer, string, string, bool> offered = (p, hero, id) =>
        {
            for (int seed = 1; seed <= 600; seed++)
                foreach (int i in RWDirections.Roll(p, hero, seed))
                    if (RWDirections.List[i].Objectname == id) return true;
            return false;
        };
        RWPlayer lamp = new RWPlayer { Key = "test" };
        lamp.Directions.Add(AnyId("Lamp", 1));
        RWPlayer both = new RWPlayer { Key = "test" };
        both.Directions.Add(AnyId("Lamp", 1));
        both.Directions.Add(AnyId("Chain", 1));
        Check("Duo: Fire Ship is offered only to a player who holds both Lamp Oil and Chain Shot", !offered(new RWPlayer { Key = "test" }, "Hero_Smacky", AnyId("Fireship", 1)) && !offered(lamp, "Hero_Smacky", AnyId("Fireship", 1)) && offered(both, "Hero_Smacky", AnyId("Fireship", 1)), "");
        both.Directions.Add(AnyId("Fireship", 1));
        Check("Duo: Fire Ship has one rank, never offered or sold again", RWDirections.Ranks(AnyId("Fireship", 1)) == 1 && Bl_Address.Getload("Upgrade", AnyId("Fireship", 2)) == null
            && !offered(both, "Hero_Smacky", AnyId("Fireship", 1)) && RWDirections.NextRanks(new List<string> { AnyId("Fireship", 1) }).Count == 0, "");
        RWPlayer pool = new RWPlayer { Key = "test" };
        pool.Directions.Add(AnyId("Mend", 1));
        pool.Directions.Add(AnyId("Spring", 1));
        Check("Duo: Tide Pool is offered to a player who holds Tidal Mend and Spring Tide", offered(pool, "Hero_Fie", AnyId("Pool", 1)), "");

        RWPlayer spring = new RWPlayer { Key = "test" }, wound = new RWPlayer { Key = "test" };
        spring.Directions.Add(AnyId("Spring", 1));
        wound.Directions.Add(AnyId("Wound", 1));
        Check("Pair: a player holding Spring Tide is never offered Wound Spring, and Spring Tide still ranks up", !offered(spring, "Hero_Smacky", AnyId("Wound", 1)) && offered(spring, "Hero_Smacky", AnyId("Spring", 2)), "");
        Check("Pair: a player holding Wound Spring is never offered Spring Tide", !offered(wound, "Hero_Smacky", AnyId("Spring", 1)) && offered(wound, "Hero_Smacky", AnyId("Wound", 2)), "");

        string handsTo = "";
        foreach (string hero in AllHeroes)
            if (offered(new RWPlayer { Key = "test" }, hero, AnyId("Hands", 1))) handsTo += RWDirections.VesselName(hero) + " ";
        Check("Spare Hands: offered to every vessel with a unit building, never Sno Sno or Yossele", !handsTo.Contains("Snosno") && !handsTo.Contains("Yossele") && handsTo.Contains("Smacky") && handsTo.Contains("Spool"), handsTo);

        RWPlayer c1 = new RWPlayer { Key = "test" }, c3 = new RWPlayer { Key = "test" };
        c1.Directions.Add(AnyId("Compass", 1));
        c3.Directions.Add(AnyId("Compass", 1)); c3.Directions.Add(AnyId("Compass", 2)); c3.Directions.Add(AnyId("Compass", 3));
        List<int> four = RWDirections.Roll(c1, "Hero_Smacky", 5), five = RWDirections.Roll(c3, "Hero_Smacky", 5), again = RWDirections.Roll(c3, "Hero_Smacky", 5 ^ 7919, five);
        int kept = 0;
        foreach (int i in again) if (five.Contains(i)) kept++;
        Check("Spare Compass: 4 offered at rank I, 5 at rank III, a redraw draws others", four.Count == 4 && five.Count == 5 && again.Count == 5 && kept == 0 && !RWAny.CanRedraw(c1) && RWAny.CanRedraw(c3),
            four.Count + ", " + five.Count + ", redraw kept " + kept + " of " + again.Count);
    }

    // ---------------------------------------------------------------- effects, on a map

    static IEnumerator GapsAny()
    {
        Macro_Main boat = Macro_Main.Instance;
        boat.Selectcharacter("Hero_Smacky");
        yield return new WaitForSecondsRealtime(1f);
        yield return ToMap(Seed, RWChart.Calm);
        if (Game_Mapvars.Instance == null) { Setup("any", "no map"); yield break; }
        long me = RWMine.Session;
        RWPlayer player = RWRun.For(me);
        Entity_Master vessel = RWRun.Vessel(me);
        if (vessel == null) { Setup("any", "no vessel"); yield break; }
        Rich(me, 5000f);
        List<Vector3> used = new List<Vector3>();
        List<Entity_Master> known = new List<Entity_Master>();
        Entity_Master archer = null, hut = null;
        yield return BuildOne(vessel, "Tower_Smackyarcher", used, known, (m, c) => archer = m);
        yield return BuildOne(vessel, "Tower_Smackyhut", used, known, (m, c) => hut = m);
        if (archer == null) { Setup("any", "no archer built"); yield break; }
        Vector3 field = RWUtil.EnemyCores().Count > 0 ? RWUtil.EnemyCores()[0].Entityposition.Value : vessel.Entityposition.Value;

        // Wound Spring.
        float as0 = Multi(archer, "Attackspeed");
        TakeAny(player, "Wound", 3);
        yield return new WaitForSeconds(1.2f);
        Check("Wound Spring rank III: the holder's towers attack 20% faster", Mathf.Abs(Multi(archer, "Attackspeed") - as0 - RWAny.WoundSpeed[3]) < 0.001f, "Attackspeed Multi " + as0 + " -> " + Multi(archer, "Attackspeed"));
        DropAny(player, "Wound");

        // Spare Hands, on a Mining Hut.
        if (hut == null) Setup("Spare Hands", "no Mining Hut built");
        else
        {
            float u0 = ModKitUtil.Stat(hut, "Units", -1f), r0 = Multi(hut, "Respawn");
            int miners0 = RWUtil.Count("Mob_Smackyminer");
            TakeAny(player, "Hands", 3);
            yield return new WaitForSeconds(1.5f);
            int miners1 = RWUtil.Count("Mob_Smackyminer");
            Check("Spare Hands rank III: a Mining Hut keeps two more miners, and they come back 40% sooner", Mathf.Abs(ModKitUtil.Stat(hut, "Units", -1f) - u0 - 2f) < 0.01f && miners1 >= miners0 + 2 && Mathf.Abs(Multi(hut, "Respawn") - r0 + RWAny.HandsRespawn[3]) < 0.001f,
                "Units " + u0 + " -> " + ModKitUtil.Stat(hut, "Units", -1f) + ", miners " + miners0 + " -> " + miners1 + ", Respawn Multi " + r0 + " -> " + Multi(hut, "Respawn"));
            DropAny(player, "Hands");
        }

        // Magpie Nest: one Trinket at least to count.
        if (RWAny.Hoard(player) == 0) { player.Pure = 100; RWCardinals.Buy(player, "Cardinal_Dentures"); }
        int hoard = RWAny.Hoard(player);
        float vd0 = Multi(vessel, "Damage"), ad0 = Multi(archer, "Damage");
        TakeAny(player, "Magpie", 3);
        yield return new WaitForSeconds(1.2f);
        float want = RWAny.MagpieEach[3] * hoard;
        Check("Magpie Nest rank III: the vessel and the towers hit 2% harder for each Cardinal and Trinket", hoard > 0 && Mathf.Abs(Multi(vessel, "Damage") - vd0 - want) < 0.001f && Mathf.Abs(Multi(archer, "Damage") - ad0 - want) < 0.001f,
            hoard + " held; vessel Damage Multi " + vd0 + " -> " + Multi(vessel, "Damage") + ", archer " + ad0 + " -> " + Multi(archer, "Damage"));
        DropAny(player, "Magpie");

        // Salt Barrel: more health, then the mend.
        float p0 = Pmulti(vessel, "Health");
        TakeAny(player, "Salt", 3);
        yield return new WaitForSeconds(1.2f);
        Check("Salt Barrel rank III: the vessel has 45% more health (of its own)", Mathf.Abs(Pmulti(vessel, "Health") - p0 - RWAny.SaltHealth[3]) < 0.001f && Mathf.Abs(Hp(vessel) - HpMax(vessel)) < 0.01f,
            "Health Pmulti " + p0 + " -> " + Pmulti(vessel, "Health") + ", " + Hp(vessel) + " of " + HpMax(vessel));
        yield return NewFoe();
        if (lastFoe != null) Hurt(lastFoe, vessel, 6f);
        ClearFoes();
        float hurtAt = Hp(vessel);
        yield return new WaitForSeconds(3.1f);
        Check("Salt Barrel rank III: the vessel mends about 1 a second while hurt", Hp(vessel) - hurtAt >= 2f && Hp(vessel) - hurtAt <= 4.01f, hurtAt + " -> " + Hp(vessel) + " in 3.1 s");
        DropAny(player, "Salt");

        // Galley Kettle: 13 attacks reach the rank III top, which also hits harder; 3 s without a hit and it is gone. The first
        // attack strikes three times in one frame, as one that hits three foes does: it counts once.
        TakeAny(player, "Kettle", 3);
        yield return new WaitForSeconds(0.6f);
        Entity_Master foe = FoeNear(field, 2f, 8f, null);
        yield return new WaitForSeconds(0.4f);
        float k0 = Multi(vessel, "Attackspeed"), kd0 = Multi(vessel, "Damage");
        if (foe == null) Setup("Galley Kettle", "no foe");
        else
        {
            RWAny.Crew crew = RWAny.CrewOf(me);
            int hits0 = crew != null ? crew.KettleHits : -1;
            for (int i = 0; i < 3; i++) Hurt(vessel, foe, 0.01f);
            int once = crew != null ? crew.KettleHits - hits0 : -1;
            for (int i = 0; i < 12; i++) { yield return null; Hurt(vessel, foe, 0.01f); }
            Check("Galley Kettle: an attack that strikes three times counts once", once == 1, "counted " + once);
            yield return new WaitForSeconds(0.3f);
            float k1 = Multi(vessel, "Attackspeed"), kd1 = Multi(vessel, "Damage");
            yield return new WaitForSeconds(RWAny.KettleLasts + 0.4f);
            Check("Galley Kettle rank III: hits build to +50% attack speed and +10% damage, gone 3 s after the last", Mathf.Abs(k1 - k0 - 0.5f) < 0.001f && Mathf.Abs(kd1 - kd0 - RWAny.KettleTopDamage) < 0.001f
                && Mathf.Abs(Multi(vessel, "Attackspeed") - k0) < 0.001f && Mathf.Abs(Multi(vessel, "Damage") - kd0) < 0.001f,
                "Attackspeed Multi " + k0 + " -> " + k1 + " -> " + Multi(vessel, "Attackspeed") + ", Damage Multi " + kd0 + " -> " + kd1 + " -> " + Multi(vessel, "Damage"));
        }
        DropAny(player, "Kettle");
        ClearFoes();

        // Chain Shot at rank III: every 3rd hit jumps three times for half; then Fire Ship's bursts and Lamp Oil's.
        TakeAny(player, "Chain", 3);
        yield return new WaitForSeconds(0.6f);
        List<Vector3> skip = new List<Vector3>();
        Entity_Master a = FoeNear(field, 2f, 8f, skip);
        if (a != null) skip.Add(new Vector3(Mathf.Round(a.Entityposition.Value.x), Mathf.Round(a.Entityposition.Value.y), Mathf.Round(a.Entityposition.Value.z)));
        Entity_Master b = a != null ? FoeNear(a.Entityposition.Value, 0.5f, 1.5f, skip) : null;
        yield return new WaitForSeconds(0.4f);
        if (a == null || b == null) Setup("Chain Shot", "two foes side by side: " + (a != null) + ", " + (b != null));
        else
        {
            float b0 = Hp(b), jumped0 = RWAny.Jumped;
            Hurt(vessel, a, 1f);
            yield return null;
            Hurt(vessel, a, 1f);
            yield return null;
            float bAfterTwo = Hp(b);
            Hurt(vessel, a, 4f);
            yield return new WaitForSeconds(0.2f);
            float d = ModKitUtil.Flat(a.Entityposition.Value, b.Entityposition.Value);
            Check("Chain Shot rank III: the 3rd attack, and not before, jumps to a foe within 2 tiles for half of it", Mathf.Abs(bAfterTwo - b0) < 0.001f && RWAny.Jumped > jumped0 && (d > RWAny.ChainReach || Mathf.Abs(b0 - Hp(b) - 2f) < 0.01f),
                "the foe beside it " + b0 + " -> " + bAfterTwo + " -> " + Hp(b) + ", " + RWUtil.F(d) + " tiles away, jumps " + (RWAny.Jumped - jumped0));
        }
        ClearFoes();
        yield return new WaitForSeconds(0.3f);

        // Lamp Oil: a foe the vessel drowns bursts on the one beside it.
        TakeAny(player, "Lamp", 3);
        yield return new WaitForSeconds(0.6f);
        skip.Clear();
        a = FoeNear(field, 2f, 8f, skip);
        if (a != null) skip.Add(new Vector3(Mathf.Round(a.Entityposition.Value.x), Mathf.Round(a.Entityposition.Value.y), Mathf.Round(a.Entityposition.Value.z)));
        b = a != null ? FoeNear(a.Entityposition.Value, 0.5f, 1.2f, skip) : null;
        yield return new WaitForSeconds(0.4f);
        if (a == null || b == null) Setup("Lamp Oil", "two foes side by side: " + (a != null) + ", " + (b != null));
        else
        {
            // Chain Shot is still held, its count back at 0 after the 3rd hit above: this one doesn't jump.
            float maxA = HpMax(a), b0 = Hp(b), burned0 = RWAny.Burned, d = ModKitUtil.Flat(a.Entityposition.Value, b.Entityposition.Value);
            Hurt(vessel, a, Hp(a) + 50f);
            yield return new WaitForSeconds(0.3f);
            Check("Lamp Oil rank III: a drowned foe bursts for 35% of its max health on the foes within 1.5 tiles", RWAny.Burned > burned0 && (d > RWAny.LampReach[3] || Mathf.Abs(b0 - Hp(b) - maxA * RWAny.LampBurst[3]) < 0.05f || !ModKitUtil.Alive(b)),
                "drowned max " + maxA + "; the one beside it " + b0 + " -> " + Hp(b) + ", " + RWUtil.F(d) + " tiles away, bursts " + (RWAny.Burned - burned0));
        }
        ClearFoes();
        yield return new WaitForSeconds(0.3f);

        // Fire Ship: two foes on one tile, a third hit on a foe beside them; each jump bursts on the other.
        TakeAny(player, "Fireship", 1);
        yield return new WaitForSeconds(0.6f);
        skip.Clear();
        a = FoeNear(field, 2f, 8f, skip);
        if (a != null) skip.Add(new Vector3(Mathf.Round(a.Entityposition.Value.x), Mathf.Round(a.Entityposition.Value.y), Mathf.Round(a.Entityposition.Value.z)));
        b = a != null ? FoeNear(a.Entityposition.Value, 0.5f, 1.5f, skip) : null;
        Entity_Master c2 = b != null ? FoeNear(b.Entityposition.Value, 0f, 0.4f, null) : null;
        yield return new WaitForSeconds(0.3f);
        if (a == null || b == null || c2 == null || ModKitUtil.Flat(b.Entityposition.Value, c2.Entityposition.Value) > RWAny.FireShipReach || ModKitUtil.Flat(a.Entityposition.Value, b.Entityposition.Value) > RWAny.ChainReach)
            Setup("Fire Ship", "foes " + (a != null) + ", " + (b != null) + ", " + (c2 != null) + " not placed for it");
        else
        {
            RWAny.Crew crew = RWAny.CrewOf(me);
            if (crew != null) crew.ChainCount = RWAny.ChainEvery[3] - 1;
            float b0 = Hp(b), c0 = Hp(c2), maxB = HpMax(b), maxC = HpMax(c2), burned0 = RWAny.Burned;
            Hurt(vessel, a, 2f);
            yield return new WaitForSeconds(0.3f);
            float lost = b0 - Hp(b) + c0 - Hp(c2), wantLost = 2f * 1f + RWAny.FireShipBurst * (maxB + maxC);
            Check("Fire Ship: each Chain Shot jump bursts for 10% of the struck foe's max health on the foe beside it", RWAny.Burned >= burned0 + 2 && (Mathf.Abs(lost - wantLost) < 0.05f || !ModKitUtil.Alive(b) || !ModKitUtil.Alive(c2)),
                "two foes on a tile lost " + lost + " in all, " + wantLost + " expected (two jumps of 1, bursts of " + RWUtil.F(maxB * RWAny.FireShipBurst) + " and " + RWUtil.F(maxC * RWAny.FireShipBurst) + "); bursts " + (RWAny.Burned - burned0));
        }
        DropAny(player, "Chain");
        DropAny(player, "Lamp");
        DropAny(player, "Fireship");
        ClearFoes();

        // Notched Oar at rank III: the 5th attack doubled.
        TakeAny(player, "Oar", 3);
        yield return new WaitForSeconds(0.6f);
        foe = FoeNear(field, 2f, 8f, null);
        yield return new WaitForSeconds(0.4f);
        if (foe == null) Setup("Notched Oar", "no foe");
        else
        {
            float f0 = Hp(foe);
            for (int i = 0; i < 4; i++) { Hurt(vessel, foe, 1f); yield return null; }
            float f4 = Hp(foe);
            Hurt(vessel, foe, 1f);
            Check("Notched Oar rank III: every 5th attack of the vessel deals double", Mathf.Abs(f0 - f4 - 4f) < 0.01f && Mathf.Abs(f4 - Hp(foe) - 2f) < 0.01f, f0 + " -> " + f4 + " -> " + Hp(foe));
        }
        DropAny(player, "Oar");
        ClearFoes();

        // Fish Hook, every hit made to snag: half the hit again over 3 s, at most 5 bleeds at rank III.
        TakeAny(player, "Hook", 3);
        RWAny.ForceSnag = true;
        yield return new WaitForSeconds(0.6f);
        foe = FoeNear(field, 2f, 8f, null);
        yield return new WaitForSeconds(0.4f);
        if (foe == null) Setup("Fish Hook", "no foe");
        else
        {
            float f0 = Hp(foe), bled0 = RWAny.Bled;
            int snags0 = RWAny.Snags;
            for (int i = 0; i < 7; i++) Hurt(vessel, foe, 0.6f);
            float f1 = Hp(foe);
            yield return new WaitForSeconds(RWAny.HookLasts + 0.3f);
            Check("Fish Hook rank III: a snag bleeds half the hit over 3 s, at most 5 at once", RWAny.Snags - snags0 == RWAny.HookBleeds[3] && Mathf.Abs(f1 - Hp(foe) - 5 * 0.3f) < 0.02f && Mathf.Abs(RWAny.Bled - bled0 - 1.5f) < 0.02f,
                "snags " + (RWAny.Snags - snags0) + " of 7 hits, then bled " + (f1 - Hp(foe)) + " (" + f0 + " -> " + f1 + " -> " + Hp(foe) + ")");
        }
        RWAny.ForceSnag = false;
        DropAny(player, "Hook");
        ClearFoes();

        // Fog Horn at rank III: a foe within 3 tiles of the vessel is 25% slower, and free again once the player lets it go.
        TakeAny(player, "Fog", 3);
        yield return new WaitForSeconds(0.4f);
        foe = FoeNear(vessel.Entityposition.Value, 1f, 2.4f, null);
        yield return new WaitForSeconds(0.6f);
        float fogD = foe != null ? ModKitUtil.Flat(foe.Entityposition.Value, vessel.Entityposition.Value) : -1f;
        if (foe == null || fogD > RWAny.FogReach[3] - 0.2f) Setup("Fog Horn", "no foe near the vessel (" + RWUtil.F(fogD) + " tiles)");
        else
        {
            float s1 = Multi(foe, "Speed"), held = RWAny.FogOn(foe);
            DropAny(player, "Fog");
            yield return new WaitForSeconds(0.7f);
            Check("Fog Horn rank III: a foe near the vessel moves 25% slower, and not once it is let go", Mathf.Abs(held - RWAny.FogSlow[3]) < 0.001f && RWAny.FogOn(foe) == 0f && Mathf.Abs(Multi(foe, "Speed") - s1 - RWAny.FogSlow[3]) < 0.001f,
                "Speed Multi " + s1 + " -> " + Multi(foe, "Speed") + ", " + RWUtil.F(fogD) + " tiles away");
        }
        DropAny(player, "Fog");
        ClearFoes();

        // Clam Shell at rank III: two killing blows a map, each followed by 3 s of nothing.
        TakeAny(player, "Clam", 3);
        yield return new WaitForSeconds(0.6f);
        yield return NewFoe();
        Entity_Master striker = lastFoe;
        if (striker == null) Setup("Clam Shell", "no foe");
        else
        {
            // Another mod's hook that halves the blow is ahead of Clam Shell's: a blow it brings below a kill spends no save.
            ModKitUtil.Heal(vessel, vessel, HpMax(vessel));
            ModKitUtil.Front(vessel, halve);
            yield return new WaitForSeconds(0.25f);
            int halved0 = RWAny.Saved;
            float full = Hp(vessel);
            Hurt(striker, vessel, full * 1.5f);
            float halvedTo = Hp(vessel);
            ModKitUtil.Unfront(vessel, halve);
            Check("Clam Shell judges the blow after the other mods' hooks: one halved below a kill spends no save", RWAny.Saved == halved0 && Mathf.Abs(halvedTo - full * 0.25f) < 0.05f,
                full + " -> " + halvedTo + " after a halved blow of " + (full * 1.5f) + ", saves " + (RWAny.Saved - halved0));
            ModKitUtil.Heal(vessel, vessel, HpMax(vessel));
            yield return new WaitForSeconds(0.25f);
            int saved0 = RWAny.Saved;
            Hurt(striker, vessel, 100000f);
            float first = Hp(vessel);
            Hurt(striker, vessel, 5f);
            float safe = Hp(vessel);
            yield return new WaitForSeconds(RWAny.ClamSafe + 0.3f);
            Hurt(striker, vessel, 100000f);
            float second = Hp(vessel);
            bool alive = ModKitUtil.Alive(vessel);
            Check("Clam Shell rank III: two killing blows a map leave the vessel at 1 health, nothing hurts it for 3 s after", alive && RWAny.Saved - saved0 == 2 && Mathf.Abs(first - 1f) < 0.01f && Mathf.Abs(safe - 1f) < 0.01f && Mathf.Abs(second - 1f) < 0.01f,
                "after the blows " + first + ", " + safe + " (the 5 in its safe time), " + second + "; saves " + (RWAny.Saved - saved0) + ", alive " + alive);
            ModKitUtil.Heal(vessel, vessel, HpMax(vessel));
        }
        DropAny(player, "Clam");
        ClearFoes();

        // Fine Sieve, at the break after Tide 2: 8 x 2 of the colour held least.
        TakeAny(player, "Sieve", 3);
        string least = "";
        float low = float.MaxValue;
        foreach (string res in new[] { "Redaura", "Blueaura", "Yellowaura" }) if (Purse2(me, res) < low) { low = Purse2(me, res); least = res; }
        RWAny.Tide(2);
        Check("Fine Sieve rank III: a Tide pays 8 x its number of the colour held least", Mathf.Abs(Purse2(me, least) - low - 16f * Game_Mapvars.Instance.Resourcegainmultiplier) < 0.01f, least + " " + low + " -> " + Purse2(me, least));
        DropAny(player, "Sieve");

        // Spare Compass: a redraw a map.
        TakeAny(player, "Compass", 2);
        bool could = RWAny.CanRedraw(player);
        RWAny.Redrew(player);
        Check("Spare Compass rank II: one redraw a map", could && !RWAny.CanRedraw(player), "");
        DropAny(player, "Compass");

        // Tide Pool: Tidal Mend past full stays on as extra health until the Tide ends.
        TakeAny(player, "Mend", 1);
        TakeAny(player, "Spring", 1);
        TakeAny(player, "Pool", 1);
        ModKitUtil.Heal(vessel, archer, HpMax(archer));
        yield return new WaitForSeconds(0.6f);
        float max0 = HpMax(archer), pooled0 = RWDirections.Pooled;
        yield return ToBreakAfter(1);
        yield return new WaitForSeconds(0.6f);
        float grew = HpMax(archer) - max0;
        Check("Tide Pool: what Tidal Mend mends past full stays on as extra max health, a fifth at most", RWDirections.Pooled > pooled0 && grew > RWDirections.SpringHealth[1] + 0.01f && grew <= RWDirections.SpringHealth[1] + DirectionsPoolCap(max0) + 0.01f && Mathf.Abs(Hp(archer) - HpMax(archer)) < 0.05f,
            "max " + max0 + " -> " + HpMax(archer) + " (Spring Tide " + RWDirections.SpringHealth[1] + "), pooled " + (RWDirections.Pooled - pooled0) + ", health " + Hp(archer));

        // Off the map, a retry spent; a fresh campaign follows.
        RWCampaign.Playing = -1;
        Macro_Main old = Macro_Main.Instance;
        freeze = false;
        forcing = false;
        Tds_Multiplayer_Remote.Instance.Quitmap_ServerRpc(default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        yield return new WaitForSecondsRealtime(3f);
    }

    static readonly Entity_Master.Oninteracts halve = hit =>
    {
        float m;
        if (hit != null && hit.Type == "Hurt" && hit.Data != null && hit.Data.TryGetValue("Damagemulti", out m))
        {
            hit.Data = new Dictionary<string, float>(hit.Data);
            hit.Data["Damagemulti"] = m * 0.5f;
        }
    };

    static float DirectionsPoolCap(float max) { return RWDirections.PoolCap * (max + RWDirections.SpringHealth[1]); }
}
#endif
