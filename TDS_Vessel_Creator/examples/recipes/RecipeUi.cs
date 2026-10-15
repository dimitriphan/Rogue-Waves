using System;
using System.Collections;
using System.Collections.Generic;
using Blockage.Menu;
using UnityEngine;

// The choice dialog, a timed vote and a HUD line (docs/20 §2, §5). Read from the game's code on 2026-10-14 and
// compiled with cloud-check; NEVER RUN IN GAME. The command half of a vote (the host writes the offers into a None
// command on each vessel, the pick returns through Command_ServerRpc) is proven in Riptide:
// TDS_Mod/ModSource/Riptide/Code/06_Salvage.cs.
public static class RecipeUi
{
    // ---------------------------------------------------------------- the choice dialog

    public static bool DialogUp()
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        Blu2_Menu_Handler menu;
        return dialog != null && Bl_Menus.Menus != null && Bl_Menus.Menus.TryGetValue(dialog.gameObject.name, out menu)
            && menu != null && menu.Active;
    }

    // One card. Tomenu stays "" (anything else opens that menu after the pick); a default Display_Color takes the
    // dialog's own colour for that slot.
    public static Bla_Ui_Confirmation.Entry Card(string name, string title, string desc, Sprite image)
    {
        Bla_Ui_Confirmation.Entry card = new Bla_Ui_Confirmation.Entry();
        card.Name = name;
        card.Display_Name = title;
        card.Display_Desc = desc;
        card.Display_Image = image;
        return card;
    }

    static int asks;

    // Every machine, as a coroutine on the pack's MonoBehaviour. Opens the dialog's menu itself with Force (Runprompt's
    // own Changemenu isn't forced and is dropped during a transition, Bla_Ui_Confirmation.cs:118-121), waits until it
    // is up (Runprompt reads the parent menu and throws while it is inactive, :190), then asks without forcing.
    // done(name) gets the picked card's Name, or null when the time ran out, the menu closed or a newer Prompt took
    // over. The game's task has no timeout or cancel: closing the dialog leaves it pending for good, which is fine.
    public static IEnumerator Prompt(List<Bla_Ui_Confirmation.Entry> cards, string title, string sub, float seconds,
        Action<string> done)
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        int ask = ++asks;
        if (dialog == null || cards == null || cards.Count == 0)
        {
            done(null);
            yield break;
        }
        if (!DialogUp())
            Bl_Menus.Changemenu(dialog.gameObject.name, true);
        float wait = Time.unscaledTime + 5f;
        while (!DialogUp() && Time.unscaledTime < wait)
            yield return null;
        if (!DialogUp() || ask != asks)
        {
            done(null);
            yield break;
        }
        Cysharp.Threading.Tasks.UniTask<Bla_Ui_Confirmation.Entry> task;
        try { task = dialog.Runprompt(cards, title, sub, false); }
        catch (Exception e)
        {
            Debug.LogWarning("[RW] Runprompt: " + e.Message);
            done(null);
            yield break;
        }
        // Unscaled: the dialog doesn't pause the game online (19 §6 rule 8), and a solo pause must not stretch a vote.
        float end = Time.unscaledTime + seconds;
        while (task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Pending && ask == asks && DialogUp()
            && Time.unscaledTime < end)
            yield return null;
        if (task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded)
        {
            Bla_Ui_Confirmation.Entry picked = task.GetAwaiter().GetResult();
            done(picked != null ? picked.Name : null);
            yield break;
        }
        if (ask == asks && DialogUp())
            Bl_Menus.Closemenu(dialog.gameObject, true);
        done(null);
    }

    // A newer vote round, or the host closing the vote: the running Prompt gives up and closes the dialog.
    public static void Cancel()
    {
        asks++;
        if (DialogUp())
            Bl_Menus.Closemenu(Bla_Ui_Confirmation.Instance.gameObject, true);
    }

    // ---------------------------------------------------------------- the host's side of a timed vote

    // Picks per Sessionid, from each vessel's command (identify the voter by the vessel's Owner, never by the Json).
    public static readonly Dictionary<long, string> Votes = new Dictionary<long, string>();

    // The winner once everyone voted or the time ran out; ties and no votes go to the first option (the leader's
    // fallback). Players who left are not waited for: count only Sessionids still in Blm_Server.
    public static string Tally(List<string> options, float deadline, out bool closed)
    {
        int present = 0, voted = 0;
        if (Blm_Server.Instance != null)
            foreach (long s in Blm_Server.Instance.Sessionidsplayer.Keys)
            {
                present++;
                if (Votes.ContainsKey(s))
                    voted++;
            }
        closed = voted >= present || RecipeNet.Now >= deadline;
        string best = options.Count > 0 ? options[0] : "";
        int bestCount = 0;
        foreach (string o in options)
        {
            int n = 0;
            foreach (string v in Votes.Values)
                if (v == o)
                    n++;
            if (n > bestCount)
            {
                best = o;
                bestCount = n;
            }
        }
        return best;
    }

    // ---------------------------------------------------------------- a line under the Tide counter

    static TMPro.TMP_Text runLine;

    // Every machine, in the map scene, re-run each map (the HUD is rebuilt per scene). A copy of the stock Tide label
    // keeps its font, size and material; raycasts off so it never eats a click. Untested: the copy may carry the stock
    // label's own components (localization, tooltip), which is why it is renamed and its text set every refresh.
    public static void ShowRunLine(string text)
    {
        if (runLine == null)
        {
            Blu2_Menu_Handler main;
            GameObject wave;
            if (Bl_Menus.Menus == null || !Bl_Menus.Menus.TryGetValue("Main", out main) || main == null
                || main.Items == null || !main.Items.TryGetValue("Wave", out wave) || wave == null)
                return;
            GameObject copy = UnityEngine.Object.Instantiate(wave, wave.transform.parent);
            copy.name = "RW_RunLine";
            runLine = copy.GetComponentInChildren<TMPro.TMP_Text>();
            if (runLine == null)
            {
                UnityEngine.Object.Destroy(copy);
                return;
            }
            runLine.raycastTarget = false;
            RectTransform r = copy.transform as RectTransform;
            RectTransform w = wave.transform as RectTransform;
            if (r != null && w != null)
                r.anchoredPosition = w.anchoredPosition + new Vector2(0f, -w.rect.height);
        }
        if (runLine.text != text)
            runLine.text = text;
    }

    // ---------------------------------------------------------------- the run line's data on the client

    // The last entry of a command on this player's own vessel (Updatecommand moves an updated entry to the end).
    public static bool MyCommand(string link, out Link_Commandref found)
    {
        found = default(Link_Commandref);
        if (Game_Entity.Instance == null || Blm_Server.Instance == null || Blm_Server.Instance.Localplayer == null)
            return false;
        long me = Blm_Server.Instance.Localplayer.Sessionid.Value;
        bool any = false;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
        {
            if (e == null || e.Commandlist == null || e.Entitydata.Value.Owner != me)
                continue;
            foreach (Link_Commandref c in e.Commandlist)
                if (c.Name.ToString() == link)
                {
                    found = c;
                    any = true;
                }
            if (any)
                return true;
        }
        return false;
    }
}
