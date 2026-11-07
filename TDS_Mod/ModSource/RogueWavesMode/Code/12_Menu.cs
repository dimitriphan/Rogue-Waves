// The main menu's Rogue Waves button (docs/mode/MAIN_MENU_START.md; RogueLab M1-M5 proved the pieces): a clone of Main/Play
// beside it, with its own click and hover tip. It opens the choice dialog: New campaign, Continue campaign (only with one
// in the save; its card says where the campaign stands) and Back. A pick sets Embarking and presses the stock Play, which
// loads the boat; RWBoat then starts the campaign, and the stock boat is the lobby from there. Embarking and the campaign
// in memory are dropped whenever the main menu appears, so the stock Play starts a normal game, and a campaign stays in
// its save for Continue.
// A save without Tutorial_New_Game gets the stock tutorial prompt from Play (Mainmenu_Main.cs:51): no button until then.
public static class RWMenu
{
    public const string ItemName = "RogueWaves_Start";
    public static string Embarking = "";     // "New" or "Continue": read by RWBoat on the boat
    public static string Status = "";        // the self-test reads it: what the button did last
    public static Blu3_Button Button;
    static Mainmenu_Main seen;
    static float nextLook;
    static bool asking;
    static UnityEngine.UI.Image crest;

    public static bool OnMainMenu { get { return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.StartsWith("Mainmenu"); } }

    // Every machine, every frame; cheap away from the main menu.
    public static void Update()
    {
        if (Time.unscaledTime < nextLook)
            return;
        nextLook = Time.unscaledTime + 0.25f;
        if (!OnMainMenu)
        {
            seen = null;
            return;
        }
        Mainmenu_Main menu = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (menu == null)
            return;
        if (menu != seen)
        {
            seen = menu;
            Button = null;
            asking = false;
            Embarking = "";
            RWCampaign.AtMainMenu();
            RWCardinals.AtMainMenu();
            RWLobby.AtMainMenu();
        }
        if (Button != null && crest != null)
            RWAssets.Crisp(crest, PictureBox, RWAssets.ScreenPixels(crest.GetComponentInParent<Canvas>()));   // again after a change of UI scale
        if (Button != null || !RWUtil.Has("Tutorial", "Tutorial_New_Game"))
            return;
        Blockage.Menu.Blu2_Menu_Handler main;
        GameObject play;
        if (Blockage.Menu.Bl_Menus.Menus == null || !Blockage.Menu.Bl_Menus.Menus.TryGetValue("Main", out main) || main == null || !main.Items.TryGetValue("Play", out play) || play == null)
            return;
        Make(menu, main, play);
    }

    static void Make(Mainmenu_Main menu, Blockage.Menu.Blu2_Menu_Handler main, GameObject play)
    {
        GameObject copy = UnityEngine.Object.Instantiate(play, play.transform.parent, false);
        GameObject listed;
        if (main.Items.TryGetValue(copy.name, out listed) && listed == copy) main.Items.Remove(copy.name);   // Awake listed it as "Play(Clone)"
        copy.name = ItemName;
        copy.transform.SetSiblingIndex(play.transform.GetSiblingIndex() + 1);
        RectTransform from = play.transform as RectTransform, to = copy.transform as RectTransform;
        // It stands one step right of Play, and what stood beside Play (Hero) moves one step on: the column under Play keeps its
        // places, which reach the screen's foot already. The parent has no layout group.
        if (from != null && to != null)
        {
            float step = 0f;
            foreach (Transform t in play.transform.parent)
            {
                RectTransform other = t as RectTransform;
                if (other != null && other != from && other != to && Mathf.Abs(other.anchoredPosition.y - from.anchoredPosition.y) < 0.5f && other.anchoredPosition.x > from.anchoredPosition.x)
                    step = step == 0f ? other.anchoredPosition.x - from.anchoredPosition.x : Mathf.Min(step, other.anchoredPosition.x - from.anchoredPosition.x);
            }
            if (step == 0f) step = from.rect.width + 4f;
            foreach (Transform t in play.transform.parent)
            {
                RectTransform other = t as RectTransform;
                if (other != null && other != from && other != to && Mathf.Abs(other.anchoredPosition.y - from.anchoredPosition.y) < 0.5f && other.anchoredPosition.x > from.anchoredPosition.x)
                    other.anchoredPosition += new Vector2(step, 0f);
            }
            to.anchoredPosition = from.anchoredPosition + new Vector2(step, 0f);
            // The hover tip stood clear of Play, under Hero: it stays clear of this tile too.
            GameObject tip;
            RectTransform tipAt = main.Items.TryGetValue("Buttontip", out tip) && tip != null ? tip.transform as RectTransform : null;
            if (tipAt != null) tipAt.anchoredPosition += new Vector2(step, 0f);
        }
        foreach (TMPro.TMP_Text label in copy.GetComponentsInChildren<TMPro.TMP_Text>(true))
            label.text = RWData.ModeDisplay.ToUpperInvariant();
        Image(copy);
        Button = copy.GetComponent<Blu3_Button>();
        if (Button != null)
        {
            Button.Buttonclick = null;
            Button.Buttonenter = null;
            Button.Buttonleave = null;
            Button.Buttonclick += Clicked;
            Button.Buttonenter += Hover;
            Button.Addmenu();
        }
        copy.SetActive(true);
        RWLog.Info("main menu: the Rogue Waves button stands beside Play");
    }

