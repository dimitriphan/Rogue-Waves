// Mummer's journal pages, described for the shared ModKitVesselJournal (ModSource/Shared/ModKitJournal.cs): a vessel
// page with the rule banner, a page per building with its upgrade tree, and pages for the three Understudies (RELATED
// ITEMS on the Puppet Stage's page). ModKitVesselTabs keeps every vessel page's tab row complete, so Mummer, Sprigg,
// Argo and any later vessel all show one tab per vessel in the same order.
public static class MummerJournal
{
    static readonly Color Lilac = new Color(0.70f, 0.57f, 0.84f, 1f);

    public static Logobject Page;
    public static int Pages;

    // EFFECTS labels for Mummer's own parameters ("" hides one).
    static readonly string[][] Labels =
    {
        new[] { "Scraprange", "Scrap Range" }, new[] { "Unravel", "Unravelling" }, new[] { "Orethread", "Thread per 3 Strikes" },
        new[] { "Scrap", "Scraps" }, new[] { "Golden", "Golden Scrap" }, new[] { "Leash", "Leash" }, new[] { "Puppetspeed", "Understudy Speed" },
        new[] { "Respawn", "Stitch Time" }, new[] { "Thread", "Thread Cost" }, new[] { "Puppethealth", "Understudy Health" },
        new[] { "Puppetdamage", "Understudy Damage" }, new[] { "Burst", "Burst Damage" }, new[] { "Regen", "Healing" }, new[] { "Units", "Understudies" },
        new[] { "Count", "Extra Foes" }, new[] { "Stun", "Stun" }, new[] { "Taunt", "Taunt Range" }, new[] { "Mark", "Mark Time" },
        new[] { "Heal", "Healing" }, new[] { "Root", "Hold Time" }, new[] { "Radius", "Radius" }, new[] { "Confuse", "" }, new[] { "Master", "" },
    };

    public static void Build(Heroentryobject template)
    {
        Entityobject hero = MummerContent.HeroBeing.Entity;
        ModKitJournalSpec spec = new ModKitJournalSpec
        {
            HeroId = MummerData.Hero, Template = template, Hero = hero,
            Rule = hero.Upgradesinit.Count > 0 ? hero.Upgradesinit[0] : null,
            Title = MummerData.Displayname, Lore = MummerData.Lore, Origin = "Mummer mod",
            Portrait = Portrait(), Tab = MummerAssets.Single("Mummertabicon", 32), Accent = Lilac,
        };
        foreach (string[] kv in Labels)
            spec.Labels[kv[0]] = kv[1];
        foreach (MummerData.Build b in MummerData.Builds)
        {
            Beingobject being;
            if (!MummerContent.Beings.TryGetValue(b.Id, out being))
                continue;
            ModKitJournalBuilding jb = new ModKitJournalBuilding { Id = b.Id, Title = b.Display, Desc = b.Lore, Entity = being.Entity, Model = MummerAssets.Model(b.Id) };
            Dictionary<string, ModKitJournalUpgrade> byKey = new Dictionary<string, ModKitJournalUpgrade>();
            foreach (MummerData.Upg u in b.Upgrades)
            {
                Upgradeproperties prop;
                MummerContent.Upgrades.TryGetValue(u.Id, out prop);
                byKey[u.Key + u.Tier] = new ModKitJournalUpgrade { Id = u.Id, Title = u.Display, Desc = u.Desc, Upgrade = prop, Model = MummerAssets.Model(u.Model) ?? jb.Model };
            }
            foreach (MummerData.Upg u in b.Upgrades)
            {
                ModKitJournalUpgrade ju = byKey[u.Key + u.Tier], next;
                foreach (string n in u.Next)
                    if (byKey.TryGetValue(n.Trim(), out next))
                        ju.Next.Add(next);
                if (u.Tier == 2)
                    jb.Roots.Add(ju);
            }
            if (b.Id == MummerData.Stage)
                foreach (MummerData.Puppet p in MummerData.Puppets)
                {
                    Beingobject pb;
                    if (!MummerContent.Beings.TryGetValue(p.Id, out pb))
                        continue;
                    string sheet = p.Costume == MummerData.Costume.Plain ? "Understudy-Sheet" : "Understudy" + p.Costume + "-Sheet";
                    List<Sprite> f = MummerAssets.Frames(sheet, 16, new Vector2(0.5f, 0.5f));
                    jb.Units.Add(new ModKitJournalUnit { Id = p.Id, Title = p.Display, Desc = p.Lore + " Costs " + p.Thread + " Thread.", Entity = pb.Entity, Look = f.Count > 0 ? f[0] : null });
                }
            spec.Buildings.Add(jb);
        }
        ModKitVesselJournal journal = new ModKitVesselJournal(spec);
        Page = journal.Build();
        Pages = journal.Pages;
        MummerContent.Entry.Log = Page;
        ModKitVesselTabs.Sync();
        MummerLog.Info("journal: " + Pages + " pages, tabs added " + ModKitVesselTabs.Added + ", duplicates removed " + ModKitVesselTabs.Removed);
    }

