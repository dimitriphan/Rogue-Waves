#if SPOOL_SELFTEST
// Debug build only: the Cookie Tin's Tacking Stitch, Cross Stitch, Patchwork Army and UNTHREAD, the Thread Head's Big
// Bobbin, High Thread Count, Needle Point, Stitch in Time and Head of the Class, and the Pin Cushion's Iron Thimble and
// Voodoo Doll (scope 08; 08_Tin.cs, 09_Head.cs, 10_Cushion.cs), on a Tin, two Heads and a Cushion of their own, taken
// down after so the sections that follow meet none of their buffs. (Written 2026-11-06, not yet run on the PC.)
public static partial class SpoolSelfTest
{
    static void TopTierAura(long owner)
    {
        Helper_Resources.Giveresources(ModKitUtil.Costs("Redaura=8000;Blueaura=8000;Yellowaura=8000"), owner, Helper_Resources.Styles.Default);
    }

    // A Thread Head placed from the build menu about 'ring' tiles from the vessel (section 6's way): slot[0], or null.
    static IEnumerator PlaceThreadHead(Entity_Master hm, long owner, float ring, List<SpoolBuilding> known, SpoolHeadBrain[] slot)
    {
        slot[0] = null;
        Vector3 at = Spot(hm, SpoolContent.Beings[SpoolData.HeadToken], ring);
        if (float.IsNaN(at.x))
            yield break;
        Send(hm, "Build", Order(SpoolData.HeadToken, at, owner));
        SpoolHeadBrain h = null;
        yield return Until(() => (h = NewHead(known)) != null, 20f);
        if (h == null)
            yield break;
        known.Add(h);
        slot[0] = h;
        yield return Wait(0.5f);
    }

    // Gone without a kill (no comeback, no Thread): what the section built, so later sections meet none of it.
    static void TakeDownQuietly(Entity_Master m)
    {
        if (m != null && m.Beingserver != null && ModKitUtil.Alive(m))
            Game_Entity.Instance.Despawn(m.Beingserver, false, "Selftest");
    }

    // A point 'far' tiles from 'from', on the side away from the vessel (which picks Thread up around itself).
    static Vector3 AwayFromVessel(Entity_Master hm, Vector3 from, float far)
    {
        Vector3 away = from - ModKitUtil.Pos(hm);
        away.y = 0f;
        return from + (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.right) * far;
    }

    // What selling this Crafted refunds (SpoolUnitBrain.Gone): half its cost, or Efficient Recycler's share.
    static int SellRefund(SpoolUnitBrain u, SpoolPlayer player)
    {
        return player.Recycler > 0 ? u.Cost * SpoolData.Recyclers[player.Recycler - 1].Percent / 100 : u.Cost / 2;
    }

    static float HealthMultiSum(Entity_Master e)
    {
        Link_Stats s;
        return e != null && e.Statsserver != null && e.Statsserver.TryGetValue("Health", out s) ? s.Value_Multi : 0f;
    }

