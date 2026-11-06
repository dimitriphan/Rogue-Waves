// Spool's own art on top of the kit's ModKitAssets, which reads SpoolAssets_Library (shipped inside the mod prefab),
// slices sheets and plays sounds and effects. Here: its hero's and units' Spriteobjects, its icons, its tower material
// and its sound levels.
public static class SpoolAssets
{
    public static Spriteobject Hero(Spriteobject template, string footstep)
    {
        Vector2 foot = new Vector2(0.5f, 0f);
        List<Sprite> main = ModKitAssets.Frames("Spool-Sheet", 32, foot);
        List<Sprite> fall = ModKitAssets.Frames("SpoolFall-Sheet", 32, foot);
        List<Sprite> land = ModKitAssets.Frames("SpoolIntroR-Sheet", 32, foot);
        List<Sprite> attack = ModKitAssets.Frames("SpoolAttack-Sheet", 32, foot);
        List<Sprite> patch = ModKitAssets.Frames("SpoolPatch-Sheet", 32, foot);
        Spriteobject so = ModKitAssets.NewSpriteobject("Hero_Spool_Sprite", template, 8f, "Falling");
        // A soft step as each foot lands (the walk's frames 1 and 4).
        Dictionary<int, string> steps = new Dictionary<int, string> { { 1, footstep }, { 4, footstep } };
        ModKitAssets.Add(so, ModKitAssets.Anim("Idle", main, 6, 0, 0, 4, 6, true, false, 0, 1f, ""));
        ModKitAssets.Add(so, ModKitAssets.Anim("Move", main, 6, 4, 0, 4, 6, true, false, 0, 1f, "", string.IsNullOrEmpty(footstep) ? null : steps));
        ModKitAssets.Add(so, ModKitAssets.Anim("Falling", fall, 12, 0, 0, 1, 12, true, true, 2, 1f, ""));
        ModKitAssets.Add(so, ModKitAssets.Anim("Land", land, 12, 0, 0, 1, 12, false, true, 2, 1f, "Idle"));
        ModKitAssets.Add(so, ModKitAssets.Anim("Landquick", main, 6, 0, 0, 4, 1, false, true, 2, 10f, "Idle"));
        ModKitAssets.Add(so, ModKitAssets.Anim("Attack", attack, 4, 0, 0, 4, 4, false, true, 1, 3f, "Idle"));
        if (patch.Count >= 24)
            ModKitAssets.Add(so, ModKitAssets.Anim("Patch", patch, 6, 0, 0, 4, 6, false, true, 1, 1.6f, "Idle"));
        so.Preview = main.Count > 0 ? main[0] : null;
        return so;
    }

    // ---------------------------------------------------------------- icons (12x12 pixel art, drawn here)

    // Spool's thread and plush tones are sage since 2026-11-05 (Dimitri's pick; concept-tools/temp_art/spool_sage.py MAP).
    static readonly Dictionary<char, Color32> IconPalette = new Dictionary<char, Color32>
    {
        { 'l', new Color32(203, 230, 209, 255) }, { 'L', new Color32(162, 207, 173, 255) }, { 'm', new Color32(96, 170, 114, 255) },
        { 'b', new Color32(232, 196, 108, 255) }, { 'B', new Color32(196, 156, 70, 255) }, { 'w', new Color32(245, 240, 250, 255) },
        { 'd', new Color32(40, 64, 46, 255) }, { 'r', new Color32(228, 104, 128, 255) }, { 's', new Color32(176, 182, 196, 255) },
        { 'j', new Color32(154, 201, 165, 255) }, { 'x', new Color32(111, 174, 126, 255) }, { 'X', new Color32(79, 140, 94, 255) },
        { 'R', new Color32(214, 84, 72, 255) }, { 'u', new Color32(88, 140, 228, 255) },
    };

