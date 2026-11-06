#if ULOE_SELFTEST
// Self-test section 8b: the 24 upgrades built from scope 10 on 2026-11-05 (docs/reports/ULOE_UPGRADES_2026-11-04.md),
// the teammate Light cap (decision 18) and Supernova's SURGE. One path a building, so it builds more of each: a tier-1
// building takes one upgrade a level. Runs after section 8 with the buildings sections 5-8 made.
public static partial class UloeSelfTest
{
    static IEnumerator Upgrades24(UloeHeroBrain hero, long owner, int team, List<UloeBuilding> known, UloeGlassBrain glass, UloeBankBrain bank,
        UloeMirrorBrain mirror, UloeMirrorBrain mirrorB, UloeFloodBrain flood, UloeFloodBrain flood2)
    {
        Entity_Master hm = hero.Master;
        UloeBuilding[] got = new UloeBuilding[1];
        Entity_Master[] slot = new Entity_Master[1];
        UloeHeroBrain[] vessel = new UloeHeroBrain[1];   // R4 stall: the vessel the core sends again after a fall (Vessel)
        UloeLight test = new UloeLight();
        ClearFoes();

        // The teammate cap: a source of another player gives at most 4 a building, all such sources together.
        {
            UloeLight mateA = new UloeLight(), mateB = new UloeLight();
            yield return Wait(UloeData.LightHold + 0.3f);
            float before = ModKitUtil.Stat(glass.Master, "Light", 0f);
            mateA.Feed(glass.Master, 6f, owner + 7777);
            yield return Wait(0.2f);
            float one = ModKitUtil.Stat(glass.Master, "Light", 0f) - before;
            mateB.Feed(glass.Master, 3f, owner + 8888);
            yield return Wait(0.2f);
            float two = ModKitUtil.Stat(glass.Master, "Light", 0f) - before;
            Check("a teammate's Light: at most 4 a building", Near(one, UloeData.TeamLightCap, 0.05f), "6 offered, " + F(one) + " taken");
            // The building's own sources move its Light too (a Photon passing, a Flood Light's tick: PC_FULL_2026-11-06 read
            // 5 and 8 here once), so the second teammate's take is read from its source, and the owner's from a read just before.
            Check("... from every teammate together", mateB.Holding == 0 && Near(UloeLight.SharedOn(glass.Master), UloeData.TeamLightCap, 0.05f),
                "and 3 more offered: the second holds " + mateB.Holding + ", teammates " + F(UloeLight.SharedOn(glass.Master)) + " in all; the building's Light moved " + F(two));
            float pre = ModKitUtil.Stat(glass.Master, "Light", 0f);
            test.Feed(glass.Master, 3f, owner);
            yield return Wait(0.2f);
            Check("... while the owner's own Light is not capped", test.Holding == 1 && Near(ModKitUtil.Stat(glass.Master, "Light", 0f) - pre, 3f, 0.05f), F(pre) + " -> " + F(ModKitUtil.Stat(glass.Master, "Light", 0f)));
            yield return Wait(UloeData.LightHold + 0.3f);
        }

        // ---------------------------------------------------------------- the Magnifying Glass

        // Glass A (Burning Point): Polished Lens -> Sunspot -> Death Ray.
        yield return Upgrade(glass, "Polished3", owner);
        Check("Polished Lens follows Burning Point", Has(glass, "Polished3") && Near(glass.PerLight, 0.4f));
        {
            yield return Until(() => glass.Ramp <= 0f, 4f);
            test.Feed(glass.Master, 2f);
            yield return Wait(0.6f);
            float la = glass.Light, da = ModKitUtil.Stat(glass.Master, "Damage", 0f);
            test.Feed(glass.Master, 6f);
            yield return Wait(0.6f);
            float lb = glass.Light, db = ModKitUtil.Stat(glass.Master, "Damage", 0f);
            float want = (1f + 0.4f * lb) / (1f + 0.4f * la);
            Check("Polished Lens: +40% Damage a Light", glass.Ramp <= 0f && da > 0f && Near(db / da, want, 0.03f), "Light " + F(la) + " -> " + F(lb) + ", Damage " + F(da) + " -> " + F(db) + ", wanted x" + F(want));
        }
        yield return Upgrade(glass, "Sunspot4", owner);
        Check("Sunspot", Has(glass, "Sunspot4"));
        test.Feed(glass.Master, 9f);
        yield return Foe("Enemy_Aegis", glass.Pos, 3.5f, 1.5f, slot);
        Entity_Master foe = slot[0];
        if (foe != null)
        {
            int sun = glass.Sunspots;
            yield return Until(() => glass.Sunspots > sun || !ModKitUtil.Alive(foe), 6f);
            Check("Sunspot Illuminates its target by the lens's Light", ModKitUtil.Alive(foe) && glass.SunspotLevel >= 2 && UloeMarks.LitLevel(foe) >= glass.SunspotLevel,
                "Light " + F(glass.Light) + ", level " + glass.SunspotLevel + ", Illuminate " + UloeMarks.LitLevel(foe));
        }
        else ModKitTest.Setup("Sunspot", "no Aegis could spawn in its Range");
        ClearFoes();
        yield return Upgrade(glass, "Deathray5", owner);
        Check("Death Ray", Has(glass, "Deathray5") && Near(glass.RampMax, 2.5f));
        yield return Foe("Enemy_Aegis", glass.Pos, 3.5f, 1.5f, slot);
        foe = slot[0];
        if (foe != null)
        {
            int beams = glass.Beams;
            yield return Until(() => glass.Beams >= beams + 3 || !ModKitUtil.Alive(foe), 8f);
            float heat = glass.Ramp;
            int resets = glass.Resets;
            Kill(foe);
            yield return Foe("Enemy_Dagger", glass.Pos, 3.5f, 1.5f, slot);
            Entity_Master next = slot[0];
            int b2 = glass.Beams;
            yield return Until(() => glass.Beams > b2 || next == null || !ModKitUtil.Alive(next), 4f);
            Check("Death Ray keeps its heat on a new target", heat > 0f && glass.Resets == resets && glass.Ramp >= heat, "heat " + F(heat) + " -> " + F(glass.Ramp) + ", resets " + resets + " -> " + glass.Resets);
            // Full heat, set by hand (it takes some 20 beams): the target is Illuminated 3.
            if (next != null && ModKitUtil.Alive(next))
            {
                glass.Ramp = glass.RampMax;
                int b3 = glass.Beams;
                yield return Until(() => glass.Beams > b3 || !ModKitUtil.Alive(next), 4f);
                Check("Death Ray at full heat Illuminates at level 3", !ModKitUtil.Alive(next) || UloeMarks.LitLevel(next) == UloeData.DeathMark, "Illuminate " + UloeMarks.LitLevel(next) + ", heat " + F(glass.Ramp));
            }
            ClearFoes();
            yield return Wait(glass.Param("Deathidle_Base", 3f) + 1.2f);
            Check("Death Ray cools after 3 s with nothing to beam", glass.Ramp <= 0f, "heat " + F(glass.Ramp));
        }
        else ModKitTest.Setup("Death Ray", "no Aegis could spawn in its Range");

        // Glass B: Wide Lens -> Prism -> Focal Point. Glass C: Wide Lens -> Quick Focus.
        yield return Vessel(owner, vessel);
        // Review: no vessel back means nothing more here can be measured; an honest SETUP, and section 9 ends the run.
        if (vessel[0] == null) { ModKitTest.Setup("8b the 24 upgrades", "the vessel fell and the core did not send it again"); yield break; }
        hero = vessel[0]; hm = hero.Master;
        Vector3 at = Spot(hm, UloeContent.Beings[UloeData.Glass], 2.5f);
        yield return Build(hm, UloeData.Glass, at, known, got);
        UloeGlassBrain glassB = got[0] as UloeGlassBrain;
        at = Spot(hm, UloeContent.Beings[UloeData.Glass], 3f);
        yield return Build(hm, UloeData.Glass, at, known, got);
        UloeGlassBrain glassC = got[0] as UloeGlassBrain;
        if (Check("build two more Glasses", glassB != null && glassC != null))
        {
            float r0 = glassB.Stat("Range", 4.5f);
            yield return Upgrade(glassB, "Widelens2", owner);
            Check("Wide Lens: range +1.5", Has(glassB, "Widelens2") && Near(glassB.Stat("Range", 0f), r0 + 1.5f), F(r0) + " -> " + F(glassB.Stat("Range", 0f)));
            yield return Upgrade(glassC, "Widelens2", owner);
            float a0 = glassC.Stat("Attackspeed", 0.75f);
            yield return Upgrade(glassC, "Quick3", owner);
            Check("Quick Focus: attack speed +50%", Has(glassC, "Quick3") && Near(glassC.Stat("Attackspeed", 0f), a0 * 1.5f, 0.01f), F(a0) + " -> " + F(glassC.Stat("Attackspeed", 0f)));
            yield return Upgrade(glassB, "Prism3", owner);
            Check("Prism follows Wide Lens", Has(glassB, "Prism3"));
            // Three foes close together in its Range: the target and two beside it.
            yield return Foe("Enemy_Aegis", glassB.Pos, 4f, 2f, slot);
            Entity_Master first = slot[0];
            List<Entity_Master> crowd = new List<Entity_Master>();
            if (first != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    yield return Foe("Enemy_Aegis", ModKitUtil.Pos(first), 1.2f, 0.3f, slot);
                    if (slot[0] != null) crowd.Add(slot[0]);
                }
                int sides = glassB.Sides;
                yield return Until(() => glassB.Sides > sides, 6f);
                Check("Prism splits the beam onto the foes beside its target", glassB.Sides > sides, (glassB.Sides - sides) + " side beams, " + crowd.Count + " beside");
                yield return Upgrade(glassB, "Focal4", owner);
                Check("Focal Point follows Prism", Has(glassB, "Focal4"));
                int pierced = glassB.Pierced;
                yield return Until(() => glassB.Pierced > pierced + 1, 6f);
                Check("Focal Point burns along its line", glassB.Pierced > pierced, (glassB.Pierced - pierced) + " burned");
            }
            else ModKitTest.Setup("Prism and Focal Point", "no Aegis could spawn in its Range");
            ClearFoes();
        }

