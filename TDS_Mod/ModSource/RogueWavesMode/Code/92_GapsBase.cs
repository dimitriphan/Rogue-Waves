#if ROGUEWAVESMODE_GAPS
// The base vessels' Directions that no held stat carries (23_BaseDirections.cs, docs/scope/11), part "base" of the gaps build,
// one launch, solo: each vessel on a calm map, its Direction taken at rank III and measured on what it touches, then back to
// the boat for the next.
public static partial class RWTest
{
    static void TakeBase(RWPlayer p, string vessel, string name)
    {
        for (int r = 1; r <= 3; r++)
            RWDirections.Take(p, Bl_Address.Getload("Upgrade", RogueWavesModeKitDirections.Id(vessel, name + (r > 1 ? r.ToString() : ""))) as Upgradeproperties);
    }

    static float Calc(Entity_Master e, string stat) { Link_Stats s; return e != null && e.Statsserver != null && e.Statsserver.TryGetValue(stat, out s) ? s.Value_Calc : -1f; }

    static IEnumerator GapsBase()
    {
        foreach (string hero in new[] { "Hero_Smacky", "Hero_Snosno", "Hero_Boutman", "Hero_Fie", "Hero_Buster" })
        {
            yield return BackOnBoat();
            RWRun.Clear();
            Macro_Main.Instance.Selectcharacter(hero);
            yield return new WaitForSecondsRealtime(1f);
            yield return ToMap(Seed, RWChart.Calm);
            long me = RWMine.Session;
            RWPlayer player = RWRun.For(me);
            Entity_Master vessel = RWRun.Vessel(me);
            if (vessel == null || vessel.Being.Value.ToString() != hero) { Setup("base " + hero, "no vessel"); continue; }
            Rich(me, 5000f);
            if (hero == "Hero_Smacky") yield return Stepped(BaseTrowel(me, player, vessel));
            else if (hero == "Hero_Snosno") yield return Stepped(BaseShovel(me, player, vessel));
            else if (hero == "Hero_Boutman") yield return Stepped(BaseWorm(me, player, vessel));
            else if (hero == "Hero_Fie") yield return Stepped(BaseSign(me, player, vessel));
            else yield return Stepped(BaseLeash(me, player, vessel));
            ClearFoes();
            yield return ToBoat();
        }
    }

    // Mason's Trowel: a wall 60% sturdier; hit, it waits 3 s, then mends 1 a second.
    static IEnumerator BaseTrowel(long me, RWPlayer player, Entity_Master vessel)
    {
        Entity_Master wall = null;
        yield return BuildOne(vessel, "Tower_Smackywall", new List<Vector3>(), new List<Entity_Master>(), (m, c) => wall = m);
        if (wall == null) { Setup("Mason's Trowel", "no wall built"); yield break; }
        float p0 = Pmulti(wall, "Health");
        TakeBase(player, "Smacky", "Trowel");
        yield return new WaitForSeconds(1.2f);
        Check("Mason's Trowel rank III: the holder's walls have 60% more health", Mathf.Abs(Pmulti(wall, "Health") - p0 - RWBase.TrowelHealth[3]) < 0.001f && Mathf.Abs(Hp(wall) - HpMax(wall)) < 0.05f,
            "Health Pmulti " + p0 + " -> " + Pmulti(wall, "Health") + ", health " + Hp(wall) + " of " + HpMax(wall));
        yield return NewFoe();
        if (lastFoe == null) { Setup("Mason's Trowel mend", "no foe"); yield break; }
        float mended0 = RWBase.Mended;
        Hurt(lastFoe, wall, Mathf.Min(10f, HpMax(wall) * 0.5f));
        float hit = Hp(wall);
        yield return new WaitForSeconds(2.5f);
        float early = RWBase.Mended - mended0, before = Hp(wall);
        yield return new WaitForSeconds(2.8f);
        float late = RWBase.Mended - mended0;
        Check("Mason's Trowel rank III: a hit wall mends 1 a second once 3 s pass with no hit", early < 0.001f && late >= 1.5f && late <= 3.01f && Hp(wall) > before,
            "health " + hit + " after the hit, " + before + " at 2.5 s, " + Hp(wall) + " at 5.3 s; mended " + early + " then " + late);
    }

