using System;
using System.Collections.Generic;
using UnityEngine;

// Translating the pack's text (docs/21 §3.4). Read from the game's code on 2026-10-14 and compiled with cloud-check;
// NEVER RUN IN GAME. The game's English text is the key; the pack keeps its own table per language and writes the
// current language's rows into Bla_Localization.translations, which the game replaces on every language load.
public static class RecipeText
{
    // language name as the game spells it ("French", "German", "ChineseSimplified"…) -> English key -> text
    public static readonly Dictionary<string, Dictionary<string, string>> Table =
        new Dictionary<string, Dictionary<string, string>>();

    // Labels the pack set with Bla_Localization.SetText: set again after the rows go in, since the game refreshed its
    // labels before this hook ran.
    static readonly Dictionary<TMPro.TMP_Text, string> labels = new Dictionary<TMPro.TMP_Text, string>();

    static bool hooked;

    // Once, at boot, on every machine (each translates in its own language).
    public static void Install()
    {
        Apply();
        if (hooked)
            return;
        hooked = true;
        Blockage.Settings.Bl_Settings.Settingschanged += Apply;
    }

    public static void Add(string language, string english, string text)
    {
        Dictionary<string, string> rows;
        if (!Table.TryGetValue(language, out rows))
            Table[language] = rows = new Dictionary<string, string>();
        rows[english] = text;
    }

    // Runs after every settings change (Bl_Settings.cs:799-886); the language load itself happened just before.
    // Never throws: Settingschanged is invoked without a try.
    public static void Apply()
    {
        try
        {
            Dictionary<string, string> live = Bla_Localization.translations;
            if (live == null || Blockage.Settings.Bl_Settings.Toapplysettings == null)
                return;
            Dictionary<string, string> rows;
            if (!Table.TryGetValue(Blockage.Settings.Bl_Settings.Toapplysettings.Language ?? "", out rows))
                return;
            foreach (KeyValuePair<string, string> kv in rows)
                if (!live.ContainsKey(kv.Key))     // never overwrite a stock key's translation
                    live[kv.Key] = kv.Value;
            List<TMPro.TMP_Text> gone = new List<TMPro.TMP_Text>();
            foreach (KeyValuePair<TMPro.TMP_Text, string> kv in labels)
                if (kv.Key == null)
                    gone.Add(kv.Key);
                else
                    Bla_Localization.SetText(kv.Key, kv.Value);
            foreach (TMPro.TMP_Text t in gone)
                labels.Remove(t);
        }
        catch (Exception e) { Debug.LogWarning("[RW] translations: " + e.Message); }
    }

    // Sets a label from its English key and keeps it translated across language changes.
    public static void Set(TMPro.TMP_Text label, string english)
    {
        if (label == null)
            return;
        labels[label] = english;
        Bla_Localization.SetText(label, english);
    }

    // A translated string for code that builds text itself. Keys are fixed strings: dynamic text as a key gets
    // appended to the dev file once per variant (20 §3.1); put the number in with string.Format after the lookup.
    public static string T(string english) { return Bla_Localization.GetText(english); }
}
