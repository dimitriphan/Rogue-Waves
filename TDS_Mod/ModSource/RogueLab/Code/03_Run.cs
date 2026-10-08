using UnityEngine.SceneManagement;
using Unity.Netcode;

// Gets a solo host from the main menu into map 1 of a Rogue run (the game ignores injected input, so the mod does what
// the player would), then hands each map to RogueLabMatch. The run's state lives here, in statics of the mod's
// DontDestroyOnLoad object, which is what docs/15 §3 says carries a run across maps.
public static class RogueLabRun
{
    public static int Phase;                 // 0 menu, 1 boat, 2 loading map 1, 3 map 1, 4 loading map 2, 5 map 2, 9 done
    public static int Map;                   // maps entered in this run
    public static string NextPlan = "";      // stock Gameplan of map 2
    public static readonly Dictionary<long, float> PureAura = new Dictionary<long, float>();
    public static int ObjectId;              // the mod object's instance id when map 1 started
    static float seen = -1f, step = -1f;
    static string node = "";

    public static void Update()
    {
        try { Drive(); }
        catch (Exception e) { RogueLabLog.Test("driver error: " + e); Phase = 9; }
    }

    static void Drive()
    {
        RogueLabMatch.Tick();
        if (Phase >= 9)
            return;
        float now = Time.realtimeSinceStartup;
        string scene = SceneManager.GetActiveScene().name;

        Mainmenu_Main menu = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (Phase == 0 && menu != null)
        {
            if (seen < 0f) seen = now;
            if (now - seen > 8f && step < 0f)
            {
                RogueLabLog.Test("main menu: embarking");
                menu.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
                step = now;
            }
            if (step > 0f && now - step > 25f)
            {
                RogueLabLog.Test("embark did not leave the menu, retrying");
                menu.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
                step = -1f;
                seen = now;
            }
            return;
        }

        Macro_Main macro = Macro_Main.Instance;
        if (Phase <= 1 && macro != null && macro.IsSpawned && Tds_Multiplayer_Remote.Instance != null && Blm_Server.Instance != null && Blm_Server.Instance.Localplayer != null)
        {
            if (Phase == 0) { Phase = 1; seen = now; step = -1f; RogueLabLog.Test("in " + scene + ": waiting for the world map"); }
            if (now - seen < 8f)
                return;
            if (step < 0f)
            {
                if (RogueLabUtil.Heroentry(RogueLabData.Hero) != null)
                    macro.Selectcharacter(RogueLabData.Hero);
                string why;
                node = ModKitTest.PickNode(out why);
                RogueLabLog.Test("map: " + why + "; picked node '" + node + "'");
                if (node != "")
                    Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(node, false, default(ServerRpcParams));
                step = now;
                return;
            }
            if (now - step > 3f && now - step < 4f)
            {
                Lobby();
                macro.Playgame(true);
                step = now - 5f;
                RogueLabLog.Test("ready: starting the match");
                Phase = 2;
            }
            if (now - step > 40f)
            {
                RogueLabLog.Test("match did not start; giving up");
                Phase = 9;
            }
            return;
        }
    }

    // On the boat, after the leader picked a map: equip the mod Cardinal in its colour's slot and turn the match into the
    // run's first map by swapping the mode and Gameplan the pick wrote (docs/15 §3, §7; the scope's start route).
    static void Lobby()
    {
        Tds_Clientinfo info = Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>();
        string hero = info != null ? info.Hero.Value.ToString() : "";
        if (hero != RogueLabData.Hero)
            RogueLabProbe.Setup("Boutman picked", "the vessel is " + hero + " (Boutman locked or missing): the F-key test spawns Boutbro anyway");
        // The colour-slot route, which checks the name against the database (the Mixed route takes any string).
        Tds_Multiplayer_Remote.Instance.Changeartifact_ServerRpc(RogueLabData.Cardinal, false, default(ServerRpcParams));

        string plan = Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString();
        string twin = RogueLabBoot.Twin(plan);
        if (Tds_Masterdatabase.Instance.Getitem("Gameplan", twin) == null)
        {
            RogueLabProbe.Setup("Rogue map 1", "no Rogue twin of Gameplan '" + plan + "' (not a Mode_Normal plan)");
            return;
        }
        Tds_Multiplayerinfo.Instance.Mode.Value = new FixedString64Bytes(RogueLabData.Mode);
        Tds_Multiplayerinfo.Instance.Gameplan.Value = new FixedString64Bytes(twin);
        NextPlan = RogueLabBoot.OtherPlan(plan);
        RogueLabProbe.Info("run plan", "map 1 " + twin + " (" + Tds_Multiplayerinfo.Instance.Map.Value + "), map 2 " + RogueLabBoot.Twin(NextPlan));
    }

    // docs/15 §3: the host sends everyone from a finished map straight into the next one, as Game_Admin.Play does:
    // the four match strings, everyone ready, then Gameloading. Despawning first copies the game's own Gotomap.
    public static IEnumerator Chain()
    {
        Gameplandatabaseobject next = Tds_Masterdatabase.Instance.Getitem("Gameplan", RogueLabBoot.Twin(NextPlan)) as Gameplandatabaseobject;
        if (next == null) { RogueLabProbe.Setup("chain to map 2", "no second Gameplan"); Done(); yield break; }
        yield return new WaitForSeconds(6f);
        System.Threading.Tasks.Task despawn = Game_Entity.Instance.Despawnall(true);
        float t = 0f;
        while (!despawn.IsCompleted && t < 20f) { t += Time.deltaTime; yield return null; }
        Tds_Multiplayerinfo.Instance.Gameplan.Value = new FixedString64Bytes(next.Objectname);
        Tds_Multiplayerinfo.Instance.Map.Value = new FixedString64Bytes(next.Map.Objectname);
        Tds_Multiplayerinfo.Instance.Mode.Value = new FixedString64Bytes(next.Mode.Objectname);
        Tds_Multiplayerinfo.Instance.Soundtrack.Value = new FixedString64Bytes(next.Soundtrack.Objectname);
        Tds_Multiplayerinfo.Instance.Gamestate.Value = new FixedString64Bytes("");
        foreach (GameObject player in Blm_Server.Instance.Players)
            player.GetComponent<Tds_Clientinfo>().Activeplayer.Value = 1;
        Blm_Multiplayer_Remote.Scenechangevar scene = default(Blm_Multiplayer_Remote.Scenechangevar);
        scene.Scene = "Gameloading";
        scene.Lobbystate = "Closed";
        RogueLabLog.Test("chain: despawned " + despawn.IsCompleted + ", loading " + next.Objectname);
        Phase = 4;
        Blm_Multiplayer_Remote.Instance.Changescene_ServerRpc(scene, default(ServerRpcParams));
    }

    public static void Done()
    {
        try { RogueLabBoot.Cleanup(); } catch (Exception e) { RogueLabLog.Test("cleanup failed: " + e.Message); }
        RogueLabLog.Test("DONE pass " + RogueLabProbe.Pass + ", fail " + RogueLabProbe.Fail + ", setup " + RogueLabProbe.Setups);
        Phase = 9;
    }
}
