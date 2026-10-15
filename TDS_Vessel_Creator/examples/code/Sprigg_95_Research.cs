// EXAMPLE, frozen: Sprigg 1.x code from ModProjects (Sprigg itself waits there for its overhaul), kept because the
// kit docs cite it.
#if SPRIGG_RESEARCH
// Research build (-Define SPRIGG_RESEARCH, usually with SPRIGG_SELFTEST so it also runs inside a match): logs the data
// the next vessel (Mummer) is designed from, one "[SpriggResearch]" line per item:
//   - every enemy being (tags contain "Enemy"): AIs, stats, block, visual, drops
//   - blocks used by units, traps and towers, with their block functions
//   - every Commandobject and every Vfx name (the game resolves debuff/oneshot VFX by name from the "Vfx" label)
//   - every journal page with its parent, and the vessel index pages (pages linking to several vessel pages)
public static class SpriggResearch
{
    static int lines;
    static void W(string s) { if (lines++ < 6000) Debug.Log("[SpriggResearch] " + s); }
    static string N(UnityEngine.Object o) { return o == null ? "-" : o.name; }

    public static IEnumerator Run(string when)
    {
        W("BEGIN " + when);
        foreach (string hero in new[] { "Hero_Boutman", "Hero_Buster", "Hero_Fie", "Hero_Snosno" })
        {
            Heroentryobject h = SpriggUtil.Heroentry(hero);
            if (h == null || h.Heroloads == null) { W("no entry " + hero); continue; }
            Bl_Address.Loadgroup g = null;
            try { g = Bl_Address.Load(h.Heroloads.Objectname); } catch (Exception e) { W("load " + hero + " failed " + e.Message); }
            float t = 0f;
            while (g != null && !g.Loaded && t < 120f) { t += Time.unscaledDeltaTime; yield return null; }
            W("group " + hero + " loaded " + (g != null && g.Loaded));
        }
        try { Enemies(); } catch (Exception e) { W("enemies failed " + e); }
        yield return null;
        try { Blocks(); } catch (Exception e) { W("blocks failed " + e); }
        yield return null;
        try { Commands(); } catch (Exception e) { W("commands failed " + e); }
        try { Vfx(); } catch (Exception e) { W("vfx failed " + e); }
        yield return null;
        try { Journal(); } catch (Exception e) { W("journal failed " + e); }
        W("DONE " + when);
    }

    static IEnumerable<UnityEngine.Object> Label(string label)
    {
        List<Bl_Address.Loadlist> lists;
        if (!Bl_Address.Groups.TryGetValue(label, out lists)) yield break;
        foreach (Bl_Address.Loadlist l in lists)
            foreach (UnityEngine.Object o in l.Items.Values)
                yield return o;
    }

    static void Enemies()
    {
        List<string> seen = new List<string>();
        List<string> labels = new List<string>(Bl_Address.Groups.Keys);
        W("labels: " + string.Join(", ", labels.ToArray()));
        foreach (string label in labels)
            foreach (UnityEngine.Object o in Label(label))
            {
                Beingobject b = o as Beingobject;
                if (b == null || b.Entity == null || seen.Contains(b.Objectname)) continue;
                Entityobject e = b.Entity;
                bool enemy = e.Tags != null && (e.Tags.Contains("Enemy") || e.Tags.Contains("Boss"));
                bool resource = e.Tags != null && e.Tags.Contains("Resource");
                if (!enemy && !resource) continue;
                seen.Add(b.Objectname);
                List<string> ais = new List<string>();
                if (e.Aiscripts != null) foreach (UnityEngine.Object a in e.Aiscripts) ais.Add(N(a));
                List<string> vis = new List<string>();
                if (e.Visualscripts != null) foreach (UnityEngine.Object a in e.Visualscripts) vis.Add(N(a));
                List<string> stats = new List<string>();
                if (e.Defaultstat != null) foreach (Statproperties.Defaultstatgroup g in e.Defaultstat.Stats) stats.Add(g.Name + "=" + g.Value + (g.Leveladd != 0 ? "(+" + g.Leveladd + ")" : ""));
                List<string> drops = new List<string>();
                if (e.Drops != null) foreach (Link_Resourceset d in e.Drops) drops.Add(d.Name + " " + d.Amount);
                List<string> ups = new List<string>();
                if (e.Upgradesinit != null) foreach (Upgradeproperties u in e.Upgradesinit) ups.Add(N(u));
                string visual = "-";
                if (e.Defaultvisual != null)
                {
                    Visualproperties.Entry d = e.Defaultvisual.Default;
                    visual = d.Style + " prefab " + N(d.Visprefab) + " sprite " + N(d.Spriteobject);
                    if (d.Spriteobject != null && d.Spriteobject.Items != null) visual += " anims [" + string.Join(",", new List<string>(d.Spriteobject.Items.Keys).ToArray()) + "]";
                    if (e.Defaultvisual.Skinoverrides != null) visual += " skins [" + string.Join(",", new List<string>(e.Defaultvisual.Skinoverrides.Keys).ToArray()) + "]";
                }
                W((enemy ? "ENEMY " : "RESOURCE ") + b.Objectname + " [" + label + "] display '" + e.Display_Name + "' desc '" + e.Display_Desc + "' tags [" + string.Join(",", e.Tags.ToArray())
                    + "] ais [" + string.Join(",", ais.ToArray()) + "] vis [" + string.Join(",", vis.ToArray()) + "] stats [" + string.Join(" ", stats.ToArray()) + "] block " + N(b.Block)
                    + " drops [" + string.Join(",", drops.ToArray()) + "] init [" + string.Join(",", ups.ToArray()) + "] visual " + visual + " targetable " + e.Targetable);
            }
        W("enemy/resource beings: " + seen.Count);
    }

