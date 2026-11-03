// Yossele's journal pages, described for the shared ModKitVesselJournal (ModSource/Shared/ModKitJournal.cs): a vessel
// page with the rule banner and a page per building with its upgrade tree. ModKitVesselTabs keeps every vessel page's
// tab row complete, so each vessel of the pack shows one tab per vessel in the same order.
public static class YosseleJournal
{
    public static Logobject Page;
    public static int Pages;

    public static void Build(Heroentryobject template)
    {
        Entityobject hero = YosseleContent.HeroBeing.Entity;
        ModKitJournalSpec spec = new ModKitJournalSpec
        {
            HeroId = YosseleData.Hero, Template = template, Hero = hero,
            Rule = hero.Upgradesinit.Count > 0 ? hero.Upgradesinit[0] : null,
            Title = YosseleData.Displayname, Lore = YosseleData.Lore, Origin = "Rogue Waves",
            Portrait = Portrait(), Tab = YosseleAssets.Single("Yosseletabicon", 32), Accent = YosseleContent.Accent,
        };
        foreach (string[] kv in YosseleData.ParamLabels)
            spec.Labels[kv[0]] = kv[1];
        foreach (YosseleData.Build b in YosseleData.Builds)
        {
            Beingobject being;
            // The Bonsai has no page of its own: the Tree's Bonsai upgrade page tells it.
            if (b.Id == YosseleData.Bonsai || !YosseleContent.Beings.TryGetValue(b.Id, out being))
                continue;
            ModKitJournalBuilding jb = new ModKitJournalBuilding { Id = b.Id, Title = b.Display, Desc = b.Lore + " Grows up to " + b.Max + " stages.", Entity = being.Entity, Model = YosseleAssets.Model(b.Id) };
            Beingobject friend;
            if (b.Id == YosseleData.Cannon && YosseleContent.Beings.TryGetValue(YosseleData.Friend, out friend))
            {
                Spriteobject look = friend.Entity.Defaultvisual != null ? friend.Entity.Defaultvisual.Default.Spriteobject : null;
                jb.Units.Add(new ModKitJournalUnit { Id = YosseleData.Friend, Title = YosseleData.FriendDisplay, Desc = YosseleData.FriendLore, Entity = friend.Entity, Look = look != null ? look.Preview : null });
            }
            Dictionary<string, ModKitJournalUpgrade> byKey = new Dictionary<string, ModKitJournalUpgrade>();
            foreach (YosseleData.Upg u in b.Upgrades)
            {
                Upgradeproperties prop;
                YosseleContent.Upgrades.TryGetValue(u.Id, out prop);
                byKey[u.Key + u.Tier] = new ModKitJournalUpgrade { Id = u.Id, Title = u.Display, Desc = u.Desc, Upgrade = prop, Model = YosseleAssets.Model(u.Model) ?? jb.Model };
            }
            foreach (YosseleData.Upg u in b.Upgrades)
            {
                ModKitJournalUpgrade ju = byKey[u.Key + u.Tier], next;
                foreach (string n in u.Next)
                    if (byKey.TryGetValue(n.Trim(), out next))
                        ju.Next.Add(next);
                if (u.Tier == 2)
                    jb.Roots.Add(ju);
            }
            spec.Buildings.Add(jb);
        }
        ModKitVesselJournal journal = new ModKitVesselJournal(spec);
        Page = journal.Build();
        Pages = journal.Pages;
        YosseleContent.Entry.Log = Page;
        ModKitVesselTabs.Sync();
        YosseleLog.Info("journal: " + Pages + " pages, tabs added " + ModKitVesselTabs.Added + ", duplicates removed " + ModKitVesselTabs.Removed);
    }

    // An idle frame, front facing right, like the shipped vessel pages (Smacky4-Sheet_0, Snosnoidle-Sheet_3).
    static Sprite Portrait()
    {
        List<Sprite> f = YosseleAssets.Frames("Yossele-Sheet", 32, new Vector2(0.5f, 0.5f));
        return f.Count > 0 ? f[0] : null;
    }
}

// One-time log of the values Yossele copied or guessed, so a Player.log is enough to check a build.
public static class YosseleDiagnostics
{
    public static void Dump(Heroentryobject template)
    {
        Heroentryobject e = YosseleContent.Entry;
        YosseleLog.Info("Yossele entry: towers " + YosseleUtil.Join(e.Towerdisplay) + ", worldsprite " + e.Worldsprite.width + "x" + e.Worldsprite.height + "x" + e.Worldsprite.depth
            + ", journal " + (e.Log != null ? e.Log.name : "none"));
        foreach (KeyValuePair<string, Beingobject> kv in YosseleContent.Beings)
        {
            Entityobject ent = kv.Value.Entity;
            List<string> ais = new List<string>();
            foreach (UnityEngine.Object o in ent.Aiscripts) ais.Add(o.name);
            List<string> vis = new List<string>();
            foreach (UnityEngine.Object o in ent.Visualscripts) vis.Add(o.name);
            List<string> stats = new List<string>();
            if (ent.Defaultstat != null)
                foreach (Statproperties.Defaultstatgroup g in ent.Defaultstat.Stats) stats.Add(g.Name + "=" + g.Value + (g.Leveladd != 0 ? "+" + g.Leveladd : ""));
            List<string> costs = new List<string>();
            foreach (Link_Resourceset c in ent.Costs) costs.Add(c.Name + " " + c.Amount);
            YosseleLog.Info(kv.Key + ": block " + (kv.Value.Block != null ? kv.Value.Block.name : "-") + "; ais " + YosseleUtil.Join(ais) + "; vis " + YosseleUtil.Join(vis)
                + "; stats " + YosseleUtil.Join(stats) + "; cost " + YosseleUtil.Join(costs) + "; tags " + YosseleUtil.Join(ent.Tags) + "; upgrades " + ent.Upgrades.Count
                + "; skins " + (ent.Defaultvisual != null ? ent.Defaultvisual.Skinoverrides.Count : 0) + "; orientable " + ent.Orientable);
        }
        Entityobject hero = YosseleContent.HeroBeing.Entity;
        List<string> heroAis = new List<string>();
        foreach (UnityEngine.Object o in hero.Aiscripts) heroAis.Add(o.name);
        List<string> heroVis = new List<string>();
        foreach (UnityEngine.Object o in hero.Visualscripts) heroVis.Add(o.name);
        YosseleLog.Info("Hero_Yossele: ais " + YosseleUtil.Join(heroAis) + "; vis " + YosseleUtil.Join(heroVis) + "; anims " + YosseleUtil.Join(hero.Defaultvisual.Default.Spriteobject.Items.Keys));
        List<string> dirs = new List<string>();
        foreach (Upgradeproperties d in YosseleContent.Compostbins) dirs.Add(d.Objectname);
        foreach (Upgradeproperties d in YosseleContent.Treeshifts) dirs.Add(d.Objectname);
        YosseleLog.Info("Directions: " + YosseleUtil.Join(dirs));
    }
}