    // A spool of thread.
    public static readonly string[] ThreadIcon =
    {
        "............", "..BBBBBBBB..", "...bbbbbb...", "...LlLlLl...", "...lLlLlL...", "...LlLlLl...",
        "...lLlLlL...", "...LlLlLl.l.", "...bbbbbb.l.", "..BBBBBBBB.l", "...........l", "............"
    };
    // A needle and its thread.
    public static readonly string[] PatchIcon =
    {
        "..........s.", ".........ss.", "........ss..", ".......ss...", "......ss....", ".....ss.....",
        "....ss..ll..", "...ss..l..l.", "..ss...l..l.", ".ss.....ll..", ".s..........", "............"
    };
    // A pin, where a Cookie Tin sends its Crafted.
    public static readonly string[] PinIcon =
    {
        "....rrrr....", "...rrrrrr...", "...rrwrrr...", "...rrrrrr...", "....rrrr....", ".....ss.....",
        ".....ss.....", ".....ss.....", ".....ss.....", "......s.....", "......s.....", "............"
    };
    // A ring around a spot.
    public static readonly string[] SpotIcon =
    {
        "............", "....LLLL....", "...L....L...", "..L..ll..L..", "..L.llll.L..", "..L.llll.L..",
        "..L..ll..L..", "...L....L...", "....LLLL....", "............", "............", "............"
    };
    // An arrow back to the basket.
    public static readonly string[] ReturnIcon =
    {
        "............", "...l........", "..ll........", ".llllllll...", "..ll.....l..", "...l.....l..",
        ".........l..", "..BBBBBBBB..", "..BbbbbbbB..", "..BbBbBbBB..", "...BBBBBB...", "............"
    };
    // A stitched cross.
    public static readonly string[] MendIcon =
    {
        "............", "....wwww....", "....wrrw....", "....wrrw....", ".wwwwrrwwww.", ".wrrrrrrrrw.",
        ".wrrrrrrrrw.", ".wwwwrrwwww.", "....wrrw....", "....wrrw....", "....wwww....", "............"
    };
    // Quilting Bee's BEE: a striped bee with its wings up.
    public static readonly string[] BeeIcon =
    {
        "............", "....ww.ww...", "...wwwwwww..", "....wwwww...", "..bbbbbbbb..", ".bddbbddbbb.",
        ".bddbbddbbBs", ".bddbbddbbb.", "..bbbbbbbb..", "............", "............", "............"
    };
    // UNTHREAD: a pair of scissors.
    public static readonly string[] UnthreadIcon =
    {
        "s..........s", ".s........s.", "..s......s..", "...s....s...", "....s..s....", ".....ss.....",
        ".....ss.....", "....s..s....", "..rrr..rrr..", ".r..r..r..r.", ".r..r..r..r.", "..rr....rr.."
    };
    // Dyed in the Wool's COLOUR: three drops of dye over a skein (the button tints it the picked colour).
    public static readonly string[] ColourIcon =
    {
        "............", "..R...u...b.", ".RRR.uuu.bbb", ".RRR.uuu.bbb", "..R...u...b.", "............",
        "..ssssssss..", ".sLLLLLLLLs.", ".sLlLlLlLLs.", ".sLLLLLLLLs.", "..ssssssss..", "............"
    };
    // The yarn basket.
    public static readonly string[] StashIcon =
    {
        "............", "...llLLll...", "..lLllLlLl..", "..LlLllLlL..", ".BBBBBBBBBB.", ".BbBbBbBbBB.",
        ".BBbBbBbBbB.", ".BbBbBbBbBB.", "..BBBBBBBB..", "............", "............", "............"
    };
    // A ball of yarn.
    public static readonly string[] CarriedIcon =
    {
        "............", "....LLLL....", "...LlLlLL...", "..LlLLlLlL..", "..LLlLLlLL..", "..LlLLlLlL..",
        "..LLlLLlLL..", "...LlLlLL...", "....LLLL..l.", "...........l", "............", "............"
    };

    // box: ModKitAssets.CommandIcon for an icon the HUD's command buttons draw (pitfall #156).
    public static Sprite Pixel(string[] rows, string name, int box = 0) { return ModKitAssets.Pixel(rows, name, IconPalette, box); }

    // ---------------------------------------------------------------- tower material

    // Every library model and effect mesh goes onto a copy of the game's own tower material, so Spool's towers are
    // lit like the rest.
    public static void ApplyTowerMaterial(Material gameTower)
    {
        if (gameTower == null)
        {
            SpoolLog.Warn("no game tower material found; Spool towers keep the preview material");
            return;
        }
        Texture palette = null;
        List<Renderer> all = new List<Renderer>();
        foreach (string holder in new[] { "Models", "VFX" })
        {
            Transform h = ModKitAssets.Library.Find(holder);
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
        mat.name = "Spool_Tower";
        mat.SetTexture("_MainTex", palette);
        foreach (Renderer r in all)
            r.sharedMaterial = mat;
    }

    // Volumes come from SpoolData.SoundVolume (matched to the game's own levels); 'scale' shades one use quieter.
    public static void Play(string clipName, Vector3 position, float scale = 1f)
    {
        float volume;
        if (!SpoolData.SoundVolume.TryGetValue(clipName, out volume))
            volume = 0.02f;
        ModKitAssets.Play(clipName, position, volume * scale);
    }
}