        // ---------------------------------------------------------------- the Power Bank

        // Bank A (Short Circuit): Static Charge -> Chain Lightning -> Supernova.
        if (bank != null && bank.Master != null)
        {
            Check("Short Circuit leads to Static Charge and Surge Protector", Array.Find(UloeData.BuildFor(UloeData.Bank).Upgrades, u => u.Key == "Short").Next.Length == 2);
            yield return Upgrade(bank, "Static3", owner);
            Check("Static Charge", Has(bank, "Static3"));
            yield return Wait(1f);
            UloePhotonBrain p0 = PhotonsOf(bank).Find(p => p.Started);
            if (p0 != null)
                Check("Static Charge: Photon damage +100%", Near(ModKitUtil.Stat(p0.Master, "Damage", 0f), 1f, 0.02f) && p0.MarkLevel == 2, "damage " + F(ModKitUtil.Stat(p0.Master, "Damage", 0f)) + ", marks " + p0.MarkLevel);
            yield return Upgrade(bank, "Chain4", owner);
            Check("Chain Lightning", Has(bank, "Chain4"));
            // At a Photon's anchor (a Photon on a lit line stands at its end), armoured: the towers round the Bank felled the
            // plain Aegis before a Photon reached it (PC_FULL_2026-11-06 §4, as R4 §8 found for the Searchlight).
            UloePhotonBrain pa = PhotonsOf(bank).Find(p => p.Started);
            yield return Foe("Enemy_Aegis", pa != null ? pa.Anchor : bank.Pos, 2.2f, 0.8f, slot);
            Entity_Master a = slot[0];
            Armour(a);
            if (a != null)
            {
                yield return Foe("Enemy_Aegis", ModKitUtil.Pos(a), 1.5f, 0.3f, slot);
                Armour(slot[0]);
                int chained = 0, marked = 0;
                yield return Until(() =>
                {
                    chained = 0; marked = 0;
                    foreach (UloePhotonBrain p in PhotonsOf(bank)) { chained += p.Chained; marked += p.Marked; }
                    return chained > 0;
                }, 15f);
                Check("Chain Lightning: a Photon's hit jumps on", chained > 0, marked + " hits, " + chained + " jumps");
                Check("... marking at Static Charge's level", !ModKitUtil.Alive(a) || UloeMarks.BlindLevel(a) >= 2 || UloeMarks.LitLevel(a) >= 2, "Blind " + UloeMarks.BlindLevel(a) + ", Illuminate " + UloeMarks.LitLevel(a));
            }
            else ModKitTest.Setup("Chain Lightning", "no Aegis could spawn by the Bank");
            ClearFoes();
            yield return Upgrade(bank, "Supernova5", owner);
            Link_Command surge;
            Check("Supernova puts a SURGE button on the Bank", Has(bank, "Supernova5") && bank.Master.Commandlistserver.TryGetValue(UloeData.SurgeLink, out surge) && surge.Controllable && UloeContent.Surge.Commandfuncts[0] is UloeSurgeButton);
            if (PhotonsOf(bank).Count > 0)
                Kill(PhotonsOf(bank)[0].Master);
            yield return Wait(0.3f);
            int bursts = UloeServer.Bursts, alive = PhotonsOf(bank).Count;
            Send(bank.Master, UloeData.SurgeLink, "");
            yield return Wait(0.4f);
            Check("SURGE: every Photon bursts", bank.Novas == 1 && UloeServer.Bursts >= bursts + alive, (UloeServer.Bursts - bursts) + " bursts from " + alive + " Photons");
            Check("... and all come back at once", PhotonsOf(bank).Count == bank.Slots, PhotonsOf(bank).Count + " of " + bank.Slots);
            UloePhotonBrain p1 = PhotonsOf(bank).Find(p => p.Master != null);
            Check("... giving Light 3 for a while", bank.NovaOn && p1 != null && Near(p1.LightOut, 3f), p1 != null ? "Light " + F(p1.LightOut) : "no Photon");
            Link_Command s2;
            Check("SURGE once a Tide: the button greys", bank.Master.Commandlistserver.TryGetValue(UloeData.SurgeLink, out s2) && s2.Goalstring == "Off" && !bank.SurgeReady);
            Send(bank.Master, UloeData.SurgeLink, "");
            yield return Wait(0.3f);
            Check("... and a second press does nothing", bank.Novas == 1);
            // BATCH 10:145: the branch without Surge Protector keeps its own top, 4 + 2 an upgrade, 12 with all four (scope 10:35).
            Check("Plug In from a Power Bank with all four upgrades: Light 12", bank.UpgradeCount == 4 && Near(UloeHeroBrain.PlugLight(bank.Master), 12f),
                bank.UpgradeCount + " upgrades, Light " + F(UloeHeroBrain.PlugLight(bank.Master)));
        }
        else ModKitTest.Setup("Static Charge to Supernova", "no Power Bank");

