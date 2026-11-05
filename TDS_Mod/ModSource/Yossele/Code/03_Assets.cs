// Yossele's own art on top of the kit's ModKitAssets, which reads YosseleAssets_Library (shipped inside the mod prefab),
// slices sheets and plays sounds and effects. Here: its hero's and units' Spriteobjects, its icons, its tower material
// and its sound levels.
public static class YosseleAssets
{
    public static Spriteobject Hero(Spriteobject template, string footstep)
    {
        Vector2 foot = new Vector2(0.5f, 0f);
        List<Sprite> main = ModKitAssets.Frames("Yossele-Sheet", 32, foot);
        List<Sprite> fall = ModKitAssets.Frames("YosseleFall-Sheet", 32, foot);
        List<Sprite> land = ModKitAssets.Frames("YosseleIntroR-Sheet", 32, foot);
        List<Sprite> attack = ModKitAssets.Frames("YosseleAttack-Sheet", 32, foot);
        List<Sprite> fertilize = ModKitAssets.Frames("YosseleFertilize-Sheet", 32, foot);
        Spriteobject so = ModKitAssets.NewSpriteobject("Hero_Yossele_Sprite", template, 8f, "Falling");
        // A footfall where a pair of root legs lands: frames 0 and 3 (YosseleCharacter.Move).
        Dictionary<int, string> steps = new Dictionary<int, string> { { 0, footstep }, { 3, footstep } };
        ModKitAssets.Add(so, ModKitAssets.Anim("Idle", main, 6, 0, 0, 4, 6, true, false, 0, 1f, ""));
        ModKitAssets.Add(so, ModKitAssets.Anim("Move", main, 6, 4, 0, 4, 6, true, false, 0, 1f, "", string.IsNullOrEmpty(footstep) ? null : steps));
        ModKitAssets.Add(so, ModKitAssets.Anim("Falling", fall, 12, 0, 0, 1, 12, true, true, 2, 1f, ""));
        ModKitAssets.Add(so, ModKitAssets.Anim("Land", land, 12, 0, 0, 1, 12, false, true, 2, 1f, "Idle"));
        ModKitAssets.Add(so, ModKitAssets.Anim("Landquick", main, 6, 0, 0, 4, 1, false, true, 2, 10f, "Idle"));
        ModKitAssets.Add(so, ModKitAssets.Anim("Attack", attack, 4, 0, 0, 4, 4, false, true, 1, 3f, "Idle"));
        ModKitAssets.Add(so, ModKitAssets.Anim("Fertilize", fertilize, 6, 0, 0, 4, 6, false, true, 1, 1.6f, "Idle"));
        so.Preview = main.Count > 0 ? main[0] : null;
        return so;
    }

    // The Cordyfriend's own sheet (temporary art, ModSource/Yossele/Art/Temporary): 24 px cells, rows v0..v3, columns 0-5 Idle,
    // 6-11 Move, 12-15 Attack. Null when the sheet isn't in the library, so the caller keeps the Snoble's look.
    public static Spriteobject Friend(Spriteobject template)
    {
        List<Sprite> s = ModKitAssets.Frames(YosseleData.FriendSheet, 24, new Vector2(0.5f, 0f));
        if (s.Count < 64)
            return null;
        Spriteobject so = ModKitAssets.NewSpriteobject(YosseleData.Friend + "_Sprite", template, 7f, "Idle");
        ModKitAssets.Add(so, ModKitAssets.Anim("Idle", s, 16, 0, 0, 4, 6, true, false, 0, 1f, ""));
        ModKitAssets.Add(so, ModKitAssets.Anim("Move", s, 16, 0, 6, 4, 6, true, false, 0, 1f, ""));
        ModKitAssets.Add(so, ModKitAssets.Anim("Attack", s, 16, 0, 12, 4, 4, false, true, 1, 2f, "Idle"));
        so.Preview = s[0];
        return so;
    }

    // ---------------------------------------------------------------- icons (12x12 pixel art, drawn here)

