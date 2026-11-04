// Spool ("Crafty Crafted Crafter" ▪ Advanced): a vessel of the Rogue Waves pack for TDS - Tower Defense Strategy.
//
// The game compiles this mod with Roslyn when it loads. Build-SpoolMod.ps1 joins every file in Code/ (in name order,
// this one first) and the shared vessel-mod kit in ModSource/Shared into the Code string of the Mods_Gamescript on
// Assets/SpoolMod/SpoolMod_Default.prefab, and the game adds the first type below to that prefab's instance.
// Keep to C# 9 (the game ships Roslyn 3.8) and stay out of System.IO / System.Reflection (blocked).
//
// How the pieces fit:
//   SpoolContent   builds Spool's game data (hero entry, beings, upgrades, the Dart's needle, commands, effects) by
//                   cloning the shipped vessels' objects and swapping in the asset pack, then registers it like the
//                   game's loader.
//   SpoolServer    runs on the host: Thread (the Yarn Stash, carried, ground drops), brains, the Thread Head's
//                   placement token and comebacks.
//   *Brain          per-entity server logic (vessel, Yarn Stash, Cookie Tin, Thread Head, Pin Cushion, Loom, units).
//   SpoolClient    runs on every player: ground tufts, pickups and drop-offs in flight, sounds, owner-only messages.
//   SpoolJournal   the journal pages (through the shared ModKitJournal), ModKitBoat the boat spot and unlock.
//   Kit 1.6         ModKitPack (the Rogue Waves registry: Join, Taken, Notice), ModKitFocus (the F-key fix),
//                   ModKitTides (Tide events), ModKitTowers (range anchors), ModKitCombat (kill credit), ModKitCounter
//                   (Thread counters), ModKitDirections (Efficient Recycler, Sewing Machine). The build renames ModKit to
//                   SpoolKit, so every mod has its own copy.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Blockage.Address;

public class SpoolMod : MonoBehaviour
{
    public static SpoolMod Instance;
    public static bool Ready;

    void Start()
    {
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        SpoolLog.Info("loaded v" + SpoolData.Version);
        ModKitPack.Join(SpoolData.Part, SpoolData.Version);
        StartCoroutine(Boot());
    }

    IEnumerator Boot()
    {
#if SPOOL_PLAY && SPOOL_TUNE
        SpoolPlay.Tune();
#endif
        Heroentryobject template = null;
        while (template == null)
        {
            template = ModKitUtil.Heroentry(SpoolData.TemplateHero);
            if (template == null)
                yield return new WaitForSecondsRealtime(0.5f);
        }

        // docs/PACK.md 2.3: whoever registers Hero_Spool first keeps it; a second copy stands down and says so.
        if (ModKitPack.Taken(SpoolData.Hero))
        {
            ModKitPack.Notice(this, SpoolData.Part, "Another Spool mod is loaded: disable it while Rogue Waves is on");
            yield break;
        }

        if (!SpoolAssets.Find())
        {
            SpoolLog.Error("SpoolAssets_Library is missing from the mod; Spool is disabled");
            yield break;
        }
        // Icons go in before anything else: the game's icon table freezes on first use (see SpoolContent.AddIcons).
        SpoolContent.AddIcons();

        while (Bla_Addressables.Instance == null)
            yield return new WaitForSecondsRealtime(0.5f);

        // The template vessel's group, then the groups Spool borrows single pieces from.
        List<string> folders = new List<string> { template.Heroloads.Objectname };
        foreach (string hero in SpoolData.ExtraGroups)
        {
            Heroentryobject h = ModKitUtil.Heroentry(hero);
            if (h != null && h.Heroloads != null) folders.Add(h.Heroloads.Objectname);
            else SpoolLog.Warn("no entry for " + hero + "; its pieces fall back to Smacky's");
        }
        foreach (string folder in folders)
        {
            Bl_Address.Loadgroup group = null;
            try { group = Bl_Address.Load(folder); }
            catch (Exception e) { SpoolLog.Warn("loading " + folder + ": " + e.Message); }
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
                    SpoolLog.Error("could not load " + folder + "; Spool is disabled");
                    yield break;
                }
                SpoolLog.Warn("could not load " + folder + "; its pieces fall back to Smacky's");
            }
        }

        try
        {
            SpoolContent.Build(template);
            Ready = true;
            SpoolLog.Info("ready: " + SpoolContent.Summary());
        }
        catch (Exception e)
        {
            SpoolLog.Error("building Spool failed: " + e);
        }

        if (Ready && SpoolData.Diagnostics)
        {
            try { SpoolDiagnostics.Dump(template); }
            catch (Exception e) { SpoolLog.Warn("diagnostics failed: " + e.Message); }
        }
        // Give the other mods their turn to add icons before anything asks the game for one.
        yield return new WaitForSecondsRealtime(8f);
        if (Ready)
            SpoolContent.CheckIcons();
    }

    void Update()
    {
        if (!Ready)
            return;
        ModKitFocus.Update();
        ModKitTides.Update();
        SpoolServer.Update();
        SpoolClient.Update();
        SpoolJournal.Tick();
#if SPOOL_SELFTEST
        SpoolSelfTest.Update();
#endif
#if SPOOL_PLAY
        SpoolPlay.Update();
#endif
    }

    void LateUpdate()
    {
        if (!Ready)
            return;
        ModKitBoat.LateUpdate(SpoolData.Hero);
    }
}
