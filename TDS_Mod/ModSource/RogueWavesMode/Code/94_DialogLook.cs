#if ROGUEWAVESMODE_DIALOGLOOK
// Debug build only (-Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_SHOTS,ROGUEWAVESMODE_DIALOGLOOK): the quick round for the game's
// choice dialog, on the main menu. First how smoothly it opens, the mode's way against the stock way; then every card count a
// shop can reach and more, at four screen sizes, with a hover line longer than the mode writes. The screen size is put back.
public static partial class RWTest
{
    // Every frame's time while a thing happens; a list of the slow ones, which is what a player sees as a hitch.
    static IEnumerator Frames(string what, float seconds, Action<float> worst)
    {
        List<string> slow = new List<string>();
        float begin = Time.unscaledTime, end = begin + seconds, most = 0f;
        int frames = 0, cleanups = GC.CollectionCount(0);
        while (Time.unscaledTime < end)
        {
            yield return null;
            float dt = Time.unscaledDeltaTime;
            frames++;
            most = Mathf.Max(most, dt);
            int now = GC.CollectionCount(0);
            if (dt > 0.010f) slow.Add("+" + RWUtil.F(Time.unscaledTime - begin) + "s " + RWUtil.F(dt * 1000f) + "ms" + (now != cleanups ? " with a memory cleanup" : ""));
            cleanups = now;
        }
        Info("frames", what + ": " + frames + " frames, worst " + RWUtil.F(most * 1000f) + " ms" + (slow.Count > 0 ? "; slow: " + string.Join(", ", slow.ToArray()) : ""));
        worst(most);
    }

    static List<Bla_Ui_Confirmation.Entry> Probes(int cards, string line)
    {
        List<Bla_Ui_Confirmation.Entry> list = new List<Bla_Ui_Confirmation.Entry>();
        for (int i = 0; i < cards; i++)
            list.Add(RWDialog.Card("c" + i, "Card " + i, line, RWAssets.Sprite("RogueWaves_Crest"), RWDialog.Teal));
        return list;
    }

    // Map cards as a vote shows them (RWVote.MapCard): a name and a badge drawn on each when the dialog opens.
    // The variants split the cost when a round hitches (PC round 5 §2.3): the large picture alone, with the name, with the badge.
    static List<Bla_Ui_Confirmation.Entry> MapProbes(int cards, bool name = true, bool badge = true)
    {
        List<Bla_Ui_Confirmation.Entry> list = Probes(cards, "Calm waters" + RWCard.Separator + "Danger 3");
        for (int i = 0; i < list.Count; i++) RWDialog.Face(list[i], name ? "Map card " + i : "", badge && i == 0 ? RWAssets.Sprite("RogueWaves_Crest") : null);
        return list;
    }

    const string LongLine = "A long hover line, about as long as the longest Cardinal the shop sells, with its price and its kind in front of it, to see that it wraps and every word of it stays on the screen";

    static int lastNames;
    static bool lastBadge;

    static IEnumerator Opening(string name, bool mine, Action<float> worst, bool maps = false, bool mapName = true, bool mapBadge = true)
    {
        Coroutine timed = RogueWavesModeMod.Instance.StartCoroutine(Frames(name, 1.8f, worst));
        List<Bla_Ui_Confirmation.Entry> cards = maps ? MapProbes(3, mapName, mapBadge) : Probes(5, LongLine);
        if (mine)
        {
            string pick = "-";
            RogueWavesModeMod.Instance.StartCoroutine(RWDialog.Ask(cards, "VENDCINT", "Pure Aura: 60", 0f, n => pick = n));
        }
        else
        {
            Blockage.Menu.Bl_Menus.Changemenu(Bla_Ui_Confirmation.Instance.gameObject.name, true);
            yield return Until(() => RWDialog.Up() && Bla_Ui_Confirmation.Instance.GetComponentInParent<Blockage.Menu.Blu2_Menu_Handler>() != null, 5f);
            Bla_Ui_Confirmation.Instance.Runprompt(cards, "VENDCINT", "Pure Aura: 60", false);
        }
        yield return timed;
        if (maps)
        {
            // What the opening showed: cards with a name, and the first card's badge.
            lastNames = 0;
            lastBadge = false;
            string n;
            float p;
            foreach (RectTransform c in RWDialog.Cards(Bla_Ui_Confirmation.Instance))
            {
                if (RWDialog.Faced(c, out n, out p)) lastNames++;
                if (c.name == "c0" && c.Find(RWDialog.FaceBadgeName) != null && c.Find(RWDialog.FaceBadgeName).gameObject.activeInHierarchy) lastBadge = true;
            }
        }
        if (mine) RWDialog.Cancel();
        else Blockage.Menu.Bl_Menus.Closemenu(Bla_Ui_Confirmation.Instance.gameObject, true);
        yield return new WaitForSecondsRealtime(3f);
    }