    public static void Tick() { ModKitVesselTabs.Tick(); }

    // An idle frame, front facing right, like the shipped vessel pages (Smacky4-Sheet_0, Snosnoidle-Sheet_3).
    static Sprite Portrait()
    {
        List<Sprite> f = MummerAssets.Frames("Mummer-Sheet", 32, new Vector2(0.5f, 0.5f));
        return f.Count > 0 ? f[0] : null;
    }
}
// One-time log of the values Mummer copied or guessed, so a Player.log is enough to check a build.
public static class MummerDiagnostics
{
    public static void Dump(Heroentryobject template)
    {
        Heroentryobject e = MummerContent.Entry;
        MummerLog.Info("Mummer entry: towers " + MummerUtil.Join(e.Towerdisplay) + ", worldsprite " + e.Worldsprite.width + "x" + e.Worldsprite.height + "x" + e.Worldsprite.depth
            + ", journal " + (e.Log != null ? e.Log.name : "none"));
        foreach (KeyValuePair<string, Beingobject> kv in MummerContent.Beings)
        {
            Entityobject ent = kv.Value.Entity;
            List<string> ais = new List<string>();
            foreach (UnityEngine.Object o in ent.Aiscripts) ais.Add(o.name);
            List<string> stats = new List<string>();
            if (ent.Defaultstat != null)
                foreach (Statproperties.Defaultstatgroup g in ent.Defaultstat.Stats) stats.Add(g.Name + "=" + g.Value + (g.Leveladd != 0 ? "+" + g.Leveladd : ""));
            List<string> costs = new List<string>();
            foreach (Link_Resourceset c in ent.Costs) costs.Add(c.Name + " " + c.Amount);
            MummerLog.Info(kv.Key + ": block " + (kv.Value.Block != null ? kv.Value.Block.name : "-") + "; ais " + MummerUtil.Join(ais) + "; stats " + MummerUtil.Join(stats)
                + "; cost " + MummerUtil.Join(costs) + "; tags " + MummerUtil.Join(ent.Tags) + "; upgrades " + ent.Upgrades.Count);
        }
        Entityobject hero = MummerContent.HeroBeing.Entity;
        List<string> heroAis = new List<string>();
        foreach (UnityEngine.Object o in hero.Aiscripts) heroAis.Add(o.name);
        List<string> heroVis = new List<string>();
        foreach (UnityEngine.Object o in hero.Visualscripts) heroVis.Add(o.name);
        MummerLog.Info("Hero_Mummer: ais " + MummerUtil.Join(heroAis) + "; vis " + MummerUtil.Join(heroVis) + "; anims " + MummerUtil.Join(hero.Defaultvisual.Default.Spriteobject.Items.Keys));
    }
}
