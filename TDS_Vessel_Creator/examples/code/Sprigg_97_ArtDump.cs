#if SPRIGG_ARTDUMP || SPRIGG_SHOTS
// Debug build only (Build-SpriggMod.ps1 -Define SPRIGG_ARTDUMP; SPRIGG_SHOTS alone just provides Shot() to the self-test). Writes reference art to Player.log so new art can be
// matched to the shipped vessels: every vessel's boat Worldsprite (as a 6-column atlas), menu art, and Sno Sno's full
// sprite sheets. Then it embarks, opens the boat's vessel select and captures TDS's own frame (never the desktop).
// Each image is logged as "[SpriggDump] <name> <w>x<h> <part>/<parts> <base64 RGBA, bottom row first>" lines;
// ModSource/Sprigg/Tools/Decode-Dump.ps1 turns them back into PNGs.
using UnityEngine.SceneManagement;

public static class SpriggArtDump
{
    const int Chunk = 8000;

    static void W(string line) { Debug.Log("[SpriggDump] INFO " + line); }

    static void Emit(string name, int w, int h, Color32[] px)
    {
        byte[] bytes = new byte[w * h * 4];
        for (int i = 0; i < px.Length && i < w * h; i++)
        {
            bytes[i * 4] = px[i].r; bytes[i * 4 + 1] = px[i].g; bytes[i * 4 + 2] = px[i].b; bytes[i * 4 + 3] = px[i].a;
        }
        string b64 = Convert.ToBase64String(bytes);
        int parts = (b64.Length + Chunk - 1) / Chunk;
        for (int p = 0; p < parts; p++)
            Debug.Log("[SpriggDump] " + name + " " + w + "x" + h + " " + (p + 1) + "/" + parts + " " + b64.Substring(p * Chunk, Math.Min(Chunk, b64.Length - p * Chunk)));
    }

