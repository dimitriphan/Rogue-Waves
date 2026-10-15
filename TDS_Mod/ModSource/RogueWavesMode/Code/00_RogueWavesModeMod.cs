// RogueWavesMode: the mode part of the Rogue Waves pack (docs/PACK.md; prefab RogueWaves_Mode_Default, dev mod
// RogueWavesModeMod). This first slice carries the campaign's sea chart and its sea weather (docs/SEA_CHART.md):
//   02_Mode.cs      Mode_RogueWave (Mode_Normal's scripts and Tide table) and an RW_ twin of every base Gameplan
//   03_Chart.cs     the seeded chart as data, an exact port of concept-tools/sea_chart.py
//   04_World.cs     the chart as a world of the game's own: islands with map nodes, built on every machine from its name
//   05_Boat.cs      on the boat: warps the party onto the chart, keeps the boat on the route, sails to a chosen node
//   06_Campaign.cs  the host's campaign: seed and route, in its save
//   07_Weather.cs   Rising Water, Quickening Current, Nesting Ground: the stock map objects, put on chart maps
//   08_Icons.cs     the weather icons
//   09_Rejoin.cs    a player who drops can rejoin the running map (docs/REJOIN.md), in every map while the pack is on
//   10_Online.cs    a restarted friend keeps their Sessionid; a player whose map never loads is sent back, not everyone
// Nothing here starts a campaign by itself: RWCampaign.New / Continue do, called by the main menu button piece
// (docs/MAIN_MENU_START.md, not built yet) or, in a self-test build, by 99_SelfTest.cs.
// Build: Build-RogueWavesModeMod.ps1 [-Define ROGUEWAVESMODE_SELFTEST].
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Blockage.Address;

public class RogueWavesModeMod : MonoBehaviour
{
    public static RogueWavesModeMod Instance;
    public static bool Ready;

    void Start()
    {
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        ModKitPack.Join(RWData.Part, RWData.Version);
        RWLog.Info("loaded v" + RWData.Version);
        StartCoroutine(Boot());
    }

    IEnumerator Boot()
    {
        while (Tds_Masterdatabase.Instance == null || RWUtil.Folder("Mode") == null || RWUtil.Folder("Gameplan") == null || RWUtil.Folder("World") == null)
            yield return new WaitForSecondsRealtime(0.5f);
        while (Bla_Addressables.Instance == null)
            yield return new WaitForSecondsRealtime(0.5f);
        RWIcons.Add();
        RWCampaign.Install();

        // Mode_Normal's group holds the Modeobject and Tide table the mode copies.
        Modeentryobject normal = RWUtil.Folder("Mode").ContainsKey(RWData.StockMode) ? RWUtil.Folder("Mode")[RWData.StockMode] as Modeentryobject : null;
        if (normal != null && normal.Modeloads != null)
        {
            Bl_Address.Loadgroup group = null;
            try { group = Bl_Address.Load(normal.Modeloads.Objectname); }
            catch (Exception e) { RWLog.Warn("loading " + normal.Modeloads.Objectname + ": " + e.Message); }
            float waited = 0f;
            while (group != null && !group.Loaded && waited < 180f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        try { RWMode.Register(normal); }
        catch (Exception e) { RWLog.Warn("mode not registered: " + e); }
        try { RWLog.Info("chart pool: " + RWMode.Pool.Count + " base maps"); }
        catch (Exception e) { RWLog.Warn("chart pool: " + e.Message); }
        ModKitTides.MapStart += RWWeather.MapStart;
        ModKitTides.Tide += RWWeather.Tide;
        ModKitTides.MapEnd += RWCampaign.MapEnd;
        ModKitTides.MapEnd += RWWeather.MapEnd;
#if ROGUEWAVESMODE_SELFTEST
        RWTest.Boot();
#endif
        Ready = true;
        RWLog.Info("ready");
    }

    void Update()
    {
        // Before Ready: the id must be back in the save before the player can join anyone.
        try { RWIdentity.Update(); } catch (Exception e) { RWLog.Once("identity", "identity: " + e); }
        if (!Ready)
            return;
        try { RWLoad.Update(); } catch (Exception e) { RWLog.Once("load", "load watch: " + e); }
        try { ModKitTides.Update(); } catch (Exception e) { RWLog.Once("tides", "tides: " + e); }
        try { RWBoat.Update(); } catch (Exception e) { RWLog.Once("boat", "boat: " + e); }
        try { RWWeather.DeterUpdate(); } catch (Exception e) { RWLog.Once("deter", "deter: " + e); }
        try { RWRejoin.Update(); } catch (Exception e) { RWLog.Once("rejoin", "rejoin: " + e); }
#if ROGUEWAVESMODE_SELFTEST
        try { RWTest.Update(); } catch (Exception e) { RWLog.Once("test", "self-test: " + e); }
#endif
    }
}

public static class RWData
{
    public const string Part = "Mode";
    public const string Version = "0.3.0";               // pack version (docs/PACK.md §5); 0.1: sea chart and sea weather; 0.2: rejoin; 0.3: restart rejoin, load watch, slot guard
    public const string Mode = "Mode_RogueWave";         // the mode's entry, group and Modeobject name
    public const string ModeDisplay = "Rogue Waves";
    public const string StockMode = "Mode_Normal";
    public const string PlanPrefix = "RW_";              // + the base Gameplan's name: its twin in Mode_RogueWave
    public const string SaveFolder = "RogueWaves";
    public const string WorldPrefix = "RW_Chart_";       // + seed + "_" + one column digit per row sailed
}

public static class RWLog
{
    const string Tag = "[RogueWavesMode] ";
    static readonly List<string> said = new List<string>();

    public static void Info(string message) { Debug.Log(Tag + message); }
    public static void Warn(string message) { Debug.LogWarning(Tag + message); }

    // A warning logged once per key, for errors that would otherwise repeat every frame.
    public static void Once(string key, string message)
    {
        if (said.Contains(key)) return;
        said.Add(key);
        Warn(message);
    }
}
