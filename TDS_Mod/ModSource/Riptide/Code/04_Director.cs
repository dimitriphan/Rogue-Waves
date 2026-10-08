// The run director, host only. The game runs a Riptide match on its stock Mode_Normal; the director steers it from
// outside:
//   - on the world map, while the RIPTIDE switch is on, it turns the picked node's Gameplan and mode into Riptide's;
//   - in a match, it starts a run (seed, fresh table), writes each Tide into the table at the start of the break before
//     it (Mode_Normal reads each entry live when it starts, and only on the host), and records the end of the run.
// Run state lives in statics so it survives scene loads. Clients never read the table, so its randomness is safe.
public static class RiptideDirector
{
    // The host's RIPTIDE switch (RiptideSwitch puts it on the Setup screen; the self-test sets it directly).
    public static bool Switch;

    public static bool Running, Over;
    public static int Seed, Tide, Written;
    public static string Outcome = "";
    public static readonly List<string> Announced = new List<string>();   // banner lines already used this run
    public static readonly Dictionary<int, string> Notes = new Dictionary<int, string>();
    public static readonly Dictionary<int, List<Wavesobject.Wavecontent>> Plans = new Dictionary<int, List<Wavesobject.Wavecontent>>();

    static System.Random rng;
    static int lastEntry = -1;
    static readonly RiptideFaults faults = new RiptideFaults();
    static bool stopped;

    public static void Update()
    {
        if (stopped)
            return;
        try
        {
            WorldMap();
            Match();
        }
        catch (Exception e) { stopped = faults.Fail("director", e); }
    }

    // ---------------------------------------------------------------- the switch (world map, host)

    static void WorldMap()
    {
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        if (info == null || !info.IsServer || Macro_Main.Instance == null || Game_Mapvars.Instance != null)
            return;
        if (Running)
            EndRun("left the match");
        RiptideArtifacts.RestoreLobby();
        string mode = info.Mode.Value.ToString();
        string plan = info.Gameplan.Value.ToString();
        if (Switch && System.Array.IndexOf(RiptideData.SwappableModes, mode) >= 0)
        {
            string twin;
            if (RiptideContent.Twins.TryGetValue(plan, out twin))
                info.Gameplan.Value = twin;
            info.Mode.Value = RiptideData.Mode;
            RiptideLog.Info("switch on: " + plan + " (" + mode + ") becomes " + info.Gameplan.Value + " (" + RiptideData.Mode + ")");
        }
        else if (!Switch && mode == RiptideData.Mode)
        {
            string stock = RiptideContent.StockOf(plan);
            Gameplandatabaseobject gp = RiptideUtil.Entry<Gameplandatabaseobject>("Gameplan", stock ?? plan);
            if (stock != null)
                info.Gameplan.Value = stock;
            info.Mode.Value = gp != null && gp.Mode != null ? gp.Mode.Objectname : RiptideData.TemplateMode;
            RiptideLog.Info("switch off: back to " + info.Gameplan.Value + " (" + info.Mode.Value + ")");
        }
    }

    // ---------------------------------------------------------------- the run (match, host)

    static void Match()
    {
        if (!RiptideUtil.InRun || !Game_Entity.Instance.IsServer)
        {
            if (Running && !RiptideUtil.InMatch)
                EndRun("left the match");
            if (!RiptideUtil.InMatch)
                Over = false;
            return;
        }
        if (!Running)
        {
            if (Over)
                return;     // the run ended in this match (the end screen is up): the next match starts the next run
            StartRun();
        }
        Game_Mapvars mv = Game_Mapvars.Instance;
        int entry = mv.Entry.Value;
        if (entry != lastEntry)
        {
            lastEntry = entry;
            OnEntry(entry);
        }
        if (!Over)
        {
            string done = mv.Gamecomplete.Value.ToString();
            if (done == "Win" || done == "Lose")
                EndRun(done == "Win" ? "the Basin remains after Tide " + mv.Wave.Value : "the Basin drowned on Tide " + mv.Wave.Value);
        }
    }

