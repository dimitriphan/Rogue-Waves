#if ROGUEWAVESMODE_DIALOGLOOK
// PC round 6 (test only): which step of Unity's frame a slow frame spends its time in. A marker runs before every step of
// the player loop (TimeUpdate.*, EarlyUpdate.*, Update.*, PostLateUpdate.* ...) and notes the time; each frame over 12 ms
// inside a timed window (RWTest.Frames) is kept with its four longest steps and logged after the window, so the log line
// itself never lands inside one. The dialog round starts it and puts the game's player loop back after.
public static class RWFrameSplit
{
    static string[] names;
    static int[] seqK = new int[1024];
    static double[] seqT = new double[1024];
    static int seqN;
    static double[] sum;
    static bool on;
    static UnityEngine.LowLevel.PlayerLoopSystem saved;
    static readonly List<string> kept = new List<string>();
    public static string What = "";
    public static double Since;
    public static int Steps { get { return names != null ? names.Length : 0; } }

    public static void Start()
    {
        if (on) return;
        UnityEngine.LowLevel.PlayerLoopSystem root = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
        saved = root;
        UnityEngine.LowLevel.PlayerLoopSystem[] phases = root.subSystemList;
        if (phases == null) return;
        List<string> n = new List<string>();
        UnityEngine.LowLevel.PlayerLoopSystem[] made = new UnityEngine.LowLevel.PlayerLoopSystem[phases.Length];
        for (int p = 0; p < phases.Length; p++)
        {
            UnityEngine.LowLevel.PlayerLoopSystem phase = phases[p];
            UnityEngine.LowLevel.PlayerLoopSystem[] subs = phase.subSystemList ?? new UnityEngine.LowLevel.PlayerLoopSystem[0];
            List<UnityEngine.LowLevel.PlayerLoopSystem> list = new List<UnityEngine.LowLevel.PlayerLoopSystem>();
            for (int s = 0; s < subs.Length; s++)
            {
                int k = n.Count;
                n.Add((phase.type != null ? phase.type.Name : "?") + "." + (subs[s].type != null ? subs[s].type.Name : "?"));
                UnityEngine.LowLevel.PlayerLoopSystem marker = new UnityEngine.LowLevel.PlayerLoopSystem();
                marker.type = typeof(RWFrameSplit);
                marker.updateDelegate = () => Mark(k);
                list.Add(marker);
                list.Add(subs[s]);
            }
            phase.subSystemList = list.ToArray();
            made[p] = phase;
        }
        root.subSystemList = made;
        names = n.ToArray();
        sum = new double[names.Length];
        seqN = 0;
        kept.Clear();
        UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(root);
        on = true;
    }

    public static void Stop()
    {
        if (!on) return;
        UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(saved);
        on = false;
    }

    // Marker 0 runs first in a frame: the frame before it is judged, then a new one begins.
    static void Mark(int k)
    {
        double now = Time.realtimeSinceStartupAsDouble;
        if (k == 0)
        {
            if (seqN > 1) Judge(now);
            seqN = 0;
        }
        if (seqN < seqK.Length)
        {
            seqK[seqN] = k;
            seqT[seqN] = now;
            seqN++;
        }
    }

    static void Judge(double end)
    {
        double total = end - seqT[0];
        if (total < 0.012 || What == "") return;
        for (int i = 0; i < sum.Length; i++) sum[i] = 0;
        for (int i = 0; i < seqN; i++) sum[seqK[i]] += (i + 1 < seqN ? seqT[i + 1] : end) - seqT[i];
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(What).Append(" +").Append(RWUtil.F((float)(seqT[0] - Since))).Append("s ").Append(RWUtil.F((float)(total * 1000.0))).Append(" ms:");
        bool[] used = new bool[sum.Length];
        for (int t = 0; t < 4; t++)
        {
            int best = -1;
            for (int i = 0; i < sum.Length; i++) if (!used[i] && (best < 0 || sum[i] > sum[best])) best = i;
            if (best < 0) break;
            used[best] = true;
            sb.Append(t == 0 ? " " : ", ").Append(names[best]).Append(' ').Append(RWUtil.F((float)(sum[best] * 1000.0)));
        }
        kept.Add(sb.ToString());
    }

    public static List<string> Take()
    {
        List<string> l = new List<string>(kept);
        kept.Clear();
        return l;
    }
}
#endif
