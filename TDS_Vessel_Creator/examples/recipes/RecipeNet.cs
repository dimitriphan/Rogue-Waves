using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Online plumbing for a mode's host brain (docs/19 §6). Read from the game's code on 2026-10-14 and compiled with
// cloud-check; NEVER RUN IN GAME. Copy what you need into a part's Code folder and rename the class.
public static class RecipeNet
{
    public const string Prefix = "RW_";   // every message type the pack sends starts with this (rule 4)

    public static bool IsServer { get { return Game_Entity.Instance != null && Game_Entity.Instance.IsServer; } }

    // ---------------------------------------------------------------- who is who

    // A player's client id from the Sessionid every entity's Owner carries (host and clients). Blm_Player objects are
    // owned by their client and named after its id (Blm_Player.cs:51-61).
    public static bool ClientId(long session, out ulong client)
    {
        client = 0;
        Blm_Player p = Player(session);
        if (p == null)
            return false;
        client = p.OwnerClientId;
        return true;
    }

    public static Blm_Player Player(long session)
    {
        Blm_Player p;
        if (Blm_Server.Instance == null || !Blm_Server.Instance.Sessionidsplayer.TryGetValue(session, out p))
            return null;
        return p;
    }

    // The name that survives a client restart (the Sessionid doesn't, 18 §5). Synced, owner-written, not unique: two
    // friends with one Steam name share it. The Steam id itself is out of reach (Steamworks is not in the reference
    // list).
    public static string DurableName(long session)
    {
        Blm_Player p = Player(session);
        return p != null ? p.Displayname.Value.ToString() : "";
    }

    // An entity every map has and every machine knows: the team's core (tag "Core", the Basin). Works on clients
    // (synced Taglist), so both sides can hook the same carrier.
    public static Entity_Master Carrier()
    {
        if (Game_Entity.Instance == null)
            return null;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglist != null && e.Taglist.Contains(new Unity.Collections.FixedString64Bytes("Core")))
                return e;
        return null;
    }

    // ---------------------------------------------------------------- host to one machine (rule 4)

    // Host only. Reaches Entity_Master_Client.Pingpongclient on that machine alone, as the stock Block_Surface does
    // (Entity_Ai_Block_Surface.cs:70-81). Reliable, not throttled.
    public static void Tell(Entity_Master carrier, ulong client, string type, string msg)
    {
        if (carrier == null || carrier.Radio == null || !IsServer)
            return;
        ClientRpcParams p = default(ClientRpcParams);
        p.Send.TargetClientIds = new ulong[] { client };
        carrier.Radio.Pongload_ClientRpc(type, msg, p);
    }

    public static void TellPlayer(Entity_Master carrier, long session, string type, string msg)
    {
        ulong client;
        if (ClientId(session, out client))
            Tell(carrier, client, type, msg);
    }

    // Every machine, once per carrier (the host is a client too). The game's own load ping arrives on the same
    // delegate, so filter on the pack's prefix. Hook a new carrier on each map: entities don't outlive the scene.
    public static void Listen(Entity_Master carrier, Action<string, string> onMessage)
    {
        if (carrier == null || carrier.Client == null)
            return;
        carrier.Client.Pingpongclient += (type, msg) =>
        {
            if (type == null || !type.StartsWith(Prefix))
                return;
            try { onMessage(type, msg ?? ""); }
            catch (Exception e) { Debug.LogWarning("[RW] message " + type + ": " + e.Message); }
        };
    }

    // ---------------------------------------------------------------- late join (rule 5)

    // Host only. Each machine pings the host once when its client side of `carrier` is built
    // (Entity_Master_Client.cs:230-233): a late joiner, or a machine that just loaded the map. Answer it alone with
    // everything it missed that no synced carrier holds (debuff looks, a mod's own look, the music stage).
    public static void OnClientReady(Entity_Master carrier, Action<ulong> catchUp)
    {
        if (carrier == null || !IsServer)
            return;
        carrier.Pingpongserver += p =>
        {
            try { catchUp(p.Receive.SenderClientId); }
            catch (Exception e) { Debug.LogWarning("[RW] catch-up: " + e.Message); }
        };
    }

    // Any machine: an argument-less "ask the host" (the host answers with Tell to the sender).
    public static void AskHost(Entity_Master carrier)
    {
        if (carrier != null && carrier.Remote != null)
            carrier.Remote.Pingload_ServerRpc();
    }

    // ---------------------------------------------------------------- one clock (rule 8)

    // The host's map clock, synced: fixedTime since the map started (Game_Mapvars.cs:144). The host writes a deadline
    // (Now + seconds) into a synced carrier, such as a None command's Goalvalue; every client shows Left(deadline).
    public static float Now
    {
        get { return Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Time_Synced.Value : 0f; }
    }

    public static float Left(float deadline) { return Mathf.Max(0f, deadline - Now); }

    // ---------------------------------------------------------------- one map load can't kick everyone (rule 10)

    // Host only, called once a second while the map loads. A client still not Loaded after `grace` seconds is kicked
    // so the others don't all drop to the main menu at 120 s (Game_Load.cs:36-70). Untested; the grace is a guess.
    static readonly Dictionary<ulong, float> waiting = new Dictionary<ulong, float>();

    public static void LoadWatchdog(float grace)
    {
        if (Blm_Server.Instance == null || Blm_Multiplayer.Instance == null || !RecipeNetHost)
            return;
        foreach (KeyValuePair<ulong, Blm_Player> kv in Blm_Server.Instance.Playeridsplayer)
        {
            Tds_Clientinfo info = kv.Value != null ? kv.Value.GetComponent<Tds_Clientinfo>() : null;
            if (info == null || info.Loaded.Value)
            {
                waiting.Remove(kv.Key);
                continue;
            }
            float since;
            if (!waiting.TryGetValue(kv.Key, out since))
                waiting[kv.Key] = since = Time.unscaledTime;
            if (Time.unscaledTime - since > grace && kv.Key != NetworkManager.ServerClientId)
            {
                Debug.LogWarning("[RW] client " + kv.Key + " has not loaded the map after " + grace + " s: kicked");
                waiting.Remove(kv.Key);
                Blm_Multiplayer.Instance.Kickclient(kv.Key);
                return;   // the dictionary changes on a kick
            }
        }
    }

    // The network host, also during a load (Game_Entity isn't up yet).
    static bool RecipeNetHost { get { return NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost; } }
}
