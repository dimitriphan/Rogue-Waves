#if SPOOL_SELFTEST && SPOOL_REVIEW
// Debug build only (Build-SpoolMod.ps1 -Define SPOOL_SELFTEST,SPOOL_REVIEW): the PC checks the self-test's scenario doesn't reach, in one
// short match: docs/reports/SIMPLIFICATION_BUILD_2026-11-04.md §4 G1 and docs/reports/CODE_REVIEW_2026-11-04.md §6 SPL-1, SPL-2. A finding's
// reading is a line "TEST INFO FINDING <id>: SEEN|not seen (...)", so it says the same before and after the fix and fails nothing.
public static partial class SpoolSelfTest
{
    static void Finding(string id, bool seen, string detail) { Log("INFO FINDING " + id + ": " + (seen ? "SEEN" : "not seen") + " (" + detail + ")"); }
    static void Info(string name, string detail) { Log("INFO " + name + ": " + detail); }

    // Unity runs a yielded coroutine by itself, so an exception in one never reaches the caller: this steps them all here.
    static IEnumerator Review()
    {
        List<IEnumerator> stack = new List<IEnumerator> { ReviewSteps() };
        while (stack.Count > 0)
        {
            object step = null;
            bool more = false;
            try
            {
                more = stack[stack.Count - 1].MoveNext();
                if (more) step = stack[stack.Count - 1].Current;
            }
            catch (Exception e)
            {
                Check("the review's checks ran to their end", false, e.ToString());
                break;
            }
            if (!more) { stack.RemoveAt(stack.Count - 1); continue; }
            if (step is IEnumerator) { stack.Add((IEnumerator)step); continue; }
            yield return step;
        }
        freeze = false;
        Log("DONE pass=" + pass + " fail=" + fail + " setup=" + ModKitTest.Setups);
        phase = 9;
    }

    static float PriceScale(Entity_Master m) { Link_Command c; return m != null && m.Commandlistserver != null && m.Commandlistserver.TryGetValue("Upgrade", out c) && c != null ? c.Goalvalue : 1f; }
    static float Wallet(long owner) { return Res(owner, "Redaura") + Res(owner, "Blueaura") + Res(owner, "Yellowaura"); }

    static int UpgradedCount(long owner)
    {
        GameObject player;
        Tds_Clientinfo info;
        if (Blm_Server.Instance == null || !Blm_Server.Instance.Sessionids.TryGetValue(owner, out player) || player == null || !player.TryGetComponent(out info) || info.Activeplayerdata == null)
            return -1;
        return info.Activeplayerdata.Upgraded;
    }

    static int Borders()
    {
        int n = 0;
        foreach (Vfx_Base v in UnityEngine.Object.FindObjectsOfType<Vfx_Base>())
            if (v != null && v.name.IndexOf("Upgradeborder") >= 0) n++;
        return n;
    }

    static string Panel(Entity_Master m)
    {
        List<string> names = new List<string>();
        foreach (Link_Commandref c in m.Commandlist) names.Add(c.Name.ToString() + (c.Goalstring.ToString() != "" ? "=" + c.Goalstring : ""));
        return string.Join(", ", names.ToArray());
    }

    // One purchase, as the panel sends it: what the wallet lost, the end screen's count, how many border flashes.
    static IEnumerator BuyChecked(SpoolBuilding b, string key, long owner)
    {
        string id = b.Def.Id + "_" + key + "_Upgradeobject";
        Upgradeproperties up = Bl_Address.Getload("Upgrade", id) as Upgradeproperties;
        float price = 0f;
        if (up != null && up.Costs != null)
            foreach (Link_Resourceset c in up.Costs) price += c.Amount;
        float w0 = Wallet(owner), scale = PriceScale(b.Master);
        int count0 = UpgradedCount(owner), border0 = Borders(), most = 0;
        Send(b.Master, "Upgrade", "{\"Upgrade\":\"" + id + "\",\"Upgrader\":" + owner + "}");
        float t = 0f;
        while (t < 1f)
        {
            most = Mathf.Max(most, Borders() - border0);
            t += Time.deltaTime;
            yield return null;
        }
        float paid = w0 - Wallet(owner);
        Check("G1 " + b.Def.Id + " " + key + ": bought at its price, Upgraded +1", Has(b, key) && Mathf.Abs(paid - price * scale) < 1f && UpgradedCount(owner) == count0 + 1,
            "paid " + F(paid) + " of " + F(price) + " x " + F(scale) + ", Upgraded " + count0 + "->" + UpgradedCount(owner) + ", border flashes " + most);
        Check("G1 " + b.Def.Id + " " + key + ": the border flashes once, not twice", most <= 1, most + " flashes");
    }

