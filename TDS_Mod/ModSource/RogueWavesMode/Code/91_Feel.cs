#if ROGUEWAVESMODE_FEEL
// Debug build only (-Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_FEEL, without ROGUEWAVESMODE_SHOTS, whose captures make their own hitches):
// how smoothly the game runs through a whole campaign. Every frame's time, by what the player is doing; each dialog's opening by
// itself, the stock Exit prompt among them; and the longest frames with where they fell. Logged at the run's end. Add
// ROGUEWAVESMODE_DIALOGLOOK,ROGUEWAVESMODE_FEELMAP for the comparison in a map instead (94_DialogLook.cs), which stops the flow at the shop.
public static class RWFeel
{
    const int Bins = 256;           // a frame's whole milliseconds; the last bin holds everything longer
    const float OpeningSeconds = 1.8f, LongFrame = 0.025f, Spike = 0.040f;

    sealed class Phase
    {
        public string Name;
        public readonly int[] Hist = new int[Bins];
        public int Frames, Long;
        public float Worst;
        public double Sum;
    }

    static readonly Dictionary<string, Phase> byName = new Dictionary<string, Phase>();
    static readonly List<Phase> phases = new List<Phase>();
    static readonly List<string> openings = new List<string>(), spikes = new List<string>(), openSlow = new List<string>();
    static string seenScene = "";
    static float sceneAt, openAt, openWorst;
    static bool wasUp, opening;
    static int openFrames, openLong, cleanups, spikeCount;

    static string Title()
    {
        Bla_Ui_Confirmation d = Bla_Ui_Confirmation.Instance;
        TMPro.TMP_Text t = d != null && d.Titlebox != null ? d.Titlebox.GetComponentInChildren<TMPro.TMP_Text>() : null;
        return t != null ? t.text : "?";
    }

    static string Where(float now)
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene != seenScene)
        {
            seenScene = scene;
            sceneAt = now;
        }
        if (now - sceneAt < 15f) return scene + ", its first 15 s";
        if (scene == "Game" && Game_Mapvars.Instance != null) return scene + (ModKitTides.InBreak ? ", a break" : ", a Tide");
        return scene;
    }

    public static void Frame()
    {
        float now = Time.unscaledTime, dt = Time.unscaledDeltaTime;
        int gc = GC.CollectionCount(0);
        bool cleaned = gc != cleanups;
        cleanups = gc;
        string where = Where(now);

        bool up = RWDialog.Up();
        if (up && !wasUp)
        {
            opening = true;
            openAt = now;
            openFrames = openLong = 0;
            openWorst = 0f;
            openSlow.Clear();
        }
        wasUp = up;
        if (opening)
        {
            openFrames++;
            openWorst = Mathf.Max(openWorst, dt);
            if (dt > LongFrame) openLong++;
            if (dt > 0.010f) openSlow.Add("+" + RWUtil.F(now - openAt) + "s " + RWUtil.F(dt * 1000f) + "ms" + (cleaned ? " with a memory cleanup" : ""));
            if (now - openAt >= OpeningSeconds)
            {
                opening = false;
                openings.Add("'" + Title() + "' in " + where + ": " + openFrames + " frames, worst " + RWUtil.F(openWorst * 1000f) + " ms, " + openLong + " long" + (openSlow.Count > 0 ? "; slow: " + string.Join(", ", openSlow.ToArray()) : ""));
            }
            return;
        }

        Phase p;
        if (!byName.TryGetValue(where, out p))
        {
            p = new Phase { Name = where };
            byName[where] = p;
            phases.Add(p);
        }
        p.Frames++;
        p.Sum += dt;
        p.Worst = Mathf.Max(p.Worst, dt);
        p.Hist[Mathf.Clamp(Mathf.RoundToInt(dt * 1000f), 0, Bins - 1)]++;
        if (dt > LongFrame) p.Long++;
        if (dt > Spike && !where.EndsWith("first 15 s"))
        {
            spikeCount++;
            if (spikes.Count < 60) spikes.Add("at " + RWUtil.F(now) + "s in " + where + ": " + RWUtil.F(dt * 1000f) + " ms" + (cleaned ? " with a memory cleanup" : ""));
            // In the log's own order, so the line before it says what the test had just done.
            Debug.Log("[RogueWavesMode] TEST INFO spike: " + RWUtil.F(dt * 1000f) + " ms in " + where);
        }
    }

    static int Percentile(Phase p, float share)
    {
        int want = Mathf.CeilToInt(p.Frames * share), seen = 0;
        for (int i = 0; i < Bins; i++)
        {
            seen += p.Hist[i];
            if (seen >= want) return i;
        }
        return Bins - 1;
    }

    static void Say(string text) { Debug.Log("[RogueWavesMode] TEST INFO feel: " + text); }

    public static void Report()
    {
        foreach (Phase p in phases)
            Say(p.Name + ": " + p.Frames + " frames, mean " + RWUtil.F((float)(p.Sum / Mathf.Max(1, p.Frames) * 1000.0)) + " ms, median " + Percentile(p, 0.5f) + ", p99 " + Percentile(p, 0.99f) + ", p99.9 " + Percentile(p, 0.999f) + ", worst " + RWUtil.F(p.Worst * 1000f) + " ms, over 25 ms " + p.Long);
        foreach (string o in openings) Say("opening " + o);
        Say(spikeCount + " frames over 40 ms outside a scene's first 15 s and the openings; the first of them:");
        foreach (string s in spikes) Say("   " + s);
    }
}
#endif
