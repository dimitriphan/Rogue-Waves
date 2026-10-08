// Argo ("Fathoms Below ▪ Intermediate"): a new vessel for TDS - Tower Defense Strategy.
//
// The game compiles this mod with Roslyn when it loads. Build-ArgoMod.ps1 joins every file in this folder
// (in name order, this one first) into the Code string of the Mods_Gamescript on
// Assets/ArgoMod/ArgoMod_Default.prefab, and the game adds the first type below to that prefab's instance.
// Keep to C# 9 (the game ships Roslyn 3.8) and stay out of System.IO / System.Reflection (blocked).
//
// How the pieces fit:
//   ArgoContent   builds Argo's game data (hero entry, beings, upgrades, projectiles, commands) by cloning
//                 Smacky's and swapping in the asset pack, then registers it the way the game's loader would.
//   ArgoServer    runs on the host: attaches brains to Argo entities, Tides (Fathoms), the Naut roster.
//   *Brain        per-entity server logic (vessel, buildables, Nauts).
//   ArgoHooks     damage modifiers that ride on the game's own hit events (Pinged, Bulkhead, Whaler, cleave).
//   ArgoClient    runs on every player: turret aiming, effects, sounds, Tremor rings, owner-only warnings.
//   ArgoJournal   the journal pages, built by the shared kit (ModSource/Shared): ModKitVesselJournal, plus
//                 ModKitVesselTabs (one tab per vessel on every vessel page), ModKitBoat (a boat spot, the button
//                 unlocked) and ModKitTowers (each tower's range centred on it). The build renames ModKit to ArgoKit.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Blockage.Address;

public class ArgoMod : MonoBehaviour
{
    public static ArgoMod Instance;
    public static bool Ready;

    void Start()
    {
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        ArgoLog.Info("loaded v" + ArgoData.Version);
        StartCoroutine(Boot());
    }

    IEnumerator Boot()
    {
        Heroentryobject template = null;
        while (template == null)
        {
            template = ArgoUtil.Heroentry(ArgoData.TemplateHero);
            if (template == null)
                yield return new WaitForSecondsRealtime(0.5f);
        }

        if (!ArgoAssets.Find())
        {
            ArgoLog.Error("ArgoAssets_Library is missing from the mod; Argo is disabled");
            yield break;
        }
        // Icons go in before anything else (see ArgoContent.AddIcons): the game's icon table freezes on first use.
        ArgoContent.AddIcons();

        while (Bla_Addressables.Instance == null)
            yield return new WaitForSecondsRealtime(0.5f);

        Bl_Address.Loadgroup group = Bl_Address.Load(template.Heroloads.Objectname);
        if (group == null)
        {
            ArgoLog.Error("could not load " + template.Heroloads.Objectname + "; Argo is disabled");
            yield break;
        }
        float waited = 0f;
        while (!group.Loaded)
        {
            waited += Time.unscaledDeltaTime;
            if (waited > 180f)
            {
                ArgoLog.Error("timed out loading " + template.Heroloads.Objectname + "; Argo is disabled");
                yield break;
            }
            yield return null;
        }

        try
        {
            ArgoContent.Build(template);
            Ready = true;
            ArgoLog.Info("ready: " + ArgoContent.Summary());
        }
        catch (Exception e)
        {
            ArgoLog.Error("building Argo failed: " + e);
        }

        if (Ready && ArgoData.Diagnostics)
        {
            try { ArgoDiagnostics.Dump(template); }
            catch (Exception e) { ArgoLog.Warn("diagnostics failed: " + e.Message); }
        }
        // Give the other mods their turn to add icons before anything asks the game for one.
        yield return new WaitForSecondsRealtime(8f);
        if (Ready)
            ArgoContent.CheckIcons();
    }

    void Update()
    {
        if (!Ready)
            return;
        ArgoServer.Update();
        ArgoClient.Update();
        // Other modded vessels may finish loading after Argo; the sync is idempotent and runs every few seconds.
        ModKitVesselTabs.Tick();
#if ARGO_SELFTEST
        ArgoSelfTest.Update();
#endif
    }

    void LateUpdate()
    {
        if (!Ready)
            return;
        ModKitBoat.LateUpdate(ArgoData.Hero);
    }
}