    // Play's tile: its picture (child "Image", the ship and arrows) becomes the pack's button picture, RogueWaves_MenuButton
    // (48 px, the crest and » chevrons in Play's own flat grey), on Play's own white tile (docs/STYLE.md). Its border stays,
    // and the line down to the column stays Play's alone. Without that file the 32 px crest stands in on Rogue Teal, as
    // before. Either is sized to whole screen pixels per art pixel, never stretched to fill the 48-unit box (pitfall #156).
    public const float PictureBox = 48f;

    static void Image(GameObject copy)
    {
        Sprite art = RWAssets.Sprite("RogueWaves_MenuButton");
        bool tile = art != null;
        if (!tile) art = RWAssets.Sprite("RogueWaves_Crest32");
        UnityEngine.UI.Image box = copy.GetComponent<UnityEngine.UI.Image>();
        if (box != null && !tile) box.color = RWDialog.Teal;
        Transform picture = copy.transform.Find("Image"), down = copy.transform.Find("Linev");
        UnityEngine.UI.Image i = picture != null ? picture.GetComponent<UnityEngine.UI.Image>() : null;
        crest = null;
        if (i != null && art != null)
        {
            i.sprite = art;
            i.preserveAspect = true;
            RWAssets.Crisp(i, PictureBox, RWAssets.ScreenPixels(copy.GetComponentInParent<Canvas>()));
            crest = i;
        }
        if (down != null) down.gameObject.SetActive(false);
    }

    // Each stock button rewrites the tip under the menu on hover (Mainmenu_Main.Playhover).
    static void Hover()
    {
        try
        {
            Blockage.Menu.Blu2_Menu_Handler main = Blockage.Menu.Bl_Menus.Menus["Main"];
            GameObject tip;
            if (main.Items.TryGetValue("Buttontip", out tip) && tip != null)
                Bla_Localization.SetText(tip, RWData.ModeDisplay);
        }
        catch (Exception e) { RWLog.Once("menu tip", "main menu tip: " + e.Message); }
    }

    public static void Clicked()
    {
        if (asking || RogueWavesModeMod.Instance == null || seen == null)
            return;
        asking = true;
        RogueWavesModeMod.Instance.StartCoroutine(Choose(seen));
    }

    // What the Continue card says: where the saved campaign stands.
    public static string SavedLine()
    {
        uint seed;
        List<int> route;
        int retries, replay, playing;
        if (!RWCampaign.Saved(out seed, out route, out retries, out replay, out playing))
            return "";
        string line = "Map " + Mathf.Min(route.Count + 1, RWChart.Rows) + " of " + RWChart.Rows + ", " + RWCard.Retries(playing >= 0 ? Mathf.Max(0, retries - 1) : retries);
        if (playing >= 0) line += ". The last map was left while it ran: it counts as lost";
        string crew = RWRun.SavedCrew();
        return crew != "" ? line + ". Crew: " + crew : line;
    }

