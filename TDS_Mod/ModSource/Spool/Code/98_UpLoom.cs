#if SPOOL_SELFTEST
// Debug build only: the Loom's five upgrades from scope 08 (2026-11-04) on two fresh Looms, run on the host from
// 99_SelfTest.cs. Loom C, by the vessel, takes Direct Line, Bobbin Winder, Snare Line and Golden Thread; Loom D, beside an
// aura node, Seam Allowance, Dyed in the Wool and Seam Squad. (Written 2026-11-06, not yet run on the PC.)
public static partial class SpoolSelfTest
{
    static void LoomTopUp(long owner) { Aura(owner, "Redaura=5000;Blueaura=5000;Yellowaura=8000"); }

    // The nearest aura node within 'reach' (tagged Resource with a Healthresource: dens are tagged Resource too).
    static Entity_Master LoomNode(Vector3 at, float reach)
    {
        foreach (Entity_Master e in ModKitUtil.Area(at, reach, ModKitUtil.Tagged("Resource")))
            if (ModKitUtil.Alive(e) && e.Statsserver != null && e.Statsserver.ContainsKey("Healthresource"))
                return e;
        return null;
    }

    static List<SpoolUnitBrain> LoomSeams(SpoolLoomBrain loom) { return UnitsOf(loom).FindAll(u => u.Def.Id == SpoolData.Seam); }

