// Kit 1.3 (2026-10-03). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared vessel-mod kit: the journal, for every modded vessel (Sprigg, Argo, Mummer and any future one). It is Argo
// 1.0's VesselJournal + VesselTabs (verified in game with 7 vessels), moved here so each mod compiles the same code:
// the build scripts rename the "ModKit" prefix to the mod's own (MummerKit..., SpriggKit...), so no two mods define
// the same type. Argo 1.0 carries its own copy under the original names; the tab order rule below is identical, so all
// of them agree on every vessel page whatever order the mods load in.
//
// ModKitVesselJournal  builds one vessel's pages from a plain description (ModKitJournalSpec): a vessel page (idle
//                      portrait, name, special-rule banner, stats, lore, TOWERS & UNITS, vessel tabs), a page per
//                      building (model, stats, cost, NOTABLE UPGRADES, RELATED ITEMS for unit towers), a page per upgrade
//                      (model, banner, EFFECTS, NEXT UPGRADES) and a page per unit. Each page is cloned from the template
//                      vessel's matching page (Instantiate deep-copies its [SerializeReference] element list) and edited,
//                      so layout, fonts and colours stay the game's.
// ModKitVesselTabs     keeps the row of vessel tabs (a Grid of Link{Sidelink}) identical on every vessel page: one tab per
//                      vessel in the Hero folder, base vessels first in the game's order, then modded vessels by id. It
//                      only adds missing tabs, drops duplicates and sorts. Instantiate remaps a page's references to
//                      itself, so a page cloned from Smacky's turns Smacky's own tab into a tab for the clone (Sprigg 1.3
//                      lost Smacky's tab that way); the sync puts it back.
public class ModKitJournalSpec
{
    public string HeroId;                     // "Hero_Mummer": pages are Log_Hero_Mummer, Log_Hero_Mummer_<building id>, Log_<unit id>
    public Heroentryobject Template;          // the vessel whose pages are cloned (Smacky's)
    public Entityobject Hero;
    public Upgradeproperties Rule;            // the tier -3 tooltip upgrade, shown as a banner like Sno Sno's; null for none
    public string Title, Lore, Origin;
    public Sprite Portrait, Tab;              // an idle frame (like Smacky4-Sheet_0) and the 32x32 white line icon
    public Color Accent;
    public readonly List<ModKitJournalBuilding> Buildings = new List<ModKitJournalBuilding>();
    public readonly Dictionary<string, string> Labels = new Dictionary<string, string>();   // extra EFFECTS labels by parameter stat ("" hides one)
}

public class ModKitJournalBuilding
{
    public string Id, Title, Desc;
    public Entityobject Entity;
    public GameObject Model;
    public readonly List<ModKitJournalUpgrade> Roots = new List<ModKitJournalUpgrade>();
    public readonly List<ModKitJournalUnit> Units = new List<ModKitJournalUnit>();
}

public class ModKitJournalUpgrade
{
    public string Id, Title, Desc;
    // EFFECTS lines written for the player ("Mines 40% faster"). When set they replace the lines built from the upgrade's
    // parameters, whose names are the mod's own (a drill's "Damage" is how hard it mines, not damage to foes).
    public string[] Effects;
    public Upgradeproperties Upgrade;
    public GameObject Model;
    public readonly List<ModKitJournalUpgrade> Next = new List<ModKitJournalUpgrade>();
}

public class ModKitJournalUnit
{
    public string Id, Title, Desc;
    public Entityobject Entity;
    public Sprite Look;
}

public class ModKitVesselJournal
{
    readonly ModKitJournalSpec s;
    Logobject source, heroPage, upgradeTemplate, unitTemplate, plainBuilding, unitBuilding;
    Color templateColor;
    readonly Dictionary<ModKitJournalUpgrade, Logobject> upgradePages = new Dictionary<ModKitJournalUpgrade, Logobject>();
    readonly Dictionary<ModKitJournalUnit, Logobject> unitPages = new Dictionary<ModKitJournalUnit, Logobject>();
    public int Pages;

    public ModKitVesselJournal(ModKitJournalSpec spec) { s = spec; }

