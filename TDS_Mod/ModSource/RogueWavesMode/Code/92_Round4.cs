#if ROGUEWAVESMODE_GAPS && MODKIT_PLAY
// Debug build only, its own launch: -Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_GAPS,ROGUEWAVESMODE_SHOTS,MODKIT_PLAY with -rwonly r4
// (Tools/pc/Run-TwoPlayers.ps1 -Extra "-rwonly r4"). PC round 4's looks for Dimitri's eye (docs/reports/PC_ROUND4_2026-11-05.md):
//   R4-Y  Yossele's final sheets (ModSource/Yossele/Art/): walking and attacking towards each side of the screen, the sheet and
//         row the game draws (rows v0-v3: front right, front left, back right, back left) and a picture of each
//   R4-M  the meteor's white crystal (open decisions 11-05b item 10): the node beside the nearest stock ore, its size, and a hit
//   R4-S  Spool's sage green beside its own buildings and a stock foe, on grass
// Each is INFO lines and pictures (shot_r4_*); nothing here passes or fails but a missing look.
public static partial class RWTest
{
    static void R4Play(Entity_Master vessel)
    {
        RogueWavesModeKitPlay.Log = m => Info("r4 play", m);
        RogueWavesModeKitPlay.Host = RogueWavesModeMod.Instance;
#if ROGUEWAVESMODE_SHOTS
        RogueWavesModeKitPlay.Shot = RWShots.Shot;
#endif
        RogueWavesModeKitPlay.Init();
        RogueWavesModeKitPlay.Reset(vessel);
    }

    // The sheet and row a thing's sprites show, rows counted from the top in 32 px cells: "Yossele-Sheet row 4".
    static string SheetRow(Entity_Master m)
    {
        if (m == null || m.Client == null || m.Client.Currentvis == null) return "no look";
        List<string> parts = new List<string>();
        foreach (SpriteRenderer r in m.Client.Currentvis.GetComponentsInChildren<SpriteRenderer>(false))
        {
            if (r == null || r.sprite == null || !r.enabled) continue;
            Texture t = r.sprite.texture;
            int row = t != null ? Mathf.FloorToInt((t.height - r.sprite.rect.yMax) / 32f + 0.01f) : -1;
            parts.Add((t != null ? t.name : "?") + " row " + row + (r.flipX ? " flipX" : "") + (r.transform.lossyScale.x < 0f ? " mirrored" : ""));
        }
        return parts.Count > 0 ? string.Join("; ", parts.ToArray()) : "no sprite";
    }