    static readonly Dictionary<char, Color32> IconPalette = new Dictionary<char, Color32>
    {
        { 'c', new Color32(0x94, 0x57, 0x43, 255) }, { 'C', new Color32(0x73, 0x3E, 0x35, 255) }, { 'g', new Color32(0x72, 0xCC, 0x35, 255) },
        { 'G', new Color32(0x4B, 0x8F, 0x22, 255) }, { 'e', new Color32(0x99, 0xE5, 0x50, 255) }, { 'w', new Color32(245, 240, 228, 255) },
        { 's', new Color32(176, 182, 196, 255) }, { 'b', new Color32(0x5A, 0x3A, 0x22, 255) }, { 'r', new Color32(222, 82, 72, 255) },
        { 'u', new Color32(80, 140, 230, 255) }, { 'y', new Color32(240, 200, 60, 255) }, { 'd', new Color32(0x32, 0x3C, 0x39, 255) },
    };

    // A sprout pushing out of a heap of compost.
    public static readonly string[] FertilizeIcon =
    {
        "............", "....gg.gg...", "...gGg.gGg..", "....gg.gg...", "......G.....", "......G.....",
        "......G.....", "....bbbbb...", "...bbcbbbb..", "..bcbbbbcbb.", ".bbbbbcbbbbb", "............"
    };
    // A sack of fertilizer.
    public static readonly string[] FertilizerIcon =
    {
        "............", ".....cc.....", "....c..c....", "....cccc....", "...CccccC...", "..CccgccccC.",
        "..CcgGgcccC.", "..CccGcccccC", "..CcccccccC.", "...CCCCCCC..", "............", "............"
    };
    // Yossele's two-leaf sprout.
    public static readonly string[] GrowthIcon =
    {
        "............", "..ggg..ggg..", ".gGGgg.gGGg.", ".gGgggggGgg.", "..ggg.G.ggg.", "......G.....",
        "......G.....", ".....GG.....", "......G.....", "....cccc....", "...cCCCCc...", "............"
    };
    // A dandelion seed on its parachute.
    public static readonly string[] SeedIcon =
    {
        "............", "..w.w.w.w...", "...wwwww....", "....www.....", ".....w......", ".....s......",
        ".....s......", ".....s......", ".....b......", ".....b......", "............", "............"
    };
    // A round aura fruit with a leaf.
    public static readonly string[] ColourIcon =
    {
        "............", "......Gg....", ".....Ggg....", "....ryyu....", "...rryyuu...", "..rrryyuuu..",
        "..rrryyuuu..", "..rrryyuuu..", "...rryyuu...", "....ryyu....", "............", "............"
    };

    // box: ModKitAssets.CommandIcon for an icon the HUD's command buttons draw (pitfall #156).
    public static Sprite Pixel(string[] rows, string name, int box = 0) { return ModKitAssets.Pixel(rows, name, IconPalette, box); }

    // ---------------------------------------------------------------- tower material

    // Every library model and effect mesh goes onto a copy of the game's own tower material, so Yossele's towers are
    // lit like the rest.
    public static void ApplyTowerMaterial(Material gameTower)
    {
        if (gameTower == null)
        {
            YosseleLog.Warn("no game tower material found; Yossele towers keep the preview material");
            return;
        }
        Texture palette = null;
        List<Renderer> all = new List<Renderer>();
        foreach (string holder in new[] { "Models", "VFX" })
        {
            Transform h = ModKitAssets.Library.Find(holder);
            if (h != null) all.AddRange(h.GetComponentsInChildren<MeshRenderer>(true));
        }
        // Flat ground tiles (the vines, Yossele_Vine_Ground) keep their own cut-out material and texture: the tower
        // material carries one texture, the palette (round 6 drew the vines with it).
        all.RemoveAll(r => r.sharedMaterial != null && r.sharedMaterial.name.EndsWith("_Ground"));
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
        mat.name = "Yossele_Tower";
        mat.SetTexture("_MainTex", palette);
        foreach (Renderer r in all)
            r.sharedMaterial = mat;
    }

    // Volumes come from YosseleData.SoundVolume (matched to the game's own levels); 'scale' shades one use quieter.
    public static void Play(string clipName, Vector3 position, float scale = 1f)
    {
        float volume;
        if (!YosseleData.SoundVolume.TryGetValue(clipName, out volume))
            volume = 0.02f;
        ModKitAssets.Play(clipName, position, volume * scale);
    }
}