    static IEnumerator Choose(Mainmenu_Main menu)
    {
        string saved = SavedLine();
        List<Bla_Ui_Confirmation.Entry> cards = new List<Bla_Ui_Confirmation.Entry>();
        cards.Add(RWDialog.Card("New", "New campaign", "Start a new campaign: ten maps across a fresh sea chart", RWAssets.Sprite("RogueWaves_Crest"), RWDialog.Teal));
        if (saved != "")
            cards.Add(RWDialog.Card("Continue", "Continue", saved, RWAssets.Sprite("RogueWaves_Retry"), RWDialog.Teal));
        Bla_Ui_Confirmation.Entry back = RWDialog.Card("Back", "Back", "", Icon("Back"), new Color(0.3568f, 0.3647059f, 0.4117647f, 1f));
        back.Tomenu = "Main";
        cards.Add(back);
        string pick = null;
        yield return RWDialog.Ask(cards, "ROGUE WAVES", saved != "" ? "A campaign is waiting" : "Set sail?", 0f, n => pick = n);
        asking = false;
        Status = "picked " + (pick ?? "nothing");
        if (pick == "New" && saved != "")
        {
            asking = true;
            List<Bla_Ui_Confirmation.Entry> sure = new List<Bla_Ui_Confirmation.Entry>();
            sure.Add(RWDialog.Card("Sure", "Start over", "The saved campaign is lost", Icon("Exitgame"), new Color(1f, 0.5019608f, 0.5019608f, 1f)));
            Bla_Ui_Confirmation.Entry keep = RWDialog.Card("Keep", "Back", "", Icon("Back"), new Color(0.3568f, 0.3647059f, 0.4117647f, 1f));
            keep.Tomenu = "Main";
            sure.Add(keep);
            // One dialog at a time: the second opens only after the first has closed (kit docs/20 §2).
            yield return new WaitForSecondsRealtime(0.3f);
            pick = null;
            yield return RWDialog.Ask(sure, "NEW CAMPAIGN", "The saved campaign is lost", 0f, n => pick = n);
            asking = false;
            Status = "start over: " + (pick ?? "nothing");
            if (pick != "Sure") { ToMain(); yield break; }
            pick = "New";
        }
        if (pick != "New" && pick != "Continue")
        {
            ToMain();
            yield break;
        }
        Embarking = pick;
        RWLog.Info("main menu: embarking, " + pick);
        menu.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
    }

    static void ToMain() { try { Blockage.Menu.Bl_Menus.Changemenu("Main", true); } catch (Exception) { } }

    static Sprite Icon(string name)
    {
        try { return Helper_Uitools.Geticon(name); }
        catch (Exception) { return null; }
    }

    // The host on the boat, once it is up: starts what the button asked for. True while it has something to do.
    public static void Boat()
    {
        if (Embarking == "" || !RWUtil.IsHost)
            return;
        string what = Embarking;
        Embarking = "";
        if (what == "New")
            RWCampaign.New(0);
        else if (!RWCampaign.Continue())
            RWRejoin.Say(RWCampaign.Ended == "lost" ? "That campaign is over: it had no retries left." : "No campaign to continue.");
    }
}

// The mode part's little asset library (Tools > RogueWavesMode > Build All Assets, Assets/RogueWavesModeMod/Prefabs): the
// temporary sprites of Art/Temporary/game_size, with any final art of the same name one folder up (docs/TEMP_ART.md).
public static class RWAssets
{
    public static Transform Library;
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    public static bool Find()
    {
        if (Library != null)
            return true;
        if (RogueWavesModeMod.Instance != null)
            Library = RogueWavesModeMod.Instance.transform.Find("RWAssets_Library");
        if (Library == null)
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go.name == "RWAssets_Library" && go.scene.IsValid())
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

