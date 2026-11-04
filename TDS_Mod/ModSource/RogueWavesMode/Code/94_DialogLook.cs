#if ROGUEWAVESMODE_DIALOGLOOK
// Debug build only (-Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_SHOTS,ROGUEWAVESMODE_DIALOGLOOK): the quick round for where the
// game's choice dialog stands, on the main menu: every card count a shop can reach and more, at four screen sizes, with
// the longest hover line the mode writes and then some. Logs what moves it, with captures. The screen size is put back.
public static partial class RWTest
{
    static void Where(string when, Bla_Ui_Confirmation d)
    {
        Blockage.Menu.Blu2_Menu_Handler menu = Blockage.Menu.Bl_Menus.Menus[d.gameObject.name];
        RectTransform list = (RectTransform)d.List.transform, main = (RectTransform)list.parent;
        Info("where", when + ": transition " + menu.Intransition + ", Main anchors " + RWUtil.V(main.anchorMin) + " pos " + RWUtil.V(main.anchoredPosition) + " at " + Rc(RWDialog.OnScreen(main))
            + ", list anchors " + RWUtil.V(list.anchorMin) + " pos " + RWUtil.V(list.anchoredPosition) + " at " + Rc(RWDialog.OnScreen(list)));
    }

    static void Anims(string what, GameObject g)
    {
        Blockage.Menu.Blu4_Transition_Animator a = g != null ? g.GetComponent<Blockage.Menu.Blu4_Transition_Animator>() : null;
        if (a == null) { Info("anims", what + ": none"); return; }
        List<string> names = new List<string>();
        foreach (Blockage.Menu.Blu5_Transitionanim t in a.Transitionanimations_In)
        {
            Blockage.Menu.Blu5_Transitionanim_Anchorsize size = t.Ref as Blockage.Menu.Blu5_Transitionanim_Anchorsize;
            names.Add((t.Ref != null ? t.Ref.GetType().Name.Replace("Blu5_Transitionanim_", "") : "null") + " delay " + RWUtil.F(t.Delay) + (size != null ? " " + size.Start + " to " + size.End + " in " + RWUtil.F(size.Speed) : ""));
        }
        Info("anims", what + " in: " + string.Join("; ", names.ToArray()));
    }

    static IEnumerator Try(string name, int cards, bool watch, bool shot)
    {
        const string longest = "A long hover line, about as long as the longest Cardinal the shop sells, with its price and its kind in front of it, to see that it wraps and every word of it stays on the screen";
        List<Bla_Ui_Confirmation.Entry> list = new List<Bla_Ui_Confirmation.Entry>();
        for (int i = 0; i < cards; i++)
            list.Add(RWDialog.Card("c" + i, "Card " + i, longest, RWAssets.Sprite("RogueWaves_Crest32"), RWDialog.Teal));
        string pick = "-";
        RogueWavesModeMod.Instance.StartCoroutine(RWDialog.Ask(list, cards == 2 ? "MIND THE METEOR" : "VENDCINT", "Pure Aura: 60", 0f, n => pick = n));
        Bla_Ui_Confirmation d = Bla_Ui_Confirmation.Instance;
        if (watch)
        {
            float start = Time.unscaledTime;
            foreach (float at in new[] { 0.05f, 0.4f, 0.6f, 0.8f, 1f, 1.5f })
            {
                while (Time.unscaledTime - start < at) yield return null;
                Where(name + " +" + RWUtil.F(at), d);
            }
        }
        else
            yield return Until(() => RWDialog.Up() && RWDialog.LastCards == cards && CardNames().Count == cards, 5f);
        yield return DialogFits(name);
        if (watch)
        {
            List<string> spots = new List<string>();
            foreach (RectTransform c in RWDialog.Cards(d)) spots.Add(c.name + "@" + Rc(RWDialog.OnScreen(c)));
            Info("cards", string.Join(", ", spots.ToArray()));
        }
        if (shot) yield return Snap("dlook_" + name.Replace(" ", "_"));
        Blu3_Button first = d.List.GetComponentInChildren<Blu3_Button>();
        if (shot && first != null && first.Buttonenter != null)
        {
            first.Buttonenter();
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Snap("dlook_" + name.Replace(" ", "_") + "_hover");
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
        Bla_Ui_Confirmation d = Bla_Ui_Confirmation.Instance;
        Anims("Main", d.List.transform.parent.gameObject);
        Anims("List", d.List);
        Anims("Title", d.Titlebox);

        int w0 = Screen.width, h0 = Screen.height;
        FullScreenMode mode0 = Screen.fullScreenMode;
        Info("screen", w0 + "x" + h0 + " " + mode0);
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
            Info("screen", Screen.width + "x" + Screen.height + ": canvas " + RWUtil.V(((RectTransform)root.transform).rect.size) + ", scale " + RWUtil.F(root.scaleFactor));
            foreach (int n in new[] { 2, 3, 4, 5, 8, 12, 16, 30 })
            {
                bool first = s == 0, picture = n == 2 || n == 3 || n == 5 || n == 16 || (s > 0 && n == 8);
                yield return Try(w + "x" + h + " " + n + " cards", n, first && n == 5, picture);
            }
        }
        Screen.SetResolution(w0, h0, mode0);
        yield return Until(() => Screen.width == w0 && Screen.height == h0, 6f);
        Check("the screen size is back", Screen.width == w0 && Screen.height == h0 && Screen.fullScreenMode == mode0, Screen.width + "x" + Screen.height + " " + Screen.fullScreenMode);
        Blockage.Menu.Bl_Menus.Changemenu("Main", true);
    }
}
#endif
