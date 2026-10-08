#if RIPTIDE_SELFTEST && RIPTIDE_SANDBOX
// Probe 5 (-Define RIPTIDE_SELFTEST,RIPTIDE_SANDBOX): which APIs the game's code security allows, for the later parts
// of the design (meta-progression in PlayerPrefs, named messages for richer co-op state). The game checks the whole
// mod before running any of it, so a refused API refuses the mod: then Player.log names it, and probes 1-4 are rerun
// without this define.
public static class RiptideSandbox
{
    const string Key = "Riptide_Probe";
    const string Message = "Riptide_Probe";
    static int received, connected;

    static void Log(string m) { RiptideLog.Info("TEST " + m); }

    public static IEnumerator Run()
    {
        Log("probe 5: the mod loaded with PlayerPrefs, named messages and OnClientConnectedCallback in it");

        // PlayerPrefs: a round trip, then the key is removed again.
        string prefs;
        try
        {
            int value = 4207;
            PlayerPrefs.SetInt(Key, value);
            PlayerPrefs.Save();
            int back = PlayerPrefs.GetInt(Key, -1);
            PlayerPrefs.DeleteKey(Key);
            prefs = back == value && !PlayerPrefs.HasKey(Key) ? "" : "read back " + back;
        }
        catch (Exception e) { prefs = e.GetType().Name + ": " + e.Message; }
        RiptideSelfTest.Check("probe 5: PlayerPrefs round trip", prefs == "", prefs == "" ? "set, read, deleted" : prefs);

        // Named messages: register a handler, send to everyone (the host is a client too), unregister.
        NetworkManager nm = NetworkManager.Singleton;
        string named;
        bool sent = false;
        try
        {
            CustomMessagingManager cmm = nm != null ? nm.CustomMessagingManager : null;
            if (cmm == null)
                named = "no CustomMessagingManager";
            else
            {
                cmm.RegisterNamedMessageHandler(Message, OnMessage);
                // A string is written as a 4-byte length plus 2 bytes a character: "riptide" needs 18 bytes.
                FastBufferWriter writer = new FastBufferWriter(64, Allocator.Temp);
                try
                {
                    writer.WriteValueSafe("riptide", false);
                    cmm.SendNamedMessageToAll(Message, writer, NetworkDelivery.ReliableSequenced);
                    sent = true;
                }
                finally { writer.Dispose(); }
                named = "";
            }
        }
        catch (Exception e) { named = e.GetType().Name + ": " + e.Message; }
        RiptideSelfTest.Check("probe 5: a named message can be registered and sent", named == "" && sent, named == "" ? "sent to all" : named);
        yield return new WaitForSecondsRealtime(1.5f);
        Log("probe 5: the host received its own named message " + received + " time(s) (information: NGO may not loop a host's message back)");
        try
        {
            if (nm != null && nm.CustomMessagingManager != null)
                nm.CustomMessagingManager.UnregisterNamedMessageHandler(Message);
        }
        catch (Exception e) { Log("probe 5: unregistering threw " + e.Message); }

        // OnClientConnectedCallback: subscribe and unsubscribe (a late joiner would need a full snapshot from here).
        string callback;
        try
        {
            nm.OnClientConnectedCallback += OnConnected;
            nm.OnClientConnectedCallback -= OnConnected;
            callback = "";
        }
        catch (Exception e) { callback = e.GetType().Name + ": " + e.Message; }
        RiptideSelfTest.Check("probe 5: OnClientConnectedCallback can be subscribed", callback == "", callback == "" ? "subscribed and removed" : callback);
    }

    static void OnMessage(ulong sender, FastBufferReader reader)
    {
        string text;
        reader.ReadValueSafe(out text, false);
        if (text == "riptide")
            received++;
    }

    static void OnConnected(ulong client) { connected++; }
}
#endif
