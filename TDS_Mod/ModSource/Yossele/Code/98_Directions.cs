#if YOSSELE_SELFTEST
// Debug build only: Yossele's five Directions from scope 11 (17_Directions.cs), run from section 12b of 99_SelfTest.cs on
// the host, each granted the way the mode grants them (ModKitDirections.Grant) and taken off after. (Written 2026-11-05,
// not yet run on the PC.)
public static partial class YosseleSelfTest
{
    static readonly List<float> laserMultis = new List<float>();
    static readonly Entity_Master.Oninteracts recordLaser = RecordLaser;

    // Each laser hit on the watched foe, after the attacker's side has had it (Deep Breath scales it there).
    static void RecordLaser(Link_Interact hit)
    {
        float m;
        if (hit != null && hit.Type == "Hurt" && hit.Data != null && hit.Data.ContainsKey(YosseleData.LaserTag) && hit.Data.TryGetValue("Damagemulti", out m))
            laserMultis.Add(m);
    }

    static Upgradeproperties DirectionOf(string name) { return Bl_Address.Getload("Upgrade", ModKitDirections.Id("Yossele", name)) as Upgradeproperties; }

    static void Drop(Entity_Master hm, YosseleData.Direction[] ranks)
    {
        foreach (YosseleData.Direction d in ranks)
            Helper_Entitytools.Removeupgrade(hm, DirectionOf(d.Name));
    }

    // A sturdy foe 2.5-3.5 tiles off, kept at full health, until Yossele's laser has hit it 'hits' times (12 s at most).
    static IEnumerator Beam(Entity_Master hm, int hits, Entity_Master[] foe)
    {
        laserMultis.Clear();
        foe[0] = SpawnFoe("Enemy_Maul", ModKitUtil.Pos(hm), 3.5f, 2.5f) ?? SpawnFoe("Enemy_Dagger", ModKitUtil.Pos(hm), 3.5f, 2.5f);
        float until = Time.time + 12f;
        while (foe[0] != null && ModKitUtil.Alive(foe[0]) && laserMultis.Count < hits && Time.time < until)
        {
            ModKitUtil.Front(foe[0], recordLaser);
            float max = ModKitUtil.MaxHealth(foe[0]);
            if (max > 0f)
                SetHealth(foe[0], max);
            yield return null;
        }
    }

    // Waits, keeping the foe at full health so Yossele's laser doesn't fell it meanwhile.
    static IEnumerator Hold(Entity_Master foe, float seconds)
    {
        float until = Time.time + seconds;
        while (Time.time < until)
        {
            if (foe != null && ModKitUtil.Alive(foe) && ModKitUtil.MaxHealth(foe) > 0f)
                SetHealth(foe, ModKitUtil.MaxHealth(foe));
            yield return null;
        }
    }

    // Waits until Deep Breath has charged: no foe in reach for its 2 s past the laser's reload.
    static IEnumerator Breathe(YosseleHeroBrain hero)
    {
        ClearFoes();
        yield return Until(() => hero.Boosts.Charged, 8f);
        yield return Wait(0.2f);
    }

