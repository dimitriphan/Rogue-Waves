// Argo's journal pages, described for the shared kit's ModKitVesselJournal (ModSource/Shared/ModKitJournal.cs, which grew
// out of Argo 1.0's own VesselJournal and VesselTabs): a vessel page with the rule banner, a page per building with its
// upgrade tree, and the Nauts' pages (RELATED ITEMS on the Miner's Base's page). ModKitVesselTabs keeps every vessel
// page's tab row complete, in the same order as every other vessel mod.
public static class ArgoJournal
{
    public static Logobject Page;
    public static int Pages;

    public static void Build(Heroentryobject template)
    {
        ModKitJournalSpec spec = new ModKitJournalSpec
        {
            HeroId = ArgoData.Hero, Template = template, Hero = ArgoContent.HeroBeing.Entity,
            Rule = ArgoContent.HeroBeing.Entity.Upgradesinit.Count > 0 ? ArgoContent.HeroBeing.Entity.Upgradesinit[0] : null,
            Title = ArgoData.LogTitle, Lore = ArgoData.LogDesc, Origin = "Argo mod",
            Portrait = Portrait(), Tab = ArgoAssets.Single("Argotabicon", 32), Accent = ArgoData.Color,
        };
        foreach (KeyValuePair<string, string> kv in new Dictionary<string, string>
        {
            { "Projcount", "Pellets" }, { "Spread", "Cone" }, { "Pierce", "Pierce" }, { "Stagger", "Stagger" }, { "Broadside", "Broadside Every" },
            { "Bleedticks", "Bleed Ticks" }, { "Bleed", "Bleed Damage" }, { "Root", "Hold" }, { "Whaler", "Full Bonus At Health" }, { "Twin", "Second Harpoon After" },
            { "Lance", "Lance Every" }, { "Nautspeed", "Naut Speed" }, { "Repair", "Repair" }, { "Tunerange", "Tuned Range" },
            { "Bulkhead", "Damage Taken Cut" }, { "Divebell", "Dive" }, { "Tuneboost", "Tuning Boost" }, { "Fathommax", "Deepest Fathom" }, { "Tremor", "Tremor Radius" },
            { "Gusher", "Gusher Strikes" }, { "Mantle", "Quake Pin" }, { "Debuffbonus", "Bonus vs Slowed or Pinned" },
            { "Tetherslow", "Tether Slow" }, { "Pinstun", "Stun Beside Buildings" }, { "Reel", "Reel" }, { "Undertow", "Undertow" },
            { "Brine", "Brine" }, { "Catch", "Kelp Hold" }, { "Brinepinned", "Brine on Pinned" }, { "Flood", "Extra Pools" },
            { "Damagetaken", "Damage Taken" }, { "Spread_Per", "Pools Per Tide" }, { "Dragunder", "Drag Under" }, { "Bedrock", "Bedrock Damage" },
            { "Fault", "Fault Burst" }, { "Healtharmor", "Armour" },
        })
            spec.Labels[kv.Key] = kv.Value;

        foreach (ArgoData.Build b in ArgoData.Builds)
        {
            if (b.Hidden)
                continue;
            Beingobject being;
            ArgoContent.Beings.TryGetValue(b.Id, out being);
            ModKitJournalBuilding jb = new ModKitJournalBuilding
            {
                Id = b.Id, Title = b.Display, Desc = b.Journal, Entity = being != null ? being.Entity : null, Model = ArgoAssets.JournalModel(b.ModelName),
            };
            Dictionary<string, ModKitJournalUpgrade> byKey = new Dictionary<string, ModKitJournalUpgrade>();
            foreach (ArgoData.Upg u in b.Upgrades)
            {
                Upgradeproperties up;
                ArgoContent.Upgrades.TryGetValue(u.Id, out up);
                byKey[u.Key + u.Tier] = new ModKitJournalUpgrade { Id = u.Id, Title = u.Display, Desc = u.Desc, Effects = u.Effects, Upgrade = up, Model = ArgoAssets.JournalModel(u.Model) ?? jb.Model };
            }
            foreach (ArgoData.Upg u in b.Upgrades)
            {
                ModKitJournalUpgrade ju = byKey[u.Key + u.Tier];
                foreach (string n in u.Next)
                {
                    ModKitJournalUpgrade next;
                    if (byKey.TryGetValue(n.Trim(), out next)) ju.Next.Add(next);
                }
                if (u.Tier == 2) jb.Roots.Add(ju);
            }
            if (b.Id == ArgoData.Bell)
                foreach (ArgoData.Crew c in ArgoData.Nauts)
                {
                    Beingobject nb;
                    if (!ArgoContent.Beings.TryGetValue(c.Being, out nb)) continue;
                    jb.Units.Add(new ModKitJournalUnit
                    {
                        Id = c.Being, Title = c.Name, Entity = nb.Entity, Look = ArgoAssets.Single("Naut_" + c.Name + "-Sheet", 16),
                        Desc = c.Name + " wears the " + c.Light + ". " + ArgoData.NautJournal,
                    });
                }
            spec.Buildings.Add(jb);
        }

        ModKitVesselJournal journal = new ModKitVesselJournal(spec);
        Page = journal.Build();
        Pages = journal.Pages;
        ArgoContent.Entry.Log = Page;
        ModKitVesselTabs.Sync();
        ArgoLog.Info("journal: " + Pages + " pages, tabs added " + ModKitVesselTabs.Added + ", duplicates removed " + ModKitVesselTabs.Removed);
    }

    // An idle frame, front facing right, like the shipped vessel pages (Smacky4-Sheet_0, Snosnoidle-Sheet_3).
    static Sprite Portrait()
    {
        List<Sprite> f = ArgoAssets.Frames("Argo-Sheet", 32, new Vector2(0.5f, 0.5f));
        return f.Count > 0 ? f[0] : null;
    }
}
