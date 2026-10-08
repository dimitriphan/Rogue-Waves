// docs/13 §10 Q1, Q2, Q4 (docs/15 §11 step 9): the stock enemies, the elite modifiers' text and params, and which
// load group holds which enemy, at boot and again in a match. One "[RogueLab] TEST ENEMY|ELITE|GROUP ..." line each.
public static class RogueLabEnemies
{
    static readonly string[] Elites = { "Enemy_Health_Upgradeobject", "Enemy_Red_Upgradeobject", "Enemy_Hyper_Upgradeobject", "Enemy_Chaos_Upgradeobject",
        "Enemy_Armor_Upgradeobject", "Enemy_Explode_Upgradeobject", "Effect_Enemy_Weakened_Upgradeobject" };

    static string N(UnityEngine.Object o) { return o == null ? "-" : o.name; }

    public static void Dump(string when)
    {
        Groups(when);
        foreach (string id in Elites)
        {
            Upgradeproperties u = RogueLabUtil.Load<Upgradeproperties>("Upgrade", id);
            if (u == null) { RogueLabLog.Test("ELITE " + when + " " + id + " not loaded"); continue; }
            List<string> ps = new List<string>();
            foreach (Upgradeproperties.Param p in u.Defaultparam) ps.Add(p.Name + "=" + RogueLabUtil.F(p.Value));
            List<string> ais = new List<string>();
            foreach (UnityEngine.Object a in u.Aiscripts) ais.Add(N(a));
            RogueLabLog.Test("ELITE " + when + " " + id + " name '" + u.Display_Name + "' desc '" + u.Display_Desc + "' priority " + u.Priority + " ais [" + string.Join(",", ais.ToArray())
                + "] params " + string.Join(" ", ps.ToArray()));
        }
        if (when == "match")
            Beings();
    }

    // Every loaded folder with its item count per label, and the folders that hold enemies.
    static void Groups(string when)
    {
        Dictionary<Bl_Address.Loadlist, string> label = new Dictionary<Bl_Address.Loadlist, string>();
        foreach (KeyValuePair<string, List<Bl_Address.Loadlist>> kv in Bl_Address.Groups)
            foreach (Bl_Address.Loadlist l in kv.Value)
                label[l] = kv.Key;
        List<string> folders = new List<string>(Bl_Address.Loadedfolders.Keys);
        folders.Sort();
        foreach (string f in folders)
        {
            Bl_Address.Loadgroup g = Bl_Address.Loadedfolders[f];
            if (g == null || g.Items == null) continue;
            List<string> parts = new List<string>();
            List<string> foes = new List<string>();
            foreach (Bl_Address.Loadlist l in g.Items)
            {
                string lab = label.ContainsKey(l) ? label[l] : "?";
                parts.Add(lab + " " + l.Items.Count);
                if (lab != "Mob") continue;
                foreach (UnityEngine.Object o in l.Items.Values)
                {
                    Beingobject b = o as Beingobject;
                    if (b != null && b.Entity != null && b.Entity.Tags != null && b.Entity.Tags.Contains("Enemy")) foes.Add(b.Objectname);
                }
            }
            RogueLabLog.Test("GROUP " + when + " " + f + " loaded " + g.Loaded + " [" + string.Join(", ", parts.ToArray()) + "]" + (foes.Count > 0 ? " enemies " + string.Join(",", foes.ToArray()) : ""));
        }
    }

    static void Beings()
    {
        List<string> seen = new List<string>();
        List<Bl_Address.Loadlist> lists;
        if (!Bl_Address.Groups.TryGetValue("Mob", out lists)) return;
        foreach (Bl_Address.Loadlist l in lists)
            foreach (UnityEngine.Object o in l.Items.Values)
            {
                Beingobject b = o as Beingobject;
                if (b == null || b.Entity == null || seen.Contains(b.Objectname)) continue;
                Entityobject e = b.Entity;
                if (e.Tags == null || !(e.Tags.Contains("Enemy") || e.Tags.Contains("Boss"))) continue;
                seen.Add(b.Objectname);
                List<string> ais = new List<string>();
                if (e.Aiscripts != null) foreach (UnityEngine.Object a in e.Aiscripts) ais.Add(N(a));
                List<string> vis = new List<string>();
                if (e.Visualscripts != null) foreach (UnityEngine.Object a in e.Visualscripts) vis.Add(N(a));
                List<string> stats = new List<string>();
                if (e.Defaultstat != null) foreach (Statproperties.Defaultstatgroup g in e.Defaultstat.Stats) stats.Add(g.Name + "=" + RogueLabUtil.F(g.Value) + (g.Leveladd != 0 ? "(+" + RogueLabUtil.F(g.Leveladd) + ")" : ""));
                List<string> drops = new List<string>();
                if (e.Drops != null) foreach (Link_Resourceset d in e.Drops) drops.Add(d.Name + " " + d.Amount);
                List<string> ups = new List<string>();
                if (e.Upgradesinit != null) foreach (Upgradeproperties u in e.Upgradesinit) ups.Add(N(u));
                string skins = "";
                if (e.Defaultvisual != null && e.Defaultvisual.Skinoverrides != null) skins = string.Join(",", new List<string>(e.Defaultvisual.Skinoverrides.Keys).ToArray());
                RogueLabLog.Test("ENEMY " + b.Objectname + " '" + e.Display_Name + "' tags [" + string.Join(",", e.Tags.ToArray()) + "] ais [" + string.Join(",", ais.ToArray()) + "] vis ["
                    + string.Join(",", vis.ToArray()) + "] stats [" + string.Join(" ", stats.ToArray()) + "] drops [" + string.Join(",", drops.ToArray()) + "] init [" + string.Join(",", ups.ToArray())
                    + "] skins [" + skins + "] block " + N(b.Block));
            }
        RogueLabLog.Test("INFO enemy beings loaded in the match: " + seen.Count);
    }
}