    // Snow Shovel: Sno Sno walks four tiles; the snow on the tiles he left is at 55.
    static IEnumerator BaseShovel(long me, RWPlayer player, Entity_Master vessel)
    {
        TakeBase(player, "Snosno", "Shovel");
        yield return new WaitForSeconds(0.5f);
        Beingobject probe = ModKitUtil.Load<Beingobject>("Mob", "Tower_Snosnobox");
        Vector3 to = probe != null ? RogueWavesModeKitTest.Spot(vessel.Entityposition.Value, probe, vessel.Entitydata.Value.Team, 4f, 7f, new List<Vector3>(), "a walk") : new Vector3(float.NaN, 0f, 0f);
        if (float.IsNaN(to.x)) { Setup("Snow Shovel", "no open ground to walk to"); yield break; }
        int topped0 = RWBase.Topped;
        Vector3 from = vessel.Entityposition.Value;
        ModKitUtil.MoveTo(vessel, null, to, 0f);
        yield return Until(() => vessel == null || ModKitUtil.Flat(vessel.Entityposition.Value, to) < 0.6f, 10f);
        yield return new WaitForSeconds(1.2f);
        int deep = 0, shallow = 0;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
        {
            if (e == null || e.Beingserver == null || e.Beingserver.Entity == null || e.Beingserver.Entity.Name != "Sno") continue;
            float level = ModKitUtil.Stat(e, "Level", -1f);
            if (level >= RWBase.ShovelLevel[3] - 1f) deep++;
            else if (level >= 0f) shallow++;
        }
        Check("Snow Shovel rank III: the snow Sno Sno leaves is at level 55, where the game's is 20", RWBase.Topped - topped0 >= 2 && deep >= 2,
            "walked " + RWUtil.F(ModKitUtil.Flat(from, vessel.Entityposition.Value)) + " tiles; topped " + (RWBase.Topped - topped0) + ", snow at 54 or more " + deep + ", lower " + shallow);
    }

    // Worm Tin: Boutbro's three costs 30% lower, and the boat's price follows at once.
    static IEnumerator BaseWorm(long me, RWPlayer player, Entity_Master vessel)
    {
        Entity_Master house = null, bro = null;
        yield return BuildOne(vessel, "Tower_Boutmanhouse", new List<Vector3>(), new List<Entity_Master>(), (m, c) => house = m);
        if (house == null) { Setup("Worm Tin", "no Bouthouse built"); yield break; }
        yield return Until(() => (bro = MineLast("Mob_Boutmanbro", me)) != null, 60f);
        if (bro == null) { Setup("Worm Tin", "no Boutbro after 60 s"); yield break; }
        yield return new WaitForSeconds(1f);
        float f0 = Multi(bro, "Fishcost"), b0 = Multi(bro, "Barrelcost"), o0 = Multi(bro, "Boatcost");
        Link_Command boat;
        bool has = bro.Commandlistserver.TryGetValue("Buildoption3", out boat) && boat != null;
        float price0 = has ? boat.Goalvalue : -1f;
        int recount0 = RWBase.Recounted;
        TakeBase(player, "Boutman", "Wormtin");
        yield return new WaitForSeconds(1.2f);
        float cut = RWBase.WormCut[3];
        Check("Worm Tin rank III: Boutbro's fish, barrel and boat costs are 30% lower", Mathf.Abs(Multi(bro, "Fishcost") - f0 + cut) < 0.001f && Mathf.Abs(Multi(bro, "Barrelcost") - b0 + cut) < 0.001f && Mathf.Abs(Multi(bro, "Boatcost") - o0 + cut) < 0.001f,
            "Multi " + f0 + "/" + b0 + "/" + o0 + " -> " + Multi(bro, "Fishcost") + "/" + Multi(bro, "Barrelcost") + "/" + Multi(bro, "Boatcost"));
        // The first boat costs 1 x Boatcost (kit docs/17 §5.1): after the recount it is the lowered cost.
        Check("Worm Tin: Boutbro's price follows at once (his own recount)", has && RWBase.Recounted > recount0 && Mathf.Abs(boat.Goalvalue - Calc(bro, "Boatcost")) < 0.01f && boat.Goalvalue < price0 - 0.01f,
            "boat price " + price0 + " -> " + (has ? boat.Goalvalue.ToString() : "-") + ", Boatcost " + Calc(bro, "Boatcost") + ", recounts " + (RWBase.Recounted - recount0));
    }