    static IEnumerator Directions(YosseleHeroBrain hero, long owner, YosselePlayer player, List<YosseleBuilding> known, YosseleBuilding[] got, YosseleHengeBrain henge)
    {
        Entity_Master hm = hero.Master;
        Entity_Master[] foe = new Entity_Master[1];

        // Deep Breath: the first beam after a rest hits harder; rank III reaches farther while it waits.
        {
            ModKitDirections.Grant(hm, DirectionOf(YosseleData.Deepbreaths[0].Name));
            yield return Wait(1.2f);
            Check("Deep Breath reads back", player.Breath == 1, "rank " + player.Breath);
            yield return Breathe(hero);
            int breaths = hero.Boosts.Breaths;
            yield return Beam(hm, 2, foe);
            bool two = laserMultis.Count >= 2;
            float ratio = two && laserMultis[1] > 0f ? laserMultis[0] / laserMultis[1] : 0f;
            Check("Deep Breath: the first beam after a rest deals +40%, the next one doesn't", two && Mathf.Abs(ratio - (1f + YosseleData.BreathBonus[0])) < 0.02f && hero.Boosts.Breaths == breaths + 1,
                "hits " + laserMultis.Count + (two ? ", multi " + F(laserMultis[0]) + " then " + F(laserMultis[1]) : "") + ", charged beams +" + (hero.Boosts.Breaths - breaths));
            if (foe[0] != null && ModKitUtil.Alive(foe[0])) Kill(foe[0]);
            ModKitDirections.Grant(hm, DirectionOf(YosseleData.Deepbreaths[1].Name));
            yield return Breathe(hero);
            float range0 = Calc(hm, "Range");
            bool reachingAt2 = hero.Boosts.Reaching;
            ModKitDirections.Grant(hm, DirectionOf(YosseleData.Deepbreaths[2].Name));
            yield return Wait(1.2f);
            float range1 = Calc(hm, "Range");
            Check("Deep Breath rank III: +1 Range while it waits", player.Breath == 3 && !reachingAt2 && hero.Boosts.Reaching && Mathf.Abs(range1 - range0 - YosseleData.BreathRange) < 0.01f,
                "rank " + player.Breath + ", range " + F(range0) + " -> " + F(range1));
            yield return Beam(hm, 1, foe);
            float multi = laserMultis.Count > 0 ? laserMultis[0] : 0f;
            yield return Wait(0.2f);
            float range2 = Calc(hm, "Range");
            Check("... and the beam it lets fly deals +100%, then the reach goes", laserMultis.Count > 0 && multi >= 1.95f && !hero.Boosts.Reaching && Mathf.Abs(range2 - range0) < 0.01f,
                "multi " + F(multi) + ", range after " + F(range2));
            if (foe[0] != null && ModKitUtil.Alive(foe[0])) Kill(foe[0]);
            Drop(hm, YosseleData.Deepbreaths);
            yield return Wait(1.2f);
            Check("Deep Breath gone with its ranks", player.Breath == 0 && !hero.Boosts.Reaching && Mathf.Abs(Calc(hm, "Range") - range0) < 0.01f, "rank " + player.Breath + ", range " + F(Calc(hm, "Range")));
        }

        // Sap Bucket: each laser hit slows the foe; rank III, the holder's buildings deal 10% more to it.
        {
            ModKitDirections.Grant(hm, DirectionOf(YosseleData.Sapbuckets[0].Name));
            yield return Wait(1.2f);
            Check("Sap Bucket reads back", player.Sap == 1, "rank " + player.Sap);
            int sapped = hero.Boosts.Sapped;
            yield return Beam(hm, 1, foe);
            yield return Wait(0.1f);
            Check("Sap Bucket: a laser hit slows the foe 15% for 2 s", foe[0] != null && YosseleSap.IsSapped(foe[0]) && Mathf.Abs(YosseleSap.SlowOf(foe[0]) - YosseleData.SapSlow[0]) < 0.001f && hero.Boosts.Sapped > sapped,
                foe[0] == null ? "no foe" : "slow " + F(YosseleSap.SlowOf(foe[0])) + ", sapped +" + (hero.Boosts.Sapped - sapped));
            ModKitDirections.Grant(hm, DirectionOf(YosseleData.Sapbuckets[1].Name));
            ModKitDirections.Grant(hm, DirectionOf(YosseleData.Sapbuckets[2].Name));
            yield return Hold(foe[0], 1.2f);
            if (foe[0] != null && ModKitUtil.Alive(foe[0])) Kill(foe[0]);
            yield return Beam(hm, 1, foe);
            yield return Wait(0.1f);
            YosseleBuilding mine = null;
            foreach (YosseleBuilding b in YosseleServer.Buildings)
                if (b != null && b.Started && b.Owner == owner && ModKitUtil.Alive(b.Master)) { mine = b; break; }
            float fromMine = foe[0] != null && mine != null ? YosseleHooks.SapOf(foe[0], mine.Master) : -1f;
            float fromVessel = foe[0] != null ? YosseleHooks.SapOf(foe[0], hm) : -1f;
            Check("Sap Bucket rank III: slows 35%, and the holder's buildings deal 10% more to the sapped foe (not the vessel)",
                player.Sap == 3 && foe[0] != null && Mathf.Abs(YosseleSap.SlowOf(foe[0]) - YosseleData.SapSlow[2]) < 0.001f && Mathf.Abs(fromMine - YosseleData.SapBonus) < 0.001f && fromVessel == 0f,
                "rank " + player.Sap + ", slow " + (foe[0] != null ? F(YosseleSap.SlowOf(foe[0])) : "-") + ", from " + (mine != null ? mine.Def.Id : "no building") + " " + F(fromMine) + ", from the vessel " + F(fromVessel));
            if (foe[0] != null && ModKitUtil.Alive(foe[0])) Kill(foe[0]);
            Drop(hm, YosseleData.Sapbuckets);
            yield return Wait(1.2f);
            Check("Sap Bucket gone with its ranks", player.Sap == 0, "rank " + player.Sap);
        }

        // Fairy Ring: standing still, Yossele and the holder's buildings near it mend.
        {
            ClearFoes();
            float max = ModKitUtil.MaxHealth(hm);
            SetHealth(hm, max * 0.5f);
            YosseleBuilding near = null;
            float nearest = 99f;
            foreach (YosseleBuilding b in YosseleServer.Buildings)
            {
                if (b == null || !b.Started || b.Owner != owner || !ModKitUtil.Alive(b.Master)) continue;
                float d = ModKitUtil.Flat(b.Pos, ModKitUtil.Pos(hm));
                if (d < nearest) { nearest = d; near = b; }
            }
            ModKitDirections.Grant(hm, DirectionOf(YosseleData.Fairyrings[0].Name));
            yield return Wait(1.2f);
            Check("Fairy Ring reads back", player.Fairy == 1, "rank " + player.Fairy);
            int mends = hero.Boosts.FairyMends;
            float mended = hero.Boosts.FairyMended, h0 = ModKitUtil.Health(hm);
            yield return Wait(3.2f);
            int got1 = hero.Boosts.FairyMends - mends;
            Check("Fairy Ring: after 1.5 s still, Yossele mends 1% of its max health a second", hero.Boosts.Still && got1 >= 2 && hero.Boosts.FairyMended - mended >= 2f * YosseleData.FairyHeal[0] * max - 0.01f && ModKitUtil.Health(hm) > h0,
                "mends +" + got1 + ", mended " + F(hero.Boosts.FairyMended - mended) + ", health " + F(h0) + " -> " + F(ModKitUtil.Health(hm)));
            ModKitDirections.Grant(hm, DirectionOf(YosseleData.Fairyrings[1].Name));
            ModKitDirections.Grant(hm, DirectionOf(YosseleData.Fairyrings[2].Name));
            yield return Wait(1.2f);
            if (near == null || nearest > YosseleData.FairyRadius[2]) ModKitTest.Setup("Fairy Ring's buildings", near == null ? "no building of the owner's" : "nearest " + near.Def.Id + " is " + F(nearest) + " tiles off");
            else
            {
                float bmax = ModKitUtil.MaxHealth(near.Master);
                SetHealth(near.Master, bmax * 0.5f);
                float b0 = ModKitUtil.Health(near.Master);
                yield return Wait(2.2f);
                float b1 = ModKitUtil.Health(near.Master);
                Check("Fairy Ring rank III: a building " + F(nearest) + " tiles off mends 2% a second", b1 - b0 >= YosseleData.FairyHeal[2] * bmax - 0.01f, near.Def.Id + " " + F(b0) + " -> " + F(b1) + " of " + F(bmax));
                SetHealth(near.Master, bmax);
            }
            Drop(hm, YosseleData.Fairyrings);
            yield return Wait(1.2f);
            Check("Fairy Ring gone with its ranks", player.Fairy == 0, "rank " + player.Fairy);
            SetHealth(hm, max);
        }

        // Creeping Ivy: +1 tile of vines; rank II slows 15% more; rank III grows a tile every 2 Tides, at most 3.
        if (henge == null || henge.Master == null || !ModKitUtil.Alive(henge.Master)) ModKitTest.Setup("Creeping Ivy", "no Stonehenge standing");
        else
        {
            int length0 = henge.Length, reach0 = henge.Reach, tiles0 = henge.Tiles.Count;
            ModKitDirections.Grant(hm, DirectionOf(YosseleData.Creepingivies[0].Name));
            yield return Wait(1.2f);
            Check("Creeping Ivy: the vines reach 1 tile farther", player.Ivy == 1 && henge.Length == length0 + YosseleData.IvyReach && henge.Reach == reach0 + YosseleData.IvyReach && henge.Tiles.Count >= tiles0,
                "rank " + player.Ivy + ", length " + length0 + " -> " + henge.Length + ", reach " + reach0 + " -> " + henge.Reach + ", tiles " + tiles0 + " -> " + henge.Tiles.Count);
            Genitem vine = henge.Tiles.Count > 0 ? henge.Tiles[0] : null;
            Entity_Master onVine = vine != null ? SpawnFoe("Enemy_Aegis", vine.Position, 0.6f, 0f, true) : null;
            yield return Until(() => onVine == null || YosseleVines.IsSlowed(onVine), 2f);
            float slow1 = YosseleVines.SlowOf(onVine);
            ModKitDirections.Grant(hm, DirectionOf(YosseleData.Creepingivies[1].Name));
            yield return Hold(onVine, 1.2f);
            float slow2 = YosseleVines.SlowOf(onVine);
            if (onVine == null) ModKitTest.Setup("Creeping Ivy rank II", "no foe on a vine");
            else Check("Creeping Ivy rank II: foes on the vines slow 15% more", player.Ivy == 2 && Mathf.Abs(slow2 - slow1 - YosseleData.IvySlow) < 0.001f, "slow " + F(slow1) + " -> " + F(slow2));
            if (onVine != null && ModKitUtil.Alive(onVine)) Kill(onVine);
            ModKitDirections.Grant(hm, DirectionOf(YosseleData.Creepingivies[2].Name));
            yield return Wait(1.2f);
            int tides0 = henge.IvyTides;
            henge.TideBegan(henge.BuiltWave, 0);
            Check("Creeping Ivy rank III: each Tide the Stonehenge stands counts", player.Ivy == 3 && henge.IvyTides == tides0 + 1, "Tides " + tides0 + " -> " + henge.IvyTides);
            henge.IvyTides = 4;
            int at4 = henge.Length;
            henge.IvyTides = 40;
            int atMost = henge.Length;
            Check("... a tile more every 2 Tides, at most 3", at4 == length0 + YosseleData.IvyReach + 2 && atMost == length0 + YosseleData.IvyReach + YosseleData.IvyMax, "length " + length0 + ", after 4 Tides " + at4 + ", after 40 " + atMost);
            henge.IvyTides = tides0;
            Drop(hm, YosseleData.Creepingivies);
            yield return Wait(1.2f);
            Check("Creeping Ivy gone with its ranks", player.Ivy == 0 && henge.Length == length0, "rank " + player.Ivy + ", length " + henge.Length);
        }

        // Cutting Jar: a Cannon that falls at Growth n, rebuilt on its tile in time, starts at n x 25% (rounded down).
        {
            Beingobject being = YosseleContent.Beings[YosseleData.Cannon];
            Vector3 at = Spot(hm, being, 3.2f);
            YosseleBuilding first = null;
            if (!float.IsNaN(at.x))
            {
                yield return Build(hm, YosseleData.Cannon, at, known, got);
                first = got[0];
            }
            if (first == null) ModKitTest.Setup("Cutting Jar", "no Cannon could be built");
            else
            {
                ModKitDirections.Grant(hm, DirectionOf(YosseleData.Cuttingjars[0].Name));
                yield return Wait(1.2f);
                Check("Cutting Jar reads back", player.Cutting == 1, "rank " + player.Cutting);
                first.SetGrowth(first.Max);
                int grown = first.Growth, left = YosseleCuttings.Left, taken = YosseleCuttings.Taken;
                Kill(first.Master);
                yield return Wait(0.6f);
                Check("Cutting Jar: a building that falls leaves a cutting", YosseleCuttings.Left == left + 1 && YosseleCuttings.Count >= 1, "left +" + (YosseleCuttings.Left - left) + ", waiting " + YosseleCuttings.Count);
                yield return Build(hm, YosseleData.Cannon, at, known, got);
                int want = Mathf.FloorToInt(grown * YosseleData.CuttingKeep[0] + 0.0001f);
                Check("... rebuilt on its tile it starts at 25% of its Growth, rounded down", got[0] != null && got[0].Growth == want && YosseleCuttings.Taken == taken + 1,
                    got[0] == null ? "not rebuilt at " + at : "Growth " + grown + " -> " + got[0].Growth + " (want " + want + "), taken +" + (YosseleCuttings.Taken - taken));
                Drop(hm, YosseleData.Cuttingjars);
                yield return Wait(1.2f);
                Check("Cutting Jar gone with its ranks", player.Cutting == 0, "rank " + player.Cutting);
                if (got[0] != null && got[0].Master != null)
                    Game_Entity.Instance.Despawn(got[0].Master.Beingserver, false, "Selftest");
            }
        }
        ClearFoes();
    }
}
#endif
