// Reads the asset pack (ArgoAssets_Library, shipped inside the mod prefab) and turns it into game objects:
// sliced sprites, Spriteobjects, menu art, tower materials, sounds and particle effects.
using UnityEngine.Audio;

public static class ArgoAssets
{
    public static Transform Library;
    static readonly Dictionary<string, List<Sprite>> frames = new Dictionary<string, List<Sprite>>();
    static readonly Dictionary<string, float> volumes = new Dictionary<string, float>();
    static AudioMixerGroup mixer;
    static bool mixerSearched;

    public static bool Find()
    {
        if (Library != null)
            return true;
        if (ArgoMod.Instance != null)
            Library = ArgoMod.Instance.transform.Find("ArgoAssets_Library");
        if (Library == null)
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go.name == "ArgoAssets_Library" && go.scene.IsValid())
                {
                    Library = go.transform;
                    break;
                }
        if (Library != null)
        {
            Library.SetParent(null);
            UnityEngine.Object.DontDestroyOnLoad(Library.gameObject);
        }
        return Library != null;
    }

    // ---------------------------------------------------------------- raw items

    public static Texture2D Sheet(string name)
    {
        Transform t = Library.Find("Sprites/" + name);
        SpriteRenderer r = t != null ? t.GetComponent<SpriteRenderer>() : null;
        if (r == null || r.sprite == null)
        {
            ArgoLog.Warn("missing sheet " + name);
            return null;
        }
        return r.sprite.texture;
    }

    // Models and effects are prefab instances under inactive groups: instantiating them yields active copies.
    public static GameObject Model(string name)
    {
        Transform t = Library.Find("Models/" + name);
        return t != null ? t.gameObject : null;
    }

    public static GameObject Vfx(string name)
    {
        Transform t = Library.Find("VFX/" + name);
        return t != null ? t.gameObject : null;
    }

    public static AudioClip Clip(string name)
    {
        Transform t = Library.Find("Audio/" + name);
        AudioSource s = t != null ? t.GetComponent<AudioSource>() : null;
        return s != null ? s.clip : null;
    }

    // ---------------------------------------------------------------- sprites

    // Frames of a sheet in reading order (top-left first), cut like the game's sheets: PPU 20, pivot bottom-centre.
    public static List<Sprite> Frames(string sheet, int cell, Vector2 pivot)
    {
        string key = sheet + "/" + cell;
        List<Sprite> list;
        if (frames.TryGetValue(key, out list))
            return list;
        list = new List<Sprite>();
        Texture2D tex = Sheet(sheet);
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            int cols = tex.width / cell, rows = tex.height / cell, index = 0;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    Sprite s = Sprite.Create(tex, new Rect(c * cell, tex.height - (r + 1) * cell, cell, cell), pivot, 20f, 0, SpriteMeshType.FullRect);
                    s.name = sheet + "_" + index++;
                    list.Add(s);
                }
        }
        frames[key] = list;
        return list;
    }

    public static Sprite Single(string sheet, int cell)
    {
        List<Sprite> f = Frames(sheet, cell, new Vector2(0.5f, 0.5f));
        return f.Count > 0 ? f[0] : null;
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

    static Spriteobject NewSpriteobject(string name, Spriteobject template, float speedmulti, string folder)
    {
        Spriteobject so = ScriptableObject.CreateInstance<Spriteobject>();
        so.name = name;
        so.Defaultspeedmulti = template != null ? template.Defaultspeedmulti : speedmulti;
        so.Defaultfolder = template != null && !string.IsNullOrEmpty(template.Defaultfolder) ? template.Defaultfolder : folder;
        so.Itemslist = new List<Spritesinlist>();
        so.Items = new Dictionary<string, Spritesinlist>();
        return so;
    }

    static void Add(Spriteobject so, Spritesinlist a)
    {
        so.Itemslist.Add(a);
        so.Items[a.Name] = a;
    }

    // Argo's sheets follow the shipped vessels': Idle and Move rows v0..v3 on one 192x256 sheet (front facing right,
    // front facing left, back, back mirrored), 12-frame Falling and Land strips, and 4-variant Attack and Sonar sheets.
    public static Spriteobject Hero(Spriteobject template, string footstep)
    {
        Vector2 foot = new Vector2(0.5f, 0f);
        List<Sprite> main = Frames("Argo-Sheet", 32, foot);
        List<Sprite> fall = Frames("ArgoFall-Sheet", 32, foot);
        List<Sprite> land = Frames("ArgoIntroR-Sheet", 32, foot);
        List<Sprite> attack = Frames("ArgoAttack-Sheet", 32, foot);
        List<Sprite> sonar = Frames("ArgoSonar-Sheet", 32, foot);
        Spriteobject so = NewSpriteobject("Hero_Argo_Sprite", template, 8f, "Falling");
        // Lead boots: a step on each boot's slam (Move frames 2 and 5).
        Dictionary<int, string> steps = string.IsNullOrEmpty(footstep) ? null : new Dictionary<int, string> { { 2, footstep }, { 5, footstep } };
        Add(so, Anim("Idle", main, 6, 0, 4, 6, true, false, 0, 1f, ""));
        Add(so, Anim("Move", main, 6, 4, 4, 6, true, false, 0, 1f, "", steps));
        Add(so, Anim("Falling", fall, 12, 0, 1, 12, true, true, 2, 1f, ""));
        Add(so, Anim("Land", land, 12, 0, 1, 12, false, true, 2, 1f, "Idle"));
        Add(so, Anim("Landquick", main, 6, 0, 4, 1, false, true, 2, 10f, "Idle"));
        Add(so, Anim("Attack", attack, 4, 0, 4, 4, false, true, 1, 2f, "Idle"));
        Add(so, Anim("Sonar", sonar, 6, 0, 4, 6, false, true, 1, 1.5f, "Idle"));
        so.Preview = main.Count > 0 ? main[0] : null;
        return so;
    }

    // A Naut: Idle is the first Move frame, like the Mining Hut's miners. The "Tune" skin swaps Idle for the wrench taps.
    public static Spriteobject Naut(string crew, Spriteobject template, bool tune)
    {
        Vector2 foot = new Vector2(0.5f, 0f);
        List<Sprite> sheet = Frames("Naut_" + crew + "-Sheet", 16, foot);
        Spriteobject so = NewSpriteobject("Mob_Argonaut_" + crew + (tune ? "_Tune" : "") + "_Sprite", template, 4f, "Idle");
        if (tune)
            Add(so, Anim("Idle", Frames("NautTune_" + crew + "-Sheet", 16, foot), 6, 0, 4, 6, true, false, 0, 1f, ""));
        else
            Add(so, Anim("Idle", sheet, 6, 0, 4, 1, true, false, 0, 1f, ""));
        Add(so, Anim("Move", sheet, 6, 0, 4, 6, true, false, 0, 1f, ""));
        so.Preview = sheet.Count > 0 ? sheet[0] : null;
        return so;
    }

    // The boat's character select feeds Worldsprite to a VFX graph. Match the template's layout.
    public static Texture2DArray Worldsprite(Texture2DArray template)
    {
        int w = template != null ? template.width : 16;
        int h = template != null ? template.height : 16;
        int depth = template != null ? template.depth : 48;
        Texture2D mini = Sheet("SArgoSheet");
        Texture2DArray array = new Texture2DArray(w, h, depth, TextureFormat.RGBA32, false);
        array.filterMode = FilterMode.Point;
        array.wrapMode = TextureWrapMode.Clamp;
        for (int i = 0; i < depth; i++)
        {
            Color[] pixels = null;
            if (mini != null && w == mini.width && h == mini.height) pixels = mini.GetPixels();
            else if (mini != null && w == 16 && h == 16) pixels = Cell(mini, 16, i);
            else if (mini != null) pixels = Resample(mini, w, h);
            if (pixels != null)
                array.SetPixels(pixels, i);
        }
        array.Apply(false);
        array.name = "Argo_Worldsprite";
        return array;
    }

    static Color[] Cell(Texture2D sheet, int cell, int index)
    {
        int cols = sheet.width / cell, rows = sheet.height / cell;
        index %= cols * rows;
        int r = index / cols, c = index % cols;
        return sheet.GetPixels(c * cell, sheet.height - (r + 1) * cell, cell, cell);
    }

    static Color[] Resample(Texture2D src, int w, int h)
    {
        Color[] o = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                o[y * w + x] = src.GetPixel(x * src.width / w, y * src.height / h);
        return o;
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

    public static string[] IconFor(Color crew)
    {
        return CrewIcon(crew.r > 0.8f && crew.g > 0.6f ? 'y' : crew.g > crew.r ? 's' : 'r');
    }

    public static Sprite Pixel(string[] rows, string name)
    {
        int h = rows.Length, w = rows[0].Length;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c;
                px[(h - 1 - y) * w + x] = x < rows[y].Length && IconPalette.TryGetValue(rows[y][x], out c) ? c : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply(false);
        tex.name = name;
        Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 20f, 0, SpriteMeshType.FullRect);
        s.name = name;
        return s;
    }

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
        Transform models = Library.Find("Models");
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

    // ---------------------------------------------------------------- client playback

    // The mixer group the game's own world sounds use (the "Upgrade" sound's group, General_Main), so Argo's sounds
    // follow the same volume settings and effects as every other tower and vessel.
    static AudioMixerGroup Mixer()
    {
        if (mixerSearched)
            return mixer;
        mixerSearched = true;
        Blockage.Sound.Bla_Audio audio = Blockage.Sound.Bla_Audio.Instance;
        if (audio == null)
            return null;
        if (audio.Database != null)
            foreach (Blockage.Sound.Bl_Sound_Group g in audio.Database.Items)
                if (g != null && g.Sounds != null)
                    foreach (Blockage.Sound.Soundpair p in g.Sounds)
                        if (p != null && p.key == ArgoData.ReferenceSound && p.group != null)
                            return mixer = p.group;
        if (audio.Mixer == null)
            return null;
        AudioMixerGroup[] groups = audio.Mixer.FindMatchingGroups("General_Main");
        return mixer = groups.Length > 0 ? groups[0] : null;
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
        AudioClip clip = Clip(clipName);
        if (clip == null)
            return;
        GameObject go = new GameObject("Argo_Sfx_" + clipName);
        go.transform.position = position;
        AudioSource s = go.AddComponent<AudioSource>();
        s.clip = clip;
        s.volume = Volume(clipName, clip) * scale;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        Blockage.Sound.Bla_Audio audio = Blockage.Sound.Bla_Audio.Instance;
        s.minDistance = audio != null && audio.Mindist > 0 ? audio.Mindist : 4f;
        s.maxDistance = audio != null && audio.Maxdist > 0 ? audio.Maxdist : 40f;
        s.outputAudioMixerGroup = Mixer();
        s.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
        s.Play();
        UnityEngine.Object.Destroy(go, clip.length + 0.2f);
    }

    public static GameObject Effect(string vfxName, Vector3 position, float lifetime = 3f)
    {
        GameObject template = Vfx(vfxName);
        if (template == null)
            return null;
        GameObject go = UnityEngine.Object.Instantiate(template, position, Quaternion.identity);
        go.SetActive(true);
        if (lifetime > 0)
            UnityEngine.Object.Destroy(go, lifetime);
        return go;
    }
}
