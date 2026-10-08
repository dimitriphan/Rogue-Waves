// Host-only rules. Everything that changes the game state happens here or in the brains, and reaches other players
// through the engine's own networking (spawns, stats, skins, upgrades, projectiles, Vis RPCs).
public static class ArgoServer
{
    public static int Tide;
    public static readonly List<ArgoHeroBrain> Heroes = new List<ArgoHeroBrain>();
    public static readonly List<ArgoTowerBrain> Towers = new List<ArgoTowerBrain>();
    public static readonly List<ArgoNautBrain> Nauts = new List<ArgoNautBrain>();
    static Game_Entity hookedEntity;
    static Game_Mapvars hookedMapvars;

    // Set by a Diving Bell just before it spawns a Naut (Onspawn fires synchronously inside the spawn call).
    public static ArgoTowerBrain PendingBell;
    public static Entity_Master PendingPost;

    public static void Update()
    {
        Game_Entity ge = Game_Entity.Instance;
        if (ge != hookedEntity)
        {
            hookedEntity = ge;
            Heroes.Clear();
            Towers.Clear();
            Nauts.Clear();
            ArgoHooks.Reset();
            if (ge != null)
            {
                ge.Onspawn += OnSpawn;
                ge.Ondespawn += OnDespawn;
            }
        }
        if (ge == null || !ge.IsServer)
            return;
        Game_Mapvars mv = Game_Mapvars.Instance;
        if (mv != hookedMapvars)
        {
            hookedMapvars = mv;
            if (mv != null)
            {
                Tide = mv.Wave.Value;
                mv.Wave.OnValueChanged += OnWave;
            }
        }
        Heroes.RemoveAll(h => h == null);
        Towers.RemoveAll(t => t == null);
        Nauts.RemoveAll(n => n == null);
        ArgoHooks.Update();
    }

    // ---------------------------------------------------------------- spawns

    static void OnSpawn(Entity_Master master)
    {
        try
        {
            if (!ArgoUtil.IsServer || master == null || master.Beingserver == null || master.Beingserver.Being == null)
                return;
            string being = master.Beingserver.Being.Objectname;
            if (being == ArgoData.Hero)
            {
                master.gameObject.AddComponent<ArgoHeroBrain>().Master = master;
                return;
            }
            ArgoData.Build build = ArgoData.BuildFor(being);
            if (build != null)
            {
                ArgoTowerBrain brain = master.gameObject.AddComponent<ArgoTowerBrain>();
                brain.Master = master;
                brain.Def = build;
                Towers.Add(brain);
                return;
            }
            ArgoData.Crew crew = ArgoData.CrewFor(being);
            if (crew != null)
            {
                ArgoNautBrain naut = master.gameObject.AddComponent<ArgoNautBrain>();
                naut.Master = master;
                naut.Crew = crew;
                naut.Bell = PendingBell;
                naut.Post = PendingPost;
                PendingBell = null;
                PendingPost = null;
                Nauts.Add(naut);
            }
        }
        catch (Exception e)
        {
            ArgoLog.Warn("spawn hook: " + e.Message);
        }
    }

    static void OnDespawn(Entity_Master master, bool killed, string reason)
    {
        try
        {
            if (!ArgoUtil.IsServer || master == null)
                return;
            ArgoNautBrain naut = master.GetComponent<ArgoNautBrain>();
            if (naut != null)
            {
                naut.Gone(killed);
                Nauts.Remove(naut);
                return;
            }
            ArgoTowerBrain tower = master.GetComponent<ArgoTowerBrain>();
            if (tower != null)
            {
                Towers.Remove(tower);
                if (killed)
                    ArgoUtil.Vis(master, "Argo_Towerdown", ArgoUtil.Pos(master), 0);
            }
            if (killed && master.GetComponent<ArgoHeroBrain>() != null)
                ArgoUtil.Vis(master, "Argo_Herodeath", ArgoUtil.Pos(master), 0);
            // Pay Dirt: every Drill whose Tremors hold a felled foe digs it up for ore.
            if (killed && tower == null && master.Taglistserver != null && master.Taglistserver.Contains("Enemy"))
            {
                Vector3 at = ArgoUtil.Pos(master);
                foreach (ArgoTowerBrain t in Towers.ToArray())
                    if (t != null && t.IsDrill && t.Has("Paydirt"))
                        t.PayDirt(master, at);
            }
            // Any tower can be someone's post: the Naut working there heads home.
            foreach (ArgoNautBrain n in Nauts)
                if (n != null && n.Post == master)
                    n.PostLost();
        }
        catch (Exception e)
        {
            ArgoLog.Warn("despawn hook: " + e.Message);
        }
    }

    // ---------------------------------------------------------------- Tides

    static void OnWave(int previous, int current)
    {
        if (!ArgoUtil.IsServer || current <= previous)
        {
            Tide = current;
            return;
        }
        Tide = current;
        try
        {
            foreach (ArgoTowerBrain t in Towers.ToArray())
                if (t != null) t.OnTide(current);
        }
        catch (Exception e)
        {
            ArgoLog.Warn("tide " + current + ": " + e);
        }
    }

    // ---------------------------------------------------------------- lookups

    // The Naut already working on this tower (one per tower, across every Diving Bell on the team).
    public static ArgoNautBrain TunerOf(Entity_Master tower)
    {
        foreach (ArgoNautBrain n in Nauts)
            if (n != null && n.Post == tower && n.Master != null)
                return n;
        return null;
    }

    public static ArgoTowerBrain TowerOf(Entity_Master master)
    {
        return master != null ? master.GetComponent<ArgoTowerBrain>() : null;
    }
}
