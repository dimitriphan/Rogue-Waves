#if ROGUEWAVESMODE_SELFTEST
// P1-P2 (RWPixels, pitfall #174): the main menu's button pictures on whole screen pixels, and the flicker they show on the half
// pixels the game's own layout gives them while the menu's camera sways. Read from the frame the game draws: each picture's
// square, frame after frame, first where RWPixels puts the pictures, then where the game does, then RWPixels again.
public static partial class RWTest
{
    static readonly string[] PixelTiles = { "Play", "Hero", "Settings", "More", "Exit" };
    const int FlickerFrames = 144;
    const int Shade = 48;    // a channel moving more than this between two frames is a picture's pixel changing, not the sea behind

    static List<RectTransform> MenuPictures(Blockage.Menu.Blu2_Menu_Handler main)
    {
        List<RectTransform> pics = new List<RectTransform>();
        foreach (string n in PixelTiles)
        {
            GameObject tile;
            RectTransform pic = main.Items.TryGetValue(n, out tile) && tile != null ? tile.transform.Find("Image") as RectTransform : null;
            if (pic != null && pic.gameObject.activeInHierarchy) pics.Add(pic);
        }
        RectTransform crest = RWMenu.Button != null ? RWMenu.Button.transform.Find("Image") as RectTransform : null;
        if (crest != null) pics.Add(crest);
        return pics;
    }

    // Screen pixels from the screen's corner to the picture's, and the picture's size in screen pixels.
    static Vector2 ScreenCorner(RectTransform pic, out Vector2 size)
    {
        Canvas c = pic.GetComponentInParent<Canvas>();
        RectTransform root = c != null ? c.rootCanvas.transform as RectTransform : null;
        float pixels = RWAssets.ScreenPixels(c);
        size = pic.rect.size * pixels;
        if (root == null) return Vector2.zero;
        return ((Vector2)root.InverseTransformPoint(pic.TransformPoint(pic.rect.min)) - root.rect.min) * pixels;
    }

    static string Places(List<RectTransform> pics, out int off)
    {
        List<string> said = new List<string>();
        off = 0;
        foreach (RectTransform p in pics)
        {
            Vector2 size, at = ScreenCorner(p, out size);
            bool whole = Mathf.Abs(at.x - Mathf.Round(at.x)) < 0.02f && Mathf.Abs(at.y - Mathf.Round(at.y)) < 0.02f;
            if (!whole) off++;
            said.Add(p.parent.name + " (" + RWUtil.F(at.x) + "," + RWUtil.F(at.y) + ")" + (whole ? "" : " half"));
        }
        return string.Join(", ", said.ToArray());
    }

    // Frames in which any picture's square changed from the frame before, and the most pixels changed in one square.
    static IEnumerator Flicker(List<RectTransform> pics, Action<int, int> result)
    {
        List<Rect> squares = new List<Rect>();
        foreach (RectTransform p in pics)
        {
            Vector2 size, at = ScreenCorner(p, out size);
            float x = Mathf.Max(0f, Mathf.Floor(at.x) - 1f), y = Mathf.Max(0f, Mathf.Floor(at.y) - 1f);
            float w = Mathf.Min(Screen.width - x, Mathf.Ceil(size.x) + 2f), h = Mathf.Min(Screen.height - y, Mathf.Ceil(size.y) + 2f);
            if (w >= 1f && h >= 1f) squares.Add(new Rect(x, y, w, h));
        }
        Texture2D[] tex = new Texture2D[squares.Count];
        Color32[][] last = new Color32[squares.Count][];
        for (int i = 0; i < squares.Count; i++)
            tex[i] = new Texture2D((int)squares[i].width, (int)squares[i].height, TextureFormat.RGB24, false);
        int changed = 0, worst = 0;
        for (int f = 0; f <= FlickerFrames; f++)
        {
            yield return new WaitForEndOfFrame();
            bool any = false;
            for (int i = 0; i < squares.Count; i++)
            {
                tex[i].ReadPixels(squares[i], 0, 0, false);
                Color32[] now = tex[i].GetPixels32();
                if (last[i] != null)
                {
                    int n = 0;
                    for (int k = 0; k < now.Length; k++)
                        if (Mathf.Abs(now[k].r - last[i][k].r) > Shade || Mathf.Abs(now[k].g - last[i][k].g) > Shade || Mathf.Abs(now[k].b - last[i][k].b) > Shade)
                            n++;
                    if (n > 0) any = true;
                    worst = Math.Max(worst, n);
                }
                last[i] = now;
            }
            if (f > 0 && any) changed++;
        }
        foreach (Texture2D t in tex) UnityEngine.Object.Destroy(t);
        result(changed, worst);
    }

    static IEnumerator WholePass()
    {
        int seen = RWPixels.Passes;
        yield return Until(() => RWPixels.Passes >= seen + 2, 10f);
        yield return new WaitForSecondsRealtime(0.5f);
    }

    static IEnumerator Pixels(Blockage.Menu.Blu2_Menu_Handler main)
    {
        List<RectTransform> pics = MenuPictures(main);
        if (pics.Count == 0) { Setup("P1 the main menu's pictures", "none found under " + string.Join(", ", PixelTiles)); yield break; }
        yield return WholePass();
        int off;
        string ours = Places(pics, out off);
        Check("P1 the main menu's button pictures stand on whole screen pixels", off == 0, ours + "; last pass looked at " + RWPixels.Seen + " pictures, moved " + RWPixels.Moved);
        int onFrames = 0, onWorst = 0, stockFrames = 0, stockWorst = 0;
        yield return Flicker(pics, (n, w) => { onFrames = n; onWorst = w; });

        // The game's own places (the Rogue Waves tile's crest stays where RWMenu crisps it). RWPixels comes back on whatever happens.
        int stockOff = 0, againOff = 0;
        string stock = "";
        RWPixels.Restore();
        try
        {
            yield return new WaitForSecondsRealtime(0.5f);
            stock = Places(pics, out stockOff);
            yield return Flicker(pics, (n, w) => { stockFrames = n; stockWorst = w; });
        }
        finally { RWPixels.On = true; }
        yield return WholePass();
        Places(pics, out againOff);

        Info("P2 the game's own places", stock + ": " + stockOff + " of " + pics.Count + " on half pixels");
        string read = onFrames + " of " + FlickerFrames + " frames changed (most pixels in one square " + onWorst + "); where the game puts them " + stockFrames
            + " (most " + stockWorst + "); back on whole pixels after: " + (againOff == 0);
        // The control: the same reading must see the flicker at the game's own places, or a still reading with the fix proves
        // nothing. Only a pass is held back by it; a flicker or a picture left on a half pixel with the fix fails whatever the
        // control saw. At an even UI scale the game's places are whole already and there is nothing to compare.
        const string P2 = "P2 no button picture flickers on whole pixels";
        bool still = onFrames == 0 && againOff == 0;
        if (!still || stockFrames > 0)
            Check(P2, still, read);
        else if (stockOff > 0)
            Setup(P2, "the reading saw no flicker at the game's half pixels either: " + read);
        else
            Info(P2, "nothing to compare: the game's own places are on whole pixels at this UI scale: " + read);
        Line("INFO FINDING P2 the flicker on the game's half pixels: " + (stockFrames > 0 ? "SEEN" : "not seen") + " (" + stockFrames + " of " + FlickerFrames + " frames)");
    }
}
#endif
