// One-time log of the values Argo copied or guessed, so a Player.log is enough to check a build.
public static class ArgoDiagnostics
{
    public static void Dump(Heroentryobject template)
    {
        ArgoLog.Info("template " + template.Objectname + ": loadingsize " + template.Loadingsize + ", landingsize " + template.Landingsize + ", idlesize " + template.Idlesize
            + ", worldsprite " + (template.Worldsprite != null ? template.Worldsprite.width + "x" + template.Worldsprite.height + "x" + template.Worldsprite.depth : "none"));
        Heroentryobject e = ArgoContent.Entry;
        ArgoLog.Info("Argo entry: towers " + ModKitUtil.Join(e.Towerdisplay) + ", worldsprite " + e.Worldsprite.width + "x" + e.Worldsprite.height + "x" + e.Worldsprite.depth
            + ", loading " + (e.Loadingsprite != null ? e.Loadingsprite.width + "x" + e.Loadingsprite.height : "none") + ", log " + (e.Log != null ? e.Log.name : "none"));
        foreach (KeyValuePair<string, Beingobject> kv in ArgoContent.Beings)
        {
            Entityobject ent = kv.Value.Entity;
            List<string> ais = new List<string>();
            foreach (UnityEngine.Object o in ent.Aiscripts) ais.Add(o.name);
            List<string> vis = new List<string>();
            foreach (UnityEngine.Object o in ent.Visualscripts) vis.Add(o.name);
            List<string> stats = new List<string>();
            if (ent.Defaultstat != null)
                foreach (Statproperties.Defaultstatgroup g in ent.Defaultstat.Stats) stats.Add(g.Name + "=" + g.Value + (g.Leveladd != 0f ? "(+" + g.Leveladd + ")" : ""));
            List<string> costs = new List<string>();
            foreach (Link_Resourceset c in ent.Costs) costs.Add(c.Name + " " + c.Amount);
            ArgoLog.Info(kv.Key + ": ais " + ModKitUtil.Join(ais) + "; vis " + ModKitUtil.Join(vis) + "; tags " + ModKitUtil.Join(ent.Tags) + "; stats " + ModKitUtil.Join(stats)
                + "; cost " + ModKitUtil.Join(costs) + "; model " + (ent.Defaultvisual.Default.Visprefab != null ? ent.Defaultvisual.Default.Visprefab.name : "sprite")
                + "; upgrades " + ent.Upgrades.Count + "; skins " + ModKitUtil.Join(ent.Defaultvisual.Skinoverrides.Keys));
        }
        Entityobject hero = ArgoContent.HeroBeing.Entity;
        List<string> heroAis = new List<string>();
        foreach (UnityEngine.Object o in hero.Aiscripts) heroAis.Add(o.name);
        List<string> heroVis = new List<string>();
        foreach (UnityEngine.Object o in hero.Visualscripts) heroVis.Add(o.name);
        ArgoLog.Info("Hero_Argo: ais " + ModKitUtil.Join(heroAis) + "; vis " + ModKitUtil.Join(heroVis) + "; anims " + ModKitUtil.Join(hero.Defaultvisual.Default.Spriteobject.Items.Keys));
        List<string> missing = new List<string>();
        foreach (string[] p in ArgoData.Projectiles)
            if (!ArgoContent.Projectiles.ContainsKey(p[0])) missing.Add(p[0]);
        foreach (ArgoData.Build b in ArgoData.Builds)
        {
            if (ModKitAssets.Quiet(b.ModelName) == null) missing.Add(b.ModelName);
            foreach (ArgoData.Upg u in b.Upgrades)
                if (ModKitAssets.Quiet(u.Model) == null) missing.Add(u.Model);
        }
        ArgoLog.Info("Command_Buildoption5: " + ArgoContent.Option5Note + "; commands " + ModKitUtil.Join(ArgoContent.Commands.Keys));
        if (missing.Count > 0)
            ArgoLog.Warn("missing: " + ModKitUtil.Join(missing));
    }
}