        // Bank B: Extra Cell -> Bright Spark -> Photon Swarm. Bank C: Short Circuit -> Surge Protector.
        yield return Vessel(owner, vessel);
        // Review: no vessel back means nothing more here can be measured; an honest SETUP, and section 9 ends the run.
        if (vessel[0] == null) { ModKitTest.Setup("8b the 24 upgrades", "the vessel fell and the core did not send it again"); yield break; }
        hero = vessel[0]; hm = hero.Master;
        at = Spot(hm, UloeContent.Beings[UloeData.Bank], 3.5f);
        yield return Build(hm, UloeData.Bank, at, known, got);
        UloeBankBrain bankB = got[0] as UloeBankBrain;
        at = Spot(hm, UloeContent.Beings[UloeData.Bank], 4f);
        yield return Build(hm, UloeData.Bank, at, known, got);
        UloeBankBrain bankC = got[0] as UloeBankBrain;
        if (Check("build two more Power Banks", bankB != null && bankC != null))
        {
            int slots = bankB.Slots;
            yield return Upgrade(bankB, "Extracell2", owner);
            Check("Extra Cell: one more Photon", Has(bankB, "Extracell2") && bankB.Slots == slots + 1, slots + " -> " + bankB.Slots);
            yield return Upgrade(bankB, "Bright3", owner);
            yield return Until(() => PhotonsOf(bankB).Exists(p => p.Started), 12f);
            UloePhotonBrain pb = PhotonsOf(bankB).Find(p => p.Started);
            Check("Bright Spark: a Photon gives Light 2 within 2.5", Has(bankB, "Bright3") && pb != null && Near(pb.LightOut, 2f) && Near(pb.LightRadius, 2.5f), pb != null ? F(pb.LightOut) + " within " + F(pb.LightRadius) : "no Photon");
            yield return Upgrade(bankB, "Swarm4", owner);
            Check("Photon Swarm: two more, leash 5", Has(bankB, "Swarm4") && bankB.Slots == slots + 3 && (pb == null || Near(pb.Leash, 5f)), bankB.Slots + " Photons" + (pb != null ? ", leash " + F(pb.Leash) : ""));

            yield return Upgrade(bankC, "Short2", owner);
            yield return Upgrade(bankC, "Surge3", owner);
            Check("Surge Protector follows Short Circuit", Has(bankC, "Surge3"));
            yield return Near(hm, bankC.Master, 3f);
            yield return Wait(UloeData.PlugCooldown + 0.1f);
            Send(hm, UloeData.PlugLink, Ref(bankC.Master));
            yield return Wait(0.6f);
            Check("plugged into it, Uloe is shielded", hero.Plug == bankC.Master && hero.Shielded, hero.LastPlug);
            yield return FarFoe("Enemy_Dagger", slot);
            Entity_Master far = slot[0];
            if (far != null && hero.Shielded)
            {
                float h0 = ModKitUtil.Health(hm), healed = hero.SurgeHealed;
                Hit(far, hm, 10f);
                yield return null;
                // A heal can land in the frame between the hit and the read: it goes back on.
                float lost = h0 - ModKitUtil.Health(hm) + (hero.SurgeHealed - healed);
                Check("Surge Protector: 40% less damage", Near(lost, 6f, 0.3f), "a hit of 10 took " + F(lost));
                float h1 = ModKitUtil.Health(hm);
                yield return Wait(2.2f);
                Check("... and heals 2 a second", ModKitUtil.Health(hm) >= h1 + 3.9f || ModKitUtil.Health(hm) >= ModKitUtil.MaxHealth(hm) - 0.01f, F(h1) + " -> " + F(ModKitUtil.Health(hm)) + " in 2.2 s");
            }
            else ModKitTest.Setup("Surge Protector's numbers", far == null ? "no foe could stand by the enemy core" : "not shielded");
            // BATCH 10:145: a safe socket gives Plug In at most Light 8. Short Circuit and Surge Protector are all such a
            // Bank can take (the branch ends there); a third upgrade forced on, as the batch check counted it (Extra
            // Cell), would make 4 + 2 x 3 = 10 without the cap.
            yield return Wait(UloeData.LightTick + 0.1f);
            float bright = UloeData.BrightLight[hero.Player != null ? hero.Player.Bright : 0];
            bool onC = hero.Plug == bankC.Master;
            Check("Plug In on a Surge Protector Bank with all it can take: Light 8", bankC.UpgradeCount == 2 && Near(UloeHeroBrain.PlugLight(bankC.Master), 8f) && (!onC || Near(hero.LightNow, 8f + bright)),
                bankC.UpgradeCount + " upgrades, Light " + F(UloeHeroBrain.PlugLight(bankC.Master)) + (onC ? ", Uloe gives " + F(hero.LightNow) : ", not plugged in"));
            Upgradeproperties extra;
            if (UloeContent.Upgrades.TryGetValue(UloeData.Bank + "_Extracell2_Upgradeobject", out extra) && extra != null)
            {
                Helper_Entitytools.Upgrade(bankC.Master, extra, Helper_Entitytools.Upgradecheck.Forced, null);
                yield return Wait(UloeData.LightTick + 0.2f);
                onC = hero.Plug == bankC.Master;
                Check("... still Light 8 with a third upgrade forced on", bankC.UpgradeCount == 3 && Near(UloeHeroBrain.PlugLight(bankC.Master), 8f) && (!onC || Near(hero.LightNow, 8f + bright)),
                    bankC.UpgradeCount + " upgrades, Light " + F(UloeHeroBrain.PlugLight(bankC.Master)) + (onC ? ", Uloe gives " + F(hero.LightNow) : ", not plugged in"));
            }
            else ModKitTest.Setup("Plug In's cap with a third upgrade", "no Extra Cell upgrade object");
            Send(hm, UloeData.PlugLink, "{\"Unplug\":1}");
            yield return Wait(0.5f);
            Check("unplugged, the shield goes", !hero.Shielded);
        }

