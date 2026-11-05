// Kit 1.9 (2026-11-05). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared kit: host-side map events, from one poll of Game_Mapvars (docs/PACK.md section 3). The part calls Update()
// every frame; listeners subscribe once at boot.
//   MapStart        a map's Game_Mapvars appeared and finished loading (Maploaded)
//   Tide(wave)      Game_Mapvars.Wave rose (a Tide starts; the argument is the new Wave, 1 at the game's TIDE 1)
//   MapEnd(result)  Gamecomplete left "Running" ("Win", "Lose"...), or the map went away while running ("Leave")
//   Break(wave)     the Tide table moved onto a break: the opening one (wave 0) or the one after Tide <wave>
// Events fire on the host only (IsServer); clients read synced state instead.
// Kit 1.7: Game_Mapvars.Instance keeps pointing at a destroyed map (Unity's == null is true, the reference isn't), so
// "Leave" fires when the map object is gone, not when the next map wakes (QUALITY_CHECK_2026-10-15 B1).
public static class ModKitTides
{
    public static event Action MapStart;
    public static event Action<int> Tide;
    public static event Action<string> MapEnd;
    public static event Action<int> Break;
    public static int Wave = -1;
    public static bool InBreak;      // the table's current entry is a break, as of the last Update on the host

    static Game_Mapvars seen;
    static bool started, ended;
    static int entry = -1;

    public static void Update()
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        if (mv == null) mv = null;   // a destroyed map counts as none
        bool gone = !ReferenceEquals(seen, null) && seen == null;
        bool host = Game_Entity.Instance != null && Game_Entity.Instance.IsServer;
        if (gone || !ReferenceEquals(mv, seen))
        {
            if (gone && started && !ended)
                Fire(MapEnd, "Leave");
            seen = mv;
            started = ended = false;
            Wave = entry = -1;
            InBreak = false;
        }
        if (mv == null || !host)
            return;
        if (!started)
        {
            if (!mv.Maploaded.Value)
                return;
            started = true;
            // Wave is still its first -1 here; the game writes 0 a moment later, which is no Tide (kit 1.9, SHARED-2).
            Wave = Mathf.Max(mv.Wave.Value, 0);
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
        if (mv.Entry.Value != entry)
        {
            entry = mv.Entry.Value;
            InBreak = EntryIsBreak(mv, entry);
            if (InBreak)
                Fire(Break, wave);
        }
        string state = mv.Gamecomplete.Value.ToString();
        if (!ended && state != "Running" && state != "")
        {
            ended = true;
            Fire(MapEnd, state);
        }
    }

    // The row the table is playing is Entry - 1: Mode_Normal.Run starts Entry at 1 and reads Wave[Entry - 1] (kit docs/15 section 4).
    public static bool EntryIsBreak(Game_Mapvars mv, int e)
    {
        List<Wavesobject.Waves> table = mv != null && mv.Mode != null && mv.Mode.Enemies != null ? mv.Mode.Enemies.Wave : null;
        return table != null && e >= 1 && e <= table.Count && !table[e - 1].Wave;
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