    public Logobject Build()
    {
        source = s.Template != null ? s.Template.Log : null;
        if (source == null || source.Elements == null)
            throw new Exception("template vessel has no journal page");
        templateColor = s.Template.Color;
        FindTemplates();
        heroPage = Clone(source, "Log_" + s.HeroId);
        heroPage.Title = s.Title;
        heroPage.Maindesc = s.Lore;
        heroPage.Unlockconditions = new List<Logobject.Condition>();
        if (s.Tab != null) heroPage.Thumbnail = s.Tab;
        Swap(heroPage.Elements, s.Portrait, s.Hero, null);
        AddRule();
        Towers();
        Pages = 1 + s.Buildings.Count + upgradePages.Count + unitPages.Count;
        return heroPage;
    }

    // ---------------------------------------------------------------- templates

    void FindTemplates()
    {
        foreach (Logitems.Base b in Flat(source.Elements))
        {
            Logitems.Link link = b as Logitems.Link;
            if (link == null || link.Sidelink || link.Target == null || link.Target.Parent != source)
                continue;
            bool units = false;
            foreach (Logitems.Base c in Flat(link.Target.Elements))
            {
                Logitems.LinkImage related = c as Logitems.LinkImage;
                if (related != null && related.Target != null && related.Target.name.StartsWith("Log_Mob_"))
                {
                    units = true;
                    if (unitTemplate == null) unitTemplate = related.Target;
                }
                Logitems.LinkUpgrade lu = c as Logitems.LinkUpgrade;
                if (upgradeTemplate == null && lu != null && lu.Target != null)
                    upgradeTemplate = lu.Target;
            }
            if (units && unitBuilding == null) unitBuilding = link.Target;
            if (!units && plainBuilding == null) plainBuilding = link.Target;
        }
        if (plainBuilding == null) plainBuilding = unitBuilding;
        if (unitBuilding == null) unitBuilding = plainBuilding;
        if (plainBuilding == null || upgradeTemplate == null)
            throw new Exception("template vessel has no building or upgrade pages");
    }

    // Instantiate copies a Logobject together with its element objects, so the clone can be edited without touching the
    // template's page. If a build of the game ever shares them instead, stop rather than edit the template's pages.
    static Logobject Clone(Logobject src, string name)
    {
        Logobject l = UnityEngine.Object.Instantiate(src);
        l.name = name;
        if (l.Elements != null && src.Elements != null && l.Elements.Count > 0 && ReferenceEquals(l.Elements[0], src.Elements[0]))
            throw new Exception("journal page elements are shared");
        return l;
    }

    // A detached copy of one element of a page. The scratch page is destroyed (its element objects live on), so it never
    // shows up as a stray page with a vessel-tab row of its own.
    static T Fresh<T>(Logobject page, Func<T, bool> match) where T : Logitems.Base
    {
        Logobject copy = UnityEngine.Object.Instantiate(page);
        T found = null;
        foreach (Logitems.Base b in Flat(copy.Elements))
            if (found == null && b is T && match((T)b))
                found = (T)b;
        UnityEngine.Object.Destroy(copy);
        return found;
    }

    public static List<Logitems.Base> Flat(List<Logitems.Base> items)
    {
        List<Logitems.Base> o = new List<Logitems.Base>();
        if (items == null) return o;
        foreach (Logitems.Base b in items)
        {
            if (b == null) continue;
            o.Add(b);
            if (b is Logitems.Group) o.AddRange(Flat(((Logitems.Group)b).Children));
            if (b is Logitems.DoubleColumn) { o.AddRange(Flat(((Logitems.DoubleColumn)b).leftChildren)); o.AddRange(Flat(((Logitems.DoubleColumn)b).rightChildren)); }
            if (b is Logitems.Link && ((Logitems.Link)b).Child != null) o.Add(((Logitems.Link)b).Child);
        }
        return o;
    }

    static bool Decor(Sprite sp)
    {
        return sp == null || sp.name.StartsWith("Fill") || sp.name.StartsWith("Dotline");
    }

    static bool Near(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;
    }

