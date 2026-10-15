// Round 4 (0.4.0), the main menu half: probes M1-M5 and M7 of docs/mode/MAIN_MENU_START.md §5 in the Rogue Waves repository.
// The run starts from a "Rogue Waves" button the mod clones from the main menu's Play, not from Play itself, and the
// boat takes the mod's mode with no Cardinal equipped. M6 (the flag clears when the main menu comes back) is the mod's
// own code and is not run: getting back to the main menu from a match is not something RogueLab has done yet.
using System.Globalization;

public static class RogueLabMenu
{
    public const string ItemName = "RogueLab_Start";
    public static bool Flag;                 // "Rogue" flag: set by the button, read on the boat (M4)
    public static bool Done;                 // the menu half finished and Play was sent
    static bool running, handlerRan, stockPlayAfterClick;

    // Called from the driver while the main menu is up, instead of round 1-3's SendMessage("Play").
    public static IEnumerator Run(Mainmenu_Main menu)
    {
        if (running) { Embark(menu); yield break; }   // the driver's retry: the menu is still up, use the stock Play
        running = true;
        // Round 4 waited for Main's Active flag, which never rose in 20 s on the title screen: now it leaves the title
        // the way the Start menu's Play does (Transitiontomain), waits for the menu's items, and goes on either way.
        Blockage.Menu.Blu2_Menu_Handler main = null;
        GameObject probePlay = null;
        menu.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
        float t = 0f;
        while (t < 20f && (main == null || !main.Items.TryGetValue("Play", out probePlay) || probePlay == null))
        {
            Blockage.Menu.Bl_Menus.Menus.TryGetValue("Main", out main);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        yield return new WaitForSecondsRealtime(6f);       // Transitiontomain's camera move and its 5 s title delay
        if (main == null)
        {
            RogueLabProbe.Setup("M1 main menu button", "no Main menu after " + RogueLabUtil.F(t) + " s; embarking with the stock Play");
            Embark(menu);
            yield break;
        }
        RogueLabProbe.Info("M1 main menu state", "Main active " + main.Active + ", layer " + main.Layer + ", active layer " + (Blockage.Menu.Bl_Menus.Ui != null ? Blockage.Menu.Bl_Menus.Ui.Activelayer.ToString() : "-")
            + ", Play item " + (probePlay != null) + " after " + RogueLabUtil.F(t) + " s");

        // M1: the clone.
        GameObject play;
        Blu3_Button clone = null;
        if (!main.Items.TryGetValue("Play", out play) || play == null)
            RogueLabProbe.Setup("M1 main menu button", "no Main/Play item; items [" + string.Join(", ", new List<string>(main.Items.Keys).ToArray()) + "]");
        else
        {
            GameObject copy = UnityEngine.Object.Instantiate(play, play.transform.parent, false);
            GameObject listed;
            if (main.Items.TryGetValue(copy.name, out listed) && listed == copy) main.Items.Remove(copy.name);   // Awake listed it as "Play(Clone)"
            copy.name = ItemName;
            copy.transform.SetSiblingIndex(play.transform.GetSiblingIndex() + 1);
            RectTransform from = play.transform as RectTransform, to = copy.transform as RectTransform;
            if (from != null && to != null) to.anchoredPosition = from.anchoredPosition + new Vector2(0f, -(from.rect.height + 8f));
            clone = copy.GetComponent<Blu3_Button>();
            if (clone != null)
            {
                clone.Buttonclick = null;          // a delegate is not copied by Instantiate; cleared in case
                clone.Buttonenter = null;
                clone.Buttonleave = null;
                clone.Buttonclick += Clicked;
                clone.Addmenu();
            }
            copy.SetActive(true);
            yield return null;
            GameObject registered;
            // Round 5: the clone was inactive in the hierarchy because the whole Main menu was (the title screen was still
            // up): pass when the clone is shown exactly when Play is. M4b looks again once the menu is up.
            bool ok = clone != null && copy.activeSelf && copy.activeInHierarchy == play.activeInHierarchy && main.Items.TryGetValue(ItemName, out registered) && registered == copy;
            RogueLabProbe.Check("M1 Rogue Waves button cloned from Main/Play", ok, "active " + copy.activeInHierarchy + " (self " + copy.activeSelf + ", Play " + play.activeInHierarchy + "), Blu3_Button " + (clone != null)
                + ", registered " + main.Items.ContainsKey(ItemName) + ", menu of the clone " + (clone != null && clone.Menu != null ? clone.Menu.gameObject.name : "none")
                + "; Play rect " + Rect(from) + ", clone rect " + Rect(to) + ", parent layout " + Layout(play.transform.parent));
        }

        // M2: the clone's click from code (the game ignores injected input).
        if (clone == null) { Embark(menu); yield break; }
        string sceneBefore = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        clone.Buttonclick();
        yield return new WaitForSecondsRealtime(1.5f);
        Blockage.Menu.Blu2_Menu_Handler playMenu;
        stockPlayAfterClick = Blockage.Menu.Bl_Menus.Menus.TryGetValue("Play", out playMenu) && playMenu != null && playMenu.Active;
        RogueLabProbe.Check("M2 the clone's click runs the mod's handler and not the stock Play", handlerRan && !stockPlayAfterClick
            && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == sceneBefore, "handler ran " + handlerRan + ", stock Play menu active " + stockPlayAfterClick);

        // M3: the New / Continue / Back dialog on the main menu, clicked by the mod. An exception in it must not cost M4.
        IEnumerator prompt = Prompt();
        while (true)
        {
            object cur;
            try { if (!prompt.MoveNext()) break; cur = prompt.Current; }
            catch (Exception e) { RogueLabProbe.Check("M3 dialog finished cleanly", false, e.GetType().Name + ": " + e.Message); break; }
            yield return cur;
        }

        // M4: the flag, then the stock embark. M4b: is the clone shown beside Play once the Main menu is up?
        if (clone != null)
        {
            GameObject p4;
            main.Items.TryGetValue("Play", out p4);
            RogueLabProbe.Info("M4b button beside Play with the Main menu up", "Main active " + main.Active + ", Play shown " + (p4 != null && p4.activeInHierarchy)
                + ", clone shown " + clone.gameObject.activeInHierarchy + ", canvas " + (main.GetComponent<Canvas>() != null && main.GetComponent<Canvas>().enabled));
        }
        Flag = true;
        Embark(menu);
    }

    static void Clicked() { handlerRan = true; RogueLabLog.Test("M2 handler: Rogue Waves button clicked"); }

    static void Embark(Mainmenu_Main menu)
    {
        RogueLabLog.Test("main menu: embarking (flag " + Flag + ")");
        menu.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
        Done = true;
    }

    static IEnumerator Prompt()
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        if (dialog == null) { RogueLabProbe.Setup("M3 dialog on the main menu", "no Bla_Ui_Confirmation"); yield break; }
        Blockage.Menu.Blu2_Menu_Handler handler = dialog.GetComponentInParent<Blockage.Menu.Blu2_Menu_Handler>();
        string before = "handler before " + (handler != null ? handler.gameObject.name : "none active");
        if (handler == null)
        {
            Blockage.Menu.Bl_Menus.Changemenu(dialog.gameObject.name);
            float w = 0f;
            while (dialog.GetComponentInParent<Blockage.Menu.Blu2_Menu_Handler>() == null && w < 10f) { w += Time.unscaledDeltaTime; yield return null; }
            before += ", after Changemenu " + RogueLabUtil.F(w) + " s: " + (dialog.GetComponentInParent<Blockage.Menu.Blu2_Menu_Handler>() != null);
        }
        List<Bla_Ui_Confirmation.Entry> options = new List<Bla_Ui_Confirmation.Entry>();
        string[] names = { "New", "Continue", "Back" };
        string[] shown = { "New campaign", "Continue campaign", "Back" };
        for (int i = 0; i < names.Length; i++)
        {
            Bla_Ui_Confirmation.Entry e = new Bla_Ui_Confirmation.Entry();
            e.Name = "RogueLab_" + names[i];
            e.Display_Name = shown[i];
            e.Display_Desc = i == 0 ? "Start a new Rogue Waves campaign" : i == 1 ? "Pick up the saved campaign" : "";
            e.Display_Color = new Color(0.247f, 0.561f, 0.588f, 1f);   // Rogue Teal
            if (i == 2) e.Tomenu = "Main";
            options.Add(e);
        }
        Cysharp.Threading.Tasks.UniTask<Bla_Ui_Confirmation.Entry> task;
        try { task = dialog.Runprompt(options, "ROGUE WAVES", "RogueLab round 4 (no click needed)", true); }
        catch (Exception e) { RogueLabProbe.Check("M3 dialog on the main menu", false, before + "; Runprompt threw " + e.Message); yield break; }
        yield return new WaitForSecondsRealtime(2f);
        int cards = 0;
        Transform first = null;
        if (dialog.List != null)
            foreach (Transform c in dialog.List.transform)
                if (c.name.StartsWith("RogueLab_")) { cards++; if (c.name == "RogueLab_New") first = c; }
        Blu3_Button button = first != null ? first.GetComponent<Blu3_Button>() : null;
        if (button != null && button.Buttonclick != null) button.Buttonclick();
        // A UniTask's Status can't be read after GetResult ("can not await twice", round 5): read it once, then the result.
        float t = 0f;
        Cysharp.Threading.Tasks.UniTaskStatus status = task.Status;
        while (status == Cysharp.Threading.Tasks.UniTaskStatus.Pending && t < 5f) { t += Time.unscaledDeltaTime; yield return null; status = task.Status; }
        string pick = status.ToString();
        if (status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded)
            try { pick = task.GetAwaiter().GetResult().Name; } catch (Exception e) { pick = "result threw " + e.Message; }
        RogueLabProbe.Check("M3 New / Continue / Back dialog on the main menu", cards == 3 && pick == "RogueLab_New", before + "; cards " + cards + ", pick " + pick);
        if (status == Cysharp.Threading.Tasks.UniTaskStatus.Pending)
            Blockage.Menu.Bl_Menus.Closemenu(dialog.gameObject, true);
        Blockage.Menu.Bl_Menus.Changemenu("Main");
        yield return new WaitForSecondsRealtime(1f);
    }