        // ---------------------------------------------------------------- the Mirror

        // Mirror A (Silver Backing, Glare): Hall of Mirrors -> Heliostat. Mirror B (Vanity Lights, Bounce Card): Reflection.
        // Mirror D: Silver Backing -> Tempered Glass.
        UloeLight lightM = new UloeLight();
        if (mirror != null && mirror.Master != null)
        {
            yield return Upgrade(mirror, "Hall4", owner);
            Check("Hall of Mirrors follows Glare", Has(mirror, "Hall4"));
            yield return FarFoe("Enemy_Dagger", slot);
            Entity_Master far = slot[0];
            if (far != null)
            {
                yield return Foe("Enemy_Dagger", ModKitUtil.Pos(far), 1.5f, 0.3f, slot);
                Entity_Master beside = slot[0];
                lightM.Feed(mirror.Master, 3f);
                yield return Wait(0.2f);
                if (beside != null)
                {
                    int spread = mirror.Spread;
                    Hit(far, mirror.Master, 2f);
                    yield return Wait(0.1f);
                    Check("Hall of Mirrors: the mark spreads round the attacker", mirror.Spread > spread && UloeMarks.BlindLevel(beside) >= mirror.Level, "spread " + (mirror.Spread - spread) + ", Blind beside " + UloeMarks.BlindLevel(beside));
                }
                else ModKitTest.Setup("Hall of Mirrors", "no second foe beside the first");
            }
            else ModKitTest.Setup("Hall of Mirrors", "no foe could stand by the enemy core");
            ClearFoes();
            yield return Upgrade(mirror, "Heliostat5", owner);
            Check("Heliostat", Has(mirror, "Heliostat5"));
            lightM.Feed(mirror.Master, 3f);
            yield return Foe("Enemy_Aegis", mirror.Pos, 3.5f, 1f, slot);
            foe = slot[0];
            if (foe != null)
            {
                float h0 = ModKitUtil.Health(foe);
                int helio = mirror.Heliobeams;
                yield return Until(() => mirror.Heliobeams > helio, 3f);
                lightM.Feed(mirror.Master, 3f);
                Check("Heliostat beams the nearest foe, by its Light", mirror.Heliobeams > helio && (!ModKitUtil.Alive(foe) || ModKitUtil.Health(foe) < h0), (mirror.Heliobeams - helio) + " beams, " + F(h0) + " -> " + F(ModKitUtil.Health(foe)));
                Check("... and gives it its Blind", !ModKitUtil.Alive(foe) || UloeMarks.BlindLevel(foe) >= 1, "Blind " + UloeMarks.BlindLevel(foe));
            }
            else ModKitTest.Setup("Heliostat", "no Aegis could spawn by the Mirror");
            ClearFoes();
        }
        if (mirrorB != null && mirrorB.Master != null)
        {
            yield return Upgrade(mirrorB, "Reflection4", owner);
            Check("Reflection follows Bounce Card", Has(mirrorB, "Reflection4"));
            yield return FarFoe("Enemy_Aegis", slot);
            Entity_Master far = slot[0];
            if (far != null)
            {
                lightM.Feed(mirrorB.Master, 2f);
                yield return Wait(0.2f);
                // R4 §8: the Mirror holds what all its sources give (Light 12 in round 4), not the test's 2, so the share
                // wanted follows the Light it holds, by scope 10:202: 30% of each hit back, 60% from Light 6.
                float lightNow = mirrorB.Light, want = lightNow >= 6f ? 0.6f : 0.3f;
                float h0 = ModKitUtil.Health(far), back = mirrorB.ReflectedDamage;
                Hit(far, mirrorB.Master, 10f);
                yield return Wait(0.1f);
                Check("Reflection: its share of the hit back at the attacker, by its Light", Near(mirrorB.ReflectedDamage - back, 10f * want, 0.05f) && ModKitUtil.Health(far) < h0,
                    F(mirrorB.ReflectedDamage - back) + " of 10 back at Light " + F(lightNow) + ", wanted " + F(10f * want) + ", the foe " + F(h0) + " -> " + F(ModKitUtil.Health(far)));
                // The 30% half: dimmed under Light 6 for one hit by a test modifier, taken off in the same frame (the hit
                // and its Reflection run at once), since its sources keep it bright.
                float raw = mirrorB.Stat(UloeData.LightStat, 0f);
                Link_Modifier dim = raw > 2f ? Helper_Entitytools.Addmod(mirrorB.Master, UloeData.LightStat, 2f - raw, Helper_Entitytools.Modtypes.Add, 0.5f) : null;
                float dimmed = mirrorB.Light;
                back = mirrorB.ReflectedDamage;
                bool hit30 = dimmed < 6f && ModKitUtil.Alive(far);
                if (hit30)
                    Hit(far, mirrorB.Master, 10f);
                float got30 = mirrorB.ReflectedDamage - back;
                ModKitUtil.Unmod(dim);
                if (hit30)
                    Check("... 30% under Light 6", Near(got30, 3f, 0.05f), F(got30) + " of 10 back at Light " + F(dimmed));
                else ModKitTest.Setup("Reflection's 30% under Light 6", "Light " + F(dimmed) + " after dimming, the foe alive " + ModKitUtil.Alive(far));
                lightM.Feed(mirrorB.Master, 7f);
                yield return Wait(0.2f);
                back = mirrorB.ReflectedDamage;
                if (ModKitUtil.Alive(far))
                {
                    Hit(far, mirrorB.Master, 10f);
                    yield return Wait(0.1f);
                    Check("... 60% from Light 6", mirrorB.Light >= 6f && Near(mirrorB.ReflectedDamage - back, 6f, 0.05f), F(mirrorB.ReflectedDamage - back) + " of 10 back at Light " + F(mirrorB.Light));
                }
                else ModKitTest.Setup("Reflection's 60% from Light 6", "the foe fell");
            }
            else ModKitTest.Setup("Reflection", "no foe could stand by the enemy core");
            ClearFoes();
        }
        yield return Vessel(owner, vessel);
        // Review: no vessel back means nothing more here can be measured; an honest SETUP, and section 9 ends the run.
        if (vessel[0] == null) { ModKitTest.Setup("8b the 24 upgrades", "the vessel fell and the core did not send it again"); yield break; }
        hero = vessel[0]; hm = hero.Master;
        at = Spot(hm, UloeContent.Beings[UloeData.Mirror], 3f);
        yield return Build(hm, UloeData.Mirror, at, known, got);
        UloeMirrorBrain mirrorD = got[0] as UloeMirrorBrain;
        if (mirrorD != null)
        {
            yield return Upgrade(mirrorD, "Silver2", owner);
            float m0 = ModKitUtil.MaxHealth(mirrorD.Master);
            yield return Upgrade(mirrorD, "Tempered3", owner);
            float m1 = ModKitUtil.MaxHealth(mirrorD.Master);
            Check("Tempered Glass: health +200% of its base", Has(mirrorD, "Tempered3") && m1 >= m0 + 199f, F(m0) + " -> " + F(m1));
            yield return FarFoe("Enemy_Dagger", slot);
            Entity_Master far = slot[0];
            if (far != null)
            {
                yield return Wait(0.3f);
                float h0 = ModKitUtil.Health(mirrorD.Master);
                Hit(far, mirrorD.Master, 10f);
                yield return Wait(0.1f);
                Check("... and takes 20% less damage", Near(h0 - ModKitUtil.Health(mirrorD.Master), 8f, 0.2f), "a hit of 10 took " + F(h0 - ModKitUtil.Health(mirrorD.Master)));
            }
            ClearFoes();
        }
        else ModKitTest.Setup("Tempered Glass", "no fourth Mirror");

