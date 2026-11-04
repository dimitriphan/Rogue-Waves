// Spool's journal pages, described for the shared ModKitVesselJournal (ModSource/Shared/ModKitJournal.cs): a vessel
// page with the rule banner, a page per buildable with its upgrade tree (the Thread Head's too), and unit pages as
// RELATED ITEMS: Stitched and Dart on the Cookie Tin's, the Seam on the Loom's, the Mangled on the Pin Cushion's.
// ModKitVesselTabs keeps every vessel page's tab row complete.
public static class SpoolJournal
{
    static readonly Color Purple = new Color(0.576f, 0.388f, 0.682f, 1f);

    public static Logobject Page;
    public static int Pages;

    // Which units each buildable's page lists.
    static readonly Dictionary<string, string[]> UnitsOf = new Dictionary<string, string[]>
    {
        { SpoolData.Tin, new[] { SpoolData.Stitched, SpoolData.Dart } },
        { SpoolData.Loom, new[] { SpoolData.Seam } },
        { SpoolData.Cushion, new[] { SpoolData.Mangled } },
    };

    public static void Build(Heroentryobject template)
    {
        Entityobject hero = SpoolContent.HeroBeing.Entity;
        ModKitJournalSpec spec = new ModKitJournalSpec
        {
            HeroId = SpoolData.Hero, Template = template, Hero = hero,
            Rule = hero.Upgradesinit.Count > 0 ? hero.Upgradesinit[0] : null,
            Title = SpoolData.Displayname, Lore = SpoolData.Lore, Origin = "Rogue Waves",
            Portrait = Portrait(), Tab = ModKitAssets.Single("Spooltabicon", 32), Accent = Purple,
        };
        foreach (SpoolData.Build b in SpoolData.Builds)
        {
            Beingobject being;
            if (!SpoolContent.Beings.TryGetValue(b.Id, out being))
                continue;
            ModKitJournalBuilding jb = new ModKitJournalBuilding { Id = b.Id, Title = b.Display, Desc = b.Lore, Entity = being.Entity, Model = ModKitAssets.Quiet(b.Id) };
            Dictionary<string, ModKitJournalUpgrade> byKey = new Dictionary<string, ModKitJournalUpgrade>();
            foreach (SpoolData.Upg u in b.Upgrades)
            {
                Upgradeproperties prop;
                SpoolContent.Upgrades.TryGetValue(u.Id, out prop);
                byKey[u.Key + u.Tier] = new ModKitJournalUpgrade { Id = u.Id, Title = u.Display, Desc = u.Desc, Effects = u.Effects, Upgrade = prop, Model = ModKitAssets.Quiet(u.Model) ?? jb.Model };
            }
            foreach (SpoolData.Upg u in b.Upgrades)
            {
                ModKitJournalUpgrade ju = byKey[u.Key + u.Tier], next;
                foreach (string n in u.Next)
                    if (byKey.TryGetValue(n.Trim(), out next))
                        ju.Next.Add(next);
                if (u.Tier == 2)
                    jb.Roots.Add(ju);
            }
            string[] units;
            if (UnitsOf.TryGetValue(b.Id, out units))
                foreach (string id in units)
                {
                    SpoolData.Unit u = SpoolData.UnitFor(id);
                    Beingobject ub;
                    if (u == null || !SpoolContent.Beings.TryGetValue(id, out ub))
                        continue;
                    List<Sprite> f = ModKitAssets.Frames(u.Sheet, u.Cell, new Vector2(0.5f, 0.5f));
                    string cost = u.Thread > 0 ? " Costs " + u.Thread + " Thread." : "";
                    jb.Units.Add(new ModKitJournalUnit { Id = id, Title = u.Display, Desc = u.Lore + cost, Entity = ub.Entity, Look = f.Count > 0 ? f[0] : null });
                }
            spec.Buildings.Add(jb);
        }
        ModKitVesselJournal journal = new ModKitVesselJournal(spec);
        Page = journal.Build();
        Pages = journal.Pages;
        SpoolContent.Entry.Log = Page;
        ModKitVesselTabs.Sync();
        SpoolLog.Info("journal: " + Pages + " pages, tabs added " + ModKitVesselTabs.Added + ", duplicates removed " + ModKitVesselTabs.Removed);
    }

    public static void Tick() { ModKitVesselTabs.Tick(); }

    // An idle frame, front facing right, like the shipped vessel pages (Smacky4-Sheet_0, Snosnoidle-Sheet_3).
    static Sprite Portrait()
    {
        List<Sprite> f = ModKitAssets.Frames("Spool-Sheet", 32, new Vector2(0.5f, 0.5f));
        return f.Count > 0 ? f[0] : null;
    }
}
// One-time log of the values Spool copied or guessed, so a Player.log is enough to check a build.
public static class SpoolDiagnostics
{
    public static void Dump(Heroentryobject template)
    {
        Heroentryobject e = SpoolContent.Entry;
        SpoolLog.Info("Spool entry: towers " + ModKitUtil.Join(e.Towerdisplay) + ", worldsprite " + e.Worldsprite.width + "x" + e.Worldsprite.height + "x" + e.Worldsprite.depth
            + ", journal " + (e.Log != null ? e.Log.name : "none"));
        foreach (KeyValuePair<string, Beingobject> kv in SpoolContent.Beings)
        {
            Entityobject ent = kv.Value.Entity;
            List<string> ais = new List<string>();
            foreach (UnityEngine.Object o in ent.Aiscripts) ais.Add(o.name);
            List<string> stats = new List<string>();
            if (ent.Defaultstat != null)
                foreach (Statproperties.Defaultstatgroup g in ent.Defaultstat.Stats) stats.Add(g.Name + "=" + g.Value + (g.Leveladd != 0 ? "+" + g.Leveladd : ""));
            List<string> costs = new List<string>();
            foreach (Link_Resourceset c in ent.Costs) costs.Add(c.Name + " " + c.Amount);
            SpoolLog.Info(kv.Key + ": block " + (kv.Value.Block != null ? kv.Value.Block.name : "-") + "; ais " + ModKitUtil.Join(ais) + "; stats " + ModKitUtil.Join(stats)
                + "; cost " + ModKitUtil.Join(costs) + "; tags " + ModKitUtil.Join(ent.Tags) + "; upgrades " + ent.Upgrades.Count);
        }
        Entityobject hero = SpoolContent.HeroBeing.Entity;
        List<string> heroAis = new List<string>();
        foreach (UnityEngine.Object o in hero.Aiscripts) heroAis.Add(o.name);
        List<string> heroVis = new List<string>();
        foreach (UnityEngine.Object o in hero.Visualscripts) heroVis.Add(o.name);
        SpoolLog.Info("Hero_Spool: ais " + ModKitUtil.Join(heroAis) + "; vis " + ModKitUtil.Join(heroVis) + "; anims " + ModKitUtil.Join(hero.Defaultvisual.Default.Spriteobject.Items.Keys));
    }
}