    // Puts the vessel's things where the template's were: portrait, being (name and icon), stats and cost, model, the
    // accent colour on the side bars, and the Origins line.
    void Swap(List<Logitems.Base> items, Sprite portrait, Entityobject entity, Transform model)
    {
        foreach (Logitems.Base b in Flat(items))
        {
            if (b is Logitems.Image)
            {
                Logitems.Image x = (Logitems.Image)b;
                if (!Decor(x.Source)) { if (portrait != null) x.Source = portrait; }
                else if (x.Source != null && x.Source.name == "Fillbox" && Near(x.color, templateColor)) x.color = new Color(s.Accent.r, s.Accent.g, s.Accent.b, x.color.a);
            }
            else if (b is Logitems.Image3D && model != null) ((Logitems.Image3D)b).Source = model;
            else if (b is Logitems.TextImageBeing && entity != null) ((Logitems.TextImageBeing)b).Entity = entity;
            else if (b is Logitems.Stats && entity != null) ((Logitems.Stats)b).stats = entity.Defaultstat;
            else if (b is Logitems.Drops && entity != null) ((Logitems.Drops)b).entity = entity;
            else if (b is Logitems.Text && ((Logitems.Text)b).text != null && ((Logitems.Text)b).text.StartsWith("Origins")) ((Logitems.Text)b).text = "Origins - " + s.Origin;
        }
    }

    // The special rule, placed where Sno Sno's page has it: right under the name row.
    void AddRule()
    {
        if (s.Rule == null)
            return;
        List<Logitems.Base> top = heroPage.Elements;
        if (top.Exists(b => b is Logitems.LinkUpgrade && ((Logitems.LinkUpgrade)b).Target == null))
            return;
        int at = top.FindIndex(b => b is Logitems.Group && ((Logitems.Group)b).Children != null && ((Logitems.Group)b).Children.Exists(c => c is Logitems.TextImageBeing));
        top.Insert(at >= 0 ? at + 1 : Mathf.Min(4, top.Count), new Logitems.LinkUpgrade { Target = null, Targetupgrade = s.Rule, Size = new Vector2(176f, 40f), Sidelink = false });
    }

    // TOWERS & UNITS: one spinning model per building, linked to its page.
    void Towers()
    {
        foreach (Logitems.Base b in Flat(heroPage.Elements))
        {
            Logitems.Group g = b as Logitems.Group;
            if (g == null || g.Children == null)
                continue;
            List<Logitems.Link> links = new List<Logitems.Link>();
            foreach (Logitems.Base c in g.Children)
            {
                Logitems.Link l = c as Logitems.Link;
                if (l != null && !l.Sidelink && l.Target != null && l.Target.Parent == source)
                    links.Add(l);
            }
            if (links.Count == 0)
                continue;
            // The template's own link items are reused (and deep-copied from a fresh clone of its page when this vessel has
            // more buildings): brand-new Link + Image3D items did not draw, so every field must be set as on a shipped page.
            int at = g.Children.IndexOf(links[0]);
            g.Children.RemoveAll(c => links.Contains(c as Logitems.Link));
            for (int i = 0; i < s.Buildings.Count; i++)
            {
                ModKitJournalBuilding building = s.Buildings[i];
                Logitems.Link link = i < links.Count ? links[i] : Fresh<Logitems.Link>(source, l => !l.Sidelink && l.Target != null && l.Target.Parent == source && l.Child is Logitems.Image3D);
                if (link == null)
                    continue;
                Logitems.Image3D picture = link.Child as Logitems.Image3D;
                if (picture != null && building.Model != null)
                    picture.Source = building.Model.transform;
                link.Target = BuildingPage(building);
                g.Children.Insert(Mathf.Min(at++, g.Children.Count), link);
            }
            return;
        }
    }

    // ---------------------------------------------------------------- building, upgrade and unit pages