    static IEnumerator UpTinHead(SpoolHeroBrain hero, long owner, SpoolPlayer player)
    {
        Entity_Master hm = hero.Master;
        ClearFoes();
        ClearDrops();
        TopTierAura(owner);
        yield return Wait(0.5f);
        List<SpoolBuilding> known = new List<SpoolBuilding>(SpoolServer.Buildings);
        SpoolBuilding[] got = new SpoolBuilding[1];
        if (player.Home == null)
            yield return Build(hm, SpoolData.Stash, Spot(hm, SpoolContent.Beings[SpoolData.Stash], 2.5f), known, got);
        if (player.Home == null)
        {
            ModKitTest.Setup("the Tin's, Head's and Cushion's new upgrades", "no Yarn Stash");
            yield break;
        }

        // ---------------------------------------------------------------- Cookie Tin
        SpoolTinBrain tin = null;
        {
            // No Thread at first, so the new Tin has nothing out when UNTHREAD is first pressed (none carried either: the
            // vessel would hand it over as it passes the Stash).
            player.Stash = 0;
            player.Carried = 0;
            SpoolServer.Show(player);
            yield return Build(hm, SpoolData.Tin, Spot(hm, SpoolContent.Beings[SpoolData.Tin], 3f), known, got);
            tin = got[0] as SpoolTinBrain;
        }
        if (tin == null) ModKitTest.Setup("the Cookie Tin's new upgrades", "no Tin could be built");
        else
        {
            yield return Wait(0.3f);
            Check("UNTHREAD on every Cookie Tin", tin.Master.Commandlistserver.ContainsKey(SpoolData.UnthreadLink));
            int presses = tin.Unthreads;
            Send(tin.Master, SpoolData.UnthreadLink, "");
            yield return Wait(0.4f);
            Check("UNTHREAD with no Crafted out sells nothing (it warns)", tin.Unthreads == presses && UnitsOf(tin).Count == 0, "presses " + (tin.Unthreads - presses) + ", out " + UnitsOf(tin).Count);

            Fill(player);
            yield return Until(() => UnitsOf(tin).FindAll(u => u.Started).Count >= 2, 14f);
            // The Stash emptied so nothing new is stitched while the press is read.
            player.Stash = 0;
            SpoolServer.Show(player);
            List<SpoolUnitBrain> cast = UnitsOf(tin);
            int want = 0, refunded = SpoolServer.Refunded, sold = tin.Unthreaded;
            foreach (SpoolUnitBrain u in cast) want += SellRefund(u, player);
            Send(tin.Master, SpoolData.UnthreadLink, "");
            yield return Wait(0.5f);
            bool allGone = cast.TrueForAll(u => u == null || u.Master == null);
            Check("UNTHREAD sells every Crafted the Tin has out, each refunding as a sold one", cast.Count >= 2 && allGone && tin.Unthreaded == sold + cast.Count && SpoolServer.Refunded == refunded + want,
                cast.Count + " out, all gone " + allGone + ", sold " + (tin.Unthreaded - sold) + ", refunded " + (SpoolServer.Refunded - refunded) + " of " + want);
            Fill(player);
            yield return Until(() => UnitsOf(tin).Count > 0, tin.StitchTime + 2f);
            Check("... and the Tin keeps stitching after", UnitsOf(tin).Count > 0, UnitsOf(tin).Count + " out");

            // Tacking Stitch (after Woolen Lining, its level-1 step).
            yield return Upgrade(tin, "Woolen2", owner);
            yield return Upgrade(tin, "Tacking3", owner);
            int stitchedThread = SpoolData.UnitFor(SpoolData.Stitched).Thread;
            Check("Tacking Stitch: the Tin stitches Tacked Stitched for the Stitched's Thread", Has(tin, "Tacking3") && tin.Kind == SpoolData.Tacked && tin.Cost(tin.Kind) == stitchedThread,
                "kind " + tin.Kind + ", cost " + tin.Cost(tin.Kind));
            // New Design is the other level-2 choice: the tree offers it no more, so this Tin makes no Darts.
            List<Upgradeproperties> next = Has(tin, "Tacking3") ? tin.Master.Upgradelistserver[SpoolData.Tin + "_Tacking3_Upgradeobject"].Upgrades : null;
            Check("... and no Darts: New Design is not offered after it", next != null && !next.Exists(u => u.Objectname.Contains("_Design")) && tin.Kind == SpoolData.Tacked,
                next != null ? next.Count + " next" : "Tacking Stitch not held");
            Genitem tile = ModKitTest.FoeTile(SpoolContent.Beings[SpoolData.Stitched], tin.Pos, 3f, 5f, used);
            if (tile != null)
            {
                Send(tin.Master, SpoolData.TargetLink, At(tile.Position));
                yield return Wait(0.6f);
            }
            KillAll(tin);
            Fill(player);
            SpoolUnitBrain tacked = null;
            yield return Until(() => (tacked = UnitsOf(tin).Find(u => u.Started && u.Def.Id == SpoolData.Tacked)) != null, 8f);
            yield return Wait(0.2f);
            if (tacked == null) ModKitTest.Setup("Tacking Stitch's Crafted", "none stitched, Stash " + player.Stash);
            else
            {
                float multi = 1f + tin.Param("Craftedhealth_Multi", 0f) + tin.Param("Tackedhealth_Multi", 0f), baseHealth = BaseStat(tacked.Master, "Health");
                Check("... with +100% health beside Woolen Lining's +50%", tacked.Cost == stitchedThread && Mathf.Abs(tin.Param("Tackedhealth_Multi", 0f) - 1f) < 0.001f
                    && Mathf.Abs(ModKitUtil.MaxHealth(tacked.Master) - baseHealth * multi) < 0.05f && Mathf.Abs(ModKitUtil.Health(tacked.Master) - ModKitUtil.MaxHealth(tacked.Master)) < 0.05f,
                    "cost " + tacked.Cost + ", health " + F(ModKitUtil.Health(tacked.Master)) + "/" + F(ModKitUtil.MaxHealth(tacked.Master)) + " (base " + F(baseHealth) + " x" + F(multi) + ")");
                Check("... standing on Sno Sno's blocking block", tacked.Master.Beingserver.Being.Block != null && tacked.Master.Beingserver.Being.Block.name == SpoolContent.StuffedBlock && SpoolContent.StuffedBlock != "",
                    SpoolContent.StuffedBlock);
                yield return Until(() => tacked.Master == null || tacked.Holding, 15f);
                Check("... which runs to its spot and holds it", tacked.Master != null && tacked.Arrived && tacked.Holding && ModKitUtil.Flat(ModKitUtil.Pos(tacked.Master), tin.HasSpot ? tin.Spot : tin.Pos) < 1.2f,
                    (tin.HasSpot ? "spot " + tin.LastTarget : "no spot (" + (tile == null ? "no tile" : tin.LastTarget) + "), at the Tin") + ", holding " + tacked.Holding
                    + (tacked.Master != null ? ", " + F(ModKitUtil.Flat(ModKitUtil.Pos(tacked.Master), tin.HasSpot ? tin.Spot : tin.Pos)) + " from it" : ""));
            }

            // Cross Stitch: +100% Damage for this Tin's Crafted.
            yield return Upgrade(tin, "Cross4", owner);
            KillAll(tin);
            Fill(player);
            SpoolUnitBrain cross = null;
            yield return Until(() => (cross = UnitsOf(tin).Find(u => u.Started && u.DamageMulti > 0f)) != null, 8f);
            yield return Wait(0.5f);
            if (cross == null) Check("Cross Stitch: its Crafted deal +100% damage", false, "none stitched after the upgrade, held " + Has(tin, "Cross4"));
            else
            {
                // Multis add: the other bonuses it may hold sit beside Cross Stitch's.
                float baseDamage = BaseStat(cross.Master, "Damage"), wantDamage = baseDamage * (1f + cross.DamageMulti + cross.HeadBonus + cross.SocksBonus + cross.MeritBonus);
                Check("Cross Stitch: its Crafted deal +100% damage", Has(tin, "Cross4") && Mathf.Abs(cross.DamageMulti - 1f) < 0.001f && Mathf.Abs(ModKitUtil.Stat(cross.Master, "Damage", 0f) - wantDamage) < 0.02f,
                    "damage " + F(ModKitUtil.Stat(cross.Master, "Damage", 0f)) + ", wanted " + F(wantDamage) + " (base " + F(baseDamage) + ", Multi " + F(cross.DamageMulti) + ")");
            }

            // Patchwork Army: Units +3, Respawn halved (scope 08: 6 -> 9 alive, one every 4 -> 2 seconds).
            const int more = 3;
            const float faster = 0.5f;
            TopTierAura(owner);
            yield return Wait(0.3f);
            int slots = tin.Slots;
            float time = tin.StitchTime;
            yield return Upgrade(tin, "Army5", owner);
            Check("Patchwork Army: +3 alive at once, stitched twice as fast", Has(tin, "Army5") && tin.Slots == slots + more * (player.Buttons > 0 ? SpoolData.ButtonPair : 1) && Mathf.Abs(tin.StitchTime - time * faster) < 0.01f,
                "Units " + slots + " -> " + tin.Slots + ", stitch time " + F(time) + " -> " + F(tin.StitchTime) + " s");
            KillAll(tin);
            float t0 = -1f, until = Time.time + tin.StitchTime * tin.Slots + 10f;
            while (UnitsOf(tin).Count < tin.Slots && Time.time < until)
            {
                if (player.Stash < 40) Fill(player);   // the other Tins stitch from it too
                if (t0 < 0f && UnitsOf(tin).Count > 0) t0 = Time.time;
                yield return null;
            }
            float took = t0 >= 0f ? Time.time - t0 : -1f, expect = tin.StitchTime * (tin.Slots - 1);
            yield return Wait(tin.StitchTime + 0.5f);
            Check("... and the Tin fills its Units, one every 2 s, and stops there", UnitsOf(tin).Count == tin.Slots && took >= expect - 3f && took <= expect + 3f,
                UnitsOf(tin).Count + " alive of " + tin.Slots + ", " + (tin.Slots - 1) + " more took " + F(took) + " s (" + F(expect) + " wanted)");
        }

        // ---------------------------------------------------------------- Thread Heads
        // The porter (Threaded Delivery, Big Bobbin, High Thread Count) is tested and taken down before the buffer
        // (Patchwork Job, Needle Point, Stitch in Time, Head of the Class), so neither picks up the other's Thread.
        SpoolHeadBrain[] slot = new SpoolHeadBrain[1];
        yield return PlaceThreadHead(hm, owner, 3f, known, slot);
        SpoolHeadBrain porter = slot[0];
        if (porter == null) ModKitTest.Setup("Big Bobbin, High Thread Count", "no Thread Head could be placed");
        else
        {
            ClearFoes();
            ClearDrops();
            yield return Upgrade(porter, "Delivery2", owner);
            yield return Upgrade(porter, "Bobbin3", owner);
            Check("Big Bobbin: a Thread Head carries 40 and collects from 5 tiles", Has(porter, "Bobbin3") && porter.Cap == 40 && Mathf.Abs(porter.Reach - 5f) < 0.01f,
                "carries " + porter.Cap + ", collects from " + F(porter.Reach));
            // Past the old cap and the old reach: 30 carried, Thread 4.2 tiles off.
            porter.Carried = 30;
            int collected = porter.Collected;
            Vector3 dp = AwayFromVessel(hm, porter.Pos, 4.2f);
            SpoolServer.Drop(dp, 4);
            yield return Wait(1f);
            Check("... it pulls Thread in from 4.2 tiles while carrying 30", porter.Collected - collected == 4 && SpoolServer.DropsNear(dp, 0.5f) == 0,
                "pulled " + (porter.Collected - collected) + ", left " + SpoolServer.DropsNear(dp, 0.5f) + ", carried " + porter.Carried);
            porter.Carried = 0;

            yield return Upgrade(porter, "Count4", owner);
            Check("High Thread Count", Has(porter, "Count4"));
            float reach = porter.Param("Countrange_Base", 3f);
            Entity_Master[] foe = new Entity_Master[1];
            for (int round = 0; round < 2; round++)
            {
                bool inside = round == 0;
                yield return Foe("Enemy_Dagger", porter.Pos, inside ? reach - 1f : reach + 3f, inside ? 0.5f : reach + 1.2f, foe);
                if (foe[0] == null) { ModKitTest.Setup("High Thread Count " + (inside ? "in reach" : "out of reach"), "no Dagger could stand there"); continue; }
                float apart = ModKitUtil.Flat(ModKitUtil.Pos(foe[0]), porter.Pos), bh = ModKitUtil.BaseHealth(foe[0]);
                int plainDrop = Mathf.Min(SpoolData.DropMax, (bh >= SpoolData.LargeHealth ? SpoolData.DropLarge : SpoolData.DropSmall) + (SpoolServer.Elite(foe[0]) ? SpoolData.DropElite : 0));
                int dropped = SpoolServer.Dropped, counted = SpoolServer.Counted;
                Kill(foe[0]);
                yield return Wait(0.3f);
                if (inside != (apart <= reach))
                    ModKitTest.Setup("High Thread Count " + (inside ? "in reach" : "out of reach"), "the Dagger fell " + F(apart) + " from the Head");
                else if (inside)
                    Check("High Thread Count: a foe felled within 3 tiles drops 1 more Thread", SpoolServer.Dropped - dropped == plainDrop + 1 && SpoolServer.Counted == counted + 1,
                        (SpoolServer.Dropped - dropped) + " dropped (" + plainDrop + " plain), " + F(apart) + " from the Head");
                else
                    Check("... one felled farther drops none more", SpoolServer.Dropped - dropped == plainDrop && SpoolServer.Counted == counted,
                        (SpoolServer.Dropped - dropped) + " dropped (" + plainDrop + " plain), " + F(apart) + " from the Head");
            }
            ClearFoes();
            porter.Carried = 0;
            TakeDownQuietly(porter.Master);
        }

        yield return PlaceThreadHead(hm, owner, 4.5f, known, slot);
        SpoolHeadBrain head = slot[0];
        SpoolUnitBrain near = null;
        if (head == null) ModKitTest.Setup("Needle Point, Stitch in Time, Head of the Class", "no Thread Head could be placed");
        else if (tin == null) ModKitTest.Setup("Needle Point, Stitch in Time, Head of the Class", "no Tin's Crafted to buff");
        else
        {
            yield return Upgrade(head, "Patchwork2", owner);
            // One of the Tin's Crafted walks over beside the Head; another stays at the Tin's spot.
            Genitem by = ModKitTest.FoeTile(SpoolContent.Beings[SpoolData.Stitched], head.Pos, 1f, 2.2f, used);
            near = UnitsOf(tin).Find(u => u.Started);
            if (by != null && near != null)
            {
                near.Spot = by.Position;
                near.Arrived = false;
                yield return Until(() => near.Master == null || (near.Arrived && ModKitUtil.Flat(ModKitUtil.Pos(near.Master), head.Pos) <= 2.7f), 20f);
            }
            if (near == null || near.Master == null || !near.Arrived || ModKitUtil.Flat(ModKitUtil.Pos(near.Master), head.Pos) > 2.7f)
            {
                ModKitTest.Setup("Needle Point, Stitch in Time, Head of the Class", by == null ? "no tile beside the Head" : near == null ? "no Crafted out" : "the Crafted didn't reach the Head");
                near = null;
            }
        }
        if (near != null)
        {
            float baseDamage = BaseStat(near.Master, "Damage"), d0 = ModKitUtil.Stat(near.Master, "Damage", 0f);
            yield return Upgrade(head, "Needle3", owner);
            yield return Wait(1f);
            float d1 = ModKitUtil.Stat(near.Master, "Damage", 0f);
            Check("Needle Point: your Crafted within 3 tiles deal +25%", Has(head, "Needle3") && Mathf.Abs(near.HeadBonus - 0.25f) < 0.001f && Mathf.Abs(d1 - d0 - baseDamage * 0.25f) < 0.02f,
                "bonus " + F(near.HeadBonus) + ", damage " + F(d0) + " -> " + F(d1) + ", " + F(ModKitUtil.Flat(ModKitUtil.Pos(near.Master), head.Pos)) + " from the Head");
            float inD, inG, outD, outG;
            SpoolServer.HeadBuffs(owner, head.Pos + Vector3.right * 2.7f, out inD, out inG);
            SpoolServer.HeadBuffs(owner, head.Pos + Vector3.right * 3.3f, out outD, out outG);
            SpoolUnitBrain farOne = UnitsOf(tin).Find(u => u != near && u.Started && ModKitUtil.Flat(ModKitUtil.Pos(u.Master), head.Pos) > 3.5f);
            Check("... and no farther", Mathf.Abs(inD - 0.25f) < 0.001f && outD == 0f && (farOne == null || farOne.HeadBonus == 0f),
                "at 2.7 tiles " + F(inD) + ", at 3.3 " + F(outD) + (farOne != null ? ", a Crafted " + F(ModKitUtil.Flat(ModKitUtil.Pos(farOne.Master), head.Pos)) + " off: " + F(farOne.HeadBonus) : ""));

            // Stitch in Time: a hit from a foe by the enemy core, before and after.
            Entity_Master[] far = new Entity_Master[1];
            yield return FarFoe("Enemy_Dagger", far);
            if (far[0] == null) ModKitTest.Setup("Stitch in Time", "no foe to hit with");
            else
            {
                SetHealthFull(near.Master);
                float h = ModKitUtil.Health(near.Master);
                Hit(far[0], near.Master, 10f);
                float plain = h - ModKitUtil.Health(near.Master);
                yield return Upgrade(head, "Time4", owner);
                yield return Wait(1f);
                yield return FarFoe("Enemy_Dagger", far);
                SetHealthFull(near.Master);
                h = ModKitUtil.Health(near.Master);
                int guarded0 = SpoolUnitBrain.HeadGuarded;
                if (far[0] != null) Hit(far[0], near.Master, 10f);
                float guarded = h - ModKitUtil.Health(near.Master);
                Check("Stitch in Time: your Crafted within 3 tiles take 30% less damage", Has(head, "Time4") && far[0] != null && Mathf.Abs(near.HeadGuard - 0.3f) < 0.001f && near.Guarded
                    && plain > 0f && Mathf.Abs(guarded - plain * 0.7f) < 0.05f && SpoolUnitBrain.HeadGuarded == guarded0 + 1,
                    "guard " + F(near.HeadGuard) + ", hit " + F(plain) + " -> " + F(guarded) + ", guarded hits +" + (SpoolUnitBrain.HeadGuarded - guarded0));

                // Head of the Class: +50% and 45% less within 4 tiles, the best of each (not added to Needle's or Time's).
                TopTierAura(owner);
                yield return Wait(0.3f);
                yield return Upgrade(head, "Class5", owner);
                yield return Wait(1f);
                float d2 = ModKitUtil.Stat(near.Master, "Damage", 0f);
                Check("Head of the Class: +50% damage for your Crafted", Has(head, "Class5") && Mathf.Abs(near.HeadBonus - 0.5f) < 0.001f && Mathf.Abs(d2 - d0 - baseDamage * 0.5f) < 0.02f,
                    "bonus " + F(near.HeadBonus) + ", damage " + F(d0) + " -> " + F(d2));
                yield return FarFoe("Enemy_Dagger", far);
                SetHealthFull(near.Master);
                h = ModKitUtil.Health(near.Master);
                if (far[0] != null) Hit(far[0], near.Master, 10f);
                guarded = h - ModKitUtil.Health(near.Master);
                Check("... and 45% less damage taken", far[0] != null && Mathf.Abs(near.HeadGuard - 0.45f) < 0.001f && Mathf.Abs(guarded - plain * 0.55f) < 0.05f,
                    "guard " + F(near.HeadGuard) + ", hit " + F(plain) + " -> " + F(guarded));
                SpoolServer.HeadBuffs(owner, head.Pos, out inD, out inG);
                Check("... the best of each, not the sum with Needle Point's and Stitch in Time's", Mathf.Abs(inD - 0.5f) < 0.001f && Mathf.Abs(inG - 0.45f) < 0.001f, "damage " + F(inD) + ", guard " + F(inG));
                SpoolServer.HeadBuffs(owner, head.Pos + Vector3.right * 3.7f, out inD, out inG);
                SpoolServer.HeadBuffs(owner, head.Pos + Vector3.right * 4.3f, out outD, out outG);
                Check("... reaching 4 tiles", Mathf.Abs(inD - 0.5f) < 0.001f && Mathf.Abs(inG - 0.45f) < 0.001f && outD == 0f && outG == 0f,
                    "at 3.7 tiles " + F(inD) + "/" + F(inG) + ", at 4.3 " + F(outD) + "/" + F(outG));
                if (far[0] != null) Kill(far[0]);
            }
            SetHealthFull(near.Master);

            // Head of the Class: what it picks up goes straight to the Yarn Stash; while MEND is on it keeps it.
            if (Has(head, "Class5"))
            {
                ClearDrops();
                player.Stash = 10;
                SpoolServer.Show(player);
                head.Carried = 0;
                int sent = head.SentHome, delivered = SpoolServer.Delivered;
                Vector3 dp = AwayFromVessel(hm, head.Pos, 2.5f);
                SpoolServer.Drop(dp, 3);
                yield return Wait(1f);
                Check("Head of the Class: the Thread it picks up goes straight to the Yarn Stash", head.SentHome - sent == 3 && head.Carried == 0 && SpoolServer.Delivered - delivered >= 3,
                    "sent " + (head.SentHome - sent) + ", carried " + head.Carried + ", delivered " + (SpoolServer.Delivered - delivered));
                Send(head.Master, SpoolData.MendLink, "");
                yield return Wait(0.3f);
                sent = head.SentHome;
                SpoolServer.Drop(dp, 2);
                yield return Wait(1f);
                Check("... except while MEND is on: it keeps its Thread to mend with", head.Mending && head.Carried == 2 && head.SentHome == sent,
                    "mending " + head.Mending + ", carried " + head.Carried + ", sent " + (head.SentHome - sent));
                if (head.Mending) Send(head.Master, SpoolData.MendLink, "");
                yield return Wait(0.3f);
            }
        }
        if (head != null)
        {
            head.Carried = 0;
            TakeDownQuietly(head.Master);
        }
        ClearFoes();

        // ---------------------------------------------------------------- Pin Cushion
        SpoolCushionBrain cushion = null;
        {
            yield return Build(hm, SpoolData.Cushion, Spot(hm, SpoolContent.Beings[SpoolData.Cushion], 3f), known, got);
            cushion = got[0] as SpoolCushionBrain;
        }
        if (cushion == null) ModKitTest.Setup("Iron Thimble, Voodoo Doll", "no Pin Cushion could be built");
        else
        {
            SpoolCushionBrain c = cushion;
            yield return Upgrade(c, "Sharper2", owner);
            yield return Upgrade(c, "Barrage3", owner);
            TopTierAura(owner);
            yield return Wait(0.3f);
            float m0 = HealthMultiSum(c.Master);
            yield return Upgrade(c, "Thimble4", owner);
            float m1 = HealthMultiSum(c.Master);
            Check("Iron Thimble: +200% health", Has(c, "Thimble4") && Mathf.Abs(m1 - m0 - 2f) < 0.001f, "Health Multi " + F(m0) + " -> " + F(m1) + ", max " + F(ModKitUtil.MaxHealth(c.Master)));
            float cut = c.Param("Thimble_Base", 0.25f);
            Entity_Master[] far = new Entity_Master[1];
            yield return FarFoe("Enemy_Dagger", far);
            if (far[0] == null) ModKitTest.Setup("Iron Thimble's cut", "no Dagger by the enemy core");
            else
            {
                ModKitUtil.Heal(hm, c.Master, 5000f);
                yield return Wait(0.1f);
                float h = ModKitUtil.Health(c.Master), back = c.Reflected, taken = c.Taken, rate = ModKitUtil.Stat(c.Master, "Reflect", 0.4f);
                Hit(far[0], c.Master, 20f);
                float took = h - ModKitUtil.Health(c.Master);
                Check("Iron Thimble: 25% less damage taken; pricks count what the attacker dealt", Mathf.Abs(cut - 0.25f) < 0.001f && Mathf.Abs(took - 20f * (1f - cut)) < 0.05f
                    && Mathf.Abs(c.Reflected - back - 20f * rate) < 0.05f && Mathf.Abs(c.Taken - taken - 20f * (1f - cut)) < 0.05f,
                    "took " + F(took) + " of 20, pricked back " + F(c.Reflected - back) + " (rate " + F(rate) + "), counted taken " + F(c.Taken - taken));
                Kill(far[0]);
            }

            // Voodoo Doll: the first hit of a Tide that leaves it under half.
            TopTierAura(owner);
            yield return Wait(0.3f);
            yield return Upgrade(c, "Voodoo5", owner);
            Check("Voodoo Doll", Has(c, "Voodoo5"));
            ClearFoes();
            ModKitUtil.Heal(hm, c.Master, 50000f);
            yield return Wait(0.2f);
            float max = ModKitUtil.MaxHealth(c.Master), mark = c.Param("Voodoo_Base", 0.5f);
            int dolls = c.Dolls;
            Hit(hm, c.Master, (ModKitUtil.Health(c.Master) - max * (mark + 0.1f)) / (1f - cut));
            yield return Wait(0.3f);
            Check("Voodoo Doll: not while above half health", c.Dolls == dolls, F(ModKitUtil.Health(c.Master)) + " of " + F(max));
            Entity_Master[] beside = new Entity_Master[1];
            float stunReach = c.Param("Voodoorange_Base", 3f);
            yield return Foe("Enemy_Aegis", c.Pos, stunReach - 0.5f, 0.8f, beside);
            Entity_Master victim = beside[0];
            float speed0 = victim != null ? ModKitUtil.Stat(victim, "Speed", 0f) : 0f, aspd0 = victim != null ? ModKitUtil.Stat(victim, "Attackspeed", 0f) : 0f;
            bool inReach = victim != null && ModKitUtil.Flat(ModKitUtil.Pos(victim), c.Pos) <= stunReach;
            int stunned = c.Stunned;
            float healed = c.DollHealed;
            Hit(hm, c.Master, (ModKitUtil.Health(c.Master) - max * (mark - 0.05f)) / (1f - cut));
            float h2 = ModKitUtil.Health(c.Master);
            yield return Wait(0.3f);
            Check("Voodoo Doll: the first hit under half mends it by half its maximum", c.Dolls == dolls + 1 && Mathf.Abs(c.DollHealed - healed - max * 0.5f) < 0.05f && ModKitUtil.Health(c.Master) > h2 + max * 0.4f,
                "dolls +" + (c.Dolls - dolls) + ", " + F(h2) + " -> " + F(ModKitUtil.Health(c.Master)) + " of " + F(max));
            if (victim == null || !inReach || !ModKitUtil.Alive(victim))
                ModKitTest.Setup("Voodoo Doll's stun", victim == null ? "no Aegis could stand beside it" : !inReach ? "the Aegis stood " + F(ModKitUtil.Flat(ModKitUtil.Pos(victim), c.Pos)) + " off" : "the Aegis fell");
            else
            {
                float speed1 = ModKitUtil.Stat(victim, "Speed", 0f), aspd1 = ModKitUtil.Stat(victim, "Attackspeed", 0f);
                Check("... and pins every foe within 3 tiles: no walking, hardly any attacks", c.Stunned > stunned && speed1 < 0.1f && aspd1 < aspd0 * 0.5f,
                    "stunned +" + (c.Stunned - stunned) + ", speed " + F(speed0) + " -> " + F(speed1) + ", attack speed " + F(aspd0) + " -> " + F(aspd1));
                yield return Wait(c.Param("Voodoostun_Base", 3f) + 0.3f);
                if (ModKitUtil.Alive(victim))
                    Check("... for 3 seconds", ModKitUtil.Stat(victim, "Speed", 0f) >= speed0 * 0.9f, "speed " + F(ModKitUtil.Stat(victim, "Speed", 0f)) + " of " + F(speed0));
            }
            ClearFoes();
            Hit(hm, c.Master, (ModKitUtil.Health(c.Master) - max * (mark - 0.2f)) / (1f - cut));
            yield return Wait(0.3f);
            Check("Voodoo Doll: once a Tide", c.Dolls == dolls + 1, "dolls +" + (c.Dolls - dolls) + ", " + F(ModKitUtil.Health(c.Master)) + " of " + F(max));
            yield return NextTide();
            Hit(hm, c.Master, Mathf.Max(1f, (ModKitUtil.Health(c.Master) - max * (mark - 0.3f)) / (1f - cut)));
            yield return Wait(0.3f);
            Check("... and again the next Tide", c.Dolls == dolls + 2, "dolls +" + (c.Dolls - dolls));
        }

        // Taken down: the Tin's Crafted first (they outlive their Tin), then the buildings.
        if (tin != null)
        {
            foreach (SpoolUnitBrain u in UnitsOf(tin)) TakeDownQuietly(u.Master);
            TakeDownQuietly(tin.Master);
        }
        if (cushion != null) TakeDownQuietly(cushion.Master);
        ClearFoes();
        ClearDrops();
        Fill(player);
    }
}
#endif
