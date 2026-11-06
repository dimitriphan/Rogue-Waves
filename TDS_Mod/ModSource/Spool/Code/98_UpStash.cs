#if SPOOL_SELFTEST
// Debug build only: the Yarn Stash's seven upgrades (scope 08), checked as a player meets them, on the host, once the
// Stash stands. One path is bought on that Stash (Roomy Pockets, Darning, Remnant Bin, Quilting Bee); selling it takes
// them back, and the other path (Quick Patch, Elbow Patches, Mending Circle) is bought on a new one, which is sold and
// built again at the end for what follows. The numbers checked are the sheet's. (Written 2026-11-06, not yet run on the PC.)
public static partial class SpoolSelfTest
{
    static IEnumerator UpStash(SpoolHeroBrain hero, long owner, SpoolPlayer player)
    {
        Entity_Master hm = hero.Master;
        SpoolStashBrain stash = player.Home ?? SpoolServer.FirstStash(owner);
        if (stash == null || stash.Master == null || !ModKitUtil.Alive(hm))
        {
            ModKitTest.Setup("the Yarn Stash's upgrades", stash == null ? "no Yarn Stash standing" : "Spool is down");
            yield break;
        }
        ClearFoes();
        ClearDrops();
        StashAura(owner);
        yield return Wait(0.5f);

        // Roomy Pockets: Spool carries 40 and picks Thread up within 2 tiles.
        {
            Check("Roomy Pockets: without it Spool carries 20 and picks up within a tile", player.CarryCap == 20 && Mathf.Approximately(player.Reach, 1f),
                "carry " + player.CarryCap + ", reach " + F(player.Reach));
            yield return Upgrade(stash, "Pockets2", owner);
            Check("Roomy Pockets: Spool carries 40 and picks up within 2 tiles", Has(stash, "Pockets2") && player.CarryCap == 40 && Mathf.Abs(player.Reach - 2f) < 0.01f,
                "bought " + Has(stash, "Pockets2") + ", carry " + player.CarryCap + ", reach " + F(player.Reach));
            // Carrying 25 (over the old 20), Thread 1.7 tiles away comes in. Spool picks up before it drops off, so what it
            // hands over at the Stash in the same tick counts as picked up.
            ClearDrops();
            yield return Wait(0.3f);
            Vector3 drop = StashOpenSpot(owner, ModKitUtil.Pos(hm), 1.7f);
            int delivered = SpoolServer.Delivered;
            player.Carried = 25;
            SpoolServer.Show(player);
            SpoolServer.Drop(drop, 3);
            yield return Wait(0.6f);
            int took = player.Carried - 25 + SpoolServer.Delivered - delivered;
            Check("Roomy Pockets: carrying 25, Spool picks up Thread 1.7 tiles away", took == 3 && SpoolServer.DropsNear(drop, 0.3f) == 0,
                "took " + took + ", carried " + player.Carried + ", left there " + SpoolServer.DropsNear(drop, 0.3f));
            player.Carried = 0;
            SpoolServer.Show(player);
        }

        // Darning: PATCH heals everything of yours within 1.5 tiles of its target as much as the target.
        {
            yield return Upgrade(stash, "Darning3", owner);
            Check("Darning bought", Has(stash, "Darning3"));
            ClearFoes();
            // A Tin's Crafted stand together at its spot: the one with the most others within 1.3 tiles is the target.
            List<SpoolUnitBrain> all = StashCrafted(owner), near = null;
            SpoolUnitBrain target = null;
            foreach (SpoolUnitBrain u in all)
            {
                Vector3 up = ModKitUtil.Pos(u.Master);
                List<SpoolUnitBrain> n = all.FindAll(o => o != u && ModKitUtil.Flat(ModKitUtil.Pos(o.Master), up) <= 1.3f);
                if (near == null || n.Count > near.Count)
                {
                    target = u;
                    near = n;
                }
            }
            if (target == null || near.Count == 0) ModKitTest.Setup("Darning", "no two of Spool's units stand within 1.3 tiles of each other (" + all.Count + " out)");
            else
            {
                Vector3 tp = ModKitUtil.Pos(target.Master);
                SpoolUnitBrain far = all.Find(o => o != target && ModKitUtil.Flat(ModKitUtil.Pos(o.Master), tp) > 2.5f);
                yield return Walk(hm, tp, 2f, 12f);
                yield return Until(() => Time.time >= hero.PatchReady, 6f);
                List<SpoolUnitBrain> hurt = new List<SpoolUnitBrain>(near) { target };
                if (far != null) hurt.Add(far);
                foreach (SpoolUnitBrain u in hurt)
                    Hit(hm, u.Master, ModKitUtil.MaxHealth(u.Master) * 0.4f);
                yield return Wait(0.2f);
                float[] hp0 = new float[hurt.Count];
                for (int i = 0; i < hurt.Count; i++)
                    hp0[i] = ModKitUtil.Health(hurt[i].Master);
                float heal = Mathf.Min(ModKitUtil.MaxHealth(target.Master) * 0.2f, 500f);
                player.Stash = 40;
                SpoolServer.Show(player);
                int darned = hero.Darned, patches = hero.Patches;
                Send(hm, SpoolData.PatchLink, "{\"Ref\":" + target.Master.NetworkObjectId + "}");
                yield return Until(() => hero.Patches > patches, 1f);
                yield return Wait(0.2f);
                List<string> off = new List<string>();
                for (int i = 0; i < hurt.Count; i++)
                {
                    Entity_Master m = hurt[i].Master;
                    if (m == null) { off.Add("one fell"); continue; }
                    float gain = ModKitUtil.Health(m) - hp0[i];
                    float want = hurt[i] == far ? 0f : Mathf.Min(heal, ModKitUtil.MaxHealth(m) - hp0[i]);
                    if (Mathf.Abs(gain - want) > 0.1f) off.Add((hurt[i] == far ? "far " : hurt[i] == target ? "target " : "near ") + F(gain) + " of " + F(want));
                }
                Check("Darning: PATCH heals each of yours within 1.5 tiles of its target as much as the target", hero.Patches == patches + 1 && off.Count == 0 && hero.Darned - darned >= near.Count,
                    "PATCH " + hero.LastPatch + ", heal " + F(heal) + ", " + near.Count + " beside it, darned +" + (hero.Darned - darned) + (far != null ? ", one 2.5+ away" : ", none far")
                    + (off.Count > 0 ? "; off: " + string.Join(", ", off.ToArray()) : ""));
            }
        }

        // Remnant Bin: Thread lasts 30 s on the ground; a tuft that runs out brings half of it home, rounded down.
        {
            StashAura(owner);
            yield return Wait(0.3f);
            yield return Upgrade(stash, "Remnant4", owner);
            ClearDrops();
            // Out of every carrier's reach: Spool's 2 tiles and the Thread Heads'.
            Vector3 q = StashOpenSpot(owner, ModKitUtil.Pos(hm), 8f);
            SpoolServer.Drop(q, 5);
            SpoolDrop d = SpoolServer.Drops.Find(o => ModKitUtil.Flat(o.Pos, q) <= 0.1f);
            Check("Remnant Bin: Thread lasts 30 s on the ground", Has(stash, "Remnant4") && d != null && Mathf.Abs(d.Until - Time.time - 30f) < 0.5f,
                "bought " + Has(stash, "Remnant4") + ", " + (d != null ? F(d.Until - Time.time) + " s" : "no drop"));
            // The sweep is run here, in the frame the clock is set, so no stitch or delivery moves the Stash in between.
            player.Stash = 10;
            SpoolServer.Show(player);
            int remnants = SpoolServer.Remnants;
            if (d != null) d.Until = Time.time - 0.01f;
            SpoolServer.Sweep();
            Check("Remnant Bin: a tuft of 5 that runs out brings 2 home", d != null && SpoolServer.Remnants == remnants + 2 && player.Stash == 12 && !SpoolServer.Drops.Contains(d),
                "brought " + (SpoolServer.Remnants - remnants) + ", Stash 10 -> " + player.Stash);
            SpoolServer.Drop(q, 6);
            d = SpoolServer.Drops.Find(o => ModKitUtil.Flat(o.Pos, q) <= 0.1f);
            player.Stash = player.Cap - 1;
            remnants = SpoolServer.Remnants;
            if (d != null) d.Until = Time.time - 0.01f;
            SpoolServer.Sweep();
            Check("Remnant Bin: of a tuft of 6, the 1 of 3 that fits comes home, the rest is lost", d != null && SpoolServer.Remnants == remnants + 1 && player.Stash == player.Cap,
                "brought " + (SpoolServer.Remnants - remnants) + ", Stash " + player.Stash + " of " + player.Cap);
            player.Stash = 20;
            SpoolServer.Show(player);
            ClearDrops();
        }

        // Quilting Bee: BEE, once a Tide, every Cookie Tin stitches up to its limit for no Thread and the Crafted deal +50%
        // damage for 15 s.
        {
            StashAura(owner);
            yield return Wait(0.3f);
            yield return Upgrade(stash, "Bee5", owner);
            Link_Command bee;
            bool shown = stash.Master.Commandlistserver.TryGetValue(SpoolData.BeeLink, out bee) && bee != null;
            Check("Quilting Bee adds BEE to the Stash", Has(stash, "Bee5") && shown && bee.Goalstring == "", "bought " + Has(stash, "Bee5") + ", BEE " + (shown ? "'" + bee.Goalstring + "'" : "missing"));
            List<SpoolTinBrain> tins = SpoolServer.Owned<SpoolTinBrain>(owner);
            if (!shown || tins.Count == 0) ModKitTest.Setup("Quilting Bee", shown ? "no Cookie Tin standing" : "no BEE button");
            else
            {
                ClearFoes();
                // Room in every Tin and no Thread in the Stash: whatever the Tins make now is the Bee's. Reuse Reduce
                // Recycle's Thread from the fallen is cleared, so no carrier brings Thread in.
                player.Stash = 0;
                SpoolServer.Show(player);
                foreach (SpoolTinBrain tin in tins)
                {
                    List<SpoolUnitBrain> cast = UnitsOf(tin);
                    for (int i = 0; i < 2 && i < cast.Count; i++)
                        Kill(cast[i].Master);
                }
                ClearDrops();
                yield return Wait(0.5f);
                int free = 0, spent = 0;
                foreach (SpoolTinBrain tin in tins)
                {
                    free += Mathf.Max(0, tin.Slots - tin.Alive);
                    spent += tin.Spent;
                }
                SpoolUnitBrain old = StashCrafted(owner).Find(u => u.Def.Sellable);
                float d0 = old != null ? ModKitUtil.Stat(old.Master, "Damage", 0f) : 0f;
                int out0 = StashOut(owner), sewn = stash.BeeSewn, spurred = stash.BeeSpurred, bees = stash.Bees;
                Send(stash.Master, SpoolData.BeeLink, "");
                yield return Until(() => stash.Bees > bees, 1f);
                float pressed = Time.time;
                int full = 0, spentNow = 0;
                foreach (SpoolTinBrain tin in tins)
                {
                    if (tin.Alive >= tin.Slots) full++;
                    spentNow += tin.Spent;
                }
                Check("BEE: every Cookie Tin stitches up to its limit at once, for no Thread", stash.Bees == bees + 1 && full == tins.Count && stash.BeeSewn - sewn == free && spentNow == spent,
                    "pressed " + (stash.Bees - bees) + ", Tins full " + full + "/" + tins.Count + ", sewn " + (stash.BeeSewn - sewn) + " of " + free + " free, Thread spent " + (spentNow - spent));
                Check("BEE greys out once it has sewn this Tide", bee.Goalstring == "Off", "'" + bee.Goalstring + "'");
                yield return Wait(0.6f);
                float d1 = old != null && old.Master != null ? ModKitUtil.Stat(old.Master, "Damage", 0f) : 0f;
                Check("BEE: the Crafted out deal +50% damage", old != null && Mathf.Abs(d1 - d0 - BaseStat(old.Master, "Damage") * 0.5f) < 0.02f,
                    old == null ? "no Crafted out before" : "damage " + F(d0) + " -> " + F(d1) + " (base " + F(BaseStat(old.Master, "Damage")) + ")");
                Check("BEE: ... and the ones it sewed, once their stats are in", stash.BeeWaiting == 0 && stash.BeeSpurred - spurred == out0 + stash.BeeSewn - sewn,
                    "spurred +" + (stash.BeeSpurred - spurred) + " of " + out0 + " out and " + (stash.BeeSewn - sewn) + " sewn, waiting " + stash.BeeWaiting);
                int again = stash.BeeAgain;
                Send(stash.Master, SpoolData.BeeLink, "");
                yield return Wait(0.3f);
                Check("BEE: once a Tide", stash.Bees == bees + 1 && stash.BeeAgain == again + 1, "sewed +" + (stash.Bees - bees) + ", refused +" + (stash.BeeAgain - again));
                yield return Wait(Mathf.Max(0f, pressed + 15.5f - Time.time));
                float d2 = old != null && old.Master != null ? ModKitUtil.Stat(old.Master, "Damage", 0f) : 0f;
                Check("BEE: the damage wears off after 15 s", old != null && old.Master != null && Mathf.Abs(d2 - d0) < 0.02f, "damage " + F(d0) + " -> " + F(d1) + " -> " + F(d2));
                yield return NextTide();
                Check("BEE comes back with the next Tide", bee.Goalstring == "" && !stash.BeeUsed, "'" + bee.Goalstring + "'");
            }
        }

        // The other path, on a new Stash. Selling this one takes its upgrades back.
        float h0 = ModKitUtil.MaxHealth(hm), dmg0 = ModKitUtil.Stat(hm, "Damage", 0f);
        {
            yield return StashSell(hm, player, stash);
            ClearDrops();
            Vector3 q = StashOpenSpot(owner, ModKitUtil.Pos(hm), 8f);
            SpoolServer.Drop(q, 1);
            SpoolDrop d = SpoolServer.Drops.Find(o => ModKitUtil.Flat(o.Pos, q) <= 0.1f);
            Check("a sold Stash takes its upgrades back: Spool carries 20 within a tile, Thread lasts 15 s", player.Home == null && player.CarryCap == 20 && Mathf.Approximately(player.Reach, 1f)
                && d != null && Mathf.Abs(d.Until - Time.time - 15f) < 0.5f, "home " + (player.Home != null) + ", carry " + player.CarryCap + ", reach " + F(player.Reach) + ", drop " + (d != null ? F(d.Until - Time.time) + " s" : "none"));
            ClearDrops();
        }
        SpoolStashBrain[] built = new SpoolStashBrain[1];
        yield return StashBuild(hm, player, built);
        stash = built[0];
        if (stash == null)
        {
            ModKitTest.Setup("Quick Patch, Elbow Patches and Mending Circle", "no new Yarn Stash");
            yield break;
        }
        StashAura(owner);
        yield return Wait(0.5f);

        // Quick Patch: PATCH costs 15 Thread and is ready again after 2.5 s; the picker shows the price.
        {
            yield return Upgrade(stash, "Quick2", owner);
            yield return Wait(0.6f);   // the vessel's half-second check puts the price on the button
            Link_Command pc = hm.Commandlistserver[SpoolData.PatchLink];
            Check("Quick Patch: PATCH costs 15, is ready after 2.5 s, and its button carries the price", Has(stash, "Quick2") && hero.PatchCost == 15 && Mathf.Abs(hero.PatchCooldown - 2.5f) < 0.01f
                && Mathf.Approximately(pc.Goalvalue, 15f) && Mathf.Approximately(StashPatchPrice(hm), 15f),
                "bought " + Has(stash, "Quick2") + ", cost " + hero.PatchCost + ", cooldown " + F(hero.PatchCooldown) + ", button " + F(pc.Goalvalue) + ", synced " + F(StashPatchPrice(hm)));
            yield return Walk(hm, stash.Pos, 2.5f, 12f);
            yield return Until(() => Time.time >= hero.PatchReady, 6f);
            Hit(hm, stash.Master, 40f);
            yield return Wait(0.2f);
            player.Stash = 40;
            SpoolServer.Show(player);
            int traffic = Traffic(owner), patches = hero.Patches;
            Send(hm, SpoolData.PatchLink, "{\"Ref\":" + stash.Master.NetworkObjectId + "}");
            yield return Until(() => hero.Patches > patches, 1f);
            float left = hero.PatchReady - Time.time;
            int cost = 40 - player.Stash + (Traffic(owner) - traffic);
            Check("Quick Patch: a PATCH takes 15 Thread and threads for 2.5 s", hero.Patches == patches + 1 && cost == 15 && left > 2.2f && left < 2.55f,
                hero.LastPatch + ", took " + cost + " (other traffic " + (Traffic(owner) - traffic) + "), ready in " + F(left) + " s");
        }

        // Elbow Patches: Spool's Health +60% and Damage +100%.
        {
            yield return Upgrade(stash, "Elbow3", owner);
            yield return Wait(0.6f);
            float h1 = ModKitUtil.MaxHealth(hm), dmg1 = ModKitUtil.Stat(hm, "Damage", 0f), bh = BaseStat(hm, "Health"), bd = BaseStat(hm, "Damage");
            Check("Elbow Patches: Spool's Health +60% and Damage +100%", Has(stash, "Elbow3") && Mathf.Abs(h1 - h0 - bh * 0.6f) < 0.05f && Mathf.Abs(dmg1 - dmg0 - bd) < 0.02f,
                "bought " + Has(stash, "Elbow3") + ", health " + F(h0) + " -> " + F(h1) + " (base " + F(bh) + "), damage " + F(dmg0) + " -> " + F(dmg1) + " (base " + F(bd) + ")");
        }

        // Mending Circle: at the start of each Tide every building of yours heals 25% of its maximum Health. The ones a
        // Sewing Machine reaches are left out (it mends in the same moment).
        {
            StashAura(owner);
            yield return Wait(0.3f);
            yield return Upgrade(stash, "Circle4", owner);
            List<SpoolBuilding> hurt = new List<SpoolBuilding>();
            if (!StashSewn(stash, player)) hurt.Add(stash);
            foreach (SpoolBuilding b in SpoolServer.Buildings)
                if (hurt.Count < 3 && b != stash && (b is SpoolTinBrain || b is SpoolLoomBrain) && b.Started && b.Owner == owner && ModKitUtil.Alive(b.Master) && !StashSewn(b, player))
                    hurt.Add(b);
            if (hurt.Count == 0) ModKitTest.Setup("Mending Circle", "every building stands where a Sewing Machine mends");
            else
            {
                foreach (SpoolBuilding b in hurt)
                    Hit(hm, b.Master, ModKitUtil.MaxHealth(b.Master) * 0.5f);
                yield return Wait(0.2f);
                float[] hp0 = new float[hurt.Count];
                for (int i = 0; i < hurt.Count; i++)
                    hp0[i] = ModKitUtil.Health(hurt[i].Master);
                int circled = SpoolServer.Circled;
                yield return NextTide();
                List<string> seen = new List<string>();
                int right = 0;
                for (int i = 0; i < hurt.Count; i++)
                {
                    Entity_Master m = hurt[i].Master;
                    float max = m != null ? ModKitUtil.MaxHealth(m) : 0f, gain = m != null ? ModKitUtil.Health(m) - hp0[i] : 0f;
                    float want = Mathf.Min(max * 0.25f, max - hp0[i]);
                    if (m != null && hp0[i] < max - 1f && Mathf.Abs(gain - want) < 0.1f) right++;
                    seen.Add(hurt[i].Def.Display + " +" + F(gain) + " of " + F(want));
                }
                Check("Mending Circle: at the start of a Tide every building of yours heals 25% of its maximum Health", Has(stash, "Circle4") && right == hurt.Count && SpoolServer.Circled - circled >= hurt.Count,
                    "bought " + Has(stash, "Circle4") + ", healed +" + (SpoolServer.Circled - circled) + ": " + string.Join(", ", seen.ToArray()));
            }
        }

        // Sold, the Stash takes Elbow Patches and Quick Patch back; one is built again for the checks after these.
        {
            yield return StashSell(hm, player, stash);
            yield return Wait(0.6f);
            Link_Command pc = hm.Commandlistserver[SpoolData.PatchLink];
            Check("a sold Stash takes Elbow Patches and Quick Patch back", Mathf.Abs(ModKitUtil.MaxHealth(hm) - h0) < 0.05f && Mathf.Abs(ModKitUtil.Stat(hm, "Damage", 0f) - dmg0) < 0.02f
                && hero.PatchCost == 20 && Mathf.Approximately(pc.Goalvalue, 20f),
                "health " + F(ModKitUtil.MaxHealth(hm)) + " (was " + F(h0) + "), damage " + F(ModKitUtil.Stat(hm, "Damage", 0f)) + " (was " + F(dmg0) + "), PATCH " + hero.PatchCost + ", button " + F(pc.Goalvalue));
            yield return StashBuild(hm, player, built);
            Check("the Yarn Stash is built again for what follows", built[0] != null && player.Home == built[0]);
        }
    }