    Logobject BuildingPage(ModKitJournalBuilding b)
    {
        Logobject page = Clone(b.Units.Count > 0 ? unitBuilding : plainBuilding, "Log_" + s.HeroId + "_" + b.Id);
        page.Title = b.Title;
        page.Maindesc = b.Desc;
        page.Parent = heroPage;
        page.Unlockconditions = new List<Logobject.Condition>();
        Swap(page.Elements, null, b.Entity, b.Model != null ? b.Model.transform : null);
        foreach (Logitems.Base x in Flat(page.Elements))
        {
            Logitems.Group g = x as Logitems.Group;
            if (g == null || g.Children == null)
                continue;
            if (g.Children.Exists(c => c is Logitems.LinkUpgrade))
                Links(g.Children, b.Roots);
            if (g.Children.Exists(c => c is Logitems.LinkImage && ((Logitems.LinkImage)c).Target != null && ((Logitems.LinkImage)c).Target.name.StartsWith("Log_Mob_")))
                Related(g.Children, b.Units);
        }
        return page;
    }

    // NOTABLE UPGRADES / NEXT UPGRADES: the template's LinkUpgrade entries become links to this vessel's upgrade pages.
    void Links(List<Logitems.Base> children, List<ModKitJournalUpgrade> upgrades)
    {
        Logitems.LinkUpgrade sample = (Logitems.LinkUpgrade)children.Find(c => c is Logitems.LinkUpgrade && ((Logitems.LinkUpgrade)c).Target != null);
        if (sample == null)
            return;
        int at = children.IndexOf(sample);
        children.RemoveAll(c => c is Logitems.LinkUpgrade && ((Logitems.LinkUpgrade)c).Target != null);
        foreach (ModKitJournalUpgrade u in upgrades)
            if (u.Upgrade != null)
                children.Insert(Mathf.Min(at++, children.Count), new Logitems.LinkUpgrade { Target = UpgradePage(u), Targetupgrade = u.Upgrade, Size = sample.Size, Sidelink = sample.Sidelink });
    }

    // RELATED ITEMS: the template's unit links become links to this vessel's unit pages.
    void Related(List<Logitems.Base> children, List<ModKitJournalUnit> units)
    {
        Logitems.LinkImage sample = (Logitems.LinkImage)children.Find(c => c is Logitems.LinkImage);
        int at = children.IndexOf(sample);
        children.RemoveAll(c => c is Logitems.LinkImage);
        foreach (ModKitJournalUnit u in units)
            children.Insert(Mathf.Min(at++, children.Count), new Logitems.LinkImage
            {
                Target = UnitPage(u), Size = sample.Size, Stretch = sample.Stretch, Sidelink = sample.Sidelink, Tooltip = sample.Tooltip, Color = sample.Color,
            });
    }

    Logobject UpgradePage(ModKitJournalUpgrade u)
    {
        Logobject page;
        if (upgradePages.TryGetValue(u, out page))
            return page;
        page = Clone(upgradeTemplate, "Log_" + u.Id);
        upgradePages[u] = page;
        page.Maindesc = u.Title + ": " + u.Desc + ".";
        page.Unlockconditions = new List<Logobject.Condition>();
        Swap(page.Elements, null, null, u.Model != null ? u.Model.transform : null);
        Logitems.Group nextList = null;
        foreach (Logitems.Base b in Flat(page.Elements))
        {
            if (b is Logitems.LinkUpgrade && ((Logitems.LinkUpgrade)b).Target == null)
                ((Logitems.LinkUpgrade)b).Targetupgrade = u.Upgrade;   // the banner at the top of the page
            Logitems.Group g = b as Logitems.Group;
            if (g == null || g.Children == null)
                continue;
            if (g.Children.Exists(c => c is Logitems.TextUpgradeParam))
                Effects(g.Children, u);
            else if (g.Children.Exists(c => c is Logitems.LinkUpgrade && ((Logitems.LinkUpgrade)c).Target != null))
                nextList = g;
        }
        if (nextList != null)
        {
            if (u.Next.Count > 0) Links(nextList.Children, u.Next);
            else page.Elements.Remove(nextList);
        }
        return page;
    }

    Logobject UnitPage(ModKitJournalUnit u)
    {
        Logobject page;
        if (unitPages.TryGetValue(u, out page))
            return page;
        page = Clone(unitTemplate != null ? unitTemplate : plainBuilding, "Log_" + u.Id);
        unitPages[u] = page;
        page.Title = u.Title;
        page.Maindesc = u.Desc;
        page.Parent = heroPage;
        page.Unlockconditions = new List<Logobject.Condition>();
        if (u.Look != null) page.Thumbnail = u.Look;
        Swap(page.Elements, u.Look, u.Entity, null);
        return page;
    }

