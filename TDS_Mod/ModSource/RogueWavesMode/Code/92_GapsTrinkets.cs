#if ROGUEWAVESMODE_GAPS
// Claude's Trinkets and Cardinals (24_Trinkets.cs, docs/scope/12): the shop's rules in part "rules" (no map), the effects in
// part "trinkets", one launch, solo, Smacky on a calm map, each bought with Pure Aura and measured on the vessel, an archer,
// a wall, a foe or the Basin.
public static partial class RWTest
{
    // ---------------------------------------------------------------- rules (no map)

    static void TrinketRuleChecks()
    {
        RWPlayer punch = new RWPlayer { Key = "test", Hero = "Hero_Smacky" }, plain = new RWPlayer { Key = "test", Hero = "Hero_Smacky" };
        punch.Trinkets.Add("Cardinal_Punch1");
        string off = "";
        int run = RWRun.Cardinals.Count, ramp = RWCardinals.Ramp;
        foreach (string id in RWCardinals.Sold)
            if (RWCardinals.PriceFor(punch, id) != Mathf.FloorToInt((RWCardinals.Cost(id) + ramp * (1 + run)) * 0.8f + 0.0001f) || RWCardinals.PriceFor(plain, id) != RWCardinals.Cost(id) + ramp * run) off += id + " ";
        Check("Punch Card: every Cardinal and Trinket costs its holder 20% less, rounded down, after the ramp (25 + 3 -> 22, 15 + 3 -> 14)", off == "" && (run > 0 || RWCardinals.PriceFor(punch, "Cardinal_Cork") == 22 && RWCardinals.PriceFor(punch, "Cardinal_Dentures") == 14), off + "run " + run);
        RWPlayer two = new RWPlayer { Key = "test", Hero = "Hero_Smacky" };
        two.Trinkets.Add("Cardinal_Dentures1");
        two.Trinkets.Add("Cardinal_Dentures2");
        Check("C1: each Trinket copy a player holds and each Cardinal the run holds adds 3 Pure Aura to every price", RWCardinals.PriceFor(two, "Cardinal_Flare") == RWCardinals.Cost("Cardinal_Flare") + ramp * (2 + run) && RWCardinals.PriceFor(plain, "Cardinal_Flare") == RWCardinals.Cost("Cardinal_Flare") + ramp * run,
            RWCardinals.PriceFor(two, "Cardinal_Flare") + " with two copies, " + RWCardinals.PriceFor(plain, "Cardinal_Flare") + " with none, run " + run);

        punch.Pure = 100;
        string refused = RWCardinals.Buy(punch, "Cardinal_Oyster");
        bool offered = false;
        for (uint seed = 1; seed <= 200 && !offered; seed++)
            if (RWCardinals.Shelf(seed, 1 + (int)(seed % 10), (int)(seed % 16), c => RWCardinals.MaxedFor(punch, c), 6).Contains("Cardinal_Oyster")) offered = true;
        Check("Pair: a Punch Card holder is never offered Pearl Oyster, and the shop refuses it for nothing", !offered && refused == "Clashes with Punch Card" && punch.Pure == 100 && RWCardinals.Held(punch, "Oyster") == 0, "'" + refused + "', Pure " + punch.Pure);
        RWPlayer oyster = new RWPlayer { Key = "test", Hero = "Hero_Smacky" };
        oyster.Trinkets.Add("Cardinal_Oyster1");
        Check("Pair: a Pearl Oyster holder is never offered Punch Card", RWCardinals.MaxedFor(oyster, "Cardinal_Punch") && RWCardinals.ClashFor(oyster, "Cardinal_Punch") == "Pearl Oyster" && !RWCardinals.MaxedFor(plain, "Cardinal_Punch"), "");

        string walls = "";
        foreach (string hero in AllHeroes)
            if (!RWCardinals.MaxedFor(new RWPlayer { Key = "test", Hero = hero }, "Cardinal_Plank")) walls += RWDirections.VesselName(hero) + " ";
        Check("Spare Plank: offered to every vessel with a wall, never Buster or Argo", walls.Trim() == "Smacky Fie Boutman Snosno Yossele Spool Uloe", walls);

        int bad = 0;
        for (uint seed = 1; seed <= 30; seed++)
        {
            List<string> three = RWCardinals.Shelf(seed, 2, 5), more = RWCardinals.Shelf(seed, 2, 5, null, 3);
            if (more.Count != RWCardinals.OnShelf + 3 || more.GetRange(0, three.Count).Count != three.Count || string.Join(",", more.GetRange(0, three.Count).ToArray()) != string.Join(",", three.ToArray())) bad++;
        }
        Check("Cargo Crate: each stack puts one more card on the shelf, after the three the shelf had", bad == 0, bad + " of 30 shelves wrong");

        Check("Oilskin Coat: 10% of hits slide off, 19% with two, 27% with three", Mathf.Abs(RWTrinkets.SlideChance(1) - 0.1f) < 0.0001f && Mathf.Abs(RWTrinkets.SlideChance(2) - 0.19f) < 0.0001f && Mathf.Abs(RWTrinkets.SlideChance(3) - 0.271f) < 0.0001f, "");

        RWPlayer storm = new RWPlayer { Key = "test", Name = "a", Hero = "Hero_Smacky", StormLeft = 3, StormsDone = 1 };
        RWPlayer back = RWRun.Decode(RWRun.Encode(storm)), old = RWRun.Decode("k\tn\tHero_Smacky\t5\t\t\t0");
        Check("Bottled Storm: a storm blowing is saved with the campaign, and a line saved before it reads as none", back != null && back.StormLeft == 3 && back.StormsDone == 1 && old != null && old.StormLeft == 0 && old.StormsDone == 0 && old.Pure == 5,
            back != null ? back.StormLeft + "/" + back.StormsDone : "no line");
    }