    // The mode's dialog must open as the stock one does, with no frame long enough to see (Dimitri 2026-11-04). A dialog's first
    // opening in a launch pays for its fonts once, stock or not, so it isn't the one compared.
    static IEnumerator Smooth()
    {
        float first = 0f, mine = 0f, stock = 0f;
        int mineLong = 0, stockLong = 0;
        const int Rounds = 10;
        yield return Opening("first opening", true, w => first = w);
        for (int round = 1; round <= Rounds; round++)
        {
            yield return Opening("the mode's, round " + round, true, w => { mine = Mathf.Max(mine, w); if (w * 1000f > 25f) mineLong++; });
            yield return Opening("stock, round " + round, false, w => { stock = Mathf.Max(stock, w); if (w * 1000f > 25f) stockLong++; });
        }
        Check("The mode's dialog opens with no frame long enough to see, as the stock one does", mineLong <= stockLong + 1,
            mineLong + " of " + Rounds + " openings with a frame over 25 ms, worst " + RWUtil.F(mine * 1000f) + " ms; stock " + stockLong + ", worst " + RWUtil.F(stock * 1000f) + " ms; a launch's first opening " + RWUtil.F(first * 1000f) + " ms");
        // The map cards draw a name and a badge on each card as the dialog opens (full test 2026-11-06 §5 item 3).
        // A launch's first map cards make their name texts and badge (RWDialog.FacesMade); every later opening reuses them.
        // Its frame is reported beside the check, as the plain dialog's first opening is: round 5's first freeze was there.
        float firstMaps = 0f;
        yield return Opening("map cards, first", true, w => firstMaps = w, true);
        int made = RWDialog.FacesMade;
        bool firstDressed = lastNames == 3 && lastBadge;
        float maps = 0f;
        int mapsLong = 0, dressed = 0;
        for (int round = 1; round <= Rounds; round++)
        {
            yield return Opening("map cards, round " + round, true, w => { maps = Mathf.Max(maps, w); if (w * 1000f > 25f) mapsLong++; }, true);
            if (lastNames == 3 && lastBadge) dressed++;
        }
        Check("Map cards with their name and badge open with no frame long enough to see, as the stock dialog does", mapsLong <= stockLong + 1,
            mapsLong + " of " + Rounds + " openings with a frame over 25 ms, worst " + RWUtil.F(maps * 1000f) + " ms; stock " + stockLong + "; a launch's first map cards " + RWUtil.F(firstMaps * 1000f) + " ms");
        Check("Map cards keep their name texts and badge from one opening to the next: every card dressed, nothing new made", firstDressed && dressed == Rounds && made >= 4 && RWDialog.FacesMade == made,
            "made " + made + " on the first map cards (3 names, 1 badge), " + (RWDialog.FacesMade - made) + " more in " + Rounds + " openings after; dressed (3 names, the badge) in " + dressed + " of " + Rounds + (firstDressed ? "" : ", the first not"));
        // Which piece costs, should the check above fail again: five openings each with a piece taken off.
        string[] parts = { "picture only", "picture and name", "picture and badge" };
        for (int v = 0; v < parts.Length; v++)
        {
            float worstPart = 0f;
            int longPart = 0;
            for (int round = 1; round <= 5; round++)
                yield return Opening("map cards, " + parts[v] + ", round " + round, true, w => { worstPart = Mathf.Max(worstPart, w); if (w * 1000f > 25f) longPart++; }, true, v == 1, v == 2);
            Info("map cards", parts[v] + ": " + longPart + " of 5 openings with a frame over 25 ms, worst " + RWUtil.F(worstPart * 1000f) + " ms");
        }

        // A card's hover, by the pointer's own calls.
        string pick = "-";
        RogueWavesModeMod.Instance.StartCoroutine(RWDialog.Ask(Probes(5, LongLine), "VENDCINT", "Pure Aura: 60", 0f, n => pick = n));
        yield return new WaitForSecondsRealtime(2.5f);
        UnityEngine.EventSystems.PointerEventData data = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        float hover = 0f;
        int hoverLong = 0;
        foreach (RectTransform c in RWDialog.Cards(Bla_Ui_Confirmation.Instance))
        {
            Blu3_Button b = c.GetComponent<Blu3_Button>();
            b.OnPointerEnter(data);
            yield return Frames("hovering " + c.name, 0.4f, w => { hover = Mathf.Max(hover, w); if (w * 1000f > 25f) hoverLong++; });
            b.OnPointerExit(data);
        }
        Check("Hovering the mode's cards shows no long frame, at most one stray", hoverLong <= 1, hoverLong + " of 5 hovers with a frame over 25 ms, worst " + RWUtil.F(hover * 1000f) + " ms");
        RWDialog.Cancel();
        yield return new WaitForSecondsRealtime(3f);
    }

#if ROGUEWAVESMODE_FEELMAP
    // The same comparison in a running map with the clock held, opened from the live HUD as the mode's dialogs are, and then the
    // real VENDCINT as its button opens it.
    static IEnumerator HoverOne(string what, RectTransform c, List<string> texts)
    {
        UnityEngine.EventSystems.PointerEventData data = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        Blu3_Button b = c.GetComponent<Blu3_Button>();
        double before = Time.realtimeSinceStartupAsDouble;
        b.OnPointerEnter(data);
        float call = (float)((Time.realtimeSinceStartupAsDouble - before) * 1000.0);
        float worst = 0f;
        yield return Frames(what + " " + c.name, 0.4f, w => worst = w);
        string line = TextOf(Bla_Ui_Confirmation.Instance.Subbox);
        Info("hover", what + " " + c.name + ": call " + RWUtil.F(call) + " ms, worst frame " + RWUtil.F(worst * 1000f) + " ms, " + line.Length + " letters: " + line);
        if (texts != null) texts.Add(line);
        b.OnPointerExit(data);
        yield return new WaitForSecondsRealtime(0.3f);
    }