    static void StashAura(long owner)
    {
        Helper_Resources.Giveresources(ModKitUtil.Costs("Redaura=5000;Blueaura=5000;Yellowaura=5000"), owner, Helper_Resources.Styles.Default);
    }

    // This player's units whose stats are in, standing.
    static List<SpoolUnitBrain> StashCrafted(long owner)
    {
        List<SpoolUnitBrain> l = new List<SpoolUnitBrain>();
        foreach (SpoolUnitBrain u in SpoolServer.Units)
            if (u != null && u.Master != null && u.Started && u.Owner == owner && ModKitUtil.Alive(u.Master)) l.Add(u);
        return l;
    }

    // Every unit of this player's out, started or not.
    static int StashOut(long owner)
    {
        int n = 0;
        foreach (SpoolUnitBrain u in SpoolServer.Units)
            if (u != null && u.Master != null && u.Owner == owner && ModKitUtil.Alive(u.Master)) n++;
        return n;
    }

    // A point 'ring' tiles from 'c' that no Thread Head of this player reaches (ground Thread is a point: it needs no tile).
    static Vector3 StashOpenSpot(long owner, Vector3 c, float ring)
    {
        List<SpoolHeadBrain> heads = SpoolServer.Owned<SpoolHeadBrain>(owner);
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI / 8f;
            Vector3 p = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ring;
            if (!heads.Exists(h => ModKitUtil.Flat(h.Pos, p) <= h.Reach + 0.5f))
                return p;
        }
        return c + new Vector3(ring, 0f, 0f);
    }

    // Whether a Sewing Machine of this player's mends where this building stands each Tide.
    static bool StashSewn(SpoolBuilding b, SpoolPlayer player)
    {
        if (player.Sewing <= 0)
            return false;
        float reach = SpoolData.Sewingmachines[player.Sewing - 1].Radius + 0.75f;   // the area query reaches a little past its radius
        foreach (SpoolLoomBrain l in SpoolServer.Owned<SpoolLoomBrain>(player.Owner))
            if (ModKitUtil.Flat(l.Pos, b.Pos) <= reach) return true;
        return false;
    }

    // PATCH's price as the clients see it: the last synced PATCH entry (Updatecommand moves the one it updates to the end).
    static float StashPatchPrice(Entity_Master hm)
    {
        float price = -1f;
        if (hm.Commandlist != null)
            foreach (Link_Commandref c in hm.Commandlist)
                if (c.Name.ToString() == SpoolData.PatchLink) price = c.Goalvalue;
        return price;
    }

    static IEnumerator StashSell(Entity_Master hm, SpoolPlayer player, SpoolStashBrain stash)
    {
        Send(hm, "Sell", Sell(hm, stash.Master));
        yield return Until(() => stash == null || stash.Master == null || player.Home != stash, 12f);
        yield return Wait(0.5f);
    }

    // A new Yarn Stash beside Spool: result[0], or null.
    static IEnumerator StashBuild(Entity_Master hm, SpoolPlayer player, SpoolStashBrain[] result)
    {
        result[0] = null;
        Vector3 at = Spot(hm, SpoolContent.Beings[SpoolData.Stash], 3f);
        if (float.IsNaN(at.x))
            yield break;
        SpoolBuilding[] got = new SpoolBuilding[1];
        yield return Build(hm, SpoolData.Stash, at, new List<SpoolBuilding>(SpoolServer.Buildings), got);
        yield return Until(() => player.Home != null, 2f);
        result[0] = got[0] as SpoolStashBrain;
    }
}
#endif