    // EFFECTS: the upgrade's own lines when it has them (drawn by the game's TextUpgradeParam with Style.None, which
    // prints "[ text ]"), otherwise one line per parameter the player cares about, formatted from the upgrade's values.
    void Effects(List<Logitems.Base> children, ModKitJournalUpgrade u)
    {
        int at = children.FindIndex(c => c is Logitems.TextUpgradeParam);
        children.RemoveAll(c => c is Logitems.TextUpgradeParam);
        Upgradeproperties up = u.Upgrade;
        if (up == null)
            return;
        if (u.Effects != null)
        {
            foreach (string text in u.Effects)
            {
                Logitems.TextUpgradeParam line = string.IsNullOrEmpty(text) ? null : Fresh<Logitems.TextUpgradeParam>(upgradeTemplate, x => true);
                if (line == null)
                    continue;
                line.Item = "";   // never null: Run looks it up in Defaultparams
                line.Upgrade = up;
                line.Prefix = text;
                line.Displaystyle = Logitems.TextUpgradeParam.Style.None;
                children.Insert(Mathf.Min(at++, children.Count), line);
            }
            return;
        }
        if (up.Defaultparams == null)
            return;
        foreach (KeyValuePair<string, float> kv in up.Defaultparams)
        {
            string label = Label(kv.Key);
            Logitems.TextUpgradeParam line = !string.IsNullOrEmpty(label) ? Fresh<Logitems.TextUpgradeParam>(upgradeTemplate, x => true) : null;
            if (line == null)
                continue;
            line.Item = kv.Key;
            line.Upgrade = up;
            line.Prefix = label;
            line.Displaystyle = Style(kv.Key);
            children.Insert(Mathf.Min(at++, children.Count), line);
        }
    }

    string Label(string key)
    {
        string stat = key.Contains("_") ? key.Substring(0, key.LastIndexOf('_')) : key;
        string mine;
        if (s.Labels.TryGetValue(stat, out mine))
            return mine;
        switch (stat)
        {
            case "Damage": return "Damage";
            case "Attackspeed": return "Attack Speed";
            case "Range": return "Range";
            case "Health": return "Health";
            case "Projspeed": return "Projectile Speed";
            case "Slow": return "Slow";
            case "Duration": return "Duration";
            case "Units": return "Units";
            case "Effect": return "Effect";
            default: return null;
        }
    }

    static Logitems.TextUpgradeParam.Style Style(string key)
    {
        if (key.EndsWith("_Multi") || key.EndsWith("_Pmulti") || key.EndsWith("_Percent"))
            return Logitems.TextUpgradeParam.Style.Percent;
        if (key.EndsWith("_Base"))
            return Logitems.TextUpgradeParam.Style.Float;
        if (key.EndsWith("_Max") || key.EndsWith("_Every") || key.EndsWith("_Count"))
            return Logitems.TextUpgradeParam.Style.Int;
        return Logitems.TextUpgradeParam.Style.Add;
    }
}

public static class ModKitVesselTabs
{
    // The shipped vessels, in the game's own tab order; everything else follows, sorted by id. Argo 1.0's VesselTabs
    // uses the same rule, so every vessel mod produces the same rows.
    static readonly string[] Base = { "Hero_Smacky", "Hero_Snosno", "Hero_Buster", "Hero_Boutman", "Hero_Fie" };
    static readonly List<Logobject> rowsOn = new List<Logobject>();
    static int knownHeroes = -1;
    static float nextFullScan, nextSync;
    public static int Added, Removed, Sorted;

