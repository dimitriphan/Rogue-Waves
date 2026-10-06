#if SPRIGG_SURVEY
// Debug build only (Build-SpriggMod.ps1 -Define SPRIGG_SURVEY): loads every vessel's asset group at the main menu and
// writes its data (beings, AIs, stats, upgrade trees and params, commands, projectiles, visuals) to Player.log, one
// "[SpriggSurvey]" line per fact, so the base vessels can be studied before designing a new one.
public static class SpriggSurvey
{
    static readonly List<UnityEngine.Object> done = new List<UnityEngine.Object>();
    static readonly Dictionary<Bl_Address.Loadlist, string> labels = new Dictionary<Bl_Address.Loadlist, string>();

    static void W(string line) { Debug.Log("[SpriggSurvey] " + line.Replace("\r", "").Replace("\n", "\\n")); }

    static string N(UnityEngine.Object o) { return o != null ? o.name : "-"; }

    static string Names<T>(IEnumerable<T> items) where T : UnityEngine.Object
    {
        List<string> n = new List<string>();
        if (items != null)
            foreach (T o in items) n.Add(N(o));
        return "[" + string.Join(", ", n.ToArray()) + "]";
    }

    static string Costs(List<Link_Resourceset> costs)
    {
        List<string> n = new List<string>();
        if (costs != null)
            foreach (Link_Resourceset c in costs) n.Add(c.Name + " " + c.Amount);
        return "[" + string.Join(", ", n.ToArray()) + "]";
    }

    static string Params(Dictionary<string, float> d)
    {
        List<string> n = new List<string>();
        if (d != null)
            foreach (KeyValuePair<string, float> kv in d) n.Add(kv.Key + "=" + kv.Value);
        return "{" + string.Join("; ", n.ToArray()) + "}";
    }

    static string Strings(IEnumerable<string> items)
    {
        return items != null ? "[" + string.Join(", ", new List<string>(items).ToArray()) + "]" : "[]";
    }

    public static IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(2f);
        W("BEGIN");
        Dictionary<string, UnityEngine.Object> heroes = SpriggUtil.Folder("Hero");
        foreach (KeyValuePair<string, Dictionary<string, UnityEngine.Object>> f in Tds_Masterdatabase.Instance.Items)
            W("FOLDER " + f.Key + " (" + f.Value.Count + "): " + Strings(f.Value.Keys));
        List<Heroentryobject> entries = new List<Heroentryobject>();
        foreach (UnityEngine.Object o in heroes.Values)
            if (o is Heroentryobject && ((Heroentryobject)o).Objectname != SpriggData.Hero)
                entries.Add((Heroentryobject)o);
        foreach (Heroentryobject h in entries)
        {
            W("");
            W("HERO " + h.Objectname + " | name '" + h.Displayname + "' | sub '" + h.Displaysub + "' | skill '" + h.Displayskill + "' | towers " + Strings(h.Towerdisplay)
                + " | color " + h.Color + " | loads " + N(h.Heroloads) + " | loadingsize " + h.Loadingsize + " landingsize " + h.Landingsize + " idlesize " + h.Idlesize);
            if (h.Log != null)
                W("  LOG title '" + h.Log.Title + "' desc '" + h.Log.Maindesc + "'");
            Bl_Address.Loadgroup g = null;
            try { g = Bl_Address.Load(h.Heroloads.Objectname); }
            catch (Exception e) { W("  load failed: " + e.Message); }
            float waited = 0f;
            while (g != null && !g.Loaded && waited < 180f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (g == null || !g.Loaded)
            {
                W("  GROUP NOT LOADED");
                continue;
            }
            Relabel();
            try { DumpGroup(g); }
            catch (Exception e) { W("  dump failed: " + e); }
            yield return null;
        }
        W("");
        W("ALL COMMANDS");
        Relabel();
        foreach (KeyValuePair<Bl_Address.Loadlist, string> kv in labels)
            foreach (UnityEngine.Object o in kv.Key.Items.Values)
                if (o is Commandobject && !done.Contains(o))
                    Command((Commandobject)o, "  ");
        W("END");
    }