    static void Blocks()
    {
        List<string> want = new List<string>();
        foreach (string label in new List<string>(Bl_Address.Groups.Keys))
            foreach (UnityEngine.Object o in Label(label))
            {
                Beingobject b = o as Beingobject;
                if (b == null || b.Block == null) continue;
                string n = b.Block.name;
                if (want.Contains(n)) continue;
                want.Add(n);
                List<string> f = new List<string>();
                if (b.Block.Functs != null) foreach (Block_Funct_Base x in b.Block.Functs) f.Add(x != null ? x.GetType().Name : "null");
                List<string> pc = new List<string>();
                if (b.Block.Placementconditions != null)
                    foreach (Blockobject.Placementcondition p in b.Block.Placementconditions)
                        pc.Add(p.Position + (p.Navagatable ? " nav" : "") + (p.Notnavagatable ? " notnav" : "") + (p.Occupyable ? " occ" : "") + (p.Placeable ? " place" : "") + (p.Includeteam ? " team" : ""));
                W("BLOCK " + n + " (first seen on " + b.Objectname + ") functs [" + string.Join(",", f.ToArray()) + "] placement [" + string.Join(",", pc.ToArray()) + "] space " + (b.Block.Additionalspace != null ? b.Block.Additionalspace.Count : 0));
            }
    }

    static void Commands()
    {
        foreach (UnityEngine.Object o in Label("Command"))
        {
            Commandobject c = o as Commandobject;
            if (c == null) continue;
            W("COMMAND " + c.Objectname + " style " + c.Style + " name '" + c.Display_Name + "' icon " + c.Icon + " keys [" + (c.Keybind != null ? string.Join(",", c.Keybind.ToArray()) : "") + "] state " + c.Keybindstate + " prio " + c.Priority
                + " functs " + (c.Commandfuncts != null ? c.Commandfuncts.Count : 0));
        }
    }

    static void Vfx()
    {
        List<string> names = new List<string>();
        foreach (UnityEngine.Object o in Label("Vfx")) names.Add(o.name);
        names.Sort();
        W("VFX (" + names.Count + "): " + string.Join(", ", names.ToArray()));
        List<string> sounds = new List<string>();
        foreach (UnityEngine.Object o in Label("Sound")) sounds.Add(o.name);
        W("SOUND label (" + sounds.Count + "): " + string.Join(", ", sounds.ToArray()));
    }

    // Every page with its parent; then any page that links to two or more vessel pages (an index of vessels).
    static void Journal()
    {
        List<Logobject> heroPages = new List<Logobject>();
        foreach (UnityEngine.Object o in SpriggUtil.Folder("Hero").Values)
        {
            Heroentryobject h = o as Heroentryobject;
            if (h != null && h.Log != null) heroPages.Add(h.Log);
        }
        Logobject[] all = Resources.FindObjectsOfTypeAll<Logobject>();
        W("logobjects " + all.Length + ", vessel pages " + heroPages.Count);
        foreach (Logobject l in all)
        {
            int toHeroes = 0, side = 0;
            foreach (Logitems.Base b in Flat(l.Elements))
            {
                Logitems.Link k = b as Logitems.Link;
                Logitems.LinkImage ki = b as Logitems.LinkImage;
                Logobject t = k != null ? k.Target : ki != null ? ki.Target : null;
                if (t != null && heroPages.Contains(t)) { toHeroes++; if (k != null && k.Sidelink) side++; }
            }
            W("PAGE " + l.name + " '" + (l.Title ?? "") + "' parent " + N(l.Parent) + " elements " + (l.Elements != null ? l.Elements.Count : 0) + " heroLinks " + toHeroes + " (side " + side + ") hide " + l.Hidemode
                + " unlocks " + (l.Unlockconditions != null ? l.Unlockconditions.Count : 0) + " thumb " + N(l.Thumbnail));
            if (toHeroes - side >= 2)
                foreach (Logitems.Base b in Flat(l.Elements))
                    W("   INDEX ITEM " + b.GetType().Name + (b is Logitems.Link ? " -> " + N(((Logitems.Link)b).Target) + " side " + ((Logitems.Link)b).Sidelink : b is Logitems.LinkImage ? " -> " + N(((Logitems.LinkImage)b).Target) : ""));
        }
    }

    static List<Logitems.Base> Flat(List<Logitems.Base> items)
    {
        List<Logitems.Base> o = new List<Logitems.Base>();
        if (items == null) return o;
        foreach (Logitems.Base b in items)
        {
            if (b == null) continue;
            o.Add(b);
            if (b is Logitems.Group) o.AddRange(Flat(((Logitems.Group)b).Children));
            if (b is Logitems.DoubleColumn) { o.AddRange(Flat(((Logitems.DoubleColumn)b).leftChildren)); o.AddRange(Flat(((Logitems.DoubleColumn)b).rightChildren)); }
            if (b is Logitems.Link && ((Logitems.Link)b).Child != null) o.AddRange(Flat(new List<Logitems.Base> { ((Logitems.Link)b).Child }));
        }
        return o;
    }
}
#endif
