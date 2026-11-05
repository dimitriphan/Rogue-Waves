// Kit 1.10 (2026-11-05). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared kit: a vessel's asset pack (<Part>Assets_Library, shipped inside the mod prefab) turned into game objects:
// sliced sprites, Spriteobjects, the boat's Worldsprite, sounds and effects. Every part's build has its own copy of this
// class (ModKit is renamed per part), so Library is that part's library. The part keeps what is its own: its hero's
// sheets, its icons and their palette, its tower material and its sound levels (its Play passes the volume here).
// Before kit 1.8 each vessel carried all of this in <Vessel>Assets; the bodies came unchanged
// (CODE_SIMPLIFICATION_2026-11-04); kit 1.9 changed `Mixer`; kit 1.10 added `CommandIcon`, `Pixel`'s box and `Boxed`.
public static class ModKitAssets
{
    public static Transform Library;
    static string part = "ModKit";
    static readonly Dictionary<string, List<Sprite>> frames = new Dictionary<string, List<Sprite>>();
    static UnityEngine.Audio.AudioMixerGroup mixer;
    static bool mixerSearched;

    // Pack sounds join the mixer group of this game sound (the world SFX).
    const string ReferenceSound = "Upgrade";

    // The part's library: <part>Assets_Library under its mod object, or loose in a scene; kept across scenes.
    public static bool Find(MonoBehaviour mod, string partName)
    {
        part = partName;
        if (Library != null)
            return true;
        string name = partName + "Assets_Library";
        if (mod != null)
            Library = mod.transform.Find(name);
        if (Library == null)
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go.name == name && go.scene.IsValid())
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

    static void Warn(string message) { Debug.LogWarning("[" + part + "] " + message); }

    // ---------------------------------------------------------------- raw items

    public static Texture2D Sheet(string name)
    {
        Transform t = Library.Find("Sprites/" + name);
        SpriteRenderer r = t != null ? t.GetComponent<SpriteRenderer>() : null;
        if (r == null || r.sprite == null)
        {
            Warn("missing sheet " + name);
            return null;
        }
        return r.sprite.texture;
    }

    // Models and effects are prefab instances under inactive groups: instantiating them yields active copies.
    public static GameObject Model(string name)
    {
        Transform t = Library.Find("Models/" + name);
        if (t == null)
            Warn("missing model " + name);
        return t != null ? t.gameObject : null;
    }

