// RogueLab: the throwaway research mod of Rogue Waves' Phase 1 PC round (docs/15_ROGUE_MODE_ENGINE.md §11 in the kit).
// It is never released. It turns the chapter's "untested" lines into facts by doing each thing once, on a solo host,
// and logging one "[RogueLab] TEST PASS|FAIL|SETUP|INFO ..." line per question, then "TEST DONE" (the shape
// Run-SelfTests.ps1 reads):
//   at boot      the database folders, Mode_Normal's data, the enemy groups and elite params; registers a mod mode
//                (Mode_RogueLab: the stock scripts by name, a 3-Tide table), its Gameplans, a mod Cardinal; the save
//   on the boat  picks Boutman and the test map, equips the mod Cardinal, swaps the match to Mode_RogueLab
//   map 1        the mode, the Cardinal, a Cardinal granted mid-match, the F-key bug and its fix, a rewound Tide,
//                leaving between Tides as a win, then the host chains straight into map 2
//   map 2        what survived the scene change; a Runprompt dialog left open for Dimitri to click
//   round 2      (06_Round2.cs) boot dumps, a second prefab, a swarm's cost, an eight-card dialog, then a tour of every
//                other base map the vote could offer; TEST DONE
//   round 4      (07_Round4Menu.cs, 08_Round4.cs) starts from a cloned main menu button with no Cardinal, then map 1's
//                mechanic probes for the 2026-10-09 scope, and stops (RogueLabData.Round)
//   round 7      (09_Round7.cs) the stock Cardinals' core upgrades at boot, then map 1's base-vessel and map checks
//                of kit docs/17 §12 and docs/16 §9, and stops
// Build: Build-RogueLabMod.ps1 -Define ROGUELAB_SELFTEST (the define also switches on the kit's ModKitTest).
#if !ROGUELAB_SELFTEST
#error RogueLab is a research build: build it with -Define ROGUELAB_SELFTEST
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Blockage.Address;

public class RogueLabMod : MonoBehaviour
{
    public static RogueLabMod Instance;
    public static bool Ready;

    void Start()
    {
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        RogueLabLog.Info("loaded v" + RogueLabData.Version);
        StartCoroutine(Boot());
    }

    IEnumerator Boot()
    {
        while (Tds_Masterdatabase.Instance == null || RogueLabUtil.Folder("Mode") == null || RogueLabUtil.Folder("Hero") == null)
            yield return new WaitForSecondsRealtime(0.5f);
        while (Bla_Addressables.Instance == null)
            yield return new WaitForSecondsRealtime(0.5f);

        RogueLabProbe.Log = RogueLabLog.Test;
        ModKitTest.Log = RogueLabLog.Test;
        try { RogueLabBoot.Database(); } catch (Exception e) { RogueLabLog.Test("INFO database dump failed: " + e); }

        // Mode_Normal's group (its Modeobject and Tide table) and Boutman's (Mob_Boutmanbro for the F-key test).
        Modeentryobject normal = RogueLabUtil.Folder("Mode").ContainsKey(RogueLabData.StockMode) ? RogueLabUtil.Folder("Mode")[RogueLabData.StockMode] as Modeentryobject : null;
        List<string> folders = new List<string>();
        if (normal != null && normal.Modeloads != null) folders.Add(normal.Modeloads.Objectname);
        Heroentryobject bout = RogueLabUtil.Heroentry(RogueLabData.Hero);
        if (bout != null && bout.Heroloads != null) folders.Add(bout.Heroloads.Objectname);
        foreach (string folder in folders)
        {
            Bl_Address.Loadgroup group = null;
            try { group = Bl_Address.Load(folder); }
            catch (Exception e) { RogueLabLog.Test("INFO loading " + folder + ": " + e.Message); }
            float waited = 0f;
            while (group != null && !group.Loaded && waited < 180f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            RogueLabLog.Test("INFO group " + folder + " loaded at boot: " + (group != null && group.Loaded) + " after " + RogueLabUtil.F(waited) + " s");
        }

        try { RogueLabBoot.StockMode(normal); } catch (Exception e) { RogueLabLog.Test("INFO stock mode dump failed: " + e); }
        try { RogueLabEnemies.Dump("boot"); } catch (Exception e) { RogueLabLog.Test("INFO enemy dump failed: " + e); }
        try { RogueLabBoot.RegisterMode(normal); } catch (Exception e) { RogueLabProbe.Check("mod mode registered", false, e.ToString()); }
        try { RogueLabBoot.RegisterCardinal(); } catch (Exception e) { RogueLabProbe.Check("mod Cardinal registered", false, e.ToString()); }
        try { RogueLabBoot.Save(); } catch (Exception e) { RogueLabProbe.Check("save write", false, e.ToString()); }
        yield return RogueLabRound2.Boot();
        if (RogueLabData.Round == 7 && !RogueLabData.Rerun) yield return RogueLabRound7.Cardinals();
        Ready = true;
        RogueLabLog.Info("ready");
    }

    void Update()
    {
        if (!Ready)
            return;
        RogueLabRun.Update();
    }
}