    static void ClearCrafted(long owner)
    {
        foreach (Entity_Master e in new List<Entity_Master>(Game_Entity.Instance.Entityclient))
            if (e != null && ModKitUtil.Owner(e) == owner && (ModKitUtil.BeingName(e) == SpoolData.Stitched || ModKitUtil.BeingName(e) == SpoolData.Dart) && ModKitUtil.Alive(e))
                Game_Entity.Instance.Despawn(e.Beingserver, false, "Selftest");
    }

    static Entity_Master AtSpot(string being, long owner, Vector3 spot)
    {
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && ModKitUtil.Owner(e) == owner && ModKitUtil.BeingName(e) == being && ModKitUtil.Alive(e) && ModKitUtil.Flat(ModKitUtil.Pos(e), spot) <= 2f)
                return e;
        return null;
    }

    static IEnumerator ReviewSteps()
    {
        Log("match started: the review's checks begin");
        SpoolHeroBrain hero = SpoolServer.Heroes[0];
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Wave.Value >= 1, 150f);
        freeze = true;
        yield return Wait(2f);
        ClearFoes();
        Entity_Master hm = hero.Master;
        long owner = ModKitUtil.Owner(hm);
        SpoolPlayer player = SpoolServer.Player(owner);
        ModKitTest.Log = Log;
        Helper_Resources.Giveresources(ModKitUtil.Costs("Redaura=20000;Blueaura=20000;Yellowaura=20000"), owner, Helper_Resources.Styles.Default);
        yield return Wait(0.5f);
        List<SpoolBuilding> known = new List<SpoolBuilding>();
        SpoolBuilding[] got = new SpoolBuilding[1];
        Vector3 at = Spot(hm, SpoolContent.Beings[SpoolData.Stash], 2.2f);
        yield return Build(hm, SpoolData.Stash, at, known, got);
        SpoolBuilding stash = got[0];
        if (stash == null) { ModKitTest.Setup("the review", "no Yarn Stash"); yield break; }
        yield return Wait(1f);

        // A Thread Head away from the Stash, with no Tin up yet to spend what the Stash holds.
        Beingobject token = SpoolContent.Beings[SpoolData.HeadToken];
        at = Spot(hm, token, 6f);
        if (float.IsNaN(at.x)) at = Spot(hm, token, 4.5f);
        if (float.IsNaN(at.x)) { ModKitTest.Setup("SPL-1, SPL-2", "no tile for a Thread Head"); yield break; }
        Send(hm, "Build", "{\"Tower\":\"" + SpoolData.HeadToken + "\",\"Position\":" + Vec(at) + ",\"Orientation\":0,\"Owner\":" + owner + "}");
        SpoolHeadBrain head = null;
        yield return Until(() => (head = NewHead(known)) != null, 20f);
        if (head == null) { ModKitTest.Setup("SPL-1, SPL-2", "no Thread Head"); yield break; }
        known.Add(head);
        yield return Wait(1f);
        ClearDrops();
        Vector3 home = ModKitUtil.Pos(stash.Master);
        Info("the Thread Head's spot", F(ModKitUtil.Flat(head.Spot, home)) + " tiles from the Stash");

        // SPL-2: RETURN carrying nothing. Ten seconds after it reaches the Stash, is it walking back?
        head.Carried = 0;
        Send(head.Master, SpoolData.ReturnLink, "");
        yield return Until(() => ModKitUtil.Flat(head.Pos, home) <= SpoolData.DropOff + 0.5f, 30f);
        bool reached = ModKitUtil.Flat(head.Pos, home) <= SpoolData.DropOff + 0.5f;
        yield return Wait(10f);
        Finding("SPL-2", reached && head.Returning && ModKitUtil.Flat(head.Pos, home) < ModKitUtil.Flat(head.Spot, home) * 0.6f,
            "reached the Stash " + reached + "; 10 s on: Returning " + head.Returning + ", " + F(ModKitUtil.Flat(head.Pos, home)) + " from the Stash, " + F(ModKitUtil.Flat(head.Pos, head.Spot)) + " from its spot");
        Send(head.Master, SpoolData.SpotLink, At(head.Spot));
        yield return Until(() => ModKitUtil.Flat(head.Pos, head.Spot) < 1f, 25f);
        Info("back at its spot", F(ModKitUtil.Flat(head.Pos, head.Spot)) + " tiles from it, Returning " + head.Returning);

        // SPL-1: Threaded Delivery, a full Stash, a full Head: does it stay by the Stash, turning round?
        yield return BuyChecked(head, "Delivery2", owner);
        player.Stash = stash is SpoolStashBrain ? ((SpoolStashBrain)stash).Cap : (int)SpoolData.StashCap;
        SpoolServer.Show(player);
        head.Carried = head.Cap;
        int flips = 0;
        bool last = head.Returning;
        float t = 0f;
        float nearest = 999f;
        while (t < 12f)
        {
            if (head.Returning != last) { flips++; last = head.Returning; }
            nearest = Mathf.Min(nearest, ModKitUtil.Flat(head.Pos, home));
            t += Time.deltaTime;
            yield return null;
        }
        float fromSpot = ModKitUtil.Flat(head.Pos, head.Spot), fromHome = ModKitUtil.Flat(head.Pos, home);
        Finding("SPL-1", fromSpot > 0.9f && fromHome < 3f, "after 12 s: " + F(fromHome) + " from the Stash, " + F(fromSpot) + " from its spot, Returning flipped " + flips + " times, Stash " + player.Stash + " of " + player.Cap + ", carried " + head.Carried);
        head.Carried = 0;
        yield return Until(() => !head.Returning, 3f);