    // A model that may not exist (an upgrade's own look), without the warning.
    public static GameObject Quiet(string name)
    {
        Transform t = Library.Find("Models/" + name);
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

    // One animation: 'variants' rows from firstRow, 'count' frames from firstCol; sounds play on the frames they name.
    public static Spritesinlist Anim(string name, List<Sprite> sheet, int cols, int firstRow, int firstCol, int variants, int count, bool loop, bool reset, int priority, float speed, string to, Dictionary<int, string> sounds = null)
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

    public static Spriteobject NewSpriteobject(string name, Spriteobject template, float speedmulti, string folder)
    {
        Spriteobject so = ScriptableObject.CreateInstance<Spriteobject>();
        so.name = name;
        so.Defaultspeedmulti = template != null ? template.Defaultspeedmulti : speedmulti;
        so.Defaultfolder = template != null && !string.IsNullOrEmpty(template.Defaultfolder) ? template.Defaultfolder : folder;
        so.Itemslist = new List<Spritesinlist>();
        so.Items = new Dictionary<string, Spritesinlist>();
        return so;
    }

    public static void Add(Spriteobject so, Spritesinlist a)
    {
        so.Itemslist.Add(a);
        so.Items[a.Name] = a;
    }

    // Unit sheets: square cells (16 px, Spool's Thread Head 24), rows v0..v3; columns 0-5 Move (Idle = column 0), 6-9 Attack.
    public static Spriteobject Unit(string sheet, int cell, string name, Spriteobject template)
    {
        List<Sprite> s = Frames(sheet, cell, new Vector2(0.5f, 0f));
        Spriteobject so = NewSpriteobject(name, template, 4f, "Idle");
        Add(so, Anim("Idle", s, 10, 0, 0, 4, 1, true, false, 0, 1f, ""));
        Add(so, Anim("Move", s, 10, 0, 0, 4, 6, true, false, 0, 1f, ""));
        Add(so, Anim("Attack", s, 10, 0, 6, 4, 4, false, true, 1, 3f, "Idle"));
        so.Preview = s.Count > 0 ? s[0] : null;
        return so;
    }

    // The boat's character select feeds Worldsprite to a VFX graph. Match the template's layout. The art is the
    // library's S<part>Sheet.
    public static Texture2DArray Worldsprite(Texture2DArray template)
    {
        int w = template != null ? template.width : 16;
        int h = template != null ? template.height : 16;
        int depth = template != null ? template.depth : 48;
        Texture2D mini = Sheet("S" + part + "Sheet");
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
        array.name = part + "_Worldsprite";
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

    // The game's command buttons (the HUD's ability row and the choice dialog's cards) draw their icon in a 16-unit box,
    // and the game's own command icons are 16 px, one unit per pixel. A 12 px icon there is stretched by 4/3: its pixels
    // come out unequal and shift as the button moves or scales, which reads as jitter (pitfall #156).
    public const int CommandIcon = 16;

    // A small icon drawn in code: one string per row, one character per pixel, looked up in the part's palette. With a
    // box larger than the drawing (CommandIcon for a command), the drawing sits centred on whole pixels in a clear
    // box x box texture, so the game's slot draws it one unit per pixel.
    public static Sprite Pixel(string[] rows, string name, Dictionary<char, Color32> palette, int box = 0)
    {
        int h = rows.Length, w = rows[0].Length;
        int tw = Mathf.Max(w, box), th = Mathf.Max(h, box), ox = (tw - w) / 2, oy = (th - h) / 2;
        Color32[] px = new Color32[tw * th];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c;
                px[(oy + h - 1 - y) * tw + ox + x] = x < rows[y].Length && palette.TryGetValue(rows[y][x], out c) ? c : new Color32(0, 0, 0, 0);
            }
        return Icon(px, tw, th, name);
    }

    // An existing sprite (a library PNG) centred on whole pixels in a clear box of at least box x box, for the same
    // slots. Needs a readable texture (the parts' art importer sets isReadable); returns the sprite itself when it
    // already fills the box or can't be read.
    public static Sprite Boxed(Sprite sprite, int box, string name)
    {
        if (sprite == null)
            return null;
        Rect r = sprite.textureRect;
        int w = (int)r.width, h = (int)r.height;
        if (w >= box && h >= box)
            return sprite;
        Color32[] src;
        try { src = sprite.texture.GetPixels32(); }
        catch (Exception) { return sprite; }
        int sw = sprite.texture.width, tw = Mathf.Max(w, box), th = Mathf.Max(h, box), ox = (tw - w) / 2, oy = (th - h) / 2;
        Color32[] px = new Color32[tw * th];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[(oy + y) * tw + ox + x] = src[((int)r.y + y) * sw + (int)r.x + x];
        return Icon(px, tw, th, name);
    }

    // Pixels, bottom row first, as a point-sampled sprite at the game's 20 pixels a unit, centred.
    static Sprite Icon(Color32[] px, int w, int h, string name)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(px);
        tex.Apply(false);
        tex.name = name;
        Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 20f, 0, SpriteMeshType.FullRect);
        s.name = name;
        return s;
    }

    // ---------------------------------------------------------------- client playback

    static UnityEngine.Audio.AudioMixerGroup Mixer()
    {
        if (mixerSearched)
            return mixer;
        Blockage.Sound.Bla_Audio audio = Blockage.Sound.Bla_Audio.Instance;
        if (audio == null)
            return null;          // asked again next time: a sound played before the game's audio is up doesn't end the search (kit 1.9, ARG-5)
        mixerSearched = true;
        if (audio.Database != null)
            foreach (Blockage.Sound.Bl_Sound_Group g in audio.Database.Items)
                if (g != null && g.Sounds != null)
                    foreach (Blockage.Sound.Soundpair p in g.Sounds)
                        if (p != null && p.key == ReferenceSound && p.group != null)
                            return mixer = p.group;
        if (audio.Mixer == null)
            return null;
        UnityEngine.Audio.AudioMixerGroup[] groups = audio.Mixer.FindMatchingGroups("General_Main");
        return mixer = groups.Length > 0 ? groups[0] : null;
    }

    // A library sound at a point, at the volume the part chose for it (its own Play looks the level up).
    public static void Play(string clipName, Vector3 position, float volume)
    {
        AudioClip clip = Clip(clipName);
        if (clip == null)
            return;
        GameObject go = new GameObject(part + "_Sfx_" + clipName);
        go.transform.position = position;
        AudioSource s = go.AddComponent<AudioSource>();
        s.clip = clip;
        s.volume = volume;
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