    // Reads any texture (readable or not) through the GPU. slice >= 0 picks a Texture2DArray slice.
    static Color32[] Read(Texture t, int slice)
    {
        int w = t.width, h = t.height;
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        if (slice >= 0) Graphics.Blit(t, rt, slice, 0);
        else Graphics.Blit(t, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        Color32[] px = tex.GetPixels32();
        UnityEngine.Object.Destroy(tex);
        return px;
    }

    static void Tex(string name, Texture t)
    {
        if (t == null) { W(name + " = none"); return; }
        W(name + " = " + t.name + " " + t.width + "x" + t.height + " filter " + t.filterMode + " " + t.GetType().Name);
        Emit(name, t.width, t.height, Read(t, -1));
    }

    static void DumpArray(string name, Texture2DArray a)
    {
        if (a == null) { W(name + " = none"); return; }
        W(name + " = " + a.name + " " + a.width + "x" + a.height + "x" + a.depth + " format " + a.format + " graphics " + a.graphicsFormat
            + " mips " + a.mipmapCount + " filter " + a.filterMode + " wrap " + a.wrapMode + " readable " + a.isReadable);
        int cols = 6, rows = (a.depth + cols - 1) / cols, cw = a.width, ch = a.height;
        int aw = cols * cw, ah = rows * ch;
        Color32[] atlas = new Color32[aw * ah];
        for (int i = 0; i < a.depth; i++)
        {
            Color32[] s = Read(a, i);
            int col = i % cols, row = i / cols;
            int ox = col * cw, oy = (rows - 1 - row) * ch;
            for (int y = 0; y < ch; y++)
                for (int x = 0; x < cw; x++)
                    atlas[(oy + y) * aw + ox + x] = s[y * cw + x];
        }
        Emit(name, aw, ah, atlas);
    }

    public static void DumpSprite(string name, Sprite s)
    {
        if (s == null) { W(name + " = none"); return; }
        W(name + " = " + s.name + " rect " + s.rect + " ppu " + s.pixelsPerUnit + " pivot " + s.pivot + " tex " + s.texture.name + " " + s.texture.width + "x" + s.texture.height);
        Emit(name, s.texture.width, s.texture.height, Read(s.texture, -1));
    }

    static void DumpSpriteobject(string key, Spriteobject so)
    {
        if (so == null || so.Itemslist == null) { W(key + " spriteobject = none"); return; }
        W(key + " spriteobject " + so.name + " speedmulti " + so.Defaultspeedmulti + " folder " + so.Defaultfolder);
        List<Texture2D> sheets = new List<Texture2D>();
        foreach (Spritesinlist a in so.Itemslist)
        {
            int variants = a.Sprites != null ? a.Sprites.Count : 0;
            int frames = variants > 0 && a.Sprites[0].Variant != null ? a.Sprites[0].Variant.Count : 0;
            List<string> detail = new List<string>();
            if (variants > 0)
                for (int f = 0; f < frames; f++)
                {
                    Spritecont c = a.Sprites[0].Variant[f];
                    detail.Add((c.Item != null ? c.Item.name + "@" + c.Item.rect.x + "," + c.Item.rect.y : "null") + (string.IsNullOrEmpty(c.Sound) ? "" : "+" + c.Sound));
                }
            W(key + " anim '" + a.Name + "' variants " + variants + " frames " + frames + " loop " + a.Loop + " reset " + a.Reset + " prio " + a.Priority
                + " speed " + a.Speed + " to '" + a.Spriteto + "' v0: " + string.Join(" ", detail.ToArray()));
            if (a.Sprites == null) continue;
            foreach (Spritevariantlist v in a.Sprites)
                if (v.Variant != null)
                    foreach (Spritecont c in v.Variant)
                        if (c.Item != null && !sheets.Contains(c.Item.texture))
                            sheets.Add(c.Item.texture);
        }
        foreach (Texture2D t in sheets)
            Tex(key + "_" + t.name, t);
    }

    // Loudness of a clip's samples (peak and RMS, 0..1), or "-" when the clip can't be read.
    static string Level(AudioClip c)
    {
        if (c == null) return "null";
        try
        {
            float[] d = new float[c.samples * c.channels];
            if (!c.GetData(d, 0)) return c.name + " (" + c.loadType + ", unreadable)";
            double sum = 0; float peak = 0f;
            for (int i = 0; i < d.Length; i++) { float a = Math.Abs(d[i]); if (a > peak) peak = a; sum += d[i] * d[i]; }
            double rms = Math.Sqrt(sum / Math.Max(1, d.Length));
            return c.name + " len " + c.length.ToString("0.00") + " peak " + peak.ToString("0.000") + " rms " + rms.ToString("0.0000");
        }
        catch (Exception e) { return c.name + " (" + e.Message + ")"; }
    }

    static void AudioSurvey()
    {
        Blockage.Sound.Bla_Audio audio = Blockage.Sound.Bla_Audio.Instance;
        if (audio == null || audio.Database == null) { W("audio: no database"); return; }
        W("audio mindist " + audio.Mindist + " maxdist " + audio.Maxdist + " mixer " + (audio.Mixer != null ? audio.Mixer.name : "-"));
        if (audio.Mixer != null)
            foreach (AudioMixerGroup g in audio.Mixer.FindMatchingGroups(""))
                W("audio mixergroup " + g.name);
        foreach (Blockage.Sound.Bl_Sound_Group group in audio.Database.Items)
        {
            if (group == null) continue;
            W("audio GROUP " + group.name + " default " + (group.Defaultgroup != null ? group.Defaultgroup.name : "-") + " sounds " + (group.Sounds != null ? group.Sounds.Count : 0));
            if (group.Sounds == null) continue;
            foreach (Blockage.Sound.Soundpair p in group.Sounds)
            {
                if (p == null) continue;
                List<string> clips = new List<string>();
                if (p.val != null)
                    foreach (UnityEngine.Object o in p.val)
                        clips.Add(o is AudioClip ? Level((AudioClip)o) : (o != null ? o.GetType().Name + ":" + o.name : "null"));
                W("audio SOUND " + p.key + " | mixer " + (p.group != null ? p.group.name : "-") + " | volume " + p.Volume + " | pitchvar " + p.Pitchvar + " | " + string.Join(" ; ", clips.ToArray()));
            }
        }
        foreach (Transform t in SpriggAssets.Library.Find("Audio"))
            W("audio SPRIGG " + Level(SpriggAssets.Clip(t.name)));
    }

    public static IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(3f);
        try { AudioSurvey(); }
        catch (Exception e) { W("audio survey failed: " + e); }
#if SPRIGG_AUDIOONLY
        W("DONE");
        yield break;
#endif
        W("BEGIN");
        Dictionary<string, UnityEngine.Object> heroes = SpriggUtil.Folder("Hero");
        List<Heroentryobject> entries = new List<Heroentryobject>();
        foreach (UnityEngine.Object o in heroes.Values)
            if (o is Heroentryobject) entries.Add((Heroentryobject)o);
        foreach (Heroentryobject h in entries)
        {
            string k = h.Objectname;
            W(k + " idlesize " + h.Idlesize + " landingsize " + h.Landingsize + " loadingsize " + h.Loadingsize + " color " + h.Color);
            try
            {
                DumpArray(k + "_Worldsprite", h.Worldsprite);
                DumpSprite(k + "_Icon", h.Icon);
                DumpSprite(k + "_Worldsplash", h.Worldsplash);
                Tex(k + "_Idlesprite", h.Idlesprite);
                Tex(k + "_Landingsprite", h.Landingsprite);
                Tex(k + "_Loadingsprite", h.Loadingsprite);
            }
            catch (Exception e) { W(k + " entry dump failed: " + e); }
            yield return null;
        }

        foreach (string want in new[] { "Hero_Snosno", "Hero_Buster" })
        {
            Heroentryobject h = entries.Find(x => x.Objectname == want);
            if (h == null || h.Heroloads == null) { W(want + " not found"); continue; }
            Bl_Address.Loadgroup g = null;
            try { g = Bl_Address.Load(h.Heroloads.Objectname); }
            catch (Exception e) { W(want + " load failed: " + e.Message); }
            float waited = 0f;
            while (g != null && !g.Loaded && waited < 180f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (g == null || !g.Loaded) { W(want + " group not loaded"); continue; }
            try
            {
                foreach (Bl_Address.Loadlist list in g.Items)
                    foreach (KeyValuePair<string, UnityEngine.Object> kv in list.Items)
                    {
                        Beingobject b = kv.Value as Beingobject;
                        if (b == null || b.Objectname != want || b.Entity == null || b.Entity.Defaultvisual == null) continue;
                        DumpSpriteobject(want, b.Entity.Defaultvisual.Default.Spriteobject);
                        Visualproperties vp = b.Entity.Defaultvisual;
                        if (vp.Skinoverride != null)
                            foreach (Visualproperties.Param p in vp.Skinoverride)
                                W(want + " skin '" + p.Name + "' spriteobject " + (p.Value.Spriteobject != null ? p.Value.Spriteobject.name : "-"));
                    }
            }
            catch (Exception e) { W(want + " sprite dump failed: " + e); }
            yield return null;
        }
        W("ART DONE");
        SpriggMod.Instance.StartCoroutine(Boat());
    }

    // ---------------------------------------------------------------- the boat, captured from TDS's own frame

    public static IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        int w = Screen.width, h = Screen.height;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        int k = Mathf.Max(1, Mathf.CeilToInt(w / 960f));
        int ow = w / k, oh = h / k;
        Color32[] src = tex.GetPixels32();
        UnityEngine.Object.Destroy(tex);
        Color32[] dst = new Color32[ow * oh];
        for (int y = 0; y < oh; y++)
            for (int x = 0; x < ow; x++)
                dst[y * ow + x] = src[(y * k) * w + x * k];
        Emit("shot_" + name, ow, oh, dst);
        W("captured " + name);
    }

