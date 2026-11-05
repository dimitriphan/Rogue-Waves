// Argo's own art on top of the kit's ModKitAssets, which reads ArgoAssets_Library (shipped inside the mod prefab),
// slices sheets and plays sounds and effects. Here: its hero's and units' Spriteobjects, its icons, its tower material
// and its sound levels.
public static class ArgoAssets
{
    static readonly Dictionary<string, float> volumes = new Dictionary<string, float>();

    // The journal's model, for its 3D views. Deep Water is flat and those views look at it level, so round 5's journal
    // drew it as a line: its journal model is a coin instead, two pools back to back tipped toward the view, so a pool
    // shows whichever way the view spins it. Made once, under the inactive Models holder like the rest.
    public const float ShowcaseTilt = 40f;

    public static GameObject JournalModel(string name)
    {
        GameObject m = ModKitAssets.Quiet(name);
        if (m == null || !name.StartsWith(ArgoData.Water))
            return m;
        Transform have = ModKitAssets.Library.Find("Models/" + name + "_Showcase");
        if (have != null)
            return have.gameObject;
        GameObject root = new GameObject(name + "_Showcase");
        root.transform.SetParent(ModKitAssets.Library.Find("Models"), false);
        Transform tilt = new GameObject("Tilt").transform;
        tilt.SetParent(root.transform, false);
        tilt.localRotation = Quaternion.Euler(-ShowcaseTilt, 0f, 0f);
        // The models stand on y = -0.5 (the tile's ground); each copy is lifted so its pool's floor sits on the tilt's
        // origin, the lower one turned over.
        GameObject up = UnityEngine.Object.Instantiate(m, tilt, false);
        up.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        GameObject down = UnityEngine.Object.Instantiate(m, tilt, false);
        down.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
        down.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        return root;
    }

    static Spritesinlist Anim(string name, List<Sprite> sheet, int cols, int firstRow, int variants, int count, bool loop, bool reset, int priority, float speed, string to, Dictionary<int, string> sounds = null)
    {
        Spritesinlist a = new Spritesinlist();
        a.Name = name;
        a.Dump = new List<Sprite>();
        a.Dumpsplit = count;
        a.Sprites = new List<Spritevariantlist>();
        a.Loop = loop;
        a.Reset = reset;
        a.Priority = priority;
        a.Speed = speed;
        a.Spriteto = to ?? "";
        for (int v = 0; v < variants; v++)
        {
            Spritevariantlist variant = new Spritevariantlist();
            variant.Variant = new List<Spritecont>();
            for (int f = 0; f < count; f++)
            {
                int index = (firstRow + v) * cols + f;
                Spritecont cont = new Spritecont();
                cont.Item = index < sheet.Count ? sheet[index] : null;
                string sound;
                cont.Sound = sounds != null && sounds.TryGetValue(f, out sound) ? sound : "";
                variant.Variant.Add(cont);
            }
            a.Sprites.Add(variant);
        }
        return a;
    }

    // Argo's sheets follow the shipped vessels': Idle and Move rows v0..v3 on one 192x256 sheet (front facing right,
    // front facing left, back, back mirrored), 12-frame Falling and Land strips, and 4-variant Attack and Sonar sheets.
    public static Spriteobject Hero(Spriteobject template, string footstep)
    {
        Vector2 foot = new Vector2(0.5f, 0f);
        List<Sprite> main = ModKitAssets.Frames("Argo-Sheet", 32, foot);
        List<Sprite> fall = ModKitAssets.Frames("ArgoFall-Sheet", 32, foot);
        List<Sprite> land = ModKitAssets.Frames("ArgoIntroR-Sheet", 32, foot);
        List<Sprite> attack = ModKitAssets.Frames("ArgoAttack-Sheet", 32, foot);
        List<Sprite> sonar = ModKitAssets.Frames("ArgoSonar-Sheet", 32, foot);
        Spriteobject so = ModKitAssets.NewSpriteobject("Hero_Argo_Sprite", template, 8f, "Falling");
        // Lead boots: a step on each boot's slam (Move frames 2 and 5).
        Dictionary<int, string> steps = string.IsNullOrEmpty(footstep) ? null : new Dictionary<int, string> { { 2, footstep }, { 5, footstep } };
        ModKitAssets.Add(so, Anim("Idle", main, 6, 0, 4, 6, true, false, 0, 1f, ""));
        ModKitAssets.Add(so, Anim("Move", main, 6, 4, 4, 6, true, false, 0, 1f, "", steps));
        ModKitAssets.Add(so, Anim("Falling", fall, 12, 0, 1, 12, true, true, 2, 1f, ""));
        ModKitAssets.Add(so, Anim("Land", land, 12, 0, 1, 12, false, true, 2, 1f, "Idle"));
        ModKitAssets.Add(so, Anim("Landquick", main, 6, 0, 4, 1, false, true, 2, 10f, "Idle"));
        ModKitAssets.Add(so, Anim("Attack", attack, 4, 0, 4, 4, false, true, 1, 2f, "Idle"));
        ModKitAssets.Add(so, Anim("Sonar", sonar, 6, 0, 4, 6, false, true, 1, 1.5f, "Idle"));
        so.Preview = main.Count > 0 ? main[0] : null;
        return so;
    }

