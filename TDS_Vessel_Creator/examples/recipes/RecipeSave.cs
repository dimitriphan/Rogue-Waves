using System;
using System.Collections.Generic;
using Blockage.Tags;
using UnityEngine;

// The pack's corner of the save (docs/23 §2). Read from the game's code on 2026-10-14 and compiled with cloud-check;
// NEVER RUN IN GAME. One folder, prefixed keys, typed writes, a schema number, and state re-read on every slot change.
public static class RecipeSave
{
    public const string Folder = "RogueWaves";
    public const int Schema = 1;              // bump when a key changes meaning; migrate in Migrate()

    // A save from a newer build: read it, never write it (a downgrade must not clobber newer data).
    public static bool ReadOnly { get; private set; }

    static bool hooked;

    // The pack's state lives in statics; this rebuilds them from the save. Set it at boot.
    public static Action Reload;

    // Once, at boot, on every machine. Never keep a reference to Bla_Data.Playerdata: a slot switch replaces it.
    public static void Install()
    {
        Check();
        if (hooked || Bla_Data.Instance == null)
            return;
        hooked = true;
        // Invoked without a try (and Onsave at a match's end, before the end screen): a throw here breaks the game.
        Bla_Data.Instance.Onslotchanged += Safe;
        Bla_Data.Instance.Onslotremoved += Safe;
    }

    static void Safe()
    {
        try
        {
            Check();
            if (Reload != null)
                Reload();
        }
        catch (Exception e) { Debug.LogWarning("[RW] save reload: " + e.Message); }
    }

    // Reads the schema: missing = a new profile (write it), older = migrate, newer = read-only.
    static void Check()
    {
        ReadOnly = false;
        if (!Ready)
            return;
        Bl_Tags.Entry e = Bla_Data.Playerdata.Readdata(Folder, "RW_Schema");
        int found = e.Loaded ? (int)e.Value : 0;
        if (found > Schema)
        {
            ReadOnly = true;
            Debug.LogWarning("[RW] this save was written by a newer Rogue Waves (schema " + found + "): read only");
            return;
        }
        if (found < Schema)
        {
            Migrate(found);
            Bla_Data.Playerdata.Adddata(Folder, "RW_Schema", Schema, Bl_Tags.Addtype.Replace);
            Flush();
        }
    }

    static void Migrate(int from)
    {
        // from 0: nothing older exists. Each later schema adds a step: if (from < 2) { rename a key, convert a value }.
    }

    // An unreadable save leaves Loaded false and the game running on an empty profile that never saves (22 §1.4).
    public static bool Ready { get { return Bla_Data.Playerdata != null && Bla_Data.Playerdata.Loaded; } }

    // ---------------------------------------------------------------- typed reads and writes

    public static string ReadString(string key, string fallback)
    {
        if (!Ready)
            return fallback;
        Bl_Tags.Entry e = Bla_Data.Playerdata.Readdata(Folder, key);
        return e.Loaded && e.Valuestring != null ? e.Valuestring : fallback;
    }

    // Whole numbers above 16 777 216 (seeds, big counters) go through strings: a float loses digits.
    public static long ReadLong(string key, long fallback)
    {
        long v;
        return long.TryParse(ReadString(key, ""), out v) ? v : fallback;
    }

    public static float ReadFloat(string key, float fallback)
    {
        if (!Ready)
            return fallback;
        Bl_Tags.Entry e = Bla_Data.Playerdata.Readdata(Folder, key);
        return e.Loaded && !float.IsNaN(e.Value) && !float.IsInfinity(e.Value) ? e.Value : fallback;
    }

    // Keys start with RW_ (stock trackers search every folder) and never contain Unlock_Hero or Unlock_Artifact
    // unless they are that unlock (the boat opens a panel on the substring, 22 §3.2).
    static bool Writable(string key)
    {
        if (ReadOnly || !Ready || key == null || !key.StartsWith("RW_"))
            return false;
        return key.IndexOf("Unlock_Hero", StringComparison.Ordinal) < 0
            && key.IndexOf("Unlock_Artifact", StringComparison.Ordinal) < 0;
    }

    public static void WriteString(string key, string value)
    {
        if (Writable(key))
            Bla_Data.Playerdata.Adddata(Folder, key, value ?? "");
    }

    public static void WriteLong(string key, long value) { WriteString(key, value.ToString()); }

    // Always the typed overload: a two-argument Adddata is the float one with 0 and Replace.
    public static void WriteFloat(string key, float value)
    {
        if (Writable(key) && !float.IsNaN(value) && !float.IsInfinity(value))
            Bla_Data.Playerdata.Adddata(Folder, key, value, Bl_Tags.Addtype.Replace);
    }

    public static void Remove(string key) { if (Writable(key)) Bla_Data.Playerdata.Removedata(Folder, key); }

    // At state changes, never per frame: each save serializes the whole profile on the main thread. Not the auto
    // slot: "Restore auto save" rolls pack data back to the last stock autosave (22 §1.4).
    public static void Flush()
    {
        if (!ReadOnly && Ready && Bla_Data.Instance != null)
            Bla_Data.Instance.Savedata(false, "");
    }

    // "Forget Rogue Waves": the folder only. Stock keys that point at pack content are cleaned by their own guards
    // (22 §2.1: the saved world, vessel and Cardinal).
    public static void Forget()
    {
        if (!Ready)
            return;
        Bla_Data.Playerdata.Removefolder(Folder);
        ReadOnly = false;
        Flush();
        Check();
    }

    // ---------------------------------------------------------------- per player, across sessions (22 §2.3)

    // The host keeps one row per player keyed by the synced Steam name (RecipeNet.DurableName): the Sessionid lasts
    // one host session and changes when a friend restarts. Names are cleaned to letters and digits for the key.
    public static string PlayerKey(long session, string what)
    {
        string name = RecipeNet.DurableName(session);
        System.Text.StringBuilder b = new System.Text.StringBuilder("RW_P_");
        foreach (char c in name)
            if (char.IsLetterOrDigit(c))
                b.Append(c);
        return b.Append('_').Append(what).ToString();
    }
}