    static IEnumerator UpLoom(SpoolHeroBrain hero, long owner, SpoolPlayer player)
    {
        Entity_Master hm = hero.Master;
        int team = ModKitUtil.Team(hm);
        ClearFoes();
        // Sewing Machine (section 10b leaves rank III on the vessel) makes every Loom spin nothing: off for this section.
        foreach (SpoolData.Sewing s in SpoolData.Sewingmachines)
        {
            Upgradeproperties u = DirectionOf(s.Name);
            if (u != null)
                Helper_Entitytools.Removeupgrade(hm, u);
        }
        yield return Wait(1.2f);
        if (player.Sewing > 0)
        {
            ModKitTest.Setup("the Loom's upgrades", "Sewing Machine rank " + player.Sewing + " is still on the vessel");
            yield break;
        }
        LoomTopUp(owner);
        yield return Wait(0.5f);
        List<SpoolBuilding> known = new List<SpoolBuilding>(SpoolServer.Buildings);
        SpoolBuilding[] got = new SpoolBuilding[1];
        Beingobject loomBeing = SpoolContent.Beings[SpoolData.Loom];

        // Loom C: Direct Line, Bobbin Winder, Snare Line, Golden Thread. Five tiles from the Stash, so a drop-off at it is
        // told apart from one at the Stash.
        SpoolLoomBrain loomC = null;
        Vector3 at = new Vector3(float.NaN, 0f, 0f);
        if (player.Home != null && player.Home.Master != null)
            at = ModKitTest.Spot(player.Home.Pos, loomBeing, team, 5f, 8f, used, "Loom C");
        if (float.IsNaN(at.x))
            at = Spot(hm, loomBeing, 3f);
        if (!float.IsNaN(at.x))
        {
            yield return Walk(hm, at, 2f, 12f);
            yield return Build(hm, SpoolData.Loom, at, known, got);
            loomC = got[0] as SpoolLoomBrain;
        }
        if (Check("build Loom C (Direct Line, Bobbin Winder, Snare Line, Golden Thread)", loomC != null, "at " + at))
        {
            yield return Upgrade(loomC, "Direct2", owner);
            // Bobbin Winder: a Thread every 3 s, holds 20 (Spinrate and Hold modifiers).
            yield return Upgrade(loomC, "Bobbin3", owner);
            Check("Bobbin Winder: spins every 3 s and holds 20", Has(loomC, "Bobbin3") && Mathf.Abs(ModKitUtil.Stat(loomC.Master, "Spinrate", 0f) - 3f) < 0.01f && loomC.Hold == 20,
                "Spinrate " + F(ModKitUtil.Stat(loomC.Master, "Spinrate", 0f)) + ", Hold " + loomC.Hold);
            int spun = loomC.Spun;
            yield return Until(() => loomC.Spun > spun, 7f);   // the spin timed before the buy
            spun = loomC.Spun;
            yield return Wait(9.5f);
            Check("Bobbin Winder: 3 Threads spun in 9.5 s", loomC.Spun - spun == 3, "spun " + (loomC.Spun - spun) + ", sent " + loomC.Sent + ", held " + loomC.Held);

            // Snare Line on a Loom without Seam Allowance: one Seam, which hunts an aura beast within 6 tiles.
            LoomTopUp(owner);
            yield return Upgrade(loomC, "Snare4", owner);
            SpoolUnitBrain seam = null;
            yield return Until(() => (seam = LoomSeams(loomC).Find(u => u.Started)) != null, 3f);
            yield return Wait(1f);
            Check("Snare Line on a Direct Line Loom: one Seam to hunt with", Has(loomC, "Snare4") && seam != null && LoomSeams(loomC).Count == 1 && loomC.SeamCount == 1,
                "Seams " + LoomSeams(loomC).Count + ", reach " + F(loomC.SnareReach));
            Entity_Master beast = null;
            if (seam != null)
            {
                try { beast = SpawnFoe("Mob_Aurabeast", loomC.Pos, loomC.SnareReach - 0.5f, 4.5f); }
                catch (Exception e) { Log("Mob_Aurabeast: " + e.Message); }
                if (beast != null)
                    yield return Until(() => !ModKitUtil.Alive(beast) || (beast.Statsserver != null && beast.Statsserver.ContainsKey("Health")), 2f);
            }
            if (seam == null) ModKitTest.Setup("Snare Line's hunt", "no Seam");
            else if (!ModKitUtil.Alive(beast)) ModKitTest.Setup("Snare Line's hunt", "no aura beast could be spawned here");
            else
            {
                // Spool steps to the far side of the Loom, so its own swings leave the beast to the Seam.
                Vector3 off = ModKitUtil.Pos(beast) - loomC.Pos;
                off.y = 0f;
                yield return Walk(hm, loomC.Pos - off.normalized * 3f, 1f, 6f);
                float far = ModKitUtil.Flat(ModKitUtil.Pos(beast), loomC.Pos), hp0 = ModKitUtil.Health(beast);
                int hunts = loomC.Hunts, snared = loomC.Snared;
                yield return Until(() => loomC.Snared > snared || !ModKitUtil.Alive(beast) || seam.Master == null, 20f);
                string detail = "hunts +" + (loomC.Hunts - hunts) + ", strikes +" + (loomC.Snared - snared) + ", beast " + F(far) + " from the Loom, health " + F(hp0) + " -> "
                    + (ModKitUtil.Alive(beast) ? F(ModKitUtil.Health(beast)) : "fallen") + ", team " + ModKitUtil.Team(beast) + ", a foe " + ModKitUtil.IsFoe(beast, team);
                if (loomC.Snared == snared && (seam.Master == null || !ModKitUtil.Alive(beast)))
                    ModKitTest.Setup("Snare Line's hunt", (seam.Master == null ? "the Seam fell" : "the beast fell to something else") + " before the Seam struck; " + detail);
                else Check("Snare Line: its Seam chases an aura beast and strikes it", loomC.Hunts > hunts && loomC.Snared > snared, detail);
                if (ModKitUtil.Alive(beast))
                    Kill(beast);
                yield return Until(() => seam.Master == null || ModKitUtil.Flat(ModKitUtil.Pos(seam.Master), loomC.Pos) <= seam.Def.Leash + 0.5f, 12f);
                if (seam.Master == null) ModKitTest.Setup("Snare Line: the Seam comes back", "the Seam fell to the beast");
                else Check("Snare Line: with the beast gone the Seam comes back to its Loom", ModKitUtil.Flat(ModKitUtil.Pos(seam.Master), loomC.Pos) <= seam.Def.Leash + 0.5f && seam.Prey == null,
                    F(ModKitUtil.Flat(ModKitUtil.Pos(seam.Master), loomC.Pos)) + " from the Loom");
            }

            // Golden Thread: spins twice as fast (with Bobbin Winder every 1.5 s), and Spool drops off at it.
            SpoolStashBrain home = player.Home;
            bool apart = home != null && home.Master != null && ModKitUtil.Flat(loomC.Pos, home.Pos) > SpoolData.DropOff + 1.5f;
            bool atBefore = SpoolServer.AtStash(owner, loomC.Pos, SpoolData.DropOff);
            LoomTopUp(owner);
            yield return Upgrade(loomC, "Golden5", owner);
            Check("Golden Thread: with Bobbin Winder a Thread every 1.5 s", Has(loomC, "Golden5") && Mathf.Abs(ModKitUtil.Stat(loomC.Master, "Spinrate", 0f) - 1.5f) < 0.01f,
                "Spinrate " + F(ModKitUtil.Stat(loomC.Master, "Spinrate", 0f)));
            if (!apart) ModKitTest.Setup("Golden Thread's drop-off", home == null ? "no Yarn Stash standing" : "Loom C stands by the Stash");
            else
            {
                Check("Golden Thread: its Loom becomes a drop-off point", !atBefore && SpoolServer.AtStash(owner, loomC.Pos, SpoolData.DropOff), "before " + atBefore);
                ClearDrops();
                player.Stash = 0;
                player.Carried = 6;
                SpoolServer.Show(player);
                int drops = SpoolServer.GoldenDrops, thread = SpoolServer.GoldenThread;
                yield return Walk(hm, loomC.Pos, SpoolData.DropOff - 0.3f, 12f);
                yield return Until(() => SpoolServer.GoldenDrops > drops, 3f);
                Check("Golden Thread: Spool hands its Thread over at the Loom, into the Yarn Stash", SpoolServer.GoldenDrops > drops && SpoolServer.GoldenThread - thread >= 6 && player.Carried == 0,
                    "drop-offs +" + (SpoolServer.GoldenDrops - drops) + ", Thread +" + (SpoolServer.GoldenThread - thread) + ", carried " + player.Carried + ", Stash " + player.Stash
                    + ", Spool " + F(ModKitUtil.Flat(ModKitUtil.Pos(hm), loomC.Pos)) + " from the Loom");
            }
        }

        // Loom D beside a node: Seam Allowance, Dyed in the Wool, Seam Squad.
        Entity_Master node = LoomNode(ModKitUtil.Pos(hm), 20f);
        SpoolLoomBrain loomD = null;
        at = new Vector3(float.NaN, 0f, 0f);
        if (node != null)
        {
            yield return Walk(hm, ModKitUtil.Pos(node), 3f, 20f);
            at = ModKitTest.Spot(ModKitUtil.Pos(node), loomBeing, team, 1.5f, 2.3f, used, "Loom D");
        }
        if (float.IsNaN(at.x))
            at = Spot(hm, loomBeing, 3f);   // no node: Seam Squad is still tested
        if (!float.IsNaN(at.x))
        {
            LoomTopUp(owner);
            yield return Build(hm, SpoolData.Loom, at, known, got);
            loomD = got[0] as SpoolLoomBrain;
        }
        if (!Check("build Loom D (Seam Allowance, Dyed in the Wool, Seam Squad)", loomD != null, "at " + at + (node != null ? " beside " + ModKitUtil.BeingName(node) : ", no node within 20 tiles")))
            yield break;
        yield return Upgrade(loomD, "Seam2", owner);
        yield return Upgrade(loomD, "Dyed3", owner);
        {
            Link_Command colour;
            bool button = loomD.Master.Commandlistserver.TryGetValue(SpoolData.ColourLink, out colour) && colour != null;
            Check("Dyed in the Wool: a COLOUR button, ALL to start", Has(loomD, "Dyed3") && button && "" + colour.Goalstring == SpoolData.Colours[0] && !loomD.Dyed,
                "button " + button + ", reads " + (button ? "" + colour.Goalstring : "-"));
            Send(loomD.Master, SpoolData.ColourLink, "");
            yield return Wait(0.4f);
            Check("COLOUR: one press picks Red, shown on the button", loomD.Colour == "Redaura" && loomD.Dyed && button && "" + colour.Goalstring == "Redaura",
                "colour " + loomD.Colour + ", button " + (button ? "" + colour.Goalstring : "-"));
#if SPOOL_SHOTS
            yield return SpoolShots.Hud(loomD.Master, "colour_red");
#endif
        }
        Entity_Master ore = Helper_Entityquerytools.Entitynearest(loomD.Pos, ModKitUtil.Stat(loomD.Master, "Range", 2.5f), ModKitUtil.Tagged("Resource"));
        if (ore == null || ore.Statsserver == null || !ore.Statsserver.ContainsKey("Healthresource"))
            ModKitTest.Setup("Dyed in the Wool's trade", "no node in Loom D's reach");
        else
        {
            // Every hit pays while the node's count is held at 0, so the Loom's strikes and its Seam's both pay soon.
            int traded = loomD.Traded, seamTraded = loomD.SeamTraded, strikes = loomD.Strikes, payouts = loomD.Payouts;
            float until = Time.time + 40f;
            Link_Stats count;
            while ((loomD.Traded == traded || loomD.SeamTraded == seamTraded) && Time.time < until && ModKitUtil.Alive(ore))
            {
                if (ore.Statsserver.TryGetValue("Healthresource", out count))
                    count.Value = 0f;
                yield return null;
            }
            string detail = ModKitUtil.BeingName(ore) + ": strikes +" + (loomD.Strikes - strikes) + ", payouts +" + (loomD.Payouts - payouts) + ", traded +" + (loomD.Traded - traded)
                + ", from the Seam's hits +" + (loomD.SeamTraded - seamTraded) + ", last " + F(loomD.LastIn) + " in -> " + F(loomD.LastOut) + " Red";
            Check("Dyed in the Wool: the Loom's own payout comes in Red, at 70% of its total", loomD.Traded > traded && loomD.LastIn > 0f && Mathf.Abs(loomD.LastOut - 0.7f * loomD.LastIn) < 0.001f, detail);
            Check("Dyed in the Wool: its Seam's payout is traded too", loomD.SeamTraded > seamTraded, detail);
            // The trade on the purse itself, read around one: a gain of 10 Blue goes back and 7 Red comes in.
            float[] p0 = new float[3], p1 = new float[3];
            bool read = ModKitUtil.Purse(owner, p0) && loomD.Trade(new float[3], new float[] { 0f, 10f, 0f }) && ModKitUtil.Purse(owner, p1);
            Check("Dyed in the Wool: a 10 Blue gain leaves the purse as 7 Red, the Blue taken back", read && Mathf.Abs(p1[0] - p0[0] - 7f) < 0.05f && Mathf.Abs(p1[1] - p0[1] + 10f) < 0.05f && Mathf.Abs(p1[2] - p0[2]) < 0.05f,
                read ? "Red " + F(p1[0] - p0[0]) + ", Blue " + F(p1[1] - p0[1]) + ", Yellow " + F(p1[2] - p0[2]) : "purse not read");
        }

        // Seam Squad: 3 Seams, each back 10 s after it falls.
        LoomTopUp(owner);
        yield return Upgrade(loomD, "Squad4", owner);
        yield return Until(() => LoomSeams(loomD).Count >= 3, 5f);
        List<SpoolUnitBrain> squad = LoomSeams(loomD);
        Check("Seam Squad: 3 Seams per Loom", Has(loomD, "Squad4") && squad.Count == 3 && loomD.SeamCount == 3, "Seams " + squad.Count);
        if (squad.Count == 3)
        {
            Kill(squad[0].Master);
            float t0 = Time.time;
            yield return Wait(1f);
            int left = LoomSeams(loomD).Count;
            SpoolUnitBrain again = null;
            yield return Until(() => (again = LoomSeams(loomD).Find(u => !squad.Contains(u))) != null, 13f);
            Check("Seam Squad: a fallen Seam comes back after 10 s, the other two stay", left == 2 && again != null && Time.time - t0 > 9f && LoomSeams(loomD).Count == 3,
                "left " + left + ", back after " + F(Time.time - t0) + " s, Seams " + LoomSeams(loomD).Count);
        }
        ClearFoes();
    }
}
#endif