    // A library sprite, centred, at the game's 20 pixels a unit. Null if the library or the name isn't there.
    public static Sprite Sprite(string name)
    {
        Sprite s;
        if (sprites.TryGetValue(name, out s) && s != null)
            return s;
        if (!Find())
            return null;
        Transform t = Library.Find("Sprites/" + name);
        SpriteRenderer r = t != null ? t.GetComponent<SpriteRenderer>() : null;
        Texture2D tex = r != null && r.sprite != null ? r.sprite.texture : null;
        if (tex == null)
            return null;
        tex.filterMode = FilterMode.Point;
        s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 20f, 0, SpriteMeshType.FullRect);
        s.name = name;
        sprites[name] = s;
        return s;
    }

    // A library sprite by its name in any case: Directions name themselves in the upgrade's spelling ("Compostbin"), the
    // art files in the scope's ("Direction_CompostBin").
    public static Sprite Like(string name)
    {
        if (!Find())
            return null;
        Transform holder = Library.Find("Sprites");
        if (holder == null)
            return null;
        foreach (Transform t in holder)
            if (string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase))
                return Sprite(t.name);
        return null;
    }

    // Screen pixels per canvas unit on this canvas at rest (the player's UI scale: 3 at 2560x1440), from the root's size.
    public static float ScreenPixels(Canvas canvas)
    {
        RectTransform root = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
        return root != null && root.rect.height > 0f ? Screen.height / root.rect.height : 0f;
    }

    // Pixel art in a UI box, crisp: every art pixel covers the same whole number of screen pixels, and the picture's corner
    // sits a whole number of screen pixels from its parent's corner. A picture stretched to an uneven size (12 px in a
    // 16-unit box at a UI scale of 3 is 4 screen pixels to one art pixel in some columns and 3 in others) changes which
    // columns are wide with every sub-pixel step of a slide or tween, and reads as jitter (pitfall #156). A picture that
    // fits the box draws one unit per art pixel, or a whole multiple of that while the multiple still fits (an 8 px icon
    // fills a 16-unit box), as near as whole screen pixels allow at an uneven UI scale; a larger one at the most whole screen
    // pixels per art pixel that still fit the box, and one too large for even one screen pixel each is only fitted.
    // pixels: screen pixels per canvas unit at the picture (ScreenPixels, times any scale between). The picture keeps its
    // middle, is pinned to its parent's corner, and may be crisped again.
    public static void Crisp(UnityEngine.UI.Image image, float box, float pixels)
    {
        if (image == null || image.sprite == null || pixels <= 0f)
            return;
        RectTransform rt = image.transform as RectTransform, parent = rt != null ? rt.parent as RectTransform : null;
        Rect art = image.sprite.rect;
        float big = Mathf.Max(art.width, art.height);
        if (parent == null || big <= 0f)
            return;
        int most = Mathf.FloorToInt(box * pixels / big + 0.001f);
        if (most < 1)
        {
            image.preserveAspect = true;
            return;
        }
        int n = big <= box ? Mathf.Min(most, Mathf.Max(1, Mathf.RoundToInt(pixels)) * Mathf.FloorToInt(box / big)) : most;
        Vector2 size = new Vector2(art.width, art.height) * (n / pixels);
        Vector2 middle = (Vector2)rt.localPosition + Vector2.Scale(rt.rect.size, new Vector2(0.5f, 0.5f) - rt.pivot) - parent.rect.min;
        Vector2 corner = middle - size * 0.5f;
        corner = new Vector2(Mathf.Round(corner.x * pixels), Mathf.Round(corner.y * pixels)) / pixels;
        image.preserveAspect = false;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.sizeDelta = size;
        rt.anchoredPosition = corner + Vector2.Scale(size, rt.pivot);
    }

    public static Texture2D Texture(string name)
    {
        if (!Find())
            return null;
        Transform t = Library.Find("Sprites/" + name);
        SpriteRenderer r = t != null ? t.GetComponent<SpriteRenderer>() : null;
        if (r == null || r.sprite == null)
            return null;
        r.sprite.texture.filterMode = FilterMode.Point;
        return r.sprite.texture;
    }
}
