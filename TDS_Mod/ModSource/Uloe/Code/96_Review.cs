#if ULOE_SELFTEST && ULOE_REVIEW
// Debug build only (Build-UloeMod.ps1 -Define ULOE_SELFTEST,ULOE_REVIEW): the PC checks the self-test's scenario doesn't reach, in one
// short match: docs/reports/SIMPLIFICATION_BUILD_2026-11-04.md §4 G1 and docs/reports/CODE_REVIEW_2026-11-04.md §6 ULO-1. A finding's
// reading is a line "TEST INFO FINDING <id>: SEEN|not seen (...)", so it says the same before and after the fix and fails nothing.
public static partial class UloeSelfTest
{
    static void Finding(string id, bool seen, string detail) { Log("INFO FINDING " + id + ": " + (seen ? "SEEN" : "not seen") + " (" + detail + ")"); }

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
    static IEnumerator BuyChecked(UloeBuilding b, string key, long owner)
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

    static IEnumerator ReviewSteps()
    {
        Log("match started: the review's checks begin");
        UloeHeroBrain hero = UloeServer.Heroes[0];
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Wave.Value >= 1, 150f);
        freeze = true;
        yield return Wait(2f);
        ClearFoes();
        Entity_Master hm = hero.Master;
        long owner = ModKitUtil.Owner(hm);
        ModKitTest.Log = Log;
        Helper_Resources.Giveresources(ModKitUtil.Costs("Redaura=20000;Blueaura=20000;Yellowaura=20000"), owner, Helper_Resources.Styles.Default);
        yield return Wait(0.5f);
        List<UloeBuilding> known = new List<UloeBuilding>();
        UloeBuilding[] got = new UloeBuilding[1];
        UloeBuilding bank = null, glass = null;

        // G1: one building of each kind, its panel, one purchase; its effect is the self-test's own check.
        foreach (string[] one in new[] { new[] { UloeData.Glass, "Burn2" }, new[] { UloeData.Bank, "Short2" }, new[] { UloeData.Mirror, "Silver2" }, new[] { UloeData.Flood, "Long2" } })
        {
            Vector3 at = Spot(hm, UloeContent.Beings[one[0]], 3f);
            if (float.IsNaN(at.x)) { ModKitTest.Setup("G1 " + one[0], "no free tile"); continue; }
            yield return Build(hm, one[0], at, known, got);
            if (got[0] == null) { ModKitTest.Setup("G1 " + one[0], "it wasn't built"); continue; }
            UloeBuilding b = got[0];
            if (one[0] == UloeData.Bank) bank = b;
            if (one[0] == UloeData.Glass) glass = b;
            yield return Wait(0.7f);
            Log("INFO G1 panel of " + one[0] + ": " + Panel(b.Master));
            yield return BuyChecked(b, one[1], owner);
        }
        if (glass != null)
        {
            float w0 = Wallet(owner);
            Send(hm, "Sell", Sell(hm, glass.Master));
            yield return Until(() => glass == null || glass.Master == null, 12f);
            yield return Wait(0.5f);
            Check("G1 selling refunds", Wallet(owner) > w0, "wallet +" + F(Wallet(owner) - w0));
        }

        // ULO-1: plugged into the Power Bank, which is then sold, and then one that is destroyed.
        if (bank == null) { ModKitTest.Setup("ULO-1", "no Power Bank"); yield break; }
        yield return PlugThenLose(hero, hm, bank, owner, true, "sold");
        // A stuck PLUG IN clears only with the vessel: down and back, a new brain.
        if (hero.Plug != null || (object)hero.Plug != null)
        {
            Kill(hm);
            float t = 0f;
            while (t < 90f && (UloeServer.Heroes.Count == 0 || UloeServer.Heroes[0] == hero || !ModKitUtil.Alive(UloeServer.Heroes[0].Master)))
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
        if (UloeServer.Heroes.Count == 0 || !ModKitUtil.Alive(UloeServer.Heroes[0].Master)) { ModKitTest.Setup("ULO-1 destroyed", "the vessel didn't come back"); yield break; }
        hero = UloeServer.Heroes[0];
        hm = hero.Master;
        yield return Wait(1f);
        Vector3 at2 = Spot(hm, UloeContent.Beings[UloeData.Bank], 3f);
        yield return Build(hm, UloeData.Bank, at2, known, got);
        if (got[0] == null) { ModKitTest.Setup("ULO-1 destroyed", "no second Power Bank"); yield break; }
        yield return PlugThenLose(hero, hm, got[0], owner, false, "destroyed");
    }

    static IEnumerator PlugThenLose(UloeHeroBrain hero, Entity_Master hm, UloeBuilding bank, long owner, bool sell, string how)
    {
        yield return Near(hm, bank.Master, 3f);
        yield return Wait(UloeData.PlugCooldown + 0.1f);
        Send(hm, UloeData.PlugLink, Ref(bank.Master));
        yield return Wait(1.3f);
        if (!Check("ULO-1 plugged into a Power Bank to lose it", hero.Plug == bank.Master, hero.LastPlug))
            yield break;
        Entity_Master was = bank.Master;
        if (sell) Send(hm, "Sell", Sell(hm, bank.Master));
        else Kill(bank.Master);
        yield return Until(() => was == null, 12f);
        yield return Wait(1.2f);
        Link_Command plug;
        hm.Commandlistserver.TryGetValue(UloeData.PlugLink, out plug);
        string read = "button '" + (plug != null ? plug.Goalstring.ToString() : "-") + "', skin '" + hm.Skin.Value + "', reference " + ((object)hero.Plug != null ? "still held" : "cleared");
        int unplugs = hero.Unplugs;
        Send(hm, UloeData.PlugLink, "{\"Unplug\":1}");
        yield return Wait(0.8f);
        bool stuck = plug != null && plug.Goalstring.ToString() == "On";
        Finding("ULO-1 " + how, stuck, read + "; a press of UNPLUG: unplugs +" + (hero.Unplugs - unplugs) + ", button '" + (plug != null ? plug.Goalstring.ToString() : "-") + "'");
    }
}
#endif
