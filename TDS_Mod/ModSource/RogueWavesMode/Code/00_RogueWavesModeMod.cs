// RogueWavesMode: the mode part of the Rogue Waves pack (docs/PACK.md; prefab RogueWaves_Mode_Default, dev mod
// RogueWavesModeMod). It carries the campaign's sea chart and sea weather (docs/mode/SEA_CHART.md) and the rejoin:
//   02_Mode.cs      Mode_RogueWave (Mode_Normal's scripts and Tide table) and an RW_ twin of every base Gameplan
//   03_Chart.cs     the seeded chart as data, an exact port of concept-tools/sea_chart.py
//   04_World.cs     the chart as a world of the game's own: islands with map nodes, built on every machine from its name
//   05_Boat.cs      on the boat: warps the party onto the chart, keeps the boat on the route, sails to a chosen node
//   06_Campaign.cs  the host's campaign: seed and route, in its save
//   07_Weather.cs   Rising Water, Quickening Current, Nesting Ground: the stock map objects, put on chart maps
//   08_Icons.cs     the weather icons
//   09_Rejoin.cs    a player who drops can rejoin the running map (docs/mode/REJOIN.md), in every map while the pack is on
//   10_Online.cs    a restarted friend keeps their Sessionid; a player whose map never loads is sent back, not everyone
// Nothing here starts a campaign by itself: RWCampaign.New / Continue do, called by the main menu button
// (12_Menu.cs, docs/mode/MAIN_MENU_START.md) or, in a self-test build, by 99_SelfTest.cs.
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
        Application.logMessageReceived += RWRejoin.OnLog;
        RWLog.Info("loaded v" + RWData.Version);
        StartCoroutine(Boot());
    }

    IEnumerator Boot()
    {
#if ROGUEWAVESMODE_TWOPC
        if (RWTwo.NoPack) { yield return RWTwo.NoPackRun(); yield break; }
#endif
        while (Tds_Masterdatabase.Instance == null || ModKitUtil.Folder("Mode") == null || ModKitUtil.Folder("Gameplan") == null || ModKitUtil.Folder("World") == null)
            yield return new WaitForSecondsRealtime(0.5f);
        while (Bla_Addressables.Instance == null)
            yield return new WaitForSecondsRealtime(0.5f);
        try { RWAssets.Find(); } catch (Exception e) { RWLog.Warn("assets: " + e); }
        try { RWIcons.Add(); } catch (Exception e) { RWLog.Warn("icons: " + e); }
        RWCampaign.Install();

        // Mode_Normal's group holds the Modeobject and Tide table the mode copies.
        Modeentryobject normal = ModKitUtil.Folder("Mode").ContainsKey(RWData.StockMode) ? ModKitUtil.Folder("Mode")[RWData.StockMode] as Modeentryobject : null;
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
        try { RWTips.Boot(); } catch (Exception e) { RWLog.Warn("tips: " + e.Message); }
        yield return RWUtil.Guarded(RWContent.Boot(), "content");
        ModKitTides.MapStart += RWCampaign.MapStart;
        ModKitTides.MapStart += RWRun.MapStart;
        ModKitTides.MapStart += RWDifficulty.MapStart;
        ModKitTides.MapStart += RWWeather.MapStart;
        ModKitTides.MapStart += RWVote.MapStart;
        ModKitTides.Break += RWVote.Break;
        ModKitTides.Tide += RWCampaign.Tide;
        ModKitTides.Tide += RWWeather.Tide;
        ModKitTides.Tide += RWVote.Tide;
        ModKitTides.MapEnd += RWCampaign.MapEnd;
        ModKitTides.MapEnd += RWWeather.MapEnd;
        ModKitTides.MapEnd += RWRun.MapEnd;
        ModKitTides.MapEnd += RWVote.MapEnd;
        Blm_Server.Lobbyupdate += RWWeather.OnLobby;
#if ROGUEWAVESMODE_SELFTEST
        RWTest.Boot();
#endif
        Ready = true;
        RWLog.Info("ready");
    }

    void Update()
    {
#if ROGUEWAVESMODE_FEEL
        RWFeel.Frame();
#endif
#if ROGUEWAVESMODE_TWOPC
        try { RWTwo.Update(); } catch (Exception e) { RWLog.Once("two", "two players: " + e); }
#endif
        // Before Ready: the id must be back in the save before the player can join anyone, under its RW_ key.
        try { RWKeys.Migrate(); } catch (Exception e) { RWLog.Once("keys", "save keys: " + e); }
        try { RWIdentity.Update(); } catch (Exception e) { RWLog.Once("identity", "identity: " + e); }
        if (!Ready)
            return;
        try { RWMenu.Update(); } catch (Exception e) { RWLog.Once("menu", "main menu: " + e); }
        try { RWCard.Update(); } catch (Exception e) { RWLog.Once("card", "campaign card: " + e); }
        try { RWLobby.Update(); } catch (Exception e) { RWLog.Once("lobby", "lobby: " + e); }
        try { RWTips.Update(); } catch (Exception e) { RWLog.Once("tips", "tips: " + e); }
        try { RWLoad.Update(); } catch (Exception e) { RWLog.Once("load", "load watch: " + e); }
        try { RWRun.Tick(); } catch (Exception e) { RWLog.Once("run", "run: " + e); }
        try { RWRunLabel.Update(); } catch (Exception e) { RWLog.Once("run line", "run line: " + e); }
        try { RWHits.Update(); } catch (Exception e) { RWLog.Once("hits", "hits: " + e); }
        try { RWAny.Update(); } catch (Exception e) { RWLog.Once("any", "Any Directions: " + e); }
        try { RWBase.Update(); } catch (Exception e) { RWLog.Once("base", "base vessels' Directions: " + e); }
        try { RWTrinkets.Update(); } catch (Exception e) { RWLog.Once("trinkets", "Trinkets and Cardinals: " + e); }
        try { RWVote.Update(); } catch (Exception e) { RWLog.Once("vote", "vote: " + e); }
        try { RWAsk.Update(); } catch (Exception e) { RWLog.Once("ask", "questions: " + e); }
        try { RWShop.Update(); } catch (Exception e) { RWLog.Once("shop", "shop: " + e); }
        try { RWObjectives.Update(); } catch (Exception e) { RWLog.Once("objectives", "objectives: " + e); }
        try { RWCardinals.Update(); } catch (Exception e) { RWLog.Once("cardinals", "cardinals: " + e); }
        try { RWDifficulty.Update(); } catch (Exception e) { RWLog.Once("difficulty", "map health: " + e); }
        try { ModKitTides.Update(); } catch (Exception e) { RWLog.Once("tides", "tides: " + e); }
        try { RWBoat.Update(); } catch (Exception e) { RWLog.Once("boat", "boat: " + e); }
        try { RWWeather.DeterUpdate(); } catch (Exception e) { RWLog.Once("deter", "deter: " + e); }
        try { RWWeather.SpeedUpdate(); } catch (Exception e) { RWLog.Once("speed", "game speed: " + e); }
        try { ModKitLight.Update(); } catch (Exception e) { RWLog.Once("light", "lighting: " + e); }
        try { RWRejoin.Update(); } catch (Exception e) { RWLog.Once("rejoin", "rejoin: " + e); }
#if ROGUEWAVESMODE_SELFTEST
        try { RWTest.Update(); } catch (Exception e) { RWLog.Once("test", "self-test: " + e); }
#endif
    }
}

public static class RWData
{
    public const string Part = "Mode";
    public const string Version = "0.1.0-dev";           // the pack version plus -dev (docs/PACK.md §4), since 2026-11-03; before it the part counted
                                                         // its own: 0.1 sea chart and sea weather, 0.2 rejoin, 0.3.0 restart rejoin, load watch, slot guard
    public const string Mode = "Mode_RogueWave";         // the mode's entry, group and Modeobject name
    public const string ModeDisplay = "Rogue Waves";
    public const string StockMode = "Mode_Normal";
    public const string PlanPrefix = "RW_";              // + the base Gameplan's name: its twin in Mode_RogueWave
    public const string SaveFolder = "RogueWaves";
    public const string KeyPrefix = "RW_";               // every key in SaveFolder (kit docs/23 §2 rule 3): RWKeys
    public const string WorldPrefix = "RW_Chart_";       // + seed + "_" + one column digit per row sailed + "r" + retries left (04_World.cs)
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
