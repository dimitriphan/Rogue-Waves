// Yossele ("The Patient Golem ▪ Intermediate"): a vessel of the Rogue Waves pack for TDS - Tower Defense Strategy
// (ModSource/Yossele/DESIGN.md, docs/scope/07_Vessel_Yossele.txt).
//
// The game compiles this mod with Roslyn when it loads. Build-YosseleMod.ps1 joins every file in Code/ (in name order,
// this one first) and the shared kit in ModSource/Shared into the Code string of the Mods_Gamescript on
// Assets/YosseleMod/YosseleMod_Default.prefab, and the game adds the first type below to that prefab's instance.
// Keep to C# 9 (the game ships Roslyn 3.8) and stay out of System.IO / System.Reflection (blocked).
//
// How the pieces fit:
//   YosseleContent   builds Yossele's game data (hero entry, beings, upgrades, commands, vines, fruit, the Compost Bin
//                    Directions) by cloning the shipped vessels' objects and swapping in the asset pack.
//   YosseleServer    runs on the host: brains, Growth on each Tide, fertilizer and Compost Bin, fruit pick-ups.
//   *Brain           per-entity server logic (vessel, Mushroom Cannon, Dandelion Tree, Stonehenge, Pachira Sapling).
//   YosseleSpores    the cannon's spore stacks on foes (DOT, Sticky, Explosive Finish, Hallucinogen, Viral Outbreak).
//   YosseleVines     the Stonehenges' vine tiles (one networked ground being per tile).
//   YosseleHooks     damage modifiers on the game's own hit event (Stonehenge armour and barrier, Connected Roots).
//   YosseleClient    runs on every player: lobs, seeds, turret aim, Stonehenge facing, effects, sounds, warnings.
//   YosseleJournal   the journal pages through the shared ModKitVesselJournal.
//   Kit 1.6          ModKitPack, ModKitTides (Growth on each Tide), ModKitCounter (Growth, Seeds, fertilizer),
//                    ModKitDirections (Compost Bin), ModKitFocus, ModKitTowers, ModKitBoat. The build renames ModKit.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Blockage.Address;

public class YosseleMod : MonoBehaviour
{
    public static YosseleMod Instance;
    public static bool Ready;

    void Start()
    {
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        YosseleLog.Info("loaded v" + YosseleData.Version);
        ModKitPack.Join(YosseleData.Part, YosseleData.Version);
        StartCoroutine(Boot());
    }

    IEnumerator Boot()
    {
        Heroentryobject template = null;
        while (template == null)
        {
            template = YosseleUtil.Heroentry(YosseleData.TemplateHero);
            if (template == null)
                yield return new WaitForSecondsRealtime(0.5f);
        }

        // docs/PACK.md 2.3: whoever registers Hero_Yossele first keeps it.
        if (ModKitPack.Taken(YosseleData.Hero))
        {
            ModKitPack.Notice(this, YosseleData.Part, "Another Yossele mod is loaded: disable it while Rogue Waves is on");
            yield break;
        }

        if (!YosseleAssets.Find())
        {
            YosseleLog.Error("YosseleAssets_Library is missing from the mod; Yossele is disabled");
            yield break;
        }
        // Icons go in before anything else: the game's icon table freezes on first use (see YosseleContent.AddIcons).
        YosseleContent.AddIcons();

        while (Bla_Addressables.Instance == null)
            yield return new WaitForSecondsRealtime(0.5f);

        // The template vessel's group, then the groups Yossele borrows single pieces from.
        List<string> folders = new List<string> { template.Heroloads.Objectname };
        foreach (string hero in YosseleData.ExtraGroups)
        {
            Heroentryobject h = YosseleUtil.Heroentry(hero);
            if (h != null && h.Heroloads != null) folders.Add(h.Heroloads.Objectname);
            else YosseleLog.Warn("no entry for " + hero + "; its pieces fall back to Smacky's");
        }
        foreach (string folder in folders)
        {
            Bl_Address.Loadgroup group = null;
            try { group = Bl_Address.Load(folder); }
            catch (Exception e) { YosseleLog.Warn("loading " + folder + ": " + e.Message); }
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
                    YosseleLog.Error("could not load " + folder + "; Yossele is disabled");
                    yield break;
                }
                YosseleLog.Warn("could not load " + folder + "; its pieces fall back to Smacky's");
            }
        }

        try
        {
            YosseleContent.Build(template);
            Ready = true;
            YosseleLog.Info("ready: " + YosseleContent.Summary());
        }
        catch (Exception e)
        {
            YosseleLog.Error("building Yossele failed: " + e);
        }

        if (Ready && YosseleData.Diagnostics)
        {
            try { YosseleDiagnostics.Dump(template); }
            catch (Exception e) { YosseleLog.Warn("diagnostics failed: " + e.Message); }
        }
        // Give the other mods their turn to add icons before anything asks the game for one.
        yield return new WaitForSecondsRealtime(8f);
        if (Ready)
            YosseleContent.CheckIcons();
    }

    void Update()
    {
        if (!Ready)
            return;
        ModKitFocus.Update();
        ModKitTides.Update();
        YosseleServer.Update();
        YosseleClient.Update();
        // Other modded vessels may finish loading after Yossele; the sync is idempotent and runs every few seconds.
        ModKitVesselTabs.Tick();
#if YOSSELE_SELFTEST
        YosseleSelfTest.Update();
#endif
    }

    void LateUpdate()
    {
        if (!Ready)
            return;
        ModKitBoat.LateUpdate(YosseleData.Hero);
    }
}
