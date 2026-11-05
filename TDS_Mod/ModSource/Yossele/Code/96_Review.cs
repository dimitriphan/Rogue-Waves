#if YOSSELE_SELFTEST && YOSSELE_REVIEW
// Debug build only (Build-YosseleMod.ps1 -Define YOSSELE_SELFTEST,YOSSELE_REVIEW): the PC checks the self-test's scenario doesn't reach, in
// one short match: docs/reports/SIMPLIFICATION_BUILD_2026-11-04.md §4 G1 and docs/reports/CODE_REVIEW_2026-11-04.md §6 YOS-1. A finding's
// reading is a line "TEST INFO FINDING <id>: SEEN|not seen (...)", so it says the same before and after the fix and fails nothing.
public static partial class YosseleSelfTest
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

    // One purchase, as the panel sends it: what the wallet lost, the end screen's count, how many border flashes.
    static IEnumerator BuyChecked(YosseleBuilding b, string key, long owner)
    {
        string id = b.Def.Id + "_" + key + "_Upgradeobject";
        Upgradeproperties up = Bl_Address.Getload("Upgrade", id) as Upgradeproperties;
        float price = 0f;
        if (up != null && up.Costs != null)
            foreach (Link_Resourceset c in up.Costs) price += c.Amount;
        float w0 = Wallet(owner);
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
        // The Sapling's own mining can pay a little in the second this takes, so the wallet is read with room for it.
        Check("G1 " + b.Def.Id + " " + key + ": bought at its price, Upgraded +1", Has(b, key) && Mathf.Abs(paid - price) < 4f && UpgradedCount(owner) == count0 + 1,
            "paid " + F(paid) + " of " + F(price) + ", Upgraded " + count0 + "->" + UpgradedCount(owner) + ", border flashes " + most);
        Check("G1 " + b.Def.Id + " " + key + ": the border flashes once, not twice", most <= 1, most + " flashes");
    }

    static IEnumerator ReviewSteps()
    {
        Log("match started: the review's checks begin");
        YosseleHeroBrain hero = YosseleServer.Heroes[0];
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Wave.Value >= 1, 150f);
        freeze = true;
        yield return Wait(2f);
        ClearFoes();
        Entity_Master hm = hero.Master;
        long owner = ModKitUtil.Owner(hm);
        ModKitTest.Log = Log;
        Helper_Resources.Giveresources(ModKitUtil.Costs("Redaura=20000;Blueaura=20000;Yellowaura=20000"), owner, Helper_Resources.Styles.Default);
        yield return Wait(0.5f);
        List<YosseleBuilding> known = new List<YosseleBuilding>(YosseleServer.Buildings);
        YosseleBuilding[] got = new YosseleBuilding[1];
        YosseleBuilding sapling = null;

        foreach (string[] one in new[] { new[] { YosseleData.Cannon, "Sticky2" }, new[] { YosseleData.Dandelion, "Packets2" }, new[] { YosseleData.Pachira, "Focused2" }, new[] { YosseleData.Henge, "" } })
        {
            Beingobject being = YosseleContent.Beings[one[0]];
            Vector3 at = Spot(hm, being, 3f);
            if (float.IsNaN(at.x)) { ModKitTest.Setup("G1 " + one[0], "no free tile"); continue; }
            yield return Build(hm, one[0], at, known, got);
            if (got[0] == null) { ModKitTest.Setup("G1 " + one[0], "it wasn't built"); continue; }
            YosseleBuilding b = got[0];
            if (one[0] == YosseleData.Pachira) sapling = b;
            yield return Wait(0.7f);
            Log("INFO G1 panel of " + one[0] + ": " + Panel(b.Master));
            if (one[1] != "") yield return BuyChecked(b, one[1], owner);
        }

        // YOS-1: a fully grown Grafted Sapling set to BLUE pays a node out: what does its fruit wear?
        YosselePachiraBrain tree = sapling as YosselePachiraBrain;
        if (tree == null) { ModKitTest.Setup("YOS-1", "no Sapling"); yield break; }
        yield return Upgrade(tree, "Grafted3", owner);
        yield return Wait(0.5f);
        Send(tree.Master, YosseleData.ColourLink, "");
        yield return Wait(0.4f);
        Send(tree.Master, YosseleData.ColourLink, "");
        yield return Wait(0.4f);
        tree.SetGrowth(tree.Max);
        yield return Wait(0.3f);
        Info("the Sapling for YOS-1", "colour " + tree.Colour + ", Growth " + tree.Growth + " of " + tree.Max + ", its skin '" + tree.Master.Skin.Value + "'");
        List<Entity_Master> lying = YosseleFruit.All();
        tree.NodeEmptied(tree.Master, ModKitUtil.Pos(tree.Master) + Vector3.right * 1.5f);
        Entity_Master fruit = null;
        foreach (Entity_Master f in YosseleFruit.All())
            if (!lying.Contains(f)) fruit = f;
        if (fruit == null) { ModKitTest.Setup("YOS-1", "no fruit dropped: failed " + YosseleFruit.Failed); yield break; }
        Finding("YOS-1", fruit.Skin.Value.ToString() != tree.Colour, "the BLUE fruit's skin '" + fruit.Skin.Value + "', the Sapling's '" + tree.Master.Skin.Value + "'");
#if YOSSELE_SHOTS
        yield return YosseleShots.Close("fruit_blue", ModKitUtil.Pos(fruit), 3f);
#endif
    }
}
#endif