        // ---------------------------------------------------------------- the Flood Light

        // Flood A (Wide Flood): Halogen Bulb -> Searchlight -> Stadium Lights. Flood B (Long Throw): High Voltage -> Colour
        // Filter. Flood C, by a node if one has room: Long Throw -> High Voltage -> Searchlight -> Lighthouse.
        if (flood != null && flood.Master != null)
        {
            float lo = flood.Stat("Lightout", 2f);
            yield return Upgrade(flood, "Halogen3", owner);
            Check("Halogen Bulb: Light out +2", Has(flood, "Halogen3") && Near(flood.Stat("Lightout", 0f), lo + 2f), F(lo) + " -> " + F(flood.Stat("Lightout", 0f)));
            yield return Upgrade(flood, "Searchlight4", owner);
            Check("Searchlight", Has(flood, "Searchlight4"));
            // R4 §8: the Aegis fell to the towers round the lamp (Heliostat, Death Ray) before the first search, so the
            // slow was never read: it is armoured now (+10000 Health), and each read says where it stood.
            int searched = flood.Searched;
            yield return Foe("Enemy_Aegis", flood.Pos, flood.RingRadius - 0.5f, 0.5f, slot);
            foe = slot[0];
            Armour(foe);
            if (foe != null && flood.Node != null)
            {
                // Its speed before the light: read now, or its being's when a search caught it while it loaded.
                float s0 = flood.Searched > searched ? BaseStat(foe, "Speed") : ModKitUtil.Stat(foe, "Speed", 0f);
                yield return Until(() => flood.Searched > searched || !ModKitUtil.Alive(foe), 2.5f);
                int wantLit = flood.Output >= 5f - 0.0001f ? 2 : 1;     // scope 10:249: level 2 once its Light out is 5 or more
                Check("Searchlight: a foe in its Light is Illuminated and slowed 20%", ModKitUtil.Alive(foe) && flood.Searched > searched && UloeMarks.LitLevel(foe) >= wantLit && Near(ModKitUtil.Stat(foe, "Speed", 0f), s0 * 0.8f, 0.02f),
                    "Illuminate " + UloeMarks.LitLevel(foe) + " (wanted " + wantLit + "), speed " + F(s0) + " -> " + F(ModKitUtil.Stat(foe, "Speed", 0f)) + ", Light out " + F(flood.Output)
                    + "; the foe alive " + ModKitUtil.Alive(foe) + " (" + (foe != null ? "health " + F(ModKitUtil.Health(foe)) : "gone") + "), " + (flood.Searched - searched) + " searches, at " + F(foe != null ? ModKitUtil.Flat(ModKitUtil.Pos(foe), flood.Pos) : -1f) + " of ring " + F(flood.RingRadius));
                yield return Wait(UloeData.LightTick + 0.2f);
                if (ModKitUtil.Alive(foe) && ModKitUtil.Flat(ModKitUtil.Pos(foe), flood.Pos) <= flood.RingRadius)
                    Check("... one slow a Flood Light, not one a second", Near(ModKitUtil.Stat(foe, "Speed", 0f), s0 * 0.8f, 0.02f), "speed " + F(ModKitUtil.Stat(foe, "Speed", 0f)));
                else ModKitTest.Setup("Searchlight's one slow", "the foe " + (ModKitUtil.Alive(foe) ? "walked out of the ring, " + F(ModKitUtil.Flat(ModKitUtil.Pos(foe), flood.Pos)) + " away" : "fell"));
                // Decision 2b (docs/reports/DECIDED_CODE_FIXES_2026-11-05.md): a foe already slowed to 15% of its speed (as
                // under Fog Horn) comes into the light: the Searchlight's slow stops at the 10% floor (ModKitUtil.SlowOn),
                // and is made again at the full 20% once the other slow leaves.
                ClearFoes();
                yield return Wait(0.3f);
                int searched2 = flood.Searched;
                yield return Foe("Enemy_Aegis", flood.Pos, flood.RingRadius - 0.5f, 0.5f, slot);
                Entity_Master held = slot[0];
                Armour(held);
                // Speed is its parts times the sum of its Multis (m0 before any slow), so a slow scales it by that sum.
                float h0s = ModKitUtil.Stat(held, "Speed", 0f);
                Link_Stats sp = ModKitUtil.Speed(held);
                float m0 = sp != null ? sp.Value_Multi : 1f, room = m0 - 0.15f;
                Link_Modifier other = held != null && room > 0.01f ? ModKitUtil.SlowOn(held, room) : null;
                // The other slow at its full size shows it came first: a search in the frames before it would have cut it.
                if (other != null && Near(other.Value_Multi, -room, 0.001f) && flood.Searched == searched2)
                {
                    yield return Until(() => flood.Searched > searched2 || !ModKitUtil.Alive(held), 2.5f);
                    Check("Searchlight on a foe already slowed stops at 10% of its speed (decision 2b)", ModKitUtil.Alive(held) && flood.Searched > searched2 && Near(ModKitUtil.Stat(held, "Speed", 0f), h0s * 0.1f / m0, 0.02f),
                        "speed " + F(h0s) + " -> " + F(ModKitUtil.Stat(held, "Speed", 0f)) + ", Multi " + F(m0) + ", " + (flood.Searched - searched2) + " searches, at " + F(ModKitUtil.Alive(held) ? ModKitUtil.Flat(ModKitUtil.Pos(held), flood.Pos) : -1f) + " of ring " + F(flood.RingRadius));
                    ModKitUtil.Unmod(other);
                    yield return Wait(UloeData.LightTick + 0.2f);
                    if (ModKitUtil.Alive(held) && ModKitUtil.Flat(ModKitUtil.Pos(held), flood.Pos) <= flood.RingRadius)
                        Check("... and slows the full 20% once the other slow leaves", Near(ModKitUtil.Stat(held, "Speed", 0f), h0s * (m0 - 0.2f) / m0, 0.02f), "speed " + F(ModKitUtil.Stat(held, "Speed", 0f)));
                    else ModKitTest.Setup("Searchlight's slow made again", "the foe " + (ModKitUtil.Alive(held) ? "walked out of the ring" : "fell"));
                }
                else
                {
                    ModKitUtil.Unmod(other);
                    ModKitTest.Setup("Searchlight on a foe already slowed (decision 2b)", held == null ? "no Aegis could spawn in its ring" : "the light reached the foe before the other slow");
                }
            }
            else ModKitTest.Setup("Searchlight", foe == null ? "no Aegis could spawn in its ring" : "the Flood Light has no node");
            ClearFoes();
            float as0 = flood.Stat("Attackspeed", 0.3f);
            yield return Upgrade(flood, "Stadium5", owner);
            yield return Wait(UloeData.LightTick + 0.3f);
            Check("Stadium Lights: a 5-tile ring, AIM back, strikes twice as fast", Has(flood, "Stadium5") && Near(flood.RingRadius, 5f) && flood.Master.Commandlistserver.ContainsKey(UloeData.AimLink) && Near(flood.Stat("Attackspeed", 0f), as0 * 2f, 0.01f),
                "ring " + F(flood.RingRadius) + ", AIM " + flood.Master.Commandlistserver.ContainsKey(UloeData.AimLink) + ", strikes " + F(as0) + " -> " + F(flood.Stat("Attackspeed", 0f)) + ", second node " + (flood.Node2 != null));
        }
        else ModKitTest.Setup("Halogen Bulb to Stadium Lights", "no Wide Flood Light");
        if (flood2 != null && flood2.Master != null)
        {
            float as0 = flood2.Stat("Attackspeed", 0.3f);
            yield return Upgrade(flood2, "Voltage3", owner);
            Check("High Voltage: strikes +60% faster", Has(flood2, "Voltage3") && Near(flood2.Stat("Attackspeed", 0f), as0 * 1.6f, 0.01f), F(as0) + " -> " + F(flood2.Stat("Attackspeed", 0f)));
            yield return Upgrade(flood2, "Filter4", owner);
            Link_Command colour;
            Check("Colour Filter puts a COLOUR button on it", Has(flood2, "Filter4") && flood2.Master.Commandlistserver.TryGetValue(UloeData.ColourLink, out colour) && colour.Goalstring == "All");
            Check("... beside AIM, on a key of its own", flood2.Master.Commandlistserver.ContainsKey(UloeData.AimLink) && UloeContent.Colour.Keybind.Count == 1 && !UloeContent.Aim.Keybind.Contains(UloeContent.Colour.Keybind[0]));
            Send(flood2.Master, UloeData.ColourLink, "");
            yield return Wait(0.3f);
            Check("COLOUR cycles to Red", flood2.Colour == "Redaura", flood2.Colour);
            // A gain of Red 10 + Blue 5 in the owner's purse, traded: all of it back, then 70% of the total in Red.
            float r0 = Res(owner, "Redaura"), b0 = Res(owner, "Blueaura"), y0 = Res(owner, "Yellowaura");
            flood2.Filter(10f, 5f, 0f);
            float dr = Res(owner, "Redaura") - r0, db = Res(owner, "Blueaura") - b0, dy = Res(owner, "Yellowaura") - y0;
            Check("Colour Filter pays 70% of the payout's total in the picked colour", Near(dr, 0.7f * 15f - 10f, 0.02f) && Near(db, -5f, 0.02f) && Near(dy, 0f),
                "Red " + F(dr) + ", Blue " + F(db) + ", Yellow " + F(dy));
            int payouts = flood2.Payouts, filtered = flood2.Filtered;
            yield return Until(() => flood2.Payouts > payouts, 20f);
            if (flood2.Payouts > payouts)
                Check("... on its own strikes' payouts", flood2.Filtered > filtered, (flood2.Payouts - payouts) + " payouts, " + (flood2.Filtered - filtered) + " traded");
            else Log("no payout from the Colour Filter's node in 20 s");
        }
        else ModKitTest.Setup("High Voltage and Colour Filter", "no Long Throw Flood Light");
        UloeFloodBrain floodC = null;
        yield return Vessel(owner, vessel);
        // Review: no vessel back means nothing more here can be measured; an honest SETUP, and section 9 ends the run.
        if (vessel[0] == null) { ModKitTest.Setup("8b the 24 upgrades", "the vessel fell and the core did not send it again"); yield break; }
        hero = vessel[0]; hm = hero.Master;
        Vector3 fat = new Vector3(float.NaN, 0f, 0f);
        foreach (Entity_Master n in new List<Entity_Master>(Game_Entity.Instance.Entityclient))
            if (float.IsNaN(fat.x) && UloeUtil.IsNode(n) && UloeServer.NearestBuilding(owner, ModKitUtil.Pos(n), 6f, null) != null)   // a node with a building of ours for the beam to cross
                fat = ReachSpot(hm, ModKitUtil.Pos(n), UloeContent.Beings[UloeData.Flood], team, 0.5f, 2.3f);
        if (!float.IsNaN(fat.x))
        {
            yield return Build(hm, UloeData.Flood, fat, known, got);
            floodC = got[0] as UloeFloodBrain;
        }
        if (floodC != null)
        {
            yield return Upgrade(floodC, "Long2", owner);
            yield return Upgrade(floodC, "Voltage3", owner);
            yield return Upgrade(floodC, "Searchlight4", owner);
            yield return Upgrade(floodC, "Lighthouse5", owner);
            Check("Lighthouse: AIM goes", Has(floodC, "Lighthouse5") && !floodC.Master.Commandlistserver.ContainsKey(UloeData.AimLink) && !floodC.Directed);
            yield return Wait(UloeData.LightTick + 0.3f);
            float a0 = floodC.SweepAngle;
            int swept = floodC.Swept;
            yield return Wait(floodC.Param("Lighthouseturn_Base", 6f) + 0.5f);
            Check("Lighthouse: the beam turns, lighting buildings it crosses", floodC.Node != null && floodC.SweepAngle != a0 && floodC.Swept > swept, "node " + (floodC.Node != null) + ", angle " + F(a0) + " -> " + F(floodC.SweepAngle) + ", lit " + (floodC.Swept - swept));
        }
        else ModKitTest.Setup("Lighthouse", "no room for a third Flood Light by a node");