#if SPOOL_SHOTS
        // The sage look: Spool beside a Hardened foe, then on the map's green ground by the nearest ore nodes.
        Entity_Master hardened = SpawnFoe("Enemy_Dagger", ModKitUtil.Pos(hm), 2.5f, 1f);
        Upgradeproperties harden = Bl_Address.Getload("Upgrade", "Enemy_Health_Upgradeobject") as Upgradeproperties;
        if (hardened != null && harden != null)
        {
            yield return Wait(0.5f);
            Helper_Entitytools.Upgrade(hardened, harden, Helper_Entitytools.Upgradecheck.Forced, null);
            yield return Wait(0.5f);
            Info("the Hardened foe", "skin '" + hardened.Skin.Value + "'");
            yield return SpoolShots.Close("sage_hardened", ModKitUtil.Pos(hm), 3.5f);
            ClearFoes();
        }
        Vector3 from = ModKitUtil.Pos(hm);
        List<Entity_Master> ores = new List<Entity_Master>(ModKitUtil.Area(from, 45f, ModKitUtil.Tagged("Resource")));
        ores.Sort((a, b) => ModKitUtil.Flat(ModKitUtil.Pos(a), from).CompareTo(ModKitUtil.Flat(ModKitUtil.Pos(b), from)));
        for (int i = 0; i < ores.Count && i < 3; i++)
        {
            Entity_Master ore = ores[i];
            if (ore == null || !ModKitUtil.Alive(ore)) continue;
            ModKitUtil.MoveTo(hm, ore, Vector3.zero, 1f);
            yield return Until(() => ModKitUtil.Flat(ModKitUtil.Pos(hm), ModKitUtil.Pos(ore)) <= 2f, 15f);
            yield return SpoolShots.Close("sage_ground_" + i, ModKitUtil.Pos(hm), 3.5f);
        }