    public static void StartRun()
    {
        Running = true;
        Over = false;
        Outcome = "";
        Seed = Environment.TickCount & 0x7fffffff;
        rng = new System.Random(Seed);
        Tide = 0;
        Written = 0;
        lastEntry = -1;
        Announced.Clear();
        Notes.Clear();
        Plans.Clear();
        RiptideContent.Table.Wave.Clear();
        foreach (Wavesobject.Waves w in RiptideContent.Placeholder)
            RiptideContent.Table.Wave.Add(RiptideContent.Copy(w));
        RiptideArtifacts.BeginRun();
        RiptideSalvage.BeginRun(Seed);
        RiptideLog.Info("run starts: seed " + Seed + ", " + RiptideUtil.Players() + " player(s), map " + Tds_Multiplayerinfo.Instance.Map.Value
            + ", gameplan " + Tds_Multiplayerinfo.Instance.Gameplan.Value);
        // Mode_Normal waits Startwait before its first entry (a break): Tide 1 is written now.
        Write(1);
    }

    public static void EndRun(string how)
    {
        if (!Running)
            return;
        Over = true;
        Running = false;
        Outcome = how;
        RiptideSalvage.Close();
        RiptideLog.Info("run over: " + how + ", " + RiptideArtifacts.Salvaged.Count + " salvaged, seed " + Seed);
        if (how.StartsWith("the Basin drowned"))
            CoreShot();
    }