    static void Relabel()
    {
        labels.Clear();
        foreach (KeyValuePair<string, List<Bl_Address.Loadlist>> kv in Bl_Address.Groups)
            foreach (Bl_Address.Loadlist l in kv.Value)
                if (l != null && !labels.ContainsKey(l))
                    labels[l] = kv.Key;
    }

    static void DumpGroup(Bl_Address.Loadgroup g)
    {
        foreach (Bl_Address.Loadlist list in g.Items)
        {
            string label;
            labels.TryGetValue(list, out label);
            W("  LIST " + (label ?? "?") + " (" + list.Items.Count + ")");
            foreach (KeyValuePair<string, UnityEngine.Object> kv in list.Items)
            {
                UnityEngine.Object o = kv.Value;
                if (o == null || done.Contains(o))
                    continue;
                if (o is Beingobject) Being((Beingobject)o, "    ");
                else if (o is Upgradeproperties) Upgrade((Upgradeproperties)o, "    ");
                else if (o is Commandobject) Command((Commandobject)o, "    ");
                else if (o is Projectileobject) Projectile((Projectileobject)o, "    ");
                else if (o is Visualproperties) Visual((Visualproperties)o, "    ");
                else if (o is Statproperties) Stats((Statproperties)o, "    ");
                else if (!(o is Entityobject)) W("    ITEM " + kv.Key + " : " + o.GetType().Name);
            }
        }
    }

    static void Being(Beingobject b, string p)
    {
        done.Add(b);
        Entityobject e = b.Entity;
        W(p + "BEING " + b.Objectname + " | block " + N(b.Block) + " | notes '" + b.Notes + "'");
        if (e == null)
            return;
        done.Add(e);
        W(p + "  ENTITY " + e.Objectname + " | name '" + e.Name + "' | display '" + e.Display_Name + "' | desc '" + e.Display_Desc + "'");
        W(p + "  tags " + Strings(e.Tags) + " | ais " + Names(e.Aiscripts) + " | visscripts " + Names(e.Visualscripts));
        W(p + "  costs " + Costs(e.Costs) + " | drops " + Costs(e.Drops) + " | sellable " + e.Sellable + " | controllable " + e.Controllable
            + " | orientable " + e.Orientable + " | targetable " + e.Targetable + " | level " + e.Defaultlevel + " | selprio " + e.Selectionpriority + " | hit " + N(e.Defaulthit));
        W(p + "  upgrades " + Names(e.Upgrades) + " | upgradesinit " + Names(e.Upgradesinit));
        if (e.Defaultstat != null) Stats(e.Defaultstat, p + "  ");
        if (e.Defaultvisual != null) Visual(e.Defaultvisual, p + "  ");
        if (e.Upgrades != null)
            foreach (Upgradeproperties u in e.Upgrades) Tree(u, p + "  ");
        if (e.Upgradesinit != null)
            foreach (Upgradeproperties u in e.Upgradesinit) Tree(u, p + "  ");
    }

    static void Tree(Upgradeproperties u, string p)
    {
        if (u == null || done.Contains(u))
            return;
        Upgrade(u, p);
        if (u.Upgrades != null)
            foreach (Upgradeproperties c in u.Upgrades) Tree(c, p + "  ");
    }

    static void Upgrade(Upgradeproperties u, string p)
    {
        done.Add(u);
        W(p + "UPGRADE " + u.Objectname + " | tier " + u.Priority + " | '" + u.Display_Name + "' | desc '" + u.Display_Desc + "' | costs " + Costs(u.Costs)
            + " | params " + Params(u.Defaultparams) + " | ais " + Names(u.Aiscripts) + " | vis " + N(u.Ent_Visual) + " | next " + Names(u.Upgrades)
            + (string.IsNullOrEmpty(u.Notes) ? "" : " | notes '" + u.Notes + "'"));
    }