    // Open Sign: a Rest Stop with Regulars makes 7 regulars; the eighth is no regular.
    static IEnumerator BaseSign(long me, RWPlayer player, Entity_Master vessel)
    {
        Entity_Master shop = null;
        yield return BuildOne(vessel, "Tower_Fieshop", new List<Vector3>(), new List<Entity_Master>(), (m, c) => shop = m);
        Upgradeproperties regulars = Bl_Address.Getload("Upgrade", RWBase.Regulars) as Upgradeproperties;
        Entity_Ai_Tower_Fieshop ai = shop != null ? shop.GetComponent<Entity_Ai_Tower_Fieshop>() : null;
        if (ai == null || regulars == null) { Setup("Open Sign", "no Rest Stop, or no Regulars: " + (shop != null) + ", " + (regulars != null)); yield break; }
        Helper_Entitytools.Upgrade(shop, regulars, Helper_Entitytools.Upgradecheck.Forced, null);
        yield return new WaitForSeconds(0.5f);
        int opened0 = RWBase.Opened;
        TakeBase(player, "Fie", "Opensign");
        yield return new WaitForSeconds(1.2f);
        List<Entity_Master> foes = new List<Entity_Master>();
        Vector3 near = RWUtil.EnemyCores().Count > 0 ? RWUtil.EnemyCores()[0].Entityposition.Value : vessel.Entityposition.Value;
        for (int i = 0; i < 9; i++)
        {
            Entity_Master f = FoeNear(near, 1f, 12f, null);
            if (f != null) foes.Add(f);
        }
        yield return new WaitForSeconds(0.5f);
        int made = 0, calls = 0;
        if (ai.Duration != null)
        {
            // Fie herself may already be a regular from a throw: asked first, she is one either way.
            ai.Duration(vessel, 1f);
            made = 1;
            foreach (Entity_Master f in foes)
            {
                if (!ModKitUtil.Alive(f)) continue;
                calls++;
                if (ai.Duration(f, 1f) > 999f) made++;
            }
        }
        float shared = regulars.Defaultparams.ContainsKey("Perm_Count") ? regulars.Defaultparams["Perm_Count"] : -1f;
        int hooks = ai.Duration != null ? ai.Duration.GetInvocationList().Length : 0;
        Check("Open Sign rank III: a Rest Stop with Regulars keeps 7 regulars, the game's own count left at 4", RWBase.Opened > opened0 && calls >= 7 && made == RWBase.SignRegulars[3] && Mathf.Approximately(shared, 4f) && hooks == 1 && ai.Effectvis == "Debuff_Buffed2",
            "regulars " + made + " (Fie and " + calls + " foes asked), Perm_Count on the sheet " + shared + ", Duration hooks " + hooks + ", look " + ai.Effectvis);
    }

    // Long Leash: while Buster dashes his reach stays whole, and a dash hit lands 30% harder.
    static readonly List<float> dashMultis = new List<float>();
    static readonly Entity_Master.Oninteracts recordDash = hit =>
    {
        float m;
        if (hit != null && hit.Type == "Hurt" && hit.Data != null && hit.Data.TryGetValue("Damagemulti", out m)) dashMultis.Add(m);
    };

    static IEnumerator BaseLeash(long me, RWPlayer player, Entity_Master vessel)
    {
        TakeBase(player, "Buster", "Leash");
        yield return new WaitForSeconds(0.5f);
        float range0 = Calc(vessel, "Range");
        yield return NewFoe();
        Entity_Master foe = lastFoe;
        if (foe == null) { Setup("Long Leash", "no foe"); yield break; }
        Link_Command dash;
        if (!vessel.Commandlistserver.TryGetValue("Movelinear", out dash) || dash == null) { Setup("Long Leash", "Buster has no Movelinear"); yield break; }
        // A server-side dash, as the Speed Pads start one (kit docs/17 §7): each way in turn until one runs.
        foreach (Vector3 way in new[] { Vector3.right, Vector3.forward, Vector3.left, Vector3.back })
        {
            dash.Goalposition = way;
            Helper_Entitytools.Updatecommand(vessel, "Movelinear", "", true, true);
            yield return Until(() => ModKitUtil.State(vessel) == "Movelinear", 0.6f);
            if (ModKitUtil.State(vessel) == "Movelinear") break;
        }
        if (ModKitUtil.State(vessel) != "Movelinear") { Setup("Long Leash", "no dash started"); yield break; }
        float low = float.MaxValue;
        int frames = 0, dashed0 = RWBase.Dashed;
        dashMultis.Clear();
        ModKitUtil.Front(foe, recordDash);
        bool struck = false;
        while (ModKitUtil.State(vessel) == "Movelinear" && frames < 240)
        {
            low = Mathf.Min(low, Calc(vessel, "Range"));
            if (!struck && frames >= 3)
            {
                Helper_Entitytools.Interacthurt(vessel, foe, null);
                struck = true;
            }
            frames++;
            yield return null;
        }
        ModKitUtil.Unfront(foe, recordDash);
        float multi = dashMultis.Count > 0 ? dashMultis[0] : -1f;
        Check("Long Leash rank I: Buster's reach stays whole while he dashes", frames > 0 && low >= range0 - 0.001f, "Range " + range0 + ", lowest while dashing " + low + " over " + frames + " frames");
        Check("Long Leash rank III: a dash hit lands 30% harder", struck && RWBase.Dashed > dashed0 && Mathf.Abs(multi - (1f + RWBase.LeashHit[3])) < 0.001f,
            "struck " + struck + ", Damagemulti " + multi + ", leashed hits " + (RWBase.Dashed - dashed0));
    }
}
#endif