    static IEnumerator HoverProbe()
    {
        RWShop.Open("Shop");
        yield return new WaitForSecondsRealtime(2.5f);
        Bla_Ui_Confirmation d = Bla_Ui_Confirmation.Instance;
        List<RectTransform> cards = RWDialog.Cards(d);
        List<string> texts = new List<string>();
        foreach (RectTransform c in cards) yield return HoverOne("mine, in order", c, texts);
        for (int i = cards.Count - 1; i >= 0; i--) yield return HoverOne("mine, backwards", cards[i], null);
        for (int i = 0; i < 4; i++) yield return HoverOne("mine, the first card again", cards[0], null);

        // The words alone, with no button and no sound.
        TMPro.TMP_Text line = d.Subbox.GetComponent<TMPro.TMP_Text>();
        foreach (string text in texts)
        {
            yield return new WaitForSecondsRealtime(0.3f);
            double before = Time.realtimeSinceStartupAsDouble;
            line.text = text;
            float set = (float)((Time.realtimeSinceStartupAsDouble - before) * 1000.0);
            float worst = 0f;
            yield return Frames("words alone", 0.3f, w => worst = w);
            Info("hover", "words alone, " + text.Length + " letters: set " + RWUtil.F(set) + " ms, worst frame " + RWUtil.F(worst * 1000f) + " ms");
            line.text = "Pure Aura: 60";
        }
        RWDialog.Cancel();
        yield return new WaitForSecondsRealtime(3f);

        // The same words on the same cards in the game's own layout, with nothing of the mode's placing.
        List<Bla_Ui_Confirmation.Entry> same = new List<Bla_Ui_Confirmation.Entry>();
        for (int i = 0; i < texts.Count; i++)
            same.Add(RWDialog.Card("c" + i, "", texts[i], RWAssets.Sprite("RogueWaves_Crest"), RWDialog.Teal));
        Blockage.Menu.Bl_Menus.Changemenu(d.gameObject.name, true);
        yield return Until(() => RWDialog.Up() && d.GetComponentInParent<Blockage.Menu.Blu2_Menu_Handler>() != null, 5f);
        d.Runprompt(same, "VENDCINT", "Pure Aura: 60", false);
        yield return new WaitForSecondsRealtime(2.5f);
        cards = RWDialog.Cards(d);
        foreach (RectTransform c in cards) yield return HoverOne("stock, in order", c, null);
        for (int i = cards.Count - 1; i >= 0; i--) yield return HoverOne("stock, backwards", cards[i], null);
        for (int i = 0; i < 4; i++) yield return HoverOne("stock, the first card again", cards[0], null);
        Blockage.Menu.Bl_Menus.Closemenu(d.gameObject, true);
        yield return new WaitForSecondsRealtime(3f);
    }

