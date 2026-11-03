// Uloe ("Light Personified" ▪ Advanced): a vessel of the Rogue Waves pack for TDS - Tower Defense Strategy.
//
// The game compiles this mod with Roslyn when it loads. Build-UloeMod.ps1 joins every file in Code/ (in name order,
// this one first) and the shared vessel-mod kit in ModSource/Shared into the Code string of the Mods_Gamescript on
// Assets/UloeMod/UloeMod_Default.prefab, and the game adds the first type below to that prefab's instance.
// Keep to C# 9 (the game ships Roslyn 3.8) and stay out of System.IO / System.Reflection (blocked).
//
// How the pieces fit:
//   UloeContent   builds Uloe's game data (hero entry, beings, upgrades, the spark and the beam, commands, Blind and
//                 Illuminate, Directions) by cloning the shipped vessels' objects and swapping in the asset pack, then
//                 registers it like the game's loader.
//   UloeServer    runs on the host: Light (UloeLight, a stat fed by timed modifiers), Blind and Illuminate (UloeMarks),
//                 brains, spawn and despawn hooks.
//   *Brain        per-entity server logic (vessel, Magnifying Glass, Power Bank, Photon, Mirror, Flood Light).
//   UloeClient    runs on every player: cords, rays and rings, beams, sounds, owner-only messages.
//   UloeJournal   the journal pages (through the shared ModKitJournal), ModKitBoat the boat spot and unlock.
//   Kit 1.7       ModKitPack (the Rogue Waves registry: Join, Taken, Notice), ModKitFocus (the F-key fix),
//                 ModKitTides (Tide events), ModKitTowers (range anchors), ModKitCombat (kill credit),
//                 ModKitDirections (Uloe's four). The build renames ModKit to UloeKit, so every mod has its own copy.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Blockage.Address;

public class UloeMod : MonoBehaviour
{
    public static UloeMod Instance;
    public static bool Ready;

    void Start()
    {
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        UloeLog.Info("loaded v" + UloeData.Version);
        ModKitPack.Join(UloeData.Part, UloeData.Version);
        StartCoroutine(Boot());
    }

    IEnumerator Boot()
    {
        Heroentryobject template = null;
        while (template == null)
        {
            template = UloeUtil.Heroentry(UloeData.TemplateHero);
            if (template == null)
                yield return new WaitForSecondsRealtime(0.5f);
        }

        // docs/PACK.md 2.3: whoever registers Hero_Uloe first keeps it; a second copy stands down and says so.
        if (ModKitPack.Taken(UloeData.Hero))
        {
            ModKitPack.Notice(this, UloeData.Part, "Another Uloe mod is loaded: disable it while Rogue Waves is on");
            yield break;
        }

        if (!UloeAssets.Find())
        {
            UloeLog.Error("UloeAssets_Library is missing from the mod; Uloe is disabled");
            yield break;
        }
        // Icons go in before anything else: the game's icon table freezes on first use (see UloeContent.AddIcons).
        UloeContent.AddIcons();

        while (Bla_Addressables.Instance == null)
            yield return new WaitForSecondsRealtime(0.5f);

        // The template vessel's group, then the groups Uloe borrows single pieces from.
        List<string> folders = new List<string> { template.Heroloads.Objectname };
        foreach (string hero in UloeData.ExtraGroups)
        {
            Heroentryobject h = UloeUtil.Heroentry(hero);
            if (h != null && h.Heroloads != null) folders.Add(h.Heroloads.Objectname);
            else UloeLog.Warn("no entry for " + hero + "; its pieces fall back to Smacky's");
        }
        foreach (string folder in folders)
        {
            Bl_Address.Loadgroup group = null;
            try { group = Bl_Address.Load(folder); }
            catch (Exception e) { UloeLog.Warn("loading " + folder + ": " + e.Message); }
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
                    UloeLog.Error("could not load " + folder + "; Uloe is disabled");
                    yield break;
                }
                UloeLog.Warn("could not load " + folder + "; its pieces fall back to Smacky's");
            }
        }

        try
        {
            UloeContent.Build(template);
            Ready = true;
            UloeLog.Info("ready: " + UloeContent.Summary());
        }
        catch (Exception e)
        {
            UloeLog.Error("building Uloe failed: " + e);
        }

        if (Ready && UloeData.Diagnostics)
        {
            try { UloeDiagnostics.Dump(template); }
            catch (Exception e) { UloeLog.Warn("diagnostics failed: " + e.Message); }
        }
        // Give the other mods their turn to add icons before anything asks the game for one.
        yield return new WaitForSecondsRealtime(8f);
        if (Ready)
            UloeContent.CheckIcons();
    }

    void Update()
    {
        if (!Ready)
            return;
        ModKitFocus.Update();
        ModKitTides.Update();
        UloeServer.Update();
        UloeClient.Update();
        UloeJournal.Tick();
#if ULOE_SELFTEST
        UloeSelfTest.Update();
#endif
    }

    void LateUpdate()
    {
        if (!Ready)
            return;
        ModKitBoat.LateUpdate(UloeData.Hero);
    }
}
