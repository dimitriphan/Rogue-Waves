// Host-only rules. Everything that changes the game state happens here or in the brains, and reaches other players
// through the engine's own networking (spawns, stats, skins, upgrades, projectiles, Vis RPCs).
public static class ArgoServer
{
    public static int Tide;
    public static readonly List<ArgoHeroBrain> Heroes = new List<ArgoHeroBrain>();
    public static readonly List<ArgoTowerBrain> Towers = new List<ArgoTowerBrain>();
    public static readonly List<ArgoNautBrain> Nauts = new List<ArgoNautBrain>();
    static Game_Entity hookedEntity;
    static bool tidesHooked;

    // Set by a Miner's Base just before it spawns a Naut (Onspawn fires synchronously inside the spawn call).
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
            ArgoStatus.Reset();
            ArgoWater.Reset();
            if (ge != null)
            {
                ge.Onspawn += OnSpawn;
                ge.Ondespawn += OnDespawn;
            }
        }
        // Tides come from the kit's one poll of Game_Mapvars (kit 1.6, ModKitTides; host only).
        if (!tidesHooked)
        {
            tidesHooked = true;
            ModKitTides.Tide += OnTide;
            ModKitTides.MapStart += () => { Tide = ModKitTides.Wave; };
        }
        ModKitTides.Update();
        if (ge == null || !ge.IsServer)
            return;
        Heroes.RemoveAll(h => h == null);
        Towers.RemoveAll(t => t == null);
        Nauts.RemoveAll(n => n == null);
        ArgoHooks.Update();
        ArgoWater.Update();
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
                if (build.Id == ArgoData.Bell)
                    BaseLimit(ArgoUtil.Owner(master));
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
                // The Miner's Base button comes back once its owner's Base is gone (sold or destroyed).
                if (tower.IsBell)
                    BaseLimit(ArgoUtil.Owner(master));
            }
            if (killed && master.GetComponent<ArgoHeroBrain>() != null)
                ArgoUtil.Vis(master, "Argo_Herodeath", ArgoUtil.Pos(master), 0);
            // Pay Dirt: every Drill Rig whose Tremor holds a felled foe digs it up for ore.
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

    static void OnTide(int current)
    {
        Tide = current;
        if (!ArgoUtil.IsServer)
            return;
        try
        {
            foreach (ArgoTowerBrain t in Towers.ToArray())
                if (t != null) t.OnTide(current);
            // Trench spreads after every Drill has sunk.
            ArgoWater.OnTide();
        }
        catch (Exception e)
        {
            ArgoLog.Warn("tide " + current + ": " + e);
        }
    }

    // ---------------------------------------------------------------- 2.0 rules across buildings

    // The Miner's Base limit (one per player, Boutman's Bouthouse pattern): every Argo of that owner shows or hides its
    // fifth build option now, rather than at its next check.
    public static void BaseLimit(long owner)
    {
        foreach (ArgoHeroBrain h in Heroes)
            if (h != null && h.Master != null && ArgoUtil.Owner(h.Master) == owner)
                h.CheckBase();
    }

    public static bool HasBase(long owner)
    {
        foreach (ArgoTowerBrain t in Towers)
            if (t != null && t.IsBell && t.Master != null && ArgoUtil.Alive(t.Master) && ArgoUtil.Owner(t.Master) == owner)
                return true;
        return false;
    }

    // Fault Line: every Drill Rig hears about every reel (ArgoUtil.Reel).
    public static void Reeled(Entity_Master foe, Vector3 from, Vector3 to)
    {
        foreach (ArgoTowerBrain t in Towers.ToArray())
            if (t != null && t.IsDrill && t.Started)
            {
                try { t.Reeled(foe, from, to); }
                catch (Exception e) { ArgoLog.Warn("fault line: " + e.Message); }
            }
    }

    // ---------------------------------------------------------------- lookups

    // The Naut already working on this tower (one per tower, across every Miner's Base on the team).
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