    static IEnumerator R4Sample(Entity_Master m, float seconds, Dictionary<string, int> seen)
    {
        float t = 0f;
        while (t < seconds)
        {
            string s = SheetRow(m);
            int n;
            seen.TryGetValue(s, out n);
            seen[s] = n + 1;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    static string R4Seen(Dictionary<string, int> seen)
    {
        List<KeyValuePair<string, int>> all = new List<KeyValuePair<string, int>>(seen);
        all.Sort((a, b) => b.Value.CompareTo(a.Value));
        List<string> parts = new List<string>();
        foreach (KeyValuePair<string, int> kv in all) parts.Add(kv.Key + " x" + kv.Value);
        return string.Join(", ", parts.ToArray());
    }

    static string R4Screen(Entity_Master m)
    {
        Vector2 at;
        if (m == null) return "gone";
        bool on = RogueWavesModeKitPlay.OnScreen(m.Entityposition.Value, out at, 0f);
        return (on ? "on screen at " : "off screen at ") + Mathf.RoundToInt(at.x) + "," + Mathf.RoundToInt(at.y) + " of " + Screen.width + "x" + Screen.height;
    }

    static string R4Size(Entity_Master m)
    {
        if (m == null || m.Client == null || m.Client.Currentvis == null) return "no look";
        Bounds b = new Bounds(m.Entityposition.Value, Vector3.zero);
        bool any = false;
        foreach (Renderer r in m.Client.Currentvis.GetComponentsInChildren<Renderer>(false))
        {
            if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        return any ? m.Client.Currentvis.name + " " + RWUtil.F(b.size.x) + " x " + RWUtil.F(b.size.y) + " x " + RWUtil.F(b.size.z) : m.Client.Currentvis.name + " (no renderer)";
    }

    static IEnumerator GapsRound4()
    {
        yield return Stepped(R4Yossele());
        yield return BackOnBoat();
        yield return Stepped(R4Spool());
        yield return BackOnBoat();
    }

    static IEnumerator R4Yossele()
    {
        Macro_Main boat = Macro_Main.Instance;
        RWRun.Clear();
        boat.Selectcharacter("Hero_Yossele");
        yield return new WaitForSecondsRealtime(1f);
        yield return ToMap(Seed, RWChart.Calm);
        long me = RWMine.Session;
        RWPlayer player = RWRun.For(me);
        Entity_Master vessel = RWRun.Vessel(me);
        if (vessel == null || vessel.Being.Value.ToString() != "Hero_Yossele") { Setup("R4-Y", "no Yossele"); yield break; }
        R4Play(vessel);
        yield return RogueWavesModeKitPlay.WaitForWorld(30f);
        Camera cam = Camera.main;
        if (cam == null) { Setup("R4-Y", "no camera"); yield break; }
        Vector3 right = cam.transform.right; right.y = 0f; right.Normalize();
        Vector3 away = cam.transform.forward; away.y = 0f; away.Normalize();
        Info("R4-Y", "standing: " + SheetRow(vessel) + "; screen right is world " + RWUtil.V(right) + ", away from the camera " + RWUtil.V(away));

        // Walking: four ways, each a walk of about 4 tiles, sampled every frame while it moves.
        string[] names = { "right", "left", "away", "toward" };
        Vector3[] ways = { right, -right, away, -away };
        for (int i = 0; i < 4; i++)
        {
            vessel = RWRun.Vessel(me);
            if (vessel == null) { Setup("R4-Y walk " + names[i], "no vessel"); continue; }
            Vector3 from = vessel.Entityposition.Value;
            yield return RogueWavesModeKitPlay.Look(from + ways[i] * 2f);
            Dictionary<string, int> seen = new Dictionary<string, int>();
            yield return RogueWavesModeKitPlay.WalkTo(from + ways[i] * 4f, "Yossele heading " + names[i]);
            yield return R4Sample(vessel, 0.5f, seen);
            string where = R4Screen(vessel);
            yield return Snap("r4_yossele_walk_" + names[i]);
            yield return R4Sample(vessel, 1.2f, seen);
            float moved = vessel != null ? ModKitUtil.Flat(vessel.Entityposition.Value, from) : 0f;
            Info("R4-Y walking " + names[i], "moved " + RWUtil.F(moved) + " tiles; " + where + " at the picture; drawn: " + R4Seen(seen));
            yield return new WaitForSeconds(1.5f);
        }

        // Attacking: a foe to each side of the screen, about 3 tiles off; the laser's frames while it fires.
        foreach (int side in new[] { 1, -1 })
        {
            vessel = RWRun.Vessel(me);
            if (vessel == null) { Setup("R4-Y attack", "no vessel"); continue; }
            ClearFoes();
            string label = side > 0 ? "right" : "left";
            // Within Yossele's Range 4 (02_Data.cs): about 2 tiles to that side, or the nearest free tile past it.
            Entity_Master foe = null;
            foreach (float off in new[] { 2f, 2.8f, 1.4f })
            {
                Vector3 near = vessel.Entityposition.Value + right * (off * side);
                foe = RogueWavesModeKitTest.SpawnFoe("Enemy_Dagger", near, 0f, 0.9f, null) ?? RogueWavesModeKitTest.SpawnFoe("Enemy_Spear", near, 0f, 0.9f, null);
                if (foe != null) break;
            }
            if (foe == null) { Setup("R4-Y attack " + label, "no tile for a foe"); continue; }
            yield return RogueWavesModeKitPlay.Look(vessel.Entityposition.Value);
            Dictionary<string, int> seen = new Dictionary<string, int>();
            string where = "";
            float waited = 0f;
            // The laser fires every 2 s: wait for the first attack frame, then picture it.
            while (waited < 6f && !SheetRow(vessel).Contains("Attack")) { waited += Time.unscaledDeltaTime; string s = SheetRow(vessel); int n; seen.TryGetValue(s, out n); seen[s] = n + 1; yield return null; }
            where = R4Screen(vessel) + ", the foe " + R4Screen(foe) + " (" + RWUtil.F(foe != null ? ModKitUtil.Flat(foe.Entityposition.Value, vessel.Entityposition.Value) : -1f) + " tiles off)";
            yield return Snap("r4_yossele_attack_" + label);
            yield return R4Sample(vessel, 2.5f, seen);
            Info("R4-Y attacking a foe on the " + label, where + " at the picture, after " + RWUtil.F(waited) + " s; drawn: " + R4Seen(seen));
        }
        ClearFoes();

        // R4-M: the meteor's node in a real Tide, beside the nearest stock ore, and a hit on it.
        RWObjectiveDef goal = Array.Find(RWObjectives.All, o => o.Name == "Meteor");
        if (goal == null) { Setup("R4-M", "no Meteor objective"); yield break; }
        RWObjectives.Posted = goal;
        RWObjectives.Active = null;
        RWObjectives.Take(player);
        Game_Mapvars vars = Game_Mapvars.Instance;
        int wave = vars.Wave.Value + 1, breaks = RWVote.Breaks;
        freeze = false;
        vars.Time_Wave.Value = 0.1f;
        yield return Until(() => vars.Wave.Value >= wave, 60f);
        float t = 0f;
        while (RWObjectives.Target == null && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
        Entity_Master node = RWObjectives.Target;
        if (node == null) { Setup("R4-M", "no node after Tide " + wave + " began"); }
        else
        {
            // No clearing here: a Tide with no foes left ends at once, and the failed objective takes its node with it.
            yield return new WaitForSeconds(2f);
            Entity_Master ore = null;
            float best = 1e9f;
            foreach (Entity_Master e in RogueWavesModeKitPlay.Ore())
            {
                if (e == node) continue;
                float d = ModKitUtil.Flat(e.Entityposition.Value, node.Entityposition.Value);
                if (d < best) { best = d; ore = e; }
            }
            Info("R4-M the meteor's node", node.Being.Value + ", look " + R4Size(node) + " at " + RWUtil.V(node.Entityposition.Value) + "; the nearest stock ore " + (ore != null ? ore.Being.Value + ", look " + R4Size(ore) + ", " + RWUtil.F(best) + " tiles away" : "none"));
            yield return RogueWavesModeKitPlay.Look(node.Entityposition.Value);
            yield return new WaitForSecondsRealtime(1f);
            Info("R4-M picture", R4Screen(node));
            yield return Snap("r4_meteor");
            // The node can stand behind a cliff from the rig's angle: the same view zoomed both ways.
            Game_Cameracontroller cc = Game_Cameracontroller.Instance;
            if (cc != null)
            {
                float z0 = cc.Zoommulti;
                foreach (float f in new[] { 0.6f, 1.6f })
                {
                    cc.Zoommulti = z0 * f;
                    yield return new WaitForSecondsRealtime(1.5f);
                    yield return RogueWavesModeKitPlay.Look(node.Entityposition.Value);
                    Info("R4-M picture, zoom x" + RWUtil.F(f), "Zoommulti " + RWUtil.F(z0) + " -> " + RWUtil.F(cc.Zoommulti) + ", camera at " + RWUtil.V(Camera.main.transform.position) + ", the node " + R4Screen(node));
                    yield return Snap("r4_meteor_zoom" + (f < 1f ? "_in" : "_out"));
                }
                cc.Zoommulti = z0;
                yield return new WaitForSecondsRealtime(1f);
            }
            if (ore != null && best < 12f)
            {
                yield return RogueWavesModeKitPlay.Look((node.Entityposition.Value + ore.Entityposition.Value) * 0.5f);
                yield return new WaitForSecondsRealtime(1f);
                Info("R4-M picture with the ore", "the node " + R4Screen(node) + ", the ore " + R4Screen(ore));
                yield return Snap("r4_meteor_and_ore");
                yield return RogueWavesModeKitPlay.Look(node.Entityposition.Value);
                yield return new WaitForSecondsRealtime(1f);
            }
            vessel = RWRun.Vessel(me);
            if (vessel != null && ModKitUtil.Alive(node))
            {
                float h0 = Hp(node);
                Hurt(vessel, node, 1f);
                yield return null;
                yield return Snap("r4_meteor_hit");
                yield return new WaitForSeconds(0.1f);
                yield return Snap("r4_meteor_hit_later");
                Info("R4-M a hit", "Health " + RWUtil.F(h0) + " -> " + RWUtil.F(Hp(node)));
            }
        }
        forcing = true;
        t = 0f;
        while (RWVote.Breaks == breaks && t < 90f) { ClearFoes(); t += Time.unscaledDeltaTime; yield return null; }
        forcing = false;
        ClearFoes();
        freeze = true;
        yield return ToBoat();
    }

    static IEnumerator R4Spool()
    {
        Macro_Main boat = Macro_Main.Instance;
        RWRun.Clear();
        boat.Selectcharacter("Hero_Spool");
        yield return new WaitForSecondsRealtime(1f);
        yield return ToMap(Seed, RWChart.Calm);
        long me = RWMine.Session;
        Entity_Master vessel = RWRun.Vessel(me);
        if (vessel == null || vessel.Being.Value.ToString() != "Hero_Spool") { Setup("R4-S", "no Spool"); yield break; }
        R4Play(vessel);
        Rich(me, 8000f);
        yield return RogueWavesModeKitPlay.WaitForWorld(30f);
        Heroentryobject entry = RWUtil.Item<Heroentryobject>("Hero", "Hero_Spool");
        List<Vector3> used = new List<Vector3>();
        List<Entity_Master> known = new List<Entity_Master>();
        List<string> built = new List<string>();
        if (entry != null)
            foreach (string tower in new List<string>(entry.Towerdisplay))
            {
                Entity_Master made = null;
                yield return BuildOne(vessel, tower, used, known, (m, c) => made = m);
                built.Add(tower + (made != null ? "" : " (not built)"));
            }
        yield return new WaitForSeconds(4f);
        // The balance sweep's numbers as the game holds them: Stitched 5, Dart 5.5, the Loom 0.3 a second.
        List<string> stats = new List<string>();
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
        {
            if (e == null || e.Entitydata.Value.Owner != me || e.Statsserver == null) continue;
            string s = e.Being.Value.ToString();
            foreach (string k in new[] { "Damage", "Attackspeed", "Range" })
            {
                Link_Stats v;
                if (e.Statsserver.TryGetValue(k, out v)) s += " " + k + " " + RWUtil.F(v.Value_Calc);
            }
            if (!stats.Contains(s)) stats.Add(s);
        }
        Info("R4-S Spool's things", string.Join("; ", stats.ToArray()));
        Entity_Master foe = null;
        vessel = RWRun.Vessel(me);
        if (vessel != null) foe = RogueWavesModeKitTest.SpawnFoe("Enemy_Dagger", vessel.Entityposition.Value, 2f, 3.5f, null);
        if (vessel != null) yield return RogueWavesModeKitPlay.Look(vessel.Entityposition.Value);
        yield return new WaitForSecondsRealtime(1.5f);
        Info("R4-S Spool's look", "built " + string.Join(", ", built.ToArray()) + "; Spool " + R4Screen(vessel) + " drawn " + SheetRow(vessel) + "; a Dagger " + R4Screen(foe));
        yield return Snap("r4_spool");
        yield return new WaitForSeconds(2f);
        yield return Snap("r4_spool_later");
        ClearFoes();
        yield return ToBoat();
    }
}
#endif