#endif

        // G1: the other buildings that have upgrades. The Stash has none on his sheet.
        foreach (string[] one in new[] { new[] { SpoolData.Tin, "Recycle2" }, new[] { SpoolData.Cushion, "Sharper2" }, new[] { SpoolData.Loom, "" } })
        {
            Beingobject being = SpoolContent.Beings[one[0]];
            at = Spot(hm, being, 3f);
            if (float.IsNaN(at.x)) { ModKitTest.Setup("G1 " + one[0], "no free tile"); continue; }
            yield return Build(hm, one[0], at, known, got);
            if (got[0] == null) { ModKitTest.Setup("G1 " + one[0], "it wasn't built: menu " + hero.Menu()); continue; }
            SpoolBuilding b = got[0];
            yield return Wait(0.7f);
            Log("INFO G1 panel of " + one[0] + ": " + Panel(b.Master));
            if (one[1] != "") yield return BuyChecked(b, one[1], owner);
        }
#if MODKIT_PLAY
        // PR #86's smaller Crafted by a virtual hand: at their Tin's spot, not at the Tin they stand inside until one is picked.
        SpoolTinBrain tin = null;
        foreach (SpoolBuilding b in known) if (b is SpoolTinBrain) tin = (SpoolTinBrain)b;
        if (tin == null) { ModKitTest.Setup("the Crafted by hand", "no Cookie Tin"); yield break; }
        ModKitPlay.Log = m => Log("INFO hud: " + m);
        ModKitPlay.Host = SpoolMod.Instance;
#if SPOOL_SHOTS
        ModKitPlay.Shot = SpoolShots.Shot;
#endif
        ModKitPlay.Init();
        ModKitPlay.Reset(hm);
        yield return ModKitPlay.WaitForWorld(30f);
        Genitem tile = ModKitTest.FoeTile(SpoolContent.Beings[SpoolData.Stitched], tin.Pos, 3f, 5f, used);
        if (tile == null) { ModKitTest.Setup("the Crafted by hand", "no tile for the Tin's spot"); yield break; }
        Send(tin.Master, SpoolData.TargetLink, At(tile.Position));
        yield return Wait(0.6f);
        foreach (string kind in new[] { SpoolData.Stitched, SpoolData.Dart })
        {
            if (kind == SpoolData.Dart) yield return Upgrade(tin, "Design3", owner);
            ClearCrafted(owner);
            player.Stash = 50;
            SpoolServer.Show(player);
            Entity_Master unit = null;
            string want = kind;
            yield return Until(() => (unit = AtSpot(want, owner, tile.Position)) != null, 40f);
            if (unit == null) { ModKitTest.Setup("the Crafted by hand: " + kind, "none reached the Tin's spot in 40 s"); continue; }
#if SPOOL_SHOTS
            yield return SpoolShots.Close("crafted_" + kind, ModKitUtil.Pos(unit), 3.5f);
#endif
            int picks = 0;
            for (int i = 0; i < 3; i++)
            {
                unit = AtSpot(want, owner, tile.Position);
                if (unit == null) break;
                yield return ModKitPlay.Select(unit, kind);
                if (ModKitPlay.IsSelected(unit)) picks++;
                yield return ModKitPlay.Cancel();
                yield return Wait(0.3f);
            }
            Check("The smaller art: a click selects a " + kind + " at its spot (3 tries)", picks == 3, picks + " of 3, last " + ModKitPlay.Selected());
            unit = AtSpot(want, owner, tile.Position);
            if (unit == null) { ModKitTest.Setup("selling a " + kind, "none left at the spot"); continue; }
            int stashBefore = player.Stash;
            yield return ModKitPlay.Sell(unit, kind);
            Check("The smaller art: SELL and a click sell a " + kind, ModKitPlay.LastOk, ModKitPlay.Counts() + ", Stash " + stashBefore + " -> " + player.Stash);
        }
#endif
    }
}
#endif