    // A Naut: Idle is the first Move frame, like the Mining Hut's miners. The "Tune" skin swaps Idle for the wrench taps.
    public static Spriteobject Naut(string crew, Spriteobject template, bool tune)
    {
        Vector2 foot = new Vector2(0.5f, 0f);
        List<Sprite> sheet = ModKitAssets.Frames("Naut_" + crew + "-Sheet", 16, foot);
        Spriteobject so = ModKitAssets.NewSpriteobject("Mob_Argonaut_" + crew + (tune ? "_Tune" : "") + "_Sprite", template, 4f, "Idle");
        if (tune)
            ModKitAssets.Add(so, Anim("Idle", ModKitAssets.Frames("NautTune_" + crew + "-Sheet", 16, foot), 6, 0, 4, 6, true, false, 0, 1f, ""));
        else
            ModKitAssets.Add(so, Anim("Idle", sheet, 6, 0, 4, 1, true, false, 0, 1f, ""));
        ModKitAssets.Add(so, Anim("Move", sheet, 6, 0, 4, 6, true, false, 0, 1f, ""));
        so.Preview = sheet.Count > 0 ? sheet[0] : null;
        return so;
    }

    // ---------------------------------------------------------------- icons (12x12 pixel art, drawn here)

    static readonly Dictionary<char, Color32> IconPalette = new Dictionary<char, Color32>
    {
        { 'c', new Color32(122, 230, 239, 255) }, { 'C', new Color32(64, 176, 196, 255) }, { 'b', new Color32(226, 178, 92, 255) },
        { 'B', new Color32(176, 128, 52, 255) }, { 'g', new Color32(24, 46, 62, 255) }, { 'w', new Color32(255, 255, 255, 255) },
        { 'r', new Color32(222, 84, 86, 255) }, { 's', new Color32(84, 190, 110, 255) }, { 'y', new Color32(244, 204, 86, 255) },
        { 'z', new Color32(170, 186, 198, 255) }, { 'Z', new Color32(120, 136, 150, 255) },
    };

    public static readonly string[] SonarIcon =
    {
        "............", "...cccccc...", "..c......c..", ".c..cccc..c.", ".c.c....c.c.", ".c.c.ww.c.c.",
        ".c.c.ww.c.c.", ".c.c....c.c.", ".c..cccc..c.", "..c......c..", "...cccccc...", "............"
    };

    static string[] CrewIcon(char light)
    {
        string l = light.ToString();
        return new[]
        {
            "............", ".....LL.....".Replace("L", l), "....bbbb....", "...bbbbbB...", "..bbgggbbB..", "..bbgcgcbB..",
            "..bbgggbbB..", "..bbbbbbbB..", "...BBBBBB...", "..zzzzzzzz..", "..ZZZZZZZZ..", "............"
        };
    }

    public static readonly string[] FathomIcon =
    {
        ".....bb.....", "....b..b....", ".....bb.....", "......b.....", "...bbbbbb...", "......b.....",
        "......b.....", ".b....b....b", ".bb...b...bb", "..bb..b..bb.", "...bbbbbbb..", "............"
    };

    // 2.0: Deep Water's FLOOD (a drop and a plus) and the Drill Rig's AIM (crosshairs on the Tremor's ring).
    public static readonly string[] FloodIcon =
    {
        "............", "....c....w..", "...ccc..www.", "..ccccc..w..", ".ccwcccc....", ".cwccccc....",
        ".cwcccCC....", ".ccccCCC....", "..cCCCC.....", "...CCC......", "............", "............"
    };

    public static readonly string[] AimIcon =
    {
        "............", ".....r......", "...BBrBB....", "..B..r..B...", ".B...r...B..", ".rrrr.rrrr..",
        ".B...r...B..", "..B..r..B...", "...BBrBB....", ".....r......", "............", "............"
    };

    public static string[] IconFor(Color crew)
    {
        return CrewIcon(crew.r > 0.8f && crew.g > 0.6f ? 'y' : crew.g > crew.r ? 's' : 'r');
    }

    // box: ModKitAssets.CommandIcon for an icon the HUD's command buttons draw (pitfall #156).
    public static Sprite Pixel(string[] rows, string name, int box = 0) { return ModKitAssets.Pixel(rows, name, IconPalette, box); }

    // ---------------------------------------------------------------- tower material

    // Swap every library model onto a copy of the game's own tower material, so Argo's towers are lit like the rest.
    public static void ApplyTowerMaterial(Material gameTower)
    {
        if (gameTower == null)
        {
            ArgoLog.Warn("no game tower material found; Argo towers keep the preview material");
            return;
        }
        Texture palette = null;
        Transform models = ModKitAssets.Library.Find("Models");
        foreach (Renderer r in models.GetComponentsInChildren<Renderer>(true))
        {
            Material m = r.sharedMaterial;
            if (palette == null && m != null)
                palette = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.mainTexture;
        }
        if (palette == null)
            return;
        palette.filterMode = FilterMode.Point;
        Material mat = new Material(gameTower);
        mat.name = "Argo_Tower";
        mat.SetTexture("_MainTex", palette);
        foreach (Renderer r in models.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterial = mat;
    }

    // Playback volume that puts this clip at its target loudness (rms x volume), measured once per clip.
    public static float Volume(string clipName, AudioClip clip)
    {
        float v;
        if (volumes.TryGetValue(clipName, out v))
            return v;
        float target;
        if (!ArgoData.SoundTarget.TryGetValue(clipName, out target))
            target = 0.004f;
        v = 0.05f;
        try
        {
            float[] d = new float[clip.samples * clip.channels];
            if (clip.GetData(d, 0))
            {
                double sum = 0;
                for (int i = 0; i < d.Length; i++) sum += d[i] * d[i];
                float rms = (float)Math.Sqrt(sum / Math.Max(1, d.Length));
                if (rms > 0.0001f)
                    v = Mathf.Clamp(target / rms, 0.002f, 1f);
            }
        }
        catch (Exception) { }
        volumes[clipName] = v;
        return v;
    }

    public static void Play(string clipName, Vector3 position, float scale = 1f)
    {
        AudioClip clip = ModKitAssets.Clip(clipName);
        if (clip != null)
            ModKitAssets.Play(clipName, position, Volume(clipName, clip) * scale);
    }
}
