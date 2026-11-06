// Three online fixes from reading the game's code (kit docs/19 §3 and §5, pitfalls #93 and #94), in every map and on
// every machine while the pack is on:
//   RWIdentity  a friend who restarts their game keeps their Sessionid, so the rejoin gives their seat back
//   RWLoad      the host sends back a player whose map never loads, so the others aren't all sent to the main menu
//   RWBody      a player who joins a running map has the stock sprite body (Vfx/Mobbase) for the things already there
public static class RWIdentity
{
    // The game makes a player's Sessionid from a GUID in the save's "Metadata" folder (Blm_Player.Start), and wipes
    // that folder on every launch (Bla_Data.Temporaryfields), so a restarted friend joins as a new player and the
    // rejoin turns them away. The host gives a GUID it has seen the same Sessionid for as long as it hosts
    // (Blm_Multiplayer.Getsessionid), and the game's own reconnect then hands the vessel and towers back
    // (Game_Players.Playerconnected). So every machine keeps its GUID in the pack's save folder (RWKeys.Guid, with
    // RWKeys.Device) and puts it back into Metadata before the game reads it. The kept GUID is tied to this computer: a
    // save copied to a friend's PC must not give two players one GUID (the host would add the same Sessionid twice,
    // Blm_Player.Populatesessionid).
    public static string Guid = "";       // the GUID this machine joins with
    public static bool Restored;          // this launch put a kept GUID back
    static float nextLook;

    public static void Update()
    {
#if ROGUEWAVESMODE_TWOPC
        if (RWTwo.Identity()) return;
#endif
        float now = Time.realtimeSinceStartup;
        if (now < nextLook)
            return;
        nextLook = now + 0.5f;
        if (Blockage.Tags.Bla_Data.Instance == null || Blockage.Tags.Bla_Data.Playerdata == null || !Blockage.Tags.Bla_Data.Playerdata.Loaded)
            return;
        string live = RWUtil.ReadString("Metadata", "Player_Guid");
        string kept = RWUtil.ReadString(RWData.SaveFolder, RWKeys.Guid);
        bool here = RWUtil.ReadString(RWData.SaveFolder, RWKeys.Device) == Device();
        if (!here)
            kept = "";
        Unity.Netcode.NetworkManager net = Unity.Netcode.NetworkManager.Singleton;
        bool joined = net != null && (net.IsClient || net.IsServer);
        string want;
        if (live != "" && (joined || kept == ""))
            want = live;                                  // the host may know this one already, or nothing is kept yet
        else if (kept != "")
            want = kept;                                  // a new launch or another save slot: the kept one goes back
        else
            want = System.Guid.NewGuid().ToString();      // neither: the one the game would have made
        if (live != want)
        {
            RWUtil.WriteString("Metadata", "Player_Guid", want);
            if (want == kept)
            {
                Restored = true;
                RWLog.Info("identity: this player's id from the last launch is back, so a restart can rejoin a running map");
            }
        }
        if (kept != want || !here)
        {
            RWUtil.WriteString(RWData.SaveFolder, RWKeys.Guid, want);
            RWUtil.WriteString(RWData.SaveFolder, RWKeys.Device, Device());
            RWUtil.Flush();
            RWLog.Info("identity: this player's id is kept for a restart");
        }
        Guid = want;
    }

    // A short fingerprint of this computer (FNV-1a of Unity's device id): enough to tell two PCs apart, without writing
    // the id itself into a save that may be shared.
    static string device;
    public static string Device()
    {
        if (device != null)
            return device;
        uint h = 2166136261;
        foreach (char c in SystemInfo.deviceUniqueIdentifier ?? "")
        {
            h ^= c;
            h *= 16777619;
        }
        device = h.ToString("x8");
        return device;
    }
}

public static class RWLoad
{
    // The host starts a map only once every player has loaded it, with no time limit (Game_Init.Loadgame), and every
    // machine's loading screen gives up after 120 s and goes to the main menu (Game_Load.Loadtimeout). So one player
    // whose load never ends sends everyone back. Host, while the loading screen is up: once the host itself has loaded,
    // a player still loading Grace seconds after it began is sent back (Kickclient). The game then stops waiting for
    // them (their player object leaves Blm_Server.Players) and the others get in with time to spare: after the last
    // load the fly-in takes about 15 s more before the loading screen's own timer stops.
    public const float Grace = 75f;
    public static int Loads, Kicked;      // loading screens seen and players sent back, since launch (self-test, log)
    static Game_Load seen;
    static float since, nextLook;
    static readonly List<ulong> sent = new List<ulong>();   // this load: a kick takes a few frames to land

    public static void Update()
    {
        if (!RWUtil.IsHost || Blm_Server.Instance == null || Blm_Multiplayer.Instance == null)
        {
            seen = null;
            return;
        }
        float now = Time.realtimeSinceStartup;
        if (now < nextLook)
            return;
        nextLook = now + 1f;
        Game_Init init = Game_Init.Instance;
        bool running = init != null && init.IsSpawned && init.Gameloaded.Value;
        if (running || Macro_Main.Instance != null)
        {
            seen = null;                                  // on the boat or in a running map: no search needed
            return;
        }
        Game_Load load = UnityEngine.Object.FindObjectOfType<Game_Load>();
        if (load == null)
        {
            seen = null;
            return;
        }
        if (load != seen)
        {
            seen = load;
            since = now;
            sent.Clear();
            Loads++;
        }
        if (now - since < Grace)
            return;
        Blm_Player me = Blm_Server.Instance.Localplayer;
        Tds_Clientinfo mine = me != null ? me.GetComponent<Tds_Clientinfo>() : null;
        if (mine == null || !mine.Loaded.Value)
            return;                                       // the host is the slow one: sending others back gains nothing
        // A copy: a kick takes the player out of the dictionary.
        foreach (KeyValuePair<ulong, Blm_Player> kv in new List<KeyValuePair<ulong, Blm_Player>>(Blm_Server.Instance.Playeridsplayer))
        {
            if (kv.Key == Unity.Netcode.NetworkManager.ServerClientId || kv.Value == null || sent.Contains(kv.Key))
                continue;
            Tds_Clientinfo info = kv.Value.GetComponent<Tds_Clientinfo>();
            if (info == null || info.Loaded.Value)
                continue;
            string name = kv.Value.Displayname.Value.ToString();
            Kicked++;
            sent.Add(kv.Key);
            RWLog.Warn("load: " + name + " (client " + kv.Key + ") still loading after " + RWUtil.F(now - since) + " s: sent back so the others can play");
            RWRejoin.Say(name + " couldn't load the map in time and went back to the main menu. They can join again on the boat.");
            Blm_Multiplayer.Instance.Kickclient(kv.Key);
        }
    }
}

