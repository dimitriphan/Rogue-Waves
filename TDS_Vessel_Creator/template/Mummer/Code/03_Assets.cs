// Reads the asset pack (MummerAssets_Library, shipped inside the mod prefab) and turns it into game objects: sliced
// sprites, Spriteobjects, menu art, the tower material, sounds and effects.
using UnityEngine.Audio;

public static class MummerAssets
{
    public static Transform Library;
    static readonly Dictionary<string, List<Sprite>> frames = new Dictionary<string, List<Sprite>>();
    static AudioMixerGroup mixer;
    static bool mixerSearched;

    public static bool Find()
    {
        if (Library != null)
            return true;
        if (MummerMod.Instance != null)
            Library = MummerMod.Instance.transform.Find("MummerAssets_Library");
        if (Library == null)
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go.name == "MummerAssets_Library" && go.scene.IsValid())
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
            MummerLog.Warn("missing sheet " + name);
            return null;
        }
        return r.sprite.texture;
    }

    // Models and effects are prefab instances under inactive groups: instantiating them yields active copies.
    public static GameObject Model(string name)
    {
        Transform t = Library.Find("Models/" + name);
        if (t == null)
            MummerLog.Warn("missing model " + name);
        return t != null ? t.gameObject : null;
    }

    public static GameObject Vfx(string name)
    {
        Transform t = Library.Find("VFX/" + name);
        return t != null ? t.gameObject : null;
    }

    public static List<GameObject> AllVfx()
    {
        List<GameObject> list = new List<GameObject>();
        Transform v = Library.Find("VFX");
        if (v != null)
            foreach (Transform t in v)
                list.Add(t.gameObject);
        return list;
    }

    public static AudioClip Clip(string name)
    {
        Transform t = Library.Find("Audio/" + name);
        AudioSource s = t != null ? t.GetComponent<AudioSource>() : null;
        return s != null ? s.clip : null;
    }

    // ---------------------------------------------------------------- sprites

    // Frames of a sheet in reading order (top-left first), cut like the game's sheets: PPU 20.
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

    static Spritesinlist Anim(string name, List<Sprite> sheet, int cols, int firstRow, int firstCol, int variants, int count, bool loop, bool reset, int priority, float speed, string to, Dictionary<int, string> sounds = null)
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
                int index = (firstRow + v) * cols + firstCol + f;
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

    public static Spriteobject Hero(Spriteobject template, string footstep)
    {
        Vector2 foot = new Vector2(0.5f, 0f);
        List<Sprite> main = Frames("Mummer-Sheet", 32, foot);
        List<Sprite> fall = Frames("MummerFall-Sheet", 32, foot);
        List<Sprite> land = Frames("MummerIntroR-Sheet", 32, foot);
        List<Sprite> attack = Frames("MummerAttack-Sheet", 32, foot);
        List<Sprite> hook = Frames("MummerHook-Sheet", 32, foot);
        Spriteobject so = NewSpriteobject("Hero_Mummer_Sprite", template, 8f, "Falling");
        // Mummer hops like Sno Sno: one soft step on the landing frame of the hop.
        Dictionary<int, string> steps = new Dictionary<int, string> { { 2, footstep } };
        Add(so, Anim("Idle", main, 6, 0, 0, 4, 6, true, false, 0, 1f, ""));
        Add(so, Anim("Move", main, 6, 4, 0, 4, 6, true, false, 0, 1f, "", string.IsNullOrEmpty(footstep) ? null : steps));
        Add(so, Anim("Falling", fall, 12, 0, 0, 1, 12, true, true, 2, 1f, ""));
        Add(so, Anim("Land", land, 12, 0, 0, 1, 12, false, true, 2, 1f, "Idle"));
        Add(so, Anim("Landquick", main, 6, 0, 0, 4, 1, false, true, 2, 10f, "Idle"));
        Add(so, Anim("Attack", attack, 4, 0, 0, 4, 4, false, true, 1, 3f, "Idle"));
        Add(so, Anim("Hook", hook, 6, 0, 0, 4, 6, false, true, 1, 1.6f, "Idle"));
        so.Preview = main.Count > 0 ? main[0] : null;
        return so;
    }

    // Understudy sheets: 16px cells, rows v0..v3; columns 0-5 Move (Idle = column 0), 6-9 Attack.
    public static Spriteobject Puppet(string sheet, string name, Spriteobject template)
    {
        List<Sprite> s = Frames(sheet, 16, new Vector2(0.5f, 0f));
        Spriteobject so = NewSpriteobject(name, template, 4f, "Idle");
        Add(so, Anim("Idle", s, 10, 0, 0, 4, 1, true, false, 0, 1f, ""));
        Add(so, Anim("Move", s, 10, 0, 0, 4, 6, true, false, 0, 1f, ""));
        Add(so, Anim("Attack", s, 10, 0, 6, 4, 4, false, true, 1, 3f, "Idle"));
        so.Preview = s.Count > 0 ? s[0] : null;
        return so;
    }

    // The boat's character select feeds Worldsprite to a VFX graph. Match the template's layout.
    public static Texture2DArray Worldsprite(Texture2DArray template)
    {
        int w = template != null ? template.width : 16;
        int h = template != null ? template.height : 16;
        int depth = template != null ? template.depth : 48;
        Texture2D mini = Sheet("SMummerSheet");
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
        array.name = "Mummer_Worldsprite";
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
        { 'l', new Color32(213, 190, 236, 255) }, { 'L', new Color32(178, 146, 214, 255) }, { 'm', new Color32(130, 96, 170, 255) },
        { 'b', new Color32(232, 196, 108, 255) }, { 'B', new Color32(196, 156, 70, 255) }, { 'w', new Color32(245, 240, 250, 255) },
        { 'd', new Color32(58, 40, 64, 255) }, { 'r', new Color32(228, 104, 128, 255) }, { 's', new Color32(176, 182, 196, 255) },
    };

    // A spool of thread.
    public static readonly string[] ThreadIcon =
    {
        "............", "..BBBBBBBB..", "...bbbbbb...", "...LlLlLl...", "...lLlLlL...", "...LlLlLl...",
        "...lLlLlL...", "...LlLlLl.l.", "...bbbbbb.l.", "..BBBBBBBB.l", "...........l", "............"
    };
    // The hook barb on its thread.
    public static readonly string[] HookIcon =
    {
        "........l...", ".......l....", "......l.....", ".....l......", "....l.......", "...ls.......",
        "...s........", "..ss....s...", "..s.....ss..", "..ss...ss...", "...sssss....", "............"
    };
    // A coat hanger with a little costume.
    public static readonly string[] CostumeIcon =
    {
        ".....ss.....", "......s.....", ".....s......", "...sssssss..", "..s.......s.", ".sLLLLLLLLs.",
        "...LlllllL..", "...LlrllrL..", "...LlllllL..", "..LLlllllLL.", "..L.L.L.L.L.", "............"
    };

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

    // Every library model and effect mesh goes onto a copy of the game's own tower material, so Mummer's towers are
    // lit like the rest.
    public static void ApplyTowerMaterial(Material gameTower)
    {
        if (gameTower == null)
        {
            MummerLog.Warn("no game tower material found; Mummer towers keep the preview material");
            return;
        }
        Texture palette = null;
        List<Renderer> all = new List<Renderer>();
        foreach (string holder in new[] { "Models", "VFX" })
        {
            Transform h = Library.Find(holder);
            if (h != null) all.AddRange(h.GetComponentsInChildren<MeshRenderer>(true));
        }
        foreach (Renderer r in all)
        {
            Material m = r.sharedMaterial;
            if (palette == null && m != null)
                palette = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.mainTexture;
        }
        if (palette == null)
            return;
        palette.filterMode = FilterMode.Point;
        Material mat = new Material(gameTower);
        mat.name = "Mummer_Tower";
        mat.SetTexture("_MainTex", palette);
        foreach (Renderer r in all)
            r.sharedMaterial = mat;
    }

    // ---------------------------------------------------------------- client playback

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
                        if (p != null && p.key == MummerData.ReferenceSound && p.group != null)
                            return mixer = p.group;
        if (audio.Mixer == null)
            return null;
        AudioMixerGroup[] groups = audio.Mixer.FindMatchingGroups("General_Main");
        return mixer = groups.Length > 0 ? groups[0] : null;
    }

    // Volumes come from MummerData.SoundVolume (matched to the game's own levels); 'scale' shades one use quieter.
    public static void Play(string clipName, Vector3 position, float scale = 1f)
    {
        AudioClip clip = Clip(clipName);
        if (clip == null)
            return;
        float volume;
        if (!MummerData.SoundVolume.TryGetValue(clipName, out volume))
            volume = 0.02f;
        GameObject go = new GameObject("Mummer_Sfx_" + clipName);
        go.transform.position = position;
        AudioSource s = go.AddComponent<AudioSource>();
        s.clip = clip;
        s.volume = volume * scale;
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