    // Mode_Normal plays the core-destroyed camera shot only for the stock mode names; Riptide plays it itself, on the
    // core that fell (any fallen core ends the run).
    static void CoreShot()
    {
        if (Game_Entity.Instance == null)
            return;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Beingserver != null && e.Beingserver.Dead && RiptideUtil.HasTag(e, "Core") && e.Radio != null)
            {
                e.Radio.Vfxoneshotcheck("Cine_Coredestroy", e.Entityposition.Value, Vector3.forward);
                return;
            }
    }

    // Entry n (1-based) has just started. The table is break, Tide, break, Tide...: a break at index 2(t-1) comes before
    // Tide t at index 2t-1.
    static void OnEntry(int entry)
    {
        int i = entry - 1;
        List<Wavesobject.Waves> table = RiptideContent.Table.Wave;
        if (i < 0 || i >= table.Count)
            return;
        if (table[i].Wave)
        {
            Tide = (i + 1) / 2;
            return;
        }
        int next = i / 2 + 1;           // the Tide after this break
        int cleared = i / 2;            // the Tide before it (0 for the opening break)
        if (next <= RiptideData.RunTides && next > Written)
            Write(next);
        if (cleared >= 1 && RiptideData.IsSalvage(cleared))
            RiptideSalvage.Begin(cleared);
    }

    // ---------------------------------------------------------------- writing a Tide

    public static float Threat(RiptideData.Foe f, Beingobject being)
    {
        return f.Threat > 0f ? f.Threat : Mathf.Max(1f, RiptideUtil.BaseStat(being, "Health", 10f) / 10f);   // in Daggers
    }

    // Writes Tide t into the table (index 2t-1). Keeps the stock entry's aura handouts (as a spawn-free Wait), then
    // spends the Tide's budget on packs.
    public static void Write(int t)
    {
        int idx = 2 * t - 1;
        List<Wavesobject.Waves> table = RiptideContent.Table.Wave;
        if (idx < 0 || idx >= table.Count)
            return;
        Wavesobject.Waves w = table[idx];
        List<Wavesobject.Wavecontent> content = new List<Wavesobject.Wavecontent>();
        foreach (Wavesobject.Wavecontent c in RiptideContent.Placeholder[idx].Content)
            if (c.Resources != null && c.Resources.Count > 0)
                content.Add(Pack(null, 1, 0f, null, c.Resources, Wavesobject.Wavestyle.Wait));

        List<Wavesobject.Wavecontent> packs = Plan(t);
        content.AddRange(packs);
        w.Content = content;
        w.Note = Note(t, packs);
        w.Displayedwave = t;
        w.Wave = true;
        table[idx] = w;
        Written = t;
        Notes[t] = w.Note;
        Plans[t] = packs;
        List<string> lines = new List<string>();
        foreach (Wavesobject.Wavecontent c in content)
            lines.Add(RiptideUtil.Describe(c));
        RiptideLog.Info("tide " + t + (RiptideData.IsBreaker(t) ? " (Breaker)" : "") + ", " + RiptideData.Stretch(t) + ", budget "
            + RiptideUtil.F(RiptideData.Budget(t, RiptideUtil.Players())) + ", spent " + RiptideUtil.F(Spent) + ": "
            + string.Join("; ", lines.ToArray()) + " | note '" + w.Note + "'");
    }

    static float Spent;

    static Wavesobject.Wavecontent Pack(Beingobject enemy, int count, float interval, List<Upgradeproperties> upgrades,
        List<Link_Resourceset> resources, Wavesobject.Wavestyle style)
    {
        Wavesobject.Wavecontent c = new Wavesobject.Wavecontent();
        c.Style = style;
        c.Interval = interval;
        c.Count = count;
        c.Team = 0;
        c.Enemy = enemy;
        c.Gate = 0;
        c.Upgrades = upgrades ?? new List<Upgradeproperties>();
        c.Resources = resources != null ? new List<Link_Resourceset>(resources) : new List<Link_Resourceset>();
        return c;
    }

    struct Option
    {
        public RiptideData.Foe Foe;
        public Beingobject Being;
        public float Threat;
    }

    static List<Option> Unlocked(int t)
    {
        List<Option> list = new List<Option>();
        foreach (RiptideData.Foe f in RiptideData.Foes)
        {
            if (f.From > t)
                continue;
            Beingobject b = RiptideUtil.Load<Beingobject>("Mob", f.Id);
            if (b == null)
                continue;
            Option o = new Option();
            o.Foe = f;
            o.Being = b;
            o.Threat = Threat(f, b);
            list.Add(o);
        }
        return list;
    }

    static List<RiptideData.Elite> UnlockedElites(int t)
    {
        List<RiptideData.Elite> list = new List<RiptideData.Elite>();
        foreach (RiptideData.Elite e in RiptideData.Elites)
            if (e.From <= t && RiptideUtil.Load<Upgradeproperties>("Upgrade", e.Upgrade) != null)
                list.Add(e);
        return list;
    }

    // Spends Tide t's budget: on a Breaker, a headline pack first (a boss once one has arrived, else a few of the
    // strongest foe), then 1-4 packs, each a share of what is left. A pack is elite by chance; each pack's foe is one
    // whose count for that share lands between PackMin and PackMax, so later Tides bring heavier foes, not just more.
    static List<Wavesobject.Wavecontent> Plan(int t)
    {
        List<Wavesobject.Wavecontent> packs = new List<Wavesobject.Wavecontent>();
        Spent = 0f;
        List<Option> foes = new List<Option>(), bosses = new List<Option>();
        foreach (Option o in Unlocked(t))
        {
            if (o.Foe.Boss) bosses.Add(o);
            else foes.Add(o);
        }
        if (foes.Count == 0)
        {
            RiptideLog.Warn("tide " + t + ": no foe of the pool is loaded; keeping the stock Tide");
            return Copyfoes(t);
        }
        List<RiptideData.Elite> elites = UnlockedElites(t);
        float budget = RiptideData.Budget(t, RiptideUtil.Players());
        int count = rng.Next(RiptideData.PacksMin(t), RiptideData.PacksMax(t) + 1);

        if (RiptideData.IsBreaker(t))
        {
            bool boss = bosses.Count > 0;
            Option head = boss ? bosses[rng.Next(bosses.Count)] : foes[0];
            if (!boss)
                foreach (Option o in foes)
                    if (o.Threat > head.Threat) head = o;
            RiptideData.Elite? elite = null;
            if (t >= RiptideData.EliteHeadlineFrom && elites.Count > 0)
            {
                elite = elites[rng.Next(elites.Count)];
                if (head.Threat * elite.Value.Cost > budget * 0.8f)
                    elite = null;       // a boss that would eat the whole Tide stays plain
            }
            float cost = head.Threat * (elite.HasValue ? elite.Value.Cost : 1f);
            int n = boss ? 1 : Mathf.Clamp(Mathf.RoundToInt(budget * RiptideData.HeadlineShare / cost), 1, RiptideData.HeadlineMax);
            packs.Add(Pack(head.Being, n, RiptideData.Interval(n, head.Threat), Ups(elite), null, Wavesobject.Wavestyle.Spread));
            Spent += n * cost;
            count = Mathf.Max(1, count - 1);
        }

        for (int p = 0; p < count; p++)
        {
            float share = Mathf.Max(0f, budget - Spent) / (count - p);
            RiptideData.Elite? elite = null;
            if (elites.Count > 0 && rng.NextDouble() < RiptideData.EliteChance(t))
                elite = elites[rng.Next(elites.Count)];
            float mult = elite.HasValue ? elite.Value.Cost : 1f;
            Option o = Pick(foes, share / mult);
            float cost = o.Threat * mult;
            int n = Mathf.Clamp(Mathf.FloorToInt(share / cost), 1, RiptideData.PackMax);
            if (packs.Count > 0)
                packs.Add(Pack(null, 0, RiptideData.PackGap, null, null, Wavesobject.Wavestyle.Wait));
            packs.Add(Pack(o.Being, n, RiptideData.Interval(n, o.Threat), Ups(elite), null, Wavesobject.Wavestyle.Spread));
            Spent += n * cost;
        }
        return packs;
    }

    // A foe for one pack worth 'target' Daggers: one whose count lands between PackMin and PackMax, the newest of them
    // twice as likely as each other. When none fits, the cheapest (a small share) or the heaviest (a big one).
    static Option Pick(List<Option> foes, float target)
    {
        List<Option> fit = new List<Option>();
        Option cheapest = foes[0], heaviest = foes[0];
        foreach (Option o in foes)
        {
            float n = target / o.Threat;
            if (n >= RiptideData.PackMin && n <= RiptideData.PackMax) fit.Add(o);
            if (o.Threat < cheapest.Threat) cheapest = o;
            if (o.Threat > heaviest.Threat) heaviest = o;
        }
        if (fit.Count == 0)
            return target / cheapest.Threat < RiptideData.PackMin ? cheapest : heaviest;
        Option newest = fit[0];
        foreach (Option o in fit)
            if (o.Foe.From > newest.Foe.From) newest = o;
        int roll = rng.Next(fit.Count + 1);
        return roll >= fit.Count ? newest : fit[roll];
    }

    static List<Upgradeproperties> Ups(RiptideData.Elite? elite)
    {
        List<Upgradeproperties> list = new List<Upgradeproperties>();
        if (elite.HasValue)
        {
            Upgradeproperties u = RiptideUtil.Load<Upgradeproperties>("Upgrade", elite.Value.Upgrade);
            if (u != null) list.Add(u);
        }
        return list;
    }

    // Fallback: the placeholder's own foes (the stock Tide for this number).
    static List<Wavesobject.Wavecontent> Copyfoes(int t)
    {
        List<Wavesobject.Wavecontent> list = new List<Wavesobject.Wavecontent>();
        foreach (Wavesobject.Wavecontent c in RiptideContent.Placeholder[2 * t - 1].Content)
            if (c.Enemy != null)
                list.Add(c);
        return list;
    }

    // One banner line per Tide: the last Tide, then a Breaker, then a foe or an elite seen for the first time this run,
    // then a new stretch.
    static string Note(int t, List<Wavesobject.Wavecontent> packs)
    {
        List<string> lines = new List<string>();
        if (t == RiptideData.RunTides) lines.Add(RiptideData.NoteLast);
        if (RiptideData.IsBreaker(t)) lines.Add(RiptideData.NoteBreaker);
        foreach (RiptideData.Foe f in RiptideData.Foes)
            foreach (Wavesobject.Wavecontent c in packs)
                if (c.Enemy != null && c.Enemy.Objectname == f.Id)
                    lines.Add(RiptideData.NoteFoe(f));
        foreach (RiptideData.Elite e in RiptideData.Elites)
            foreach (Wavesobject.Wavecontent c in packs)
                if (c.Upgrades != null && c.Upgrades.Exists(u => u != null && u.Objectname == e.Upgrade))
                    lines.Add(RiptideData.NoteElite(e));
        lines.Add(RiptideData.NoteStretch(t));
        foreach (string line in lines)
        {
            if (line == "" || (Announced.Contains(line) && line != RiptideData.NoteBreaker))
                continue;
            Announced.Add(line);
            return line;
        }
        return "";
    }
}