    // ---------------------------------------------------------------- effects, on a map

    static readonly List<float> trinketMultis = new List<float>();
    static readonly Entity_Master.Oninteracts recordTrinket = hit =>
    {
        float m;
        if (hit != null && hit.Type == "Hurt" && hit.Data != null && hit.Data.TryGetValue("Damagemulti", out m)) trinketMultis.Add(m);
    };

    static string BuyTrinket(RWPlayer p, string name, int times)
    {
        string why = "";
        for (int i = 0; i < times && why == ""; i++)
        {
            p.Pure = Mathf.Max(p.Pure, RWCardinals.PriceFor(p, "Cardinal_" + name));     // C1's ramp grows with every copy held
            why = RWCardinals.Buy(p, "Cardinal_" + name);
        }
        return why;
    }

    static Link_Command WallButton(Entity_Master v, string wall)
    {
        if (v == null || v.Commandlistserver == null) return null;
        foreach (KeyValuePair<string, Link_Command> c in v.Commandlistserver)
            if (c.Value != null && c.Key.StartsWith("Buildoption") && c.Value.Goalstring == wall) return c.Value;
        return null;
    }

    static IEnumerator GapsTrinkets()
    {
        yield return BackOnBoat();
        RWRun.Clear();
        Macro_Main.Instance.Selectcharacter("Hero_Smacky");
        yield return new WaitForSecondsRealtime(1f);
        yield return ToMap(Seed, RWChart.Calm);
        long me = RWMine.Session;
        RWPlayer player = RWRun.For(me);
        Entity_Master vessel = RWRun.Vessel(me);
        Entity_Master core = RWUtil.PlayerCore();
        if (vessel == null || core == null) { Setup("trinkets", "no vessel or no Basin"); yield break; }
        Rich(me, 5000f);
        List<Vector3> used = new List<Vector3>();
        List<Entity_Master> known = new List<Entity_Master>();
        Entity_Master archer = null;
        yield return BuildOne(vessel, "Tower_Smackyarcher", used, known, (m, c) => archer = m);

        // Brass Spyglass: Smacky fights in melee.
        float range0 = Calc(vessel, "Range");
        string why = BuyTrinket(player, "Spyglass", 1);
        yield return new WaitForSeconds(1.2f);
        Check("Brass Spyglass: a melee vessel reaches 0.25 tile farther", why == "" && !RWTrinkets.Ranged(vessel) && Mathf.Abs(Calc(vessel, "Range") - range0 - RWTrinkets.SpyglassMelee) < 0.001f, "'" + why + "', Range " + range0 + " -> " + Calc(vessel, "Range"));

        // Gutting Knife x2: a healthy foe takes the vessel's hit 40% harder, one below half its health doesn't.
        why = BuyTrinket(player, "Knife", 2);
        yield return new WaitForSeconds(0.5f);
        yield return NewFoe();
        Entity_Master foe = lastFoe;
        if (foe == null) Setup("Gutting Knife", "no foe");
        else
        {
            trinketMultis.Clear();
            ModKitUtil.Front(foe, recordTrinket);
            Hurt(vessel, foe, 1f);
            float healthy = trinketMultis.Count > 0 ? trinketMultis[0] : -1f;
            Hurt(core, foe, Hp(foe) - HpMax(foe) * 0.4f);
            trinketMultis.Clear();
            Hurt(vessel, foe, 1f);
            float weak = trinketMultis.Count > 0 ? trinketMultis[0] : -1f;
            ModKitUtil.Unfront(foe, recordTrinket);
            Check("Gutting Knife x2: the vessel hits a foe above half health 40% harder, and one below half as it is", why == "" && Mathf.Abs(healthy - 1.4f) < 0.001f && Mathf.Abs(weak - 1f) < 0.001f, "'" + why + "', Damagemulti " + healthy + " then " + weak);
        }
        ClearFoes();

        if (archer == null) Setup("Oil Can, Tar Bucket", "no archer built");
        else
        {
            // Oil Can x2.
            float as0 = Multi(archer, "Attackspeed");
            why = BuyTrinket(player, "Oilcan", 2);
            yield return new WaitForSeconds(1.2f);
            Check("Oil Can x2: the holder's towers attack 10% faster", why == "" && Mathf.Abs(Multi(archer, "Attackspeed") - as0 - 2f * RWTrinkets.OilcanEach) < 0.001f, "'" + why + "', Attackspeed Multi " + as0 + " -> " + Multi(archer, "Attackspeed"));

            // Tar Bucket x2: nothing for 4 s after a hit, then 2% of its health a second.
            why = BuyTrinket(player, "Tar", 2);
            yield return NewFoe();
            if (lastFoe == null) Setup("Tar Bucket", "no foe");
            else
            {
                Hurt(lastFoe, archer, HpMax(archer) * 0.4f);
                ClearFoes();
                float hit = Hp(archer), mended0 = RWTrinkets.TarMended;
                yield return new WaitForSeconds(3.5f);
                float early = Hp(archer);
                yield return new WaitForSeconds(3f);
                float late = Hp(archer), want = HpMax(archer) * 2f * RWTrinkets.TarEach;
                Check("Tar Bucket x2: a hit tower waits 4 s, then mends 2% of its health a second", why == "" && early <= hit + 0.01f && late - early >= want * 1.5f && late - early <= want * 3.5f && RWTrinkets.TarMended > mended0,
                    "'" + why + "', health " + hit + " after the hit, " + early + " at 3.5 s, " + late + " at 6.5 s (2% is " + want + ")");
            }
        }

        // Spare Plank: Smacky's wall button costs nothing; the wall built there is the plank, the next one is paid for; the plank
        // falls and the button is free again.
        why = BuyTrinket(player, "Plank", 1);
        yield return new WaitForSeconds(0.5f);
        Link_Command button = WallButton(vessel, "Tower_Smackywall");
        float free = button != null ? button.Goalvalue : -1f;
        Entity_Master plank = null, second = null;
        float paid1 = -1f, paid2 = -1f;
        int planked0 = RWTrinkets.Planked;
        yield return BuildOne(vessel, "Tower_Smackywall", used, known, (m, c) => { plank = m; paid1 = c; });
        yield return new WaitForSeconds(0.3f);
        float after = button != null ? button.Goalvalue : -1f;
        yield return BuildOne(vessel, "Tower_Smackywall", used, known, (m, c) => { second = m; paid2 = c; });
        Check("Spare Plank: the first wall costs nothing and is the plank; the button's price is back for the next, which is paid for",
            why == "" && free == 0f && plank != null && paid1 < 0.01f && RWTrinkets.Planked == planked0 + 1 && after > 0.5f && second != null && paid2 > 1f,
            "'" + why + "', button " + free + " -> " + after + ", paid " + paid1 + " then " + paid2 + ", planks " + (RWTrinkets.Planked - planked0));
        if (plank != null)
        {
            Helper_Entitytools.Despawn(plank.Beingserver, true, "RWTest");
            yield return new WaitForSeconds(0.5f);
            Check("Spare Plank: once the plank falls, the wall button is free again", button != null && button.Goalvalue == 0f, "button " + (button != null ? button.Goalvalue.ToString() : "-"));
        }

        // Pearl Oyster: +1 a Tide while nothing is bought; a buy stops it for the map.
        why = BuyTrinket(player, "Oyster", 1);
        int p0 = player.Pure;
        RWTrinkets.Tide(1);
        int p1 = player.Pure;
        BuyTrinket(player, "Knife", 1);
        int p2 = player.Pure;
        RWTrinkets.Tide(2);
        Check("Pearl Oyster: +1 Pure Aura a Tide until the next buy, then none on this map", why == "" && p1 == p0 + 1 && player.Pure == p2, "'" + why + "', Pure " + p0 + " -> " + p1 + "; after a buy " + p2 + " -> " + player.Pure);

        // Punch Card, once the Oyster is let go: a Trinket at 12 (and C1's ramp) costs 20% less.
        string clash = BuyTrinket(player, "Punch", 1);
        player.Trinkets.RemoveAll(t => t.StartsWith("Cardinal_Oyster"));
        why = BuyTrinket(player, "Punch", 1);
        player.Pure = Mathf.Max(player.Pure, 400);
        int before = player.Pure, knifeWant = Mathf.FloorToInt((12 + RWCardinals.RampFor(player)) * 0.8f + 0.0001f);
        string knife = RWCardinals.Buy(player, "Cardinal_Knife");
        Check("Punch Card: refused beside Pearl Oyster; held, a Trinket at 12 and the ramp costs 20% less", clash == "Clashes with Pearl Oyster" && why == "" && knife == "" && before - player.Pure == knifeWant, "'" + clash + "', '" + why + "', '" + knife + "', paid " + (before - player.Pure) + " of " + knifeWant);

        // Bottled Storm: 5 Tides of 25% more damage, then a rank or Pure Aura 20.
        TakeRanks(player, "Any", "Salt", 1);
        yield return NewFoe();
        if (lastFoe == null) Setup("Bottled Storm", "no foe");
        else
        {
            float hp0 = Hp(vessel);
            Hurt(lastFoe, vessel, 4f);
            float plain = hp0 - Hp(vessel);
            why = BuyTrinket(player, "Storm", 1);
            int left = player.StormLeft;
            yield return new WaitForSeconds(0.5f);
            hp0 = Hp(vessel);
            Hurt(lastFoe, vessel, 4f);
            float lost = hp0 - Hp(vessel);
            int ranks0 = player.Directions.Count, pure0 = player.Pure, storms0 = RWTrinkets.Storms;
            for (int w = 1; w <= RWTrinkets.StormTides; w++) RWTrinkets.Tide(w);
            bool rewarded = player.Directions.Count == ranks0 + 1 || player.Pure == pure0 + RWTrinkets.StormPure;
            Check("Bottled Storm: 5 Tides of 25% more damage taken, then a Direction one rank up (or Pure Aura 20)", why == "" && left == RWTrinkets.StormTides && plain > 0f && Mathf.Abs(lost - plain * (1f + RWTrinkets.StormTaken)) < 0.01f
                && player.StormLeft == 0 && player.StormsDone == 1 && RWTrinkets.Storms == storms0 + 1 && rewarded,
                "'" + why + "', Tides " + left + ", a 4 hit took " + plain + " then " + lost + ", then left " + player.StormLeft + ", passed " + player.StormsDone + ", Directions " + ranks0 + " -> " + player.Directions.Count + ", Pure " + pure0 + " -> " + player.Pure);
            ModKitUtil.Heal(vessel, vessel, HpMax(vessel));
        }

        // Oilskin Coat: a hit that slides off does nothing.
        why = BuyTrinket(player, "Oilskin", 1);
        yield return new WaitForSeconds(0.5f);
        if (lastFoe == null || !ModKitUtil.Alive(lastFoe)) yield return NewFoe();
        if (lastFoe == null) Setup("Oilskin Coat", "no foe");
        else
        {
            RWTrinkets.ForceSlide = true;
            int slid0 = RWTrinkets.Slid;
            float hp0 = Hp(vessel);
            Hurt(lastFoe, vessel, 4f);
            RWTrinkets.ForceSlide = false;
            Check("Oilskin Coat: a hit that slides off the vessel does nothing", why == "" && RWTrinkets.Slid == slid0 + 1 && Mathf.Abs(Hp(vessel) - hp0) < 0.001f, "'" + why + "', health " + hp0 + " -> " + Hp(vessel));
        }
        ClearFoes();

        // Gull Feather: an elite the vessel drowns lifts it for 8 s.
        why = BuyTrinket(player, "Gull", 1);
        Upgradeproperties elite = Bl_Address.Getload("Upgrade", "Enemy_Health_Upgradeobject") as Upgradeproperties;
        foreach (string id in RWDifficulty.PackModifiers) if (elite == null) elite = Bl_Address.Getload("Upgrade", id) as Upgradeproperties;
        yield return NewFoe();
        if (lastFoe == null || elite == null) Setup("Gull Feather", "no foe, or no modifier loaded: " + (lastFoe != null) + ", " + (elite != null));
        else
        {
            Helper_Entitytools.Upgrade(lastFoe, elite, Helper_Entitytools.Upgradecheck.Forced, null);
            yield return new WaitForSeconds(0.3f);
            float d0 = Multi(vessel, "Damage"), s0 = Multi(vessel, "Speed");
            int feathered0 = RWTrinkets.Feathered;
            Hurt(vessel, lastFoe, Hp(lastFoe) + 50f);
            yield return new WaitForSeconds(0.5f);
            float d1 = Multi(vessel, "Damage"), s1 = Multi(vessel, "Speed");
            yield return new WaitForSeconds(RWTrinkets.GullLasts);
            Check("Gull Feather: an elite the vessel drowns gives it 20% damage and 15% speed for 8 s", why == "" && RWTrinkets.Feathered == feathered0 + 1 && Mathf.Abs(d1 - d0 - RWTrinkets.GullDamage) < 0.001f && Mathf.Abs(s1 - s0 - RWTrinkets.GullSpeed) < 0.001f
                && Mathf.Abs(Multi(vessel, "Damage") - d0) < 0.001f && Mathf.Abs(Multi(vessel, "Speed") - s0) < 0.001f,
                "'" + why + "', Damage Multi " + d0 + " -> " + d1 + " -> " + Multi(vessel, "Damage") + ", Speed " + s0 + " -> " + s1 + " -> " + Multi(vessel, "Speed"));
        }
        ClearFoes();

        // Cargo Crate x2: two more cards on a shelf rolled after.
        why = BuyTrinket(player, "Crate", 2);
        List<string> big = RWCardinals.ShelfFor("RWTest crate", RWCampaign.Seed, 1, 77, c => false);
        Check("Cargo Crate x2: VENDCINT's shelf holds two more", why == "" && RWCardinals.Stacked("Crate") == 2 && big.Count == RWCardinals.OnShelf + 2, "'" + why + "', " + big.Count + " cards");

        // Signal Flare x2: 30 damage at the toughest foe near the Basin.
        why = BuyTrinket(player, "Flare", 2);
        Entity_Master near = FoeNear(ModKitUtil.Pos(core), 2f, 6f, null);
        int flares0 = RWTrinkets.Flares;
        float flared0 = RWTrinkets.Flared;
        yield return Until(() => RWTrinkets.Flares > flares0, 4f);
        Check("Signal Flare x2: a flare of 30 at a foe within 8 tiles of the Basin", why == "" && near != null && RWTrinkets.Flares == flares0 + 1 && Mathf.Abs(RWTrinkets.Flared - flared0 - 2f * RWTrinkets.FlareDamage) < 0.01f,
            "'" + why + "', foe " + (near != null) + ", flares " + (RWTrinkets.Flares - flares0) + ", damage " + (RWTrinkets.Flared - flared0));
        ClearFoes();

        // Tin Whistle: four foes left, none marked; three, each slowed and hit 25% harder.
        why = BuyTrinket(player, "Whistle", 1);
        Vector3 field = RWUtil.EnemyCores().Count > 0 ? RWUtil.EnemyCores()[0].Entityposition.Value : vessel.Entityposition.Value;
        List<Entity_Master> last = new List<Entity_Master>();
        for (int i = 0; i < 4; i++) { Entity_Master f = FoeNear(field, 1f, 10f, null); if (f != null) last.Add(f); }
        RWTrinkets.ForceLast = true;
        yield return new WaitForSeconds(0.6f);
        int marked4 = 0;
        foreach (Entity_Master f in last) if (RWTrinkets.IsWhistled(f)) marked4++;
        float plain3 = -1f, lost3 = -1f;
        if (last.Count > 0)
        {
            float hp0 = Hp(last[0]);
            Hurt(core, last[0], 4f);
            plain3 = hp0 - Hp(last[0]);
        }
        if (last.Count == 4) Helper_Entitytools.Despawn(last[3].Beingserver, true, "RWTest");
        yield return new WaitForSeconds(0.6f);
        int marked3 = 0;
        float slowed = 0f;
        for (int i = 0; i < 3 && i < last.Count; i++)
            if (RWTrinkets.IsWhistled(last[i])) { marked3++; slowed = Multi(last[i], "Speed"); }
        if (last.Count > 0 && ModKitUtil.Alive(last[0]))
        {
            float hp0 = Hp(last[0]);
            Hurt(core, last[0], 4f);
            lost3 = hp0 - Hp(last[0]);
        }
        RWTrinkets.ForceLast = false;
        Check("Tin Whistle: with four foes left none is marked; the last three move 30% slower and take 25% more damage", why == "" && last.Count == 4 && marked4 == 0 && marked3 == 3 && slowed < 1f - RWTrinkets.WhistleSlow + 0.05f && plain3 > 0f && Mathf.Abs(lost3 - plain3 * (1f + RWTrinkets.WhistleTaken)) < 0.01f,
            "'" + why + "', foes " + last.Count + ", marked " + marked4 + " then " + marked3 + ", Speed Multi " + slowed + ", a 4 hit took " + plain3 + " then " + lost3);
        ClearFoes();

        // Cork Lifebuoy: the game's own last stand, then the Lifebuoy's.
        why = BuyTrinket(player, "Cork", 1);
        yield return new WaitForSeconds(0.5f);
        yield return NewFoe();
        if (lastFoe == null) Setup("Cork Lifebuoy", "no foe");
        else
        {
            int corked0 = RWTrinkets.Corked;
            Hurt(lastFoe, core, Hp(core) + 50f);
            float first = Hp(core);
            bool gameFirst = RWTrinkets.Corked == corked0;
            float again = -1f;
            if (gameFirst && first > 0f)
            {
                Hurt(lastFoe, core, Hp(core) + 50f);
                again = Hp(core);
            }
            Check("Cork Lifebuoy: the game's last stand takes the first killing blow, the Lifebuoy the second, each at 1 health", why == "" && gameFirst && Mathf.Approximately(first, 1f) && Mathf.Approximately(again, 1f) && RWTrinkets.Corked == corked0 + 1,
                "'" + why + "', health " + first + " then " + again + ", the game's stand first " + gameFirst);
            ModKitUtil.Heal(core, core, HpMax(core));
        }
        ClearFoes();

        // Rubber Duck x3: the vessel comes back in 40% of the Basin's time, which is whole again after.
        why = BuyTrinket(player, "Duck", 3);
        yield return new WaitForSeconds(0.5f);
        float respawn0 = Calc(core, "Respawn");
        int ducked0 = RWTrinkets.Ducked;
        Entity_Master old = RWRun.Vessel(me), back = null;
        float died = Time.time;
        if (old != null) Helper_Entitytools.Despawn(old.Beingserver, true, "RWTest");
        yield return Until(() => (back = RWRun.Vessel(me)) != null && back != old && ModKitUtil.Alive(back), 20f);
        float took = Time.time - died, want3 = respawn0 * (1f - RWTrinkets.DuckMost);
        Check("Rubber Duck x3: the vessel comes back 60% sooner, and the Basin's own time is whole again", why == "" && back != null && RWTrinkets.Ducked == ducked0 + 1 && Mathf.Abs(took - want3) < 1.5f && Mathf.Abs(Calc(core, "Respawn") - respawn0) < 0.001f,
            "'" + why + "', back in " + RWUtil.F(took) + " s (want " + RWUtil.F(want3) + " of " + RWUtil.F(respawn0) + "), Respawn now " + Calc(core, "Respawn"));
        ClearFoes();
        yield return ToBoat();
    }
}
#endif