    static void Command(Commandobject c, string p)
    {
        done.Add(c);
        List<string> f = new List<string>();
        if (c.Commandfuncts != null)
            foreach (Entity_Command_Base b in c.Commandfuncts) f.Add(b != null ? b.GetType().Name : "-");
        W(p + "COMMAND " + c.Objectname + " | style " + c.Style + " | '" + c.Display_Name + "' | desc '" + c.Display_Desc + "' | icon '" + c.Icon + "' | keys " + Strings(c.Keybind)
            + " | keystate '" + c.Keybindstate + "' | prio " + c.Priority + " | funcs " + Strings(f) + " | ui " + N(c.Customui));
    }

    static void Projectile(Projectileobject pr, string p)
    {
        done.Add(pr);
        W(p + "PROJECTILE " + pr.Objectname + " | vis " + N(pr.Vis) + " | hit " + N(pr.Hit) + " | vfx " + N(pr.Vfxstart) + "/" + N(pr.Vfxend) + " | sfx '" + pr.Sfxstart + "'/'" + pr.Sfxend
            + "' | ray " + pr.Raytype + " r " + pr.Radius + " | replicate " + pr.Clientreplicate + " | controllers " + Names(pr.Controllers) + " | params " + Params(pr.Defaultparams));
    }

    static void Stats(Statproperties s, string p)
    {
        done.Add(s);
        List<string> n = new List<string>();
        foreach (Statproperties.Defaultstatgroup g in s.Stats)
            n.Add(g.Name + "=" + g.Value + (g.Leveladd != 0 ? "(+" + g.Leveladd + "/lvl)" : "") + (string.IsNullOrEmpty(g.Value_String) ? "" : "'" + g.Value_String + "'")
                + " " + g.Style + "/" + g.Display_Type + (g.Display_Exposed ? "" : "/hidden"));
        W(p + "STATS " + s.Objectname + " " + Strings(n));
    }

    static string Entry(Visualproperties.Entry e)
    {
        string s = e.Style + " prefab " + N(e.Visprefab) + " sprite " + N(e.Spriteobject) + " '" + e.Displayname + "' addvis " + Names(e.Additionalvisualscripts) + " addprefabs " + Names(e.Additionalprefabs);
        if (e.Spriteobject != null && e.Spriteobject.Itemslist != null)
        {
            List<string> a = new List<string>();
            foreach (Spritesinlist l in e.Spriteobject.Itemslist)
            {
                int variants = l.Sprites != null ? l.Sprites.Count : 0;
                int frames = variants > 0 && l.Sprites[0].Variant != null ? l.Sprites[0].Variant.Count : 0;
                List<string> sounds = new List<string>();
                if (variants > 0 && l.Sprites[0].Variant != null)
                    for (int i = 0; i < l.Sprites[0].Variant.Count; i++)
                        if (!string.IsNullOrEmpty(l.Sprites[0].Variant[i].Sound)) sounds.Add(i + ":" + l.Sprites[0].Variant[i].Sound);
                a.Add(l.Name + "(" + variants + "x" + frames + " spd " + l.Speed + (l.Loop ? " loop" : "") + (l.Reset ? " reset" : "") + " prio " + l.Priority
                    + (string.IsNullOrEmpty(l.Spriteto) ? "" : " ->" + l.Spriteto) + (sounds.Count > 0 ? " snd " + string.Join("/", sounds.ToArray()) : "") + ")");
            }
            s += " anims " + Strings(a);
        }
        return s;
    }

    static void Visual(Visualproperties v, string p)
    {
        done.Add(v);
        W(p + "VISUAL " + v.Objectname + " | default " + Entry(v.Default));
        if (v.Skinoverrides != null)
            foreach (KeyValuePair<string, Visualproperties.Entry> kv in v.Skinoverrides)
                W(p + "  SKIN '" + kv.Key + "' " + Entry(kv.Value));
    }
}
#endif