    static void Buttons(string when)
    {
        foreach (Macro_Characterselect_Click b in UnityEngine.Object.FindObjectsOfType<Macro_Characterselect_Click>())
        {
            List<string> comps = new List<string>();
            foreach (Component c in b.GetComponents<Component>()) comps.Add(c != null ? c.GetType().Name : "null");
            W(when + " button " + b.Character + " locked " + b.Locked + " at " + b.transform.position + " parent " + (b.transform.parent != null ? b.transform.parent.name : "-")
                + " components " + string.Join(",", comps.ToArray()));
        }
    }

    static IEnumerator Boat()
    {
        float t = 0f;
        while (UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() == null && t < 60f) { t += Time.unscaledDeltaTime; yield return null; }
        yield return new WaitForSecondsRealtime(4f);
        Mainmenu_Main menu = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (menu == null) { W("no main menu"); yield break; }
        yield return Shot("mainmenu");
        W("embarking");
        menu.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
        t = 0f;
        while ((Macro_Worldvisual.Instance == null || Macro_Worldvisual.Instance.Boat == null || Macro_Main.Instance == null || !Macro_Main.Instance.IsSpawned) && t < 90f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (Macro_Worldvisual.Instance == null) { W("no world map"); yield break; }
        W("in " + SceneManager.GetActiveScene().name);
        yield return new WaitForSecondsRealtime(8f);
        yield return Shot("setup");
        Macro_Worldvisual.Instance.Opencharselect();
        yield return new WaitForSecondsRealtime(3f);
        Buttons("open");
        yield return Shot("charselect");
        foreach (Macro_Characterselect_Click b in UnityEngine.Object.FindObjectsOfType<Macro_Characterselect_Click>())
            if (b.Character == SpriggData.Hero)
            {
                b.Locked = false;
                b.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
                W("re-ran Start on Sprigg's button with Locked false");
            }
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Shot("charselect_unlocked");
        Macro_Main.Instance.Selectcharacter(SpriggData.Hero);
        yield return new WaitForSecondsRealtime(2f);
        yield return Shot("charselect_selected");
        Macro_Worldvisual.Instance.Closecharselect();
        yield return new WaitForSecondsRealtime(3f);
        yield return Shot("lobby");
        W("DONE");
    }
}
#endif
