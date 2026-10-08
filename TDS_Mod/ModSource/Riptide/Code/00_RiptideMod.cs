// Riptide: a roguelike game mode for TDS - Tower Defense Strategy (DESIGN.md). One Basin, thirty random Tides that keep
// pulling harder, and a Salvage between them where each player picks one of the game's artifacts.
//
// The game compiles this mod with Roslyn when it loads. Build-RiptideMod.ps1 joins every file in Code/ (in name order,
// this one first) into the Code string of the Mods_Gamescript on Assets/RiptideMod/RiptideMod_Default.prefab, and the
// game adds the first type below to that prefab's instance. Keep to C# 9 (the game ships Roslyn 3.8) and stay out of
// System.IO / System.Reflection (blocked). Riptide adds no art, so it doesn't use the vessel kit in ModSource/Shared.
//
// How the pieces fit:
//   RiptideContent    registers the mode (a clone of Mode_Normal's entry, mode and Tide table), the Gameplan twins,
//                     and loads every artifact group, the same way on every machine.
//   RiptideDirector   host only: the RIPTIDE switch on the world map, then the run: seed, Tide writing, the ending.
//   RiptideArtifacts  salvaged artifacts: granted on the host, shown in every player's HUD row.
//   RiptideSalvage    the pick between Tides: offers and the clock hold on the host, the dialog on every machine.
//   RiptideCommands   the SALVAGE button on every vessel and its icon.
//   RiptideSwitch     the RIPTIDE switch on the Setup screen (host) and the "TYPE n RIPTIDE" line (everyone).
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Blockage.Address;

public class RiptideMod : MonoBehaviour
{
    public static RiptideMod Instance;
    public static bool Ready;

    void Start()
    {
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        RiptideLog.Info("loaded v" + RiptideData.Version);
        StartCoroutine(Boot());
    }

    IEnumerator Boot()
    {
        Modeentryobject entry = null;
        while (entry == null)
        {
            entry = RiptideUtil.Entry<Modeentryobject>("Mode", RiptideData.TemplateMode);
            if (entry == null)
                yield return new WaitForSecondsRealtime(0.5f);
        }
        // Icons go in before anything else: the game's icon table freezes the first time any icon is asked for.
        RiptideCommands.AddIcons();
        while (Bla_Addressables.Instance == null)
            yield return new WaitForSecondsRealtime(0.5f);

        // Mode_Normal's group holds the Modeobject and its Tide table (and with them the foes it sends).
        Bl_Address.Loadgroup group = null;
        try { group = Bl_Address.Load(entry.Modeloads.Objectname); }
        catch (Exception e) { RiptideLog.Warn("loading " + entry.Modeloads.Objectname + ": " + e.Message); }
        float waited = 0f;
        while (group != null && !group.Loaded && waited < 180f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Modeobject template = RiptideUtil.Load<Modeobject>("Mode", RiptideData.TemplateMode);
        if (template == null || template.Enemies == null)
        {
            RiptideLog.Error("could not load " + RiptideData.TemplateMode + "; Riptide is disabled");
            yield break;
        }

        try
        {
            RiptideContent.Build(entry, template);
            Ready = true;
            RiptideLog.Info("ready: " + RiptideContent.Summary());
        }
        catch (Exception e)
        {
            RiptideLog.Error("building Riptide failed: " + e);
            yield break;
        }
        yield return RiptideContent.LoadArtifacts();
        // Give the other mods their turn to add icons before anything asks the game for one.
        yield return new WaitForSecondsRealtime(8f);
        RiptideCommands.CheckIcons();
    }

    void Update()
    {
        if (!Ready)
            return;
        if (!RiptideCommands.KeyChecked)
            Guard("salvage key", RiptideCommands.CheckKey);
        RiptideDirector.Update();
        RiptideArtifacts.Tick();
        Guard("salvage", RiptideSalvage.Tick);
        Guard("salvage dialog", RiptideSalvage.ClientTick);
        Guard("switch", RiptideSwitch.Tick);
#if RIPTIDE_SELFTEST
        RiptideSelfTest.Update();
#endif
    }

    // One fault guard per piece, so a piece that keeps throwing stops itself without taking the others down.
    static readonly Dictionary<string, RiptideFaults> faults = new Dictionary<string, RiptideFaults>();
    static readonly List<string> stopped = new List<string>();

    static void Guard(string name, Action tick)
    {
        if (stopped.Contains(name))
            return;
        try { tick(); }
        catch (Exception e)
        {
            RiptideFaults f;
            if (!faults.TryGetValue(name, out f))
                faults[name] = f = new RiptideFaults();
            if (f.Fail(name, e))
                stopped.Add(name);
        }
    }
}