    static IEnumerator SmoothInMap()
    {
        const int Rounds = 10;
        float mine = 0f, stock = 0f, shop = 0f;
        int mineLong = 0, stockLong = 0, shopLong = 0;
        for (int round = 1; round <= Rounds; round++)
        {
            yield return Opening("in a map, the mode's, round " + round, true, w => { mine = Mathf.Max(mine, w); if (w * 1000f > 25f) mineLong++; });
            yield return Opening("in a map, stock, round " + round, false, w => { stock = Mathf.Max(stock, w); if (w * 1000f > 25f) stockLong++; });
        }
        for (int round = 1; round <= Rounds; round++)
        {
            Coroutine timed = RogueWavesModeMod.Instance.StartCoroutine(Frames("in a map, VENDCINT's own shop, round " + round, 1.8f, w => { shop = Mathf.Max(shop, w); if (w * 1000f > 25f) shopLong++; }));
            RWShop.Open("Shop");
            yield return timed;
            RWDialog.Cancel();
            yield return new WaitForSecondsRealtime(3f);
        }
        Info("feel map", "with pretend cards " + mineLong + " of " + Rounds + " openings over 25 ms, worst " + RWUtil.F(mine * 1000f) + " ms; stock " + stockLong + ", worst " + RWUtil.F(stock * 1000f) + " ms; VENDCINT's own shop " + shopLong + ", worst " + RWUtil.F(shop * 1000f) + " ms");

        // VENDCINT's landing, the host's whole call.
        float land = 0f;
        for (int round = 1; round <= 6; round++)
        {
            Coroutine timedLand = RogueWavesModeMod.Instance.StartCoroutine(Frames("VENDCINT's landing, round " + round, 0.6f, w => land = Mathf.Max(land, w)));
            RWShop.Land();
            yield return timedLand;
            yield return new WaitForSecondsRealtime(2f);
        }
        Info("feel map", "VENDCINT's landing: worst frame " + RWUtil.F(land * 1000f) + " ms");
        yield return HoverProbe();
    }
#endif

    static IEnumerator Try(string name, int cards, bool shot)
    {
        string pick = "-";
        RogueWavesModeMod.Instance.StartCoroutine(RWDialog.Ask(Probes(cards, LongLine), cards == 2 ? "MIND THE METEOR" : "VENDCINT", "Pure Aura: 60", 0f, n => pick = n));
        Bla_Ui_Confirmation d = Bla_Ui_Confirmation.Instance;
        yield return Until(() => RWDialog.Up() && RWDialog.LastCards == cards && CardNames().Count == cards, 5f);
        yield return DialogFits(name);
        string shotName = "dlook_" + name.Replace(" ", "_");
        if (shot) yield return Snap(shotName);
        Blu3_Button first = d.List.GetComponentInChildren<Blu3_Button>();
        if (shot && first != null && first.Buttonenter != null)
        {
            first.Buttonenter();
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Snap(shotName + "_hover");
        }
        if (first != null && first.Buttonclick != null) first.Buttonclick();
        yield return Until(() => pick != "-", 5f);
        Check(name + ": the card still answers", pick != null && pick.StartsWith("c"), "picked " + pick);
        yield return new WaitForSecondsRealtime(1f);
    }

    public static IEnumerator DialogLook()
    {
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 120f);
        Mainmenu_Main title = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (title == null) { Setup("dialog look", "no main menu"); yield break; }
        yield return new WaitForSecondsRealtime(8f);
        title.Idlethreshold = 100000f;
        title.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
        yield return Until(() => RWMenu.Button != null, 25f);
        yield return new WaitForSecondsRealtime(7f);
        Blockage.Menu.Bl_Menus.Changemenu("Main", true);
        yield return new WaitForSecondsRealtime(10f);
        yield return Smooth();

        Bla_Ui_Confirmation d = Bla_Ui_Confirmation.Instance;
        int w0 = Screen.width, h0 = Screen.height;
        FullScreenMode mode0 = Screen.fullScreenMode;
        int[,] sizes = { { w0, h0 }, { 1920, 1080 }, { 1280, 720 }, { 1024, 768 } };
        for (int s = 0; s < sizes.GetLength(0); s++)
        {
            int w = sizes[s, 0], h = sizes[s, 1];
            if (s > 0)
            {
                Screen.SetResolution(w, h, mode0);
                yield return Until(() => Screen.width == w && Screen.height == h, 6f);
                yield return new WaitForSecondsRealtime(1.5f);
            }
            Canvas root = d.GetComponentInParent<Canvas>().rootCanvas;
            Info("screen", Screen.width + "x" + Screen.height + " " + mode0 + ": canvas " + RWUtil.V(((RectTransform)root.transform).rect.size));
            foreach (int n in new[] { 2, 3, 4, 5, 8, 12, 16, 30 })
                yield return Try(w + "x" + h + " " + n + " cards", n, n == 2 || n == 3 || n == 5 || n == 16 || (s > 0 && n == 8));
        }
        Screen.SetResolution(w0, h0, mode0);
        yield return Until(() => Screen.width == w0 && Screen.height == h0, 6f);
        Check("the screen size is back", Screen.width == w0 && Screen.height == h0 && Screen.fullScreenMode == mode0, Screen.width + "x" + Screen.height + " " + Screen.fullScreenMode);
        Blockage.Menu.Bl_Menus.Changemenu("Main", true);
    }
}
#endif
