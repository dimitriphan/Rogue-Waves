// Argo ("Deep Sea Diver ▪ Intermediate"): a vessel of the Rogue Waves pack for TDS - Tower Defense Strategy (2.0 design,
// ModSource/Argo/DESIGN.md; 1.4.0 was the standalone ArgoMod).
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
//   ArgoHooks     damage modifiers that ride on the game's own hit events (Bulkhead, Crushing Depth; Pinged unused).
//   ArgoWater     Deep Water and FLOOD's pools as networks (2.0); ArgoStatus who is Slowed, Pinned or Wet.
//   ArgoClient    runs on every player: turret aiming, effects, sounds, Tremor rings, owner-only warnings.
//   ArgoJournal   the journal pages, built by the shared kit (ModSource/Shared): ModKitVesselJournal, plus
//                 ModKitVesselTabs (one tab per vessel on every vessel page), ModKitBoat (a boat spot, the button
//                 unlocked) and ModKitTowers (each tower's range centred on it). The build renames ModKit to ArgoKit.
//   Kit 1.6       ModKitPack (the pack registry, the stand-down), ModKitTides (Tides), ModKitCounter (Fathom),
//                 ModKitFocus (the F-key fix), ModKitDirections (Deep Current, Heavy Salvo).
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
        ModKitPack.Join(ArgoData.Part, ArgoData.Version);
        StartCoroutine(Boot());
    }

    IEnumerator Boot()
    {
        Heroentryobject template = null;
        while (template == null)
        {
            template = ModKitUtil.Heroentry(ArgoData.TemplateHero);
            if (template == null)
                yield return new WaitForSecondsRealtime(0.5f);
        }

        // docs/PACK.md 2.3: a standalone ArgoMod (1.4.0) registers Hero_Argo too. Whoever is first keeps it; the pack's
        // Argo stands down rather than overwrite it.
        if (ModKitPack.Taken(ArgoData.Hero))
        {
            ModKitPack.Notice(this, ArgoData.Part, "Another Argo mod is loaded: disable ArgoMod while Rogue Waves is on");
            yield break;
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

        // Smacky's group (the template), then the other vessels' groups Argo clones from (the kit template's pattern).
        List<string> folders = new List<string> { template.Heroloads.Objectname };
        foreach (string hero in ArgoData.ExtraGroups)
        {
            Heroentryobject h = ModKitUtil.Heroentry(hero);
            if (h != null && h.Heroloads != null) folders.Add(h.Heroloads.Objectname);
            else ArgoLog.Warn("no entry for " + hero + "; its pieces fall back to Smacky's");
        }
        foreach (string folder in folders)
        {
            Bl_Address.Loadgroup group = null;
            try { group = Bl_Address.Load(folder); }
            catch (Exception e) { ArgoLog.Warn("loading " + folder + ": " + e.Message); }
            float waited = 0f;
            while (group != null && !group.Loaded && waited < 180f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (group == null || !group.Loaded)
            {
                if (folder == template.Heroloads.Objectname)
                {
                    ArgoLog.Error("could not load " + folder + "; Argo is disabled");
                    yield break;
                }
                ArgoLog.Warn("could not load " + folder + "; its pieces fall back to Smacky's");
            }
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
        ModKitFocus.Update();
        ArgoContent.LateClones();
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
