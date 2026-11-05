#if SPOOL_SELFTEST
// Debug build only: Spool's five Directions from scope 11 (08_Tin.cs), run from section 10c of 99_SelfTest.cs on the host,
// each granted the way the mode grants them (ModKitDirections.Grant) and taken off after. Tin B stitches Stitched (Woolen
// Lining, Discounted Material), Tin A Darts. (Written 2026-11-05, not yet run on the PC.)
public static partial class SpoolSelfTest
{
    static Upgradeproperties DirectionOf(string name) { return Bl_Address.Getload("Upgrade", ModKitDirections.Id("Spool", name)) as Upgradeproperties; }

    static void Drop(Entity_Master hm, SpoolData.Direction[] ranks)
    {
        foreach (SpoolData.Direction d in ranks)
            Helper_Entitytools.Removeupgrade(hm, DirectionOf(d.Name));
    }

    static void Fill(SpoolPlayer player)
    {
        player.Stash = player.Cap;
        SpoolServer.Show(player);
    }

    static void KillAll(SpoolTinBrain tin)
    {
        foreach (SpoolUnitBrain u in UnitsOf(tin))
            Kill(u.Master);
    }

    static IEnumerator Directions(SpoolHeroBrain hero, long owner, SpoolPlayer player, SpoolTinBrain tinA, SpoolTinBrain tinB)
    {
        Entity_Master hm = hero.Master;
        SpoolTinBrain tin = tinB ?? tinA;
        if (tin == null || tin.Master == null || !ModKitUtil.Alive(tin.Master))
        {
            ModKitTest.Setup("Spool's new Directions", "no Cookie Tin standing");
            yield break;
        }
        ClearFoes();
        float respawn = ModKitUtil.Stat(tin.Master, "Respawn", 4f);

        // Double Needle: stitch time -15 / 30 / 30%; rank III every third stitch makes one more for no Thread.
        {
            ModKitDirections.Grant(hm, DirectionOf(SpoolData.Doubleneedles[0].Name));
            yield return Wait(1.2f);
            Check("Double Needle: a Tin stitches 15% faster", player.Needle == 1 && Mathf.Abs(tin.StitchTime - respawn * 0.85f) < 0.01f, "rank " + player.Needle + ", " + F(respawn) + " -> " + F(tin.StitchTime) + " s");
            ModKitDirections.Grant(hm, DirectionOf(SpoolData.Doubleneedles[1].Name));
            ModKitDirections.Grant(hm, DirectionOf(SpoolData.Doubleneedles[2].Name));
            yield return Wait(1.2f);
            Check("Double Needle rank III: 30% faster", player.Needle == 3 && Mathf.Abs(tin.StitchTime - respawn * 0.7f) < 0.01f, F(tin.StitchTime) + " s");
            KillAll(tin);
            int stitched = tin.Stitched, doubled = tin.Doubled;
            float until = Time.time + 25f;
            while (tin.Stitched < stitched + 3 && Time.time < until)
            {
                if (player.Stash < 20) Fill(player);
                yield return null;
            }
            yield return Wait(0.5f);
            int free = 0;
            foreach (SpoolUnitBrain u in UnitsOf(tin)) if (u.Cost == 0) free++;
            Check("Double Needle rank III: the third stitch comes out as two, the second for no Thread", tin.Stitched >= stitched + 3 && tin.Doubled == doubled + 1 && free >= 1,
                "stitches +" + (tin.Stitched - stitched) + ", doubled +" + (tin.Doubled - doubled) + ", free ones alive " + free);
            Drop(hm, SpoolData.Doubleneedles);
            yield return Wait(1.2f);
            Check("Double Needle gone with its ranks", player.Needle == 0 && Mathf.Abs(tin.StitchTime - respawn) < 0.01f, "rank " + player.Needle + ", " + F(tin.StitchTime) + " s");
        }

        // Button Jar: two smaller Crafted for one's Thread, at 55 / 65 / 75% health and damage; Units doubled.
        {
            int slots = tin.Slots;
            ModKitDirections.Grant(hm, DirectionOf(SpoolData.Buttonjars[0].Name));
            yield return Wait(1.2f);
            Check("Button Jar: a Tin holds twice as many", player.Buttons == 1 && tin.Slots == slots * SpoolData.ButtonPair, "rank " + player.Buttons + ", " + slots + " -> " + tin.Slots);
            KillAll(tin);
            yield return Wait(0.5f);
            player.Stash = 40;
            SpoolServer.Show(player);
            int stitched = tin.Stitched, cost = tin.Cost(tin.Kind);
            yield return Until(() => tin.Stitched > stitched, 8f);
            yield return Wait(0.6f);
            List<SpoolUnitBrain> pair = UnitsOf(tin);
            int costs = 0;
            foreach (SpoolUnitBrain u in pair) costs += u.Cost;
            SpoolData.Unit def = SpoolData.UnitFor(tin.Kind);
            float baseHealth = def != null ? StatOf(def, "Health") : 0f, baseDamage = def != null ? StatOf(def, "Damage") : 0f;
            float woolen = 1f + tin.Param("Craftedhealth_Multi", 0f);
            SpoolUnitBrain one = pair.Count > 0 ? pair[0] : null;
            Check("Button Jar: one stitch makes two for one's Thread", pair.Count == SpoolData.ButtonPair && costs == cost && player.Stash == 40 - cost,
                pair.Count + " stitched, costs " + costs + " of " + cost + ", Stash 40 -> " + player.Stash);
            Check("... each with 55% of the health (Woolen Lining's share too) and damage", one != null && Mathf.Abs(ModKitUtil.MaxHealth(one.Master) - baseHealth * SpoolData.ButtonShare[0] * woolen) < 0.05f
                && Mathf.Abs(ModKitUtil.Stat(one.Master, "Damage", 0f) - baseDamage * SpoolData.ButtonShare[0]) < 0.02f,
                one == null ? "none" : "health " + F(ModKitUtil.MaxHealth(one.Master)) + " (base " + F(baseHealth) + " x" + F(woolen) + "), damage " + F(ModKitUtil.Stat(one.Master, "Damage", 0f)) + " (base " + F(baseDamage) + ")");
            ModKitDirections.Grant(hm, DirectionOf(SpoolData.Buttonjars[1].Name));
            ModKitDirections.Grant(hm, DirectionOf(SpoolData.Buttonjars[2].Name));
            yield return Wait(1.2f);
            KillAll(tin);
            yield return Wait(0.5f);
            Fill(player);
            stitched = tin.Stitched;
            yield return Until(() => tin.Stitched > stitched, 8f);
            yield return Wait(0.6f);
            one = UnitsOf(tin).Find(u => u.Started);
            Check("Button Jar rank III: 75% of the health and damage", player.Buttons == 3 && one != null && Mathf.Abs(one.Small - SpoolData.ButtonShare[2]) < 0.001f
                && Mathf.Abs(ModKitUtil.MaxHealth(one.Master) - baseHealth * SpoolData.ButtonShare[2] * woolen) < 0.05f,
                one == null ? "none" : "share " + F(one.Small) + ", health " + F(ModKitUtil.MaxHealth(one.Master)));
            Drop(hm, SpoolData.Buttonjars);
            yield return Wait(1.2f);
            KillAll(tin);
            Check("Button Jar gone with its ranks", player.Buttons == 0 && tin.Slots == slots, "rank " + player.Buttons + ", Units " + tin.Slots);
        }

        // Pattern Book: once a Tide the first 1 / 2 / 3 of a Tin's Crafted to fall are stitched again for free.
        {
            Fill(player);
            yield return Until(() => UnitsOf(tin).Count >= tin.Slots, 25f);
            ModKitDirections.Grant(hm, DirectionOf(SpoolData.Patternbooks[0].Name));
            yield return Wait(1.2f);
            Check("Pattern Book reads back", player.Pattern == 1, "rank " + player.Pattern);
            player.Stash = 0;
            SpoolServer.Show(player);
            int again = tin.Restitched;
            List<SpoolUnitBrain> cast = UnitsOf(tin);
            if (cast.Count < 2) ModKitTest.Setup("Pattern Book", cast.Count + " Crafted out");
            else
            {
                Kill(cast[0].Master);
                bool queued = tin.FreeWaiting == 1;
                yield return Until(() => tin.Restitched > again, 8f);
                yield return Wait(0.6f);
                SpoolUnitBrain back = UnitsOf(tin).Find(u => !cast.Contains(u));
                Check("Pattern Book: the first to fall this Tide is stitched again, with no Thread in the Stash", queued && tin.Restitched == again + 1 && back != null && back.Cost == 0,
                    "queued " + queued + ", restitched +" + (tin.Restitched - again) + ", cost " + (back != null ? back.Cost.ToString() : "-"));
                Kill(cast[1].Master);
                Check("... and only the first, at rank I", tin.FreeWaiting == 0, "waiting " + tin.FreeWaiting);
                ModKitDirections.Grant(hm, DirectionOf(SpoolData.Patternbooks[1].Name));
                ModKitDirections.Grant(hm, DirectionOf(SpoolData.Patternbooks[2].Name));
                yield return Wait(1.2f);
                cast = UnitsOf(tin);
                if (cast.Count > 0)
                {
                    again = tin.Restitched;
                    Kill(cast[0].Master);
                    yield return Wait(0.3f);
                    Check("Pattern Book rank III: the next to fall comes back at once", player.Pattern == 3 && tin.Restitched == again + 1, "restitched +" + (tin.Restitched - again) + " within 0.3 s");
                }
            }
            Drop(hm, SpoolData.Patternbooks);
            yield return Wait(1.2f);
            Check("Pattern Book gone with its ranks", player.Pattern == 0, "rank " + player.Pattern);
        }

        // Merit Badge: +3% damage and health for each foe a Crafted drowns, up to +15% at rank I.
        {
            Fill(player);
            yield return Until(() => UnitsOf(tin).Exists(u => u.Started && u.Cost > 0), 10f);
            SpoolUnitBrain hero1 = UnitsOf(tin).Find(u => u.Started);
            ModKitDirections.Grant(hm, DirectionOf(SpoolData.Meritbadges[0].Name));
            yield return Wait(1.2f);
            if (hero1 == null) ModKitTest.Setup("Merit Badge", "no Crafted out");
            else
            {
                float d0 = ModKitUtil.Stat(hero1.Master, "Damage", 0f), h0 = ModKitUtil.MaxHealth(hero1.Master);
                SpoolData.Unit def = hero1.Def;
                int drowned = 0;
                for (int i = 0; i < 6; i++)
                {
                    Entity_Master foe = SpawnFoe("Enemy_Dagger", ModKitUtil.Pos(hero1.Master), 4f, 1f);
                    if (foe == null) continue;
                    yield return null;
                    foe.Lasthit = hero1.Master;
                    Kill(foe);
                    drowned++;
                    yield return null;
                }
                float d1 = ModKitUtil.Stat(hero1.Master, "Damage", 0f), h1 = ModKitUtil.MaxHealth(hero1.Master);
                float cap = SpoolData.MeritCap[0];
                Check("Merit Badge: +3% damage and health a foe drowned, at most +15%", drowned == 6 && hero1.Drowns == 6 && Mathf.Abs(hero1.MeritBonus - cap) < 0.001f
                    && Mathf.Abs(d1 - d0 - StatOf(def, "Damage") * cap) < 0.02f && Mathf.Abs(h1 - h0 - StatOf(def, "Health") * cap) < 0.05f,
                    "drowned " + drowned + ", counted " + hero1.Drowns + ", bonus " + F(hero1.MeritBonus) + ", damage " + F(d0) + " -> " + F(d1) + ", health " + F(h0) + " -> " + F(h1));
            }
            Drop(hm, SpoolData.Meritbadges);
            yield return Wait(1.2f);
            Check("Merit Badge gone with its ranks", player.Merit == 0, "rank " + player.Merit);
        }

        // Odd Socks: a Stitched beside a Dart deals 8% more; rank III 25% more and takes 10% less.
        {
            ClearFoes();
            SpoolUnitBrain sock = tinB != null ? UnitsOf(tinB).Find(u => u.Started && u.Def.Id == SpoolData.Stitched) : null;
            SpoolUnitBrain other = tinA != null ? UnitsOf(tinA).Find(u => u.Started && u.Def.Id == SpoolData.Dart) : null;
            if (sock == null || other == null) ModKitTest.Setup("Odd Socks", sock == null ? "no Stitched out" : "no Dart out");
            else
            {
                other.Spot = ModKitUtil.Pos(sock.Master);
                other.Arrived = false;
                yield return Until(() => sock.Master == null || other.Master == null || ModKitUtil.Flat(ModKitUtil.Pos(sock.Master), ModKitUtil.Pos(other.Master)) <= SpoolData.SocksRange - 0.3f, 15f);
                float d0 = ModKitUtil.Stat(sock.Master, "Damage", 0f);
                ModKitDirections.Grant(hm, DirectionOf(SpoolData.Oddsocks[0].Name));
                yield return Wait(1.2f);
                float d1 = ModKitUtil.Stat(sock.Master, "Damage", 0f);
                // Multis add: the bonus is +8% of the base (a Merit Badge it kept adds beside it).
                Check("Odd Socks: a Crafted beside another kind deals +8%", player.Socks == 1 && Mathf.Abs(sock.SocksBonus - SpoolData.SocksBonus[0]) < 0.001f && Mathf.Abs(other.SocksBonus - SpoolData.SocksBonus[0]) < 0.001f
                    && Mathf.Abs(d1 - d0 - StatOf(sock.Def, "Damage") * SpoolData.SocksBonus[0]) < 0.02f,
                    "apart " + F(ModKitUtil.Flat(ModKitUtil.Pos(sock.Master), ModKitUtil.Pos(other.Master))) + ", bonus " + F(sock.SocksBonus) + "/" + F(other.SocksBonus) + ", damage " + F(d0) + " -> " + F(d1));
                Entity_Master foe = SpawnFoe("Enemy_Dagger", ModKitUtil.Pos(sock.Master), 6f, 4f);
                if (foe == null) ModKitTest.Setup("Odd Socks rank III's guard", "no foe to hit with");
                else
                {
                    SetHealthFull(sock.Master);
                    float h = ModKitUtil.Health(sock.Master);
                    Hit(foe, sock.Master, 5f);
                    float plain = h - ModKitUtil.Health(sock.Master);
                    ModKitDirections.Grant(hm, DirectionOf(SpoolData.Oddsocks[1].Name));
                    ModKitDirections.Grant(hm, DirectionOf(SpoolData.Oddsocks[2].Name));
                    yield return Wait(1.2f);
                    SetHealthFull(sock.Master);
                    h = ModKitUtil.Health(sock.Master);
                    int socked = SpoolUnitBrain.Socked;
                    Hit(foe, sock.Master, 5f);
                    float guarded = h - ModKitUtil.Health(sock.Master);
                    Check("Odd Socks rank III: +25%, and 10% less damage taken", player.Socks == 3 && sock.Guarded && Mathf.Abs(sock.SocksBonus - SpoolData.SocksBonus[2]) < 0.001f
                        && plain > 0f && Mathf.Abs(guarded - plain * (1f - SpoolData.SocksGuard)) < 0.02f && SpoolUnitBrain.Socked == socked + 1,
                        "bonus " + F(sock.SocksBonus) + ", hit " + F(plain) + " -> " + F(guarded) + ", guarded hits +" + (SpoolUnitBrain.Socked - socked));
                    Kill(foe);
                }
                Drop(hm, SpoolData.Oddsocks);
                yield return Wait(1.2f);
                Check("Odd Socks gone with its ranks", player.Socks == 0 && sock.Master != null && sock.SocksBonus == 0f && !sock.Guarded, "rank " + player.Socks + ", bonus " + F(sock.SocksBonus));
            }
        }
        ClearFoes();
    }

    // A unit's sheet number (SpoolData.Units' "Name=value" lines).
    static float StatOf(SpoolData.Unit def, string name)
    {
        foreach (string s in def.Stats)
            if (s.StartsWith(name + "="))
                return float.Parse(s.Substring(name.Length + 1), System.Globalization.CultureInfo.InvariantCulture);
        return 0f;
    }

    static void SetHealthFull(Entity_Master e)
    {
        Link_Stats s;
        if (e != null && e.Statsserver != null && e.Statsserver.TryGetValue("Health", out s))
            s.Value = s.Value_Calc;
    }
}
#endif
