#if ARGO_SELFTEST && ARGO_REVIEW
// Debug build only (Build-ArgoMod.ps1 -Define ARGO_SELFTEST,ARGO_REVIEW): the PC checks the self-test's scenario doesn't reach, in one short
// match: docs/reports/SIMPLIFICATION_BUILD_2026-11-04.md §4 G1, G4, A1 and docs/reports/CODE_REVIEW_2026-11-04.md §6 ARG-1. A finding's reading
// is a line "TEST INFO FINDING <id>: SEEN|not seen (...)", so it says the same before and after the fix and fails nothing.
public static partial class ArgoSelfTest
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

    public static int UpgradeSounds;   // G4: each time a client plays the upgrade sound (10_Client.cs)

    // One purchase, as the panel sends it: what the wallet lost, the end screen's count, how many border flashes and upgrade sounds.
    static IEnumerator BuyChecked(ArgoTowerBrain b, string key, long owner)
    {
        string id = b.Def.Id + "_" + key + "_Upgradeobject";
        Upgradeproperties up = Bl_Address.Getload("Upgrade", id) as Upgradeproperties;
        float price = 0f;
        if (up != null && up.Costs != null)
            foreach (Link_Resourceset c in up.Costs) price += c.Amount;
        float w0 = Wallet(owner), scale = PriceScale(b.Master);
        int count0 = UpgradedCount(owner), border0 = Borders(), most = 0, sounds0 = UpgradeSounds;
        Send(b.Master, "Upgrade", "{\"Upgrade\":\"" + id + "\",\"Upgrader\":" + owner + "}");
        float t = 0f;
        while (t < 1f)
        {
            most = Mathf.Max(most, Borders() - border0);
            t += Time.deltaTime;
            yield return null;
        }
        float paid = w0 - Wallet(owner);
        // The Drill's own mining can pay a little in the second this takes, so the wallet is read with room for it.
        Check("G1 " + b.Def.Id + " " + key + ": bought at its price, Upgraded +1", Has(b, key) && Mathf.Abs(paid - price * scale) < 4f && UpgradedCount(owner) == count0 + 1,
            "paid " + F(paid) + " of " + F(price) + " x " + F(scale) + ", Upgraded " + count0 + "->" + UpgradedCount(owner) + ", border flashes " + most);
        Check("G1 " + b.Def.Id + " " + key + ": the border flashes once, not twice", most <= 1, most + " flashes");
        Check("G4 " + b.Def.Id + " " + key + ": one upgrade sound for the buy", UpgradeSounds - sounds0 == 1, (UpgradeSounds - sounds0) + " sounds");
    }

    // What this foe's hit of 'amount' takes from this tower, on a real hit.
    static IEnumerator TakenBy(Entity_Master foe, Entity_Master tower, float amount, float[] taken)
    {
        taken[0] = -1f;
        if (foe == null || !ModKitUtil.Alive(foe) || tower == null || !ModKitUtil.Alive(tower)) yield break;
        float h0 = ModKitUtil.Health(tower);
        ModKitCombat.Hurt(foe, tower, amount, 1f, null, null);
        yield return Wait(0.15f);
        taken[0] = h0 - ModKitUtil.Health(tower);
    }

    static IEnumerator Heal(Entity_Master tower)
    {
        Link_Stats s;
        if (tower != null && tower.Statsserver != null && tower.Statsserver.TryGetValue("Health", out s)) s.Value = s.Value_Calc;
        yield return null;
    }

    static IEnumerator ReviewSteps()
    {
        Log("match started: the review's checks begin");
        ArgoHeroBrain hero = ArgoServer.Heroes[0];
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Wave.Value >= 1, 150f);
        freeze = true;
        yield return Wait(2f);
        ClearEnemies();
        Entity_Master hm = hero.Master;
        long owner = ModKitUtil.Owner(hm);
        int team = ModKitUtil.Team(hm);
        ModKitTest.Log = Log;
        Helper_Resources.Giveresources(ArgoUtil.Costs("Redaura=30000;Blueaura=30000;Yellowaura=20000"), owner, Helper_Resources.Styles.Default);
        yield return Wait(0.5f);
        List<ArgoTowerBrain> known = new List<ArgoTowerBrain>(ArgoServer.Towers);
        ArgoTowerBrain[] got = new ArgoTowerBrain[1];
        Vector3 home = ModKitUtil.Pos(hm);
        ArgoTowerBrain scatter = null, harpoon = null, bell = null;

        // G1, G4: one building of each kind with a tree, its panel, one purchase.
        foreach (string[] one in new[] { new[] { ArgoData.Scatter, "Widebore2" }, new[] { ArgoData.Harpoon, "Longline2" }, new[] { ArgoData.Bell, "Flippers2" }, new[] { ArgoData.Water, "" } })
        {
            Beingobject being = ArgoContent.Beings[one[0]];
            Vector3 at = one[0] == ArgoData.Water ? WaterSpot(home, being, team) : one[0] == ArgoData.Harpoon ? HarpoonSpot(home, being, team) : Spot(home, being, team, one[0] == ArgoData.Bell ? 1.5f : 2.5f, 6f);
            if (float.IsNaN(at.x)) { ModKitTest.Setup("G1 " + one[0], "no free tile"); continue; }
            yield return Build(hm, one[0], at, known, got);
            if (got[0] == null) { ModKitTest.Setup("G1 " + one[0], "it wasn't built"); continue; }
            ArgoTowerBrain b = got[0];
            if (one[0] == ArgoData.Scatter) scatter = b;
            if (one[0] == ArgoData.Harpoon) harpoon = b;
            if (one[0] == ArgoData.Bell) bell = b;
            yield return Wait(0.7f);
            Log("INFO G1 panel of " + one[0] + ": " + Panel(b.Master));
            if (one[1] != "") yield return BuyChecked(b, one[1], owner);
        }

        // A1: Bulkhead, on real hits: a tuned tower takes 25% less, the armour moves with the Naut, and it ends.
        if (bell != null && scatter != null && harpoon != null)
        {
            yield return Upgrade(bell, "Salts2", owner);
            Force(bell, "Bulkhead4");
            yield return Wait(0.6f);
            Check("A1 the Bell holds Bulkhead", Has(bell, "Bulkhead4"), Panel(bell.Master));
            yield return Until(() => bell.Crew[0] != null, 15f);
            Entity_Master core = ModKitTest.Core();
            Entity_Master foe = null;
            if (core != null) foe = ModKitTest.SpawnFoe("Enemy_Dagger", ModKitUtil.Pos(core), 1f, 6f, null);
            yield return Wait(0.5f);
            float[] a = new float[1], b = new float[1];
            if (foe == null || bell.Crew[0] == null) ModKitTest.Setup("A1", "no far foe, or no Naut");
            else
            {
                Send(bell.Master, "Crew_Port", Ref(scatter.Master));
                yield return Until(() => Tuned(bell, 0, scatter), 15f);
                yield return Wait(0.3f);
                yield return Heal(scatter.Master);
                yield return Heal(harpoon.Master);
                yield return TakenBy(foe, scatter.Master, 8f, a);
                yield return TakenBy(foe, harpoon.Master, 8f, b);
                Check("A1 a tuned tower takes 25% less, the tower beside it the full hit", Tuned(bell, 0, scatter) && Mathf.Abs(a[0] - 6f) < 0.05f && Mathf.Abs(b[0] - 8f) < 0.05f, "tuned " + F(a[0]) + ", untuned " + F(b[0]) + " of 8");
                Send(bell.Master, "Crew_Port", Ref(harpoon.Master));
                yield return Until(() => Tuned(bell, 0, harpoon), 15f);
                yield return Wait(0.3f);
                yield return Heal(scatter.Master);
                yield return Heal(harpoon.Master);
                yield return TakenBy(foe, scatter.Master, 8f, a);
                yield return TakenBy(foe, harpoon.Master, 8f, b);
                Check("A1 retuned to another tower, the armour moves", Tuned(bell, 0, harpoon) && Mathf.Abs(a[0] - 8f) < 0.05f && Mathf.Abs(b[0] - 6f) < 0.05f, "old tower " + F(a[0]) + ", new tower " + F(b[0]) + " of 8");
                Send(bell.Master, "Crew_Port", Ref(bell.Master));
                yield return Wait(1f);
                yield return Heal(harpoon.Master);
                yield return TakenBy(foe, harpoon.Master, 8f, b);
                Check("A1 untuned, it ends", !Tuned(bell, 0, harpoon) && Mathf.Abs(b[0] - 8f) < 0.05f, F(b[0]) + " of 8");
            }
            ClearEnemies();
        }

        // ARG-1: the bounds of a foe's attack speed, and two stuns on one foe.
        if (harpoon == null) { ModKitTest.Setup("ARG-1", "no Harpoon Gun"); yield break; }
        foreach (string kind in new[] { "Enemy_Dagger", "Enemy_Spear", "Enemy_Halberd", "Enemy_Bulwark", "Enemy_Archer" })
        {
            Entity_Master e = ModKitTest.Core() != null ? ModKitTest.SpawnFoe(kind, ModKitUtil.Pos(ModKitTest.Core()), 1f, 6f, null) : null;
            if (e == null) continue;
            yield return Wait(0.4f);
            Link_Stats s;
            if (e.Statsserver != null && e.Statsserver.TryGetValue("Attackspeed", out s)) Info("ARG-1 bounds of " + kind, "Attackspeed " + F(s.Value) + ", bounds " + F(s.Value_Bounds.x) + ".." + F(s.Value_Bounds.y));
            else Info("ARG-1 bounds of " + kind, "no Attackspeed stat");
            ClearEnemies();
        }
        // The double stun itself runs in the scenario (99_SelfTest.cs), where a foe beside a gun is to hand.

        // ARG-4: a tuned Drill Rig with Calibrate reaches farther: does its AIM's synced reach follow?
        ArgoTowerBrain drill = null;
        List<Entity_Master> ores = new List<Entity_Master>(ModKitUtil.Area(home, 45f, ModKitUtil.Tagged("Resource")));
        ores.Sort((a, b) => ModKitUtil.Flat(ModKitUtil.Pos(a), home).CompareTo(ModKitUtil.Flat(ModKitUtil.Pos(b), home)));
        foreach (Entity_Master o in ores)
        {
            if (o == null || !ModKitUtil.Alive(o)) continue;
            Vector3 dat = Spot(ModKitUtil.Pos(o), ArgoContent.Beings[ArgoData.Drill], team, 1f, ArgoData.OreReach - 0.05f);
            if (float.IsNaN(dat.x)) continue;
            yield return Build(hm, ArgoData.Drill, dat, known, got);
            drill = got[0];
            break;
        }
        if (drill == null || bell == null || bell.Crew[0] == null) { ModKitTest.Setup("ARG-4", "no Drill Rig, Bell or Naut"); yield break; }
        Force(bell, "Calibrate3");
        yield return Wait(0.6f);
        Link_Command aim;
        drill.Master.Commandlistserver.TryGetValue("Aim", out aim);
        string aim0 = aim != null ? aim.Goalstring.ToString() : "-";
        float range0 = ModKitUtil.Stat(drill.Master, "Range", 0f);
        Send(bell.Master, "Crew_Port", Ref(drill.Master));
        yield return Until(() => Tuned(bell, 0, drill), 15f);
        yield return Wait(1f);
        string aim1 = aim != null ? aim.Goalstring.ToString() : "-";
        float range1 = ModKitUtil.Stat(drill.Master, "Range", 0f);
        Finding("ARG-4", range1 > range0 && aim1 == aim0, "tuned " + Tuned(bell, 0, drill) + ": Range " + F(range0) + " -> " + F(range1) + ", the AIM entry's reach '" + aim0 + "' -> '" + aim1 + "'");
    }
}
#endif
