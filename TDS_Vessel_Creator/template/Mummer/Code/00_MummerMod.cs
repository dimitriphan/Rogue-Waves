// Mummer ("Strings Attached ▪ Advanced"): a new vessel for TDS - Tower Defense Strategy.
//
// The game compiles this mod with Roslyn when it loads. Build-MummerMod.ps1 joins every file in Code/ (in name order,
// this one first) and the shared vessel-mod kit in ModSource/Shared into the Code string of the Mods_Gamescript on
// Assets/MummerMod/MummerMod_Default.prefab, and the game adds the first type below to that prefab's instance.
// Keep to C# 9 (the game ships Roslyn 3.8) and stay out of System.IO / System.Reflection (blocked).
//
// How the pieces fit:
//   MummerContent   builds Mummer's game data (hero entry, beings, upgrades, projectile, commands, effects) by cloning
//                   the shipped vessels' objects and swapping in the asset pack, then registers it like the game's loader.
//   MummerServer    runs on the host: brains, kills -> Scraps -> Thread, Marks, costumes, Tides (unravelling).
//   *Brain          per-entity server logic (vessel, Loom, Stage, Cross, Curtain, Understudies).
//   MummerClient    runs on every player: sounds, thread lines, flying Scraps, owner-only messages.
//   MummerJournal   the journal pages (through the shared ModKitJournal), ModKitBoat the boat spot and unlock.
//   Kit 1.6         ModKitPack (the Rogue Waves registry: Join, Taken, Notice), ModKitFocus (the F-key fix),
//                   ModKitTides (Tide events), ModKitTowers (range anchors), ModKitCombat (kill credit). The build
//                   renames ModKit to MummerKit, so every mod has its own copy.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Blockage.Address;

public class MummerMod : MonoBehaviour
{
    public static MummerMod Instance;
    public static bool Ready;

    void Start()
    {
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        MummerLog.Info("loaded v" + MummerData.Version);
        ModKitPack.Join(MummerData.Part, MummerData.Version);
        StartCoroutine(Boot());
    }

    IEnumerator Boot()
    {
        Heroentryobject template = null;
        while (template == null)
        {
            template = MummerUtil.Heroentry(MummerData.TemplateHero);
            if (template == null)
                yield return new WaitForSecondsRealtime(0.5f);
        }

        // docs/PACK.md 2.3: whoever registers Hero_Mummer first keeps it; a second copy stands down and says so.
        if (ModKitPack.Taken(MummerData.Hero))
        {
            ModKitPack.Notice(this, MummerData.Part, "Another Mummer mod is loaded: disable it while Rogue Waves is on");
            yield break;
        }

        if (!MummerAssets.Find())
        {
            MummerLog.Error("MummerAssets_Library is missing from the mod; Mummer is disabled");
            yield break;
        }
        // Icons go in before anything else: the game's icon table freezes on first use (see MummerContent.AddIcons).
        MummerContent.AddIcons();

        while (Bla_Addressables.Instance == null)
            yield return new WaitForSecondsRealtime(0.5f);

        // The template vessel's group, then the groups Mummer borrows single pieces from.
        List<string> folders = new List<string> { template.Heroloads.Objectname };
        foreach (string hero in MummerData.ExtraGroups)
        {
            Heroentryobject h = MummerUtil.Heroentry(hero);
            if (h != null && h.Heroloads != null) folders.Add(h.Heroloads.Objectname);
            else MummerLog.Warn("no entry for " + hero + "; its pieces fall back to Smacky's");
        }
        foreach (string folder in folders)
        {
            Bl_Address.Loadgroup group = null;
            try { group = Bl_Address.Load(folder); }
            catch (Exception e) { MummerLog.Warn("loading " + folder + ": " + e.Message); }
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
                    MummerLog.Error("could not load " + folder + "; Mummer is disabled");
                    yield break;
                }
                MummerLog.Warn("could not load " + folder + "; its pieces fall back to Smacky's");
            }
        }

        try
        {
            MummerContent.Build(template);
            Ready = true;
            MummerLog.Info("ready: " + MummerContent.Summary());
        }
        catch (Exception e)
        {
            MummerLog.Error("building Mummer failed: " + e);
        }

        if (Ready && MummerData.Diagnostics)
        {
            try { MummerDiagnostics.Dump(template); }
            catch (Exception e) { MummerLog.Warn("diagnostics failed: " + e.Message); }
        }
        // Give the other mods their turn to add icons before anything asks the game for one.
        yield return new WaitForSecondsRealtime(8f);
        if (Ready)
            MummerContent.CheckIcons();
    }

    void Update()
    {
        if (!Ready)
            return;
        ModKitFocus.Update();
        ModKitTides.Update();
        MummerServer.Update();
        MummerClient.Update();
        MummerJournal.Tick();
#if MUMMER_SELFTEST
        MummerSelfTest.Update();
#endif
    }

    void LateUpdate()
    {
        if (!Ready)
            return;
        ModKitBoat.LateUpdate(MummerData.Hero);
    }
}
