using System;
using System.Collections.Generic;
using Blockage.Sound;
using UnityEngine;

// The game's sound table, stock music changes and the pack's own music (docs/21 §1.4, §2). Read from the game's code
// on 2026-10-14 and compiled with cloud-check; NEVER RUN IN GAME. The player (Bl_Sound) is internal: everything here
// goes through public data (Bla_Audio.Instance.Database), the public play helpers and Bla_Ambient.
public static class RecipeSound
{
    // ---------------------------------------------------------------- the table (read only)

    // The entry for a stock key, or null. The table never takes a new key (Bl_Sound builds its own copy once, before
    // mods load).
    public static Soundpair Pair(string key)
    {
        if (string.IsNullOrEmpty(key) || Bla_Audio.Instance == null || Bla_Audio.Instance.Database == null)
            return null;
        foreach (Bl_Sound_Group g in Bla_Audio.Instance.Database.Items)
            if (g != null && g.Sounds != null)
                foreach (Soundpair p in g.Sounds)
                    if (p != null && p.key == key)
                        return p;
        return null;
    }

    // Check every key the pack writes (Sfxstart/Sfxend, projectile sounds, a soundtrack group, a world's sound) at
    // boot: an unknown, empty or null key throws wherever the game plays it (Bl_Sound.cs:124-138).
    public static bool Exists(string key) { return Pair(key) != null; }

    public static List<string> Missing(IEnumerable<string> keys)
    {
        List<string> missing = new List<string>();
        foreach (string k in keys)
            if (!Exists(k))
                missing.Add(k ?? "(null)");
        return missing;
    }

    // The mixer group a stock key plays on (its own, else its table group's default): a pack AudioSource on it obeys
    // the volume sliders and the break muffle. A source with no group ignores the sliders (21 §2.3).
    public static UnityEngine.Audio.AudioMixerGroup Group(string stockKey)
    {
        if (Bla_Audio.Instance == null || Bla_Audio.Instance.Database == null)
            return null;
        foreach (Bl_Sound_Group g in Bla_Audio.Instance.Database.Items)
            if (g != null && g.Sounds != null)
                foreach (Soundpair p in g.Sounds)
                    if (p != null && p.key == stockKey)
                        return p.group != null ? p.group : g.Defaultgroup;
        return null;
    }

    // ---------------------------------------------------------------- stock sounds

    // This machine only.
    public static void Stock(string key) { if (Exists(key)) Helper_Vfxtools.Playsoundui(key); }

    public static void StockAt(Vector3 at, string key) { if (Exists(key)) Helper_Vfxtools.Playsound(at, key); }

    // Host: every machine plays it (not throttled, 21 §2.2). A late joiner misses it.
    public static void StockForAll(string key)
    {
        if (Exists(key) && Tds_Multiplayer_Radio.Instance != null && RecipeNet.IsServer)
            Tds_Multiplayer_Radio.Instance.Sfxui_ClientRpc(key);
    }

    // ---------------------------------------------------------------- music on every client

    static GameObject current;

    // Crossfades the match music to another stock key (5 s), on this machine. Call it on every client from a synced
    // trigger (a stat, a command's Goalstring) or a Tell (RecipeNet). Bla_Ambient plays its first track on the tag
    // "Gamemusic" with Override in Start, looped (Bla_Ambient.cs). The helper must live while its track plays:
    // disabling or destroying a Bla_Ambient stops its whole tag over 5 s, whoever started the track. So one helper at
    // a time, replaced here; the scene's end destroys it with the music.
    public static void Music(string stockKey)
    {
        if (!Exists(stockKey))
            return;
        if (current != null)
            UnityEngine.Object.Destroy(current);   // stops at the end of this frame; the new one starts next frame
        GameObject go = new GameObject("RW_Music_" + stockKey);
        go.SetActive(false);                       // fields first, then Start
        Bla_Ambient a = go.AddComponent<Bla_Ambient>();
        a.Tracks = new List<string> { stockKey };  // one track: two or more cycle every Duration seconds
        a.Oneshots = new List<string>();           // Update reads it every frame: null throws
        a.Groupname = "Gamemusic";
        a.Pan = false;                             // spread 180: heard the same everywhere
        a.Falloff = 9999f;
        a.Falloffmax = 9999f;
        go.SetActive(true);
        current = go;
    }

    // Stops the match music on this machine (5 s fade), e.g. before the pack's own track: a Bla_Ambient on the tag
    // with no track, turned off and dropped (its OnDisable stops the tag once more at the end of the frame).
    public static void StopMatchMusic()
    {
        if (current != null)
            UnityEngine.Object.Destroy(current);
        current = null;
        GameObject go = new GameObject("RW_MusicOff");
        go.SetActive(false);
        Bla_Ambient a = go.AddComponent<Bla_Ambient>();
        a.Tracks = new List<string>();
        a.Oneshots = new List<string>();
        a.Groupname = "Gamemusic";
        go.SetActive(true);
        a.Turnoff();
        UnityEngine.Object.Destroy(go);
    }

    // ---------------------------------------------------------------- the pack's own clip

    // A looping 2D source on the music group (a stock music key's group), fading in over `fade` seconds. Leave the
    // mode's Musicwave params out so stock stage changes don't cut in, and stop it on Gamecomplete and scene end.
    public static AudioSource OwnMusic(AudioClip clip, string stockMusicKey, float volume, float fade)
    {
        if (clip == null)
            return null;
        StopMatchMusic();
        GameObject go = new GameObject("RW_OwnMusic");
        AudioSource s = go.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = true;
        s.spatialBlend = 0f;
        s.outputAudioMixerGroup = Group(stockMusicKey);
        s.volume = 0f;
        s.Play();
        RecipeSoundFader f = go.AddComponent<RecipeSoundFader>();
        f.Target = volume;
        f.Speed = fade > 0f ? volume / fade : 1000f;
        return s;
    }

    public static void FadeOut(AudioSource s, float fade)
    {
        if (s == null)
            return;
        RecipeSoundFader f = s.GetComponent<RecipeSoundFader>();
        if (f == null)
            f = s.gameObject.AddComponent<RecipeSoundFader>();
        f.Target = 0f;
        f.Speed = fade > 0f ? Mathf.Max(0.001f, s.volume) / fade : 1000f;
        f.DestroyAtZero = true;
    }
}

// Unscaled, so a solo pause doesn't hold a fade.
public class RecipeSoundFader : MonoBehaviour
{
    public float Target, Speed = 1f;
    public bool DestroyAtZero;
    AudioSource source;

    void Update()
    {
        if (source == null)
            source = GetComponent<AudioSource>();
        if (source == null)
            return;
        source.volume = Mathf.MoveTowards(source.volume, Target, Speed * Time.unscaledDeltaTime);
        if (DestroyAtZero && source.volume <= 0f)
            Destroy(gameObject);
    }
}