        // The buildings made here go, so the later sections count as before (section 10 counts every Photon). A Sell from a
        // fallen vessel goes nowhere: wait for it to come back first.
        yield return Vessel(owner, vessel);
        if (vessel[0] != null) { hero = vessel[0]; hm = hero.Master; }
        UloeBuilding[] made = { glassB, glassC, bankB, bankC, mirrorD, floodC };
        foreach (UloeBuilding b in made)
            if (b != null && b.Master != null)
            {
                Send(hm, "Sell", Sell(hm, b.Master));
                yield return Until(() => b == null || b.Master == null, 3f);
            }
        yield return Wait(1f);
        string standing = "";
        foreach (UloeBuilding b in made)
            if (b != null && b.Master != null)
                standing += " " + b.Master.name + " (" + b.UpgradeCount + " upgrades)";
        if (standing != "")
            Log("INFO the cleanup's Sells left standing:" + standing);
        ClearFoes();
    }

    // R4 §8: +10000 Health, filled, so the towers round a test foe can't fell it before its check reads it.
    static void Armour(Entity_Master foe)
    {
        Link_Stats s;
        if (foe == null || foe.Statsserver == null || !foe.Statsserver.TryGetValue("Health", out s) || s == null)
            return;
        Helper_Entitytools.Addmod(foe, "Health", 10000f, Helper_Entitytools.Modtypes.Add, 0f);
        s.Value = s.Value_Calc;
    }
}
#endif
