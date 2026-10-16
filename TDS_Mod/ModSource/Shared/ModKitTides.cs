// Kit 1.6 (2026-10-08). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared kit: host-side map events, from one poll of Game_Mapvars (docs/PACK.md section 3). The part calls Update()
// every frame; listeners subscribe once at boot.
//   MapStart        a map's Game_Mapvars appeared and finished loading (Maploaded)
//   Tide(wave)      Game_Mapvars.Wave rose (a Tide starts; the argument is the new Wave, 1 at the game's TIDE 1)
//   MapEnd(result)  Gamecomplete left "Running" ("Win", "Lose"...), or the map went away while running ("Leave")
// Breaks between Tides are not an event yet: which Game_Mapvars value marks one is still to read (docs/15 section 4).
// Events fire on the host only (IsServer); clients read synced state instead.
public static class ModKitTides
{
    public static event Action MapStart;
    public static event Action<int> Tide;
    public static event Action<string> MapEnd;
    public static int Wave = -1;

    static Game_Mapvars seen;
    static bool started, ended;

    public static void Update()
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        bool host = Game_Entity.Instance != null && Game_Entity.Instance.IsServer;
        if (mv != seen)
        {
            if (seen == null && started && !ended)
                Fire(MapEnd, "Leave");
            seen = mv;
            started = ended = false;
            Wave = -1;
        }
        if (mv == null || !host)
            return;
        if (!started)
        {
            if (!mv.Maploaded.Value)
                return;
            started = true;
            Wave = mv.Wave.Value;
            Fire(MapStart);
        }
        int wave = mv.Wave.Value;
        if (wave > Wave)
        {
            Wave = wave;
            Fire(Tide, wave);
        }
        else if (wave < Wave)
            Wave = wave;   // a rewound Entry replays a Tide (docs/15 section 4): the next rise fires again
        string state = mv.Gamecomplete.Value.ToString();
        if (!ended && state != "Running" && state != "")
        {
            ended = true;
            Fire(MapEnd, state);
        }
    }

    static void Fire(Action a)
    {
        if (a == null) return;
        foreach (Delegate d in a.GetInvocationList())
        {
            try { ((Action)d)(); }
            catch (Exception e) { Debug.LogWarning("[RogueWaves] map start listener: " + e); }
        }
    }

    static void Fire<T>(Action<T> a, T value)
    {
        if (a == null) return;
        foreach (Delegate d in a.GetInvocationList())
        {
            try { ((Action<T>)d)(value); }
            catch (Exception e) { Debug.LogWarning("[RogueWaves] map listener: " + e); }
        }
    }
}
