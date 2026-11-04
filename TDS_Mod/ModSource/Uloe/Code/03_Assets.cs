// Uloe's own art on top of the kit's ModKitAssets, which reads UloeAssets_Library (shipped inside the mod prefab),
// slices sheets and plays sounds and effects. Here: its hero's and units' Spriteobjects, its icons, its tower material
// and its sound levels.
public static class UloeAssets
{
    // The vessel's animations; 'lit' takes Idle and Move from UloeLit-ModKitAssets.Sheet (the bulb burning, the skin while plugged in).
    public static Spriteobject Hero(Spriteobject template, string footstep, bool lit)
    {
        Vector2 foot = new Vector2(0.5f, 0f);
        List<Sprite> main = ModKitAssets.Frames(lit ? "UloeLit-Sheet" : "Uloe-Sheet", 32, foot);
        List<Sprite> fall = ModKitAssets.Frames("UloeFall-Sheet", 32, foot);
        List<Sprite> land = ModKitAssets.Frames("UloeIntroR-Sheet", 32, foot);
        List<Sprite> attack = ModKitAssets.Frames("UloeAttack-Sheet", 32, foot);
        List<Sprite> plug = ModKitAssets.Frames("UloePlug-Sheet", 32, foot);
        Spriteobject so = ModKitAssets.NewSpriteobject(lit ? "Hero_Uloe_Lit_Sprite" : "Hero_Uloe_Sprite", template, 8f, "Falling");
        // The lamp's foot comes down once in each hop of the Move (frame 4).
        Dictionary<int, string> steps = new Dictionary<int, string> { { 4, footstep } };
        ModKitAssets.Add(so, ModKitAssets.Anim("Idle", main, 6, 0, 0, 4, 6, true, false, 0, 1f, ""));
        ModKitAssets.Add(so, ModKitAssets.Anim("Move", main, 6, 4, 0, 4, 6, true, false, 0, 1f, "", string.IsNullOrEmpty(footstep) ? null : steps));
        ModKitAssets.Add(so, ModKitAssets.Anim("Falling", fall, 12, 0, 0, 1, 12, true, true, 2, 1f, ""));
        ModKitAssets.Add(so, ModKitAssets.Anim("Land", land, 12, 0, 0, 1, 12, false, true, 2, 1f, "Idle"));
        ModKitAssets.Add(so, ModKitAssets.Anim("Landquick", main, 6, 0, 0, 4, 1, false, true, 2, 10f, "Idle"));
        ModKitAssets.Add(so, ModKitAssets.Anim("Attack", attack, 4, 0, 0, 4, 4, false, true, 1, 3f, "Idle"));
        if (plug.Count >= 24)
            ModKitAssets.Add(so, ModKitAssets.Anim("Plug", plug, 6, 0, 0, 4, 6, false, true, 1, 1.6f, "Idle"));
        so.Preview = main.Count > 0 ? main[0] : null;
        return so;
    }

    // ---------------------------------------------------------------- icons (12x12 pixel art, drawn here)

    static readonly Dictionary<char, Color32> IconPalette = new Dictionary<char, Color32>
    {
        { 'g', new Color32(218, 230, 252, 255) }, { 'G', new Color32(173, 196, 240, 255) }, { 'y', new Color32(251, 242, 54, 255) },
        { 'Y', new Color32(212, 205, 60, 255) }, { 'w', new Color32(255, 253, 224, 255) }, { 's', new Color32(155, 173, 183, 255) },
        { 'S', new Color32(117, 122, 125, 255) }, { 'd', new Color32(89, 86, 82, 255) }, { 'c', new Color32(46, 45, 45, 255) },
    };

    // A plug with its two prongs, its cord trailing off.
    public static readonly string[] PlugIcon =
    {
        "....s..s....", "....s..s....", "....s..s....", "..ssssssss..", "..sSSSSSSd..", "..sSSSSSSd..",
        "...sSSSSd...", "....sSSd....", ".....cc.....", ".....cc.....", "......ccc...", "........cc.."
    };
    // A spotlight throwing its beam down and to the right.
    public static readonly string[] AimIcon =
    {
        "............", ".sss........", ".sSSs.......", ".sSSSs......", "..sSSgg.....", "...sgggy....",
        "....gyyyy...", ".....yyyyy..", "......yyyyy.", ".......yyYY.", "........yYY.", "............"
    };
    // A light bulb, the Light stat on a panel.
    public static readonly string[] LightIcon =
    {
        "....gggg....", "...gwwwwG...", "..gwwyywwG..", "..gwyyyywG..", "..gwwyywwG..", "...gwyywG...",
        "....gyyG....", "....ssSS....", "....sSSS....", "....ssSS....", ".....SS.....", "............"
    };

    public static Sprite Pixel(string[] rows, string name) { return ModKitAssets.Pixel(rows, name, IconPalette); }

    // ---------------------------------------------------------------- tower material

    // Every library model and effect mesh goes onto a copy of the game's own tower material, so Uloe's towers are
    // lit like the rest.
    public static void ApplyTowerMaterial(Material gameTower)
    {
        if (gameTower == null)
        {
            UloeLog.Warn("no game tower material found; Uloe towers keep the preview material");
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
        mat.name = "Uloe_Tower";
        mat.SetTexture("_MainTex", palette);
        foreach (Renderer r in all)
            r.sharedMaterial = mat;
    }

    // Volumes come from UloeData.SoundVolume (matched to the game's own levels); 'scale' shades one use quieter.
    public static void Play(string clipName, Vector3 position, float scale = 1f)
    {
        float volume;
        if (!UloeData.SoundVolume.TryGetValue(clipName, out volume))
            volume = 0.02f;
        ModKitAssets.Play(clipName, position, volume * scale);
    }
}