    // On the boat, once the host is up: M4's flag and M7's dump.
    public static void Boat()
    {
        RogueLabProbe.Check("M4 flag survives the embark to the boat", Flag, "scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        Blm_Multiplayer mp = Blm_Multiplayer.Instance;
        if (mp == null) { RogueLabProbe.Setup("M7 lobby data", "no Blm_Multiplayer"); return; }
        RogueLabProbe.Info("M7 lobby data", "Joinablescenes [" + string.Join(", ", mp.Joinablescenes.ToArray()) + "], Client_Canjoin " + RogueLabUtil.F(mp.Client_Canjoin)
            + ", Clientjoinscene " + mp.Clientjoinscene + ", Serverclosescene " + mp.Serverclosescene + ", Maxplayers " + mp.Maxplayers);
    }

    // M5, after the mode swap on the boat with no Cardinal: the Setup panel and what its type line says.
    public static void Setup()
    {
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        Blockage.Menu.Blu2_Menu_Handler setup;
        bool open = Blockage.Menu.Bl_Menus.Menus.TryGetValue("Setup", out setup) && setup != null && setup.Active;
        List<string> equipped = new List<string>();
        foreach (FixedString64Bytes n in info.Artifacts) equipped.Add(n.ToString());
        string line = "-";
        GameObject stats;
        Transform diff = setup != null && setup.Items.TryGetValue("Stats", out stats) && stats != null ? stats.transform.Find("Difficulty") : null;
        TMPro.TMP_Text text = diff != null ? diff.GetComponentInChildren<TMPro.TMP_Text>(true) : null;
        if (text != null) line = text.text;
        RogueLabProbe.Check("M5 boat takes the mod mode with no Cardinal", open && info.Mode.Value.ToString() == RogueLabData.Mode && !equipped.Contains(RogueLabData.Cardinal),
            "Setup open " + open + ", mode " + info.Mode.Value + ", Gameplan " + info.Gameplan.Value + ", equipped [" + string.Join(", ", equipped.ToArray()) + "], type line '" + line + "'");
    }

    static string Rect(RectTransform r)
    {
        if (r == null) return "-";
        return "(" + r.anchoredPosition.x.ToString("0", CultureInfo.InvariantCulture) + "," + r.anchoredPosition.y.ToString("0", CultureInfo.InvariantCulture) + " "
            + r.rect.width.ToString("0", CultureInfo.InvariantCulture) + "x" + r.rect.height.ToString("0", CultureInfo.InvariantCulture) + ")";
    }

    static string Layout(Transform parent)
    {
        if (parent == null) return "none";
        if (parent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() != null) return "vertical";
        if (parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null) return "horizontal";
        if (parent.GetComponent<UnityEngine.UI.GridLayoutGroup>() != null) return "grid";
        return "none (" + parent.name + ")";
    }
}