// R4 VFX: PC round 4 §5 "Open" (548 "Could not retrieve Vfx/Mobbase The GROUP exists, but item does not" on the stranger's
// and the rejoiner's machine, none on the host's or the first friend's). Every thing's client half takes the body sprite
// things are drawn on, Getload("Vfx", "Mobbase"), once, as the first step of its Start, before it waits for this machine
// to load (Entity_Master_Client.<Start>d__25, IL_0040). Mobbase is in the stock group General_Default, which the game
// loads only in a map (Game_Ui, Game_Client_Players, Game_Artifacts and Game_Client_Init each Waitload it), and
// Bl_Address.Load puts a group's lists in Groups at once but fills them as each asset arrives. A join into a running map
// gets every thing there in one go, in the frames that load starts: each half logs the line once (548 = the things on
// that map; the stranger's 548 NREs per try at IL_0073 are the same halves) and keeps no body for good, so a thing drawn
// on it (Style Sprite, as every stock foe; or no prefab) has no look on that machine (Editvis, IL_025a). A map loaded
// from the boat never meets it: the host spawns its things after its own load, and every map after a machine's first
// finds the group in (nothing is unloaded). So every machine loads the group at boot (Preload), and on a join a half
// still without the body gets it the first frame it is in. That is always before its look is built: the look waits for
// this machine's Loaded, which Game_Client_Init sets only after loading the group. The joining machine logs the counts.
public static class RWBody
{
    public const string Group = "General_Default", Label = "Vfx", Item = "Mobbase";
    public const float Settle = 3f;                  // seconds after this machine's Loaded before the count
    public static float BootSeconds = -1f;           // how long the boot's load took; -1 until it is in (self-test, log)
    public static bool InAtJoin, Told;               // this join: the body was in as the map arrived; the line is written
    public static int Things, Given, Missing;        // this join: client halves here, given the body here, still without it
    static Game_Mapvars map;
    static bool joined, gave;
    static float since, loadedAt;

    public static GameObject Body() { return ModKitUtil.Load<GameObject>(Label, Item); }

    // From the mode's boot, once the address book is up; nothing waits for it.
    public static IEnumerator Preload()
    {
        Bl_Address.Loadgroup g = null;
        try { g = Bl_Address.Load(Group); }
        catch (Exception e) { RWLog.Warn("general: loading " + Group + ": " + e.Message); }
        float t = 0f;
        while (g != null && !g.Loaded && t < 180f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (g != null && g.Loaded)
            BootSeconds = t;
        RWLog.Info("general: " + Group + " loaded at boot: " + (g != null && g.Loaded) + " after " + RWUtil.F(t) + " s; " + Label + "/" + Item + " in: " + (Body() != null));
    }

    public static string Line()
    {
        return "the stock sprite body " + Label + "/" + Item + " (" + Group + ") was in as this map arrived: " + InAtJoin
            + "; things here " + Things + ", given the body here " + Given + ", still without it " + Missing;
    }

    public static void Update()
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        if (mv != map)
        {
            map = mv;
            joined = gave = Told = InAtJoin = false;
            Things = Given = Missing = 0;
            loadedAt = -1f;
        }
        if (mv == null || Told || !RWRejoin.Late)
            return;
        float now = Time.realtimeSinceStartup;
        GameObject body = Body();
        if (!joined)
        {
            joined = true;
            since = now;
            InAtJoin = body != null;
        }
        // One pass, the first frame the body is in: a half that looked before has none, one that looks after finds it.
        if (!gave && body != null)
        {
            gave = true;
            foreach (Entity_Master_Client c in UnityEngine.Object.FindObjectsOfType<Entity_Master_Client>())
                if (c.Visbasedefault == null)
                {
                    c.Visbasedefault = body;
                    Given++;
                }
        }
        Blm_Player me = Blm_Server.Instance != null ? Blm_Server.Instance.Localplayer : null;
        Tds_Clientinfo info = me != null ? me.GetComponent<Tds_Clientinfo>() : null;
        if (loadedAt < 0f && info != null && info.Loaded.Value)
            loadedAt = now;
        bool gaveUp = now - since > 180f;
        if (!gaveUp && (loadedAt < 0f || now - loadedAt < Settle))
            return;
        Told = true;
        foreach (Entity_Master_Client c in UnityEngine.Object.FindObjectsOfType<Entity_Master_Client>())
        {
            Things++;
            if (c.Visbasedefault == null)
                Missing++;
        }
        string line = "rejoin: " + Line() + (loadedAt < 0f ? " (the map didn't finish loading here in 180 s)" : "");
        if (Missing > 0) RWLog.Warn(line);
        else RWLog.Info(line);
    }
}