    static List<Logobject> HeroPages()
    {
        List<Heroentryobject> heroes = new List<Heroentryobject>();
        Dictionary<string, UnityEngine.Object> folder = null;
        Tds_Masterdatabase db = Tds_Masterdatabase.Instance;
        if (db != null && db.Items != null)
            db.Items.TryGetValue("Hero", out folder);
        if (folder != null)
            foreach (UnityEngine.Object o in folder.Values)
            {
                Heroentryobject h = o as Heroentryobject;
                if (h != null && h.Log != null)
                    heroes.Add(h);
            }
        heroes.Sort((a, b) =>
        {
            int ia = Array.IndexOf(Base, a.Objectname), ib = Array.IndexOf(Base, b.Objectname);
            if (ia < 0) ia = 100;
            if (ib < 0) ib = 100;
            return ia != ib ? ia.CompareTo(ib) : string.CompareOrdinal(a.Objectname, b.Objectname);
        });
        List<Logobject> pages = new List<Logobject>();
        foreach (Heroentryobject h in heroes)
            if (!pages.Contains(h.Log))
                pages.Add(h.Log);
        return pages;
    }

    public static void Sync()
    {
        List<Logobject> pages = HeroPages();
        if (pages.Count == 0)
            return;
        // Tab rows live on the vessel pages; look further only now and then (or when a vessel appears).
        if (pages.Count != knownHeroes || Time.unscaledTime >= nextFullScan)
        {
            knownHeroes = pages.Count;
            nextFullScan = Time.unscaledTime + 30f;
            rowsOn.Clear();
            foreach (Logobject page in Resources.FindObjectsOfTypeAll<Logobject>())
                if (page != null && page.Elements != null && Row(page.Elements, pages) != null)
                    rowsOn.Add(page);
            foreach (Logobject p in pages)
                if (!rowsOn.Contains(p) && Row(p.Elements, pages) != null)
                    rowsOn.Add(p);
        }
        foreach (Logobject page in rowsOn)
        {
            if (page == null || page.Elements == null)
                continue;
            Logitems.Group row = Row(page.Elements, pages);
            if (row != null)
                Fix(row, pages);
        }
    }

    // Call from a MonoBehaviour's Update: keeps every vessel page's tabs complete as other mods add their vessels.
    public static void Tick()
    {
        if (Time.unscaledTime < nextSync)
            return;
        nextSync = Time.unscaledTime + 3f;
        try { Sync(); }
        catch (Exception e) { Debug.LogWarning("[ModKit] vessel tabs: " + e.Message); nextSync = Time.unscaledTime + 30f; }
    }

    // The Grid holding the vessel tabs: a group with sidelinks to vessel pages.
    static Logitems.Group Row(List<Logitems.Base> items, List<Logobject> pages)
    {
        foreach (Logitems.Base b in ModKitVesselJournal.Flat(items))
        {
            Logitems.Group g = b as Logitems.Group;
            if (g == null || g.Children == null)
                continue;
            foreach (Logitems.Base c in g.Children)
            {
                Logitems.Link l = c as Logitems.Link;
                if (l != null && l.Sidelink && l.Target != null && pages.Contains(l.Target))
                    return g;
            }
        }
        return null;
    }

    static void Fix(Logitems.Group row, List<Logobject> pages)
    {
        List<int> slots = new List<int>();
        List<Logitems.Link> tabs = new List<Logitems.Link>();
        List<Logobject> seen = new List<Logobject>();
        for (int i = 0; i < row.Children.Count; i++)
        {
            Logitems.Link l = row.Children[i] as Logitems.Link;
            if (l == null || !l.Sidelink || l.Target == null || !pages.Contains(l.Target))
                continue;
            if (seen.Contains(l.Target))
            {
                row.Children.RemoveAt(i--);
                Removed++;
                continue;
            }
            seen.Add(l.Target);
            slots.Add(i);
            tabs.Add(l);
        }
        foreach (Logobject p in pages)
            if (!seen.Contains(p))
            {
                Logitems.Link tab = new Logitems.Link { Target = p, Sidelink = true, Tooltip = true };
                row.Children.Add(tab);
                slots.Add(row.Children.Count - 1);
                tabs.Add(tab);
                Added++;
            }
        List<Logitems.Link> sorted = new List<Logitems.Link>(tabs);
        sorted.Sort((a, b) => pages.IndexOf(a.Target).CompareTo(pages.IndexOf(b.Target)));
        bool changed = false;
        for (int i = 0; i < slots.Count; i++)
            if (!ReferenceEquals(row.Children[slots[i]], sorted[i]))
            {
                row.Children[slots[i]] = sorted[i];
                changed = true;
            }
        if (changed)
            Sorted++;
    }
}
