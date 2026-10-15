// EXAMPLE, frozen: Argo 1.x code from ModProjects, kept because the kit docs cite it. The live Argo 2.0 is
// TDS_Mod/ModSource/Argo (and Assets/ArgoMod/Editor); copy from there, not from here.
// Fire() below is the early one that aims level and spreads the height over the flight (the steep-shot bug, fixed in 1.1:
// TDS_Mod/ModSource/Argo/IMPLEMENTATION_NOTES.md, "Changes in 1.1", Steep shots).
// Server-side brain for every Argo buildable. Replaces the template tower AIs (which hard-code Smacky's projectiles and
// upgrades) and Entity_Ai_General_Upgrade (mirrored exactly, so the game's upgrade panel works unchanged).
//   Scatter Cannon  volleys of real pellets (Projectile_Ai_Simple) fanned across a cone; each pellet hits what it meets.
//   Harpoon Tower   one piercing harpoon down the line that holds the most enemies (the Heavy Bow's native Pierce).
//   Diving Bell     keeps its Nauts alive and takes the PORT / STARBOARD / BOSUN orders.
//   Deep Drill      bites the nearest ore (the game's own mining, like the Sno Box), sinks a Fathom each Tide, and
//                   slows enemies inside its Tremors.
public class ArgoTowerBrain : MonoBehaviour
{
    public Entity_Master Master;
    public ArgoData.Build Def;
    public bool Started;
    public int Shots, Volleys, Bites, Quakes, Pulses, Fathom = 1;
    public float LastGusher;

    float lastAttack, nextScan, upgradeThrottle, nextTremor, nextPulse, nextHookCheck, dryWarned = -99f;
    int shotSerial, mantleTide = -1;
    readonly Dictionary<string, float> param = new Dictionary<string, float>();
    readonly List<string> keys = new List<string>();   // no HashSet: System.Core isn't referenced by the game's compiler
    readonly List<Link_Modifier> fathomMods = new List<Link_Modifier>();
    readonly Dictionary<Entity_Master, float> slowVfx = new Dictionary<Entity_Master, float>();
    readonly List<float> anchorShots = new List<float>();
    Link_Modifier fathomCounter;
    Link_Command upgradeCommand;
    Entity_Master.Interacts hitHandler;

    // Diving Bell: one slot per crew member (Port, Starboard, Bosun).
    public readonly ArgoNautBrain[] Crew = new ArgoNautBrain[3];
    public readonly Entity_Master[] Posts = new Entity_Master[3];
    readonly float[] respawnAt = new float[3];
    readonly bool[] diving = new bool[3];
    readonly Link_Being[] divers = new Link_Being[3];
    int crewCommands;

    public bool Has(string key) { return keys.Contains(key); }

    public float Param(string name, float fallback)
    {
        float v;
        return param.TryGetValue(name, out v) ? v : fallback;
    }

    public bool IsScatter { get { return Def.Id == ArgoData.Scatter; } }
    public bool IsHarpoon { get { return Def.Id == ArgoData.Harpoon; } }
    public bool IsBell { get { return Def.Id == ArgoData.Bell; } }
    public bool IsDrill { get { return Def.Id == ArgoData.Drill; } }
    public int FathomMax { get { return Mathf.RoundToInt(Param("Fathommax_Base", ArgoData.FathomMax)); } }
    public float TremorRadius { get { return ArgoUtil.Stat(Master, "Range", 2f); } }

    void Update()
    {
        if (Master == null)
        {
            Destroy(this);
            return;
        }
        if (!ArgoUtil.IsServer)
            return;
        try
        {
            if (!Started)
            {
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || Master.Radio == null)
                    return;
                Begin();
            }
            // Helper_Entitytools.Updateinteract rebuilds Interact on every upgrade or effect: keep the hit listener on it.
            if ((IsScatter || IsHarpoon) && Time.time >= nextHookCheck)
            {
                nextHookCheck = Time.time + 0.25f;
                if (Master.Interact == null || Array.IndexOf(Master.Interact.GetInvocationList(), hitHandler) < 0)
                    Master.Interact += hitHandler;
            }
            if (IsScatter) Scatter();
            else if (IsHarpoon) Harpoon();
            else if (IsBell) KeepCrew();
            else if (IsDrill) DrillTick();
        }
        catch (Exception e)
        {
            ArgoLog.Warn(Def.Id + " brain: " + e.Message);
            enabled = false;
        }
    }

    void Begin()
    {
        Started = true;
        hitHandler = OnHit;
        // Same upgrade commands Entity_Ai_General_Upgrade creates, so the game's upgrade panel works unchanged.
        upgradeCommand = Helper_Entitytools.Createcommand(Master, "Command_Upgrade", "Upgrade", false, true);
        for (int i = 1; i <= 4; i++)
        {
            Link_Command option = Helper_Entitytools.Createcommand(Master, "Command_Upgradeoption" + i, "Upgradeoption" + i, false, true);
            option.Goalvalue = i - 1;
            Helper_Entitytools.Updatecommand(Master, "Upgradeoption" + i, "", true, true);
        }
        upgradeCommand.Goalvalue = 1f;
        Helper_Entitytools.Updatecommand(Master, "Upgrade", "", true, true);
        foreach (Upgradeproperties u in new List<Upgradeproperties>(Master.Upgradelistserver.Values))
            OnUpgrade(u);
        if (IsBell)
            EnsureCrewCommands(false);
        if (IsDrill)
        {
            fathomCounter = ArgoUtil.Counter(Master, "Fathom", Fathom);
            ApplyFathom(false);
        }
        // Subscribed last: the Updatecommand calls above run each command once.
        Master.Command += OnCommand;
        Master.Upgrade += OnUpgrade;
        lastAttack = Time.fixedTime - 0.5f;
        ArgoUtil.Vis(Master, "Argo_Build", ArgoUtil.Pos(Master), 0);
    }

    // ---------------------------------------------------------------- shared targeting

    Link_Entityquery Visible()
    {
        Link_Entityquery q = ArgoUtil.Enemies(Master);
        q.Visibleonly = true;
        q.Visposition = ArgoUtil.Pos(Master) + Vector3.up * 0.8f;
        return q;
    }

    bool Ready(out float range)
    {
        range = 0f;
        float aspd = ArgoUtil.Stat(Master, "Attackspeed", 1f);
        if (aspd <= 0f || Time.fixedTime - lastAttack < 1f / aspd || Time.time < nextScan)
            return false;
        nextScan = Time.time + 0.15f;
        range = ArgoUtil.Stat(Master, "Range", 4f);
        return true;
    }

    // A straight shot of a real game projectile. Every parameter is passed each time (see ArgoContent.BuildProjectiles).
    void Fire(string projectile, Vector3 from, Vector3 dir, float speed, float range, float targetY, float pierce, Dictionary<string, float> data)
    {
        float time = Mathf.Max(0.05f, range / speed);
        Projdata p = new Projdata();
        p.Params = new Dictionary<string, float>
        {
            { "Xvelocity", dir.x * speed }, { "Zvelocity", dir.z * speed }, { "Yvelocity", (targetY - from.y) / time },
            { "Gravity", 0f }, { "Pierce", Mathf.Max(1f, pierce) }, { "Lifetime", time + 0.05f }, { "Interact", 1f }, { "Collision", 0f },
        };
        p.Paramsobject = new Dictionary<string, Entity_Master>();
        p.Data = data;
        Helper_Damagetools.Projectile(Master, projectile, from, p, "Hurt");
        Shots++;
    }

    Dictionary<string, float> ShotData()
    {
        return new Dictionary<string, float> { { "Argoshot", ++shotSerial } };
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
    }

    // ---------------------------------------------------------------- Scatter Cannon

    void Scatter()
    {
        float range;
        if (!Ready(out range))
            return;
        Vector3 pos = ArgoUtil.Pos(Master);
        Entity_Master target = Helper_Entityquerytools.Entitynearest(pos, range, Visible());
        if (target == null)
            return;
        lastAttack = Time.fixedTime;
        Volleys++;
        Vector3 aim = ArgoUtil.Pos(target);
        Vector3 dir = Flat(aim - pos);
        int count = Mathf.Max(1, Mathf.RoundToInt(ArgoUtil.Stat(Master, "Projcount", 6f)));
        float spread = Mathf.Clamp(ArgoUtil.Stat(Master, "Spread", 50f), 5f, 160f);
        int every = Mathf.RoundToInt(Param("Broadside_Every", 0f));
        bool broadside = every > 0 && Volleys % every == 0;
        if (broadside)
        {
            count = Mathf.RoundToInt(Param("Broadside_Count", 16f));
            spread = 360f;
        }
        float speed = ArgoUtil.Stat(Master, "Projspeed", 16f);
        float pierce = ArgoUtil.Stat(Master, "Pierce", 1f);
        Vector3 from = pos + Vector3.up * 0.12f + dir * 0.3f;
        for (int i = 0; i < count; i++)
        {
            // Stratified across the cone with a little jitter, so pellets cover it evenly but never line up perfectly.
            float angle = broadside ? i * 360f / count : -spread * 0.5f + spread * (i + UnityEngine.Random.Range(0.2f, 0.8f)) / count;
            Vector3 d = Quaternion.Euler(0f, angle, 0f) * dir;
            Fire(ArgoData.Pellet, broadside ? pos + Vector3.up * 0.12f + d * 0.3f : from, d, speed, range + 0.4f, aim.y, pierce, ShotData());
        }
        ArgoUtil.Vis(Master, broadside ? "Argo_Broadside" : "Argo_Scatter", aim, spread);
    }

    // ---------------------------------------------------------------- Harpoon Tower

    void Harpoon()
    {
        float range;
        if (!Ready(out range))
            return;
        Vector3 pos = ArgoUtil.Pos(Master);
        Entity_Master[] found = ArgoUtil.Area(pos, range, Visible());
        if (found.Length == 0)
            return;
        // The line through the most enemies (ties go to the nearest target): what a harpooner would pick.
        Entity_Master best = null;
        int bestCount = -1;
        float bestDist = 0f;
        foreach (Entity_Master t in found)
        {
            if (t == null || !ArgoUtil.Alive(t))
                continue;
            Vector3 dir = Flat(ArgoUtil.Pos(t) - pos);
            int n = 0;
            foreach (Entity_Master o in found)
            {
                if (o == null) continue;
                Vector3 rel = ArgoUtil.Pos(o) - pos;
                rel.y = 0f;
                float along = Vector3.Dot(rel, dir);
                if (along > 0f && along <= range && (rel - dir * along).magnitude <= 0.45f) n++;
            }
            float d = ArgoUtil.Flat(ArgoUtil.Pos(t), pos);
            if (n > bestCount || (n == bestCount && d < bestDist))
            {
                best = t;
                bestCount = n;
                bestDist = d;
            }
        }
        if (best == null)
            return;
        lastAttack = Time.fixedTime;
        Volleys++;
        Vector3 aim = ArgoUtil.Pos(best);
        Vector3 fdir = Flat(aim - pos);
        int lanceEvery = Mathf.RoundToInt(Param("Lance_Every", 0f));
        bool lance = lanceEvery > 0 && Volleys % lanceEvery == 0;
        Throw(fdir, aim.y, range, lance);
        ArgoUtil.Vis(Master, lance ? "Argo_Lance" : "Argo_Harpoon", aim, 0);
        float twin = Param("Twin_Base", 0f);
        if (twin > 0f && !lance)
            ArgoUtil.Later(this, twin, () =>
            {
                if (Master == null) return;
                Throw(fdir, aim.y, ArgoUtil.Stat(Master, "Range", range), false);
                ArgoUtil.Vis(Master, "Argo_Harpoon", aim, 1);
            });
    }

    void Throw(Vector3 dir, float targetY, float range, bool lance)
    {
        Vector3 from = ArgoUtil.Pos(Master) + Vector3.up * 0.3f + dir * 0.35f;
        float speed = ArgoUtil.Stat(Master, "Projspeed", 18f);
        Dictionary<string, float> data = ShotData();
        if (Param("Root_Base", 0f) > 0f)
        {
            anchorShots.Add(data["Argoshot"]);
            if (anchorShots.Count > 8) anchorShots.RemoveAt(0);
        }
        if (lance)
        {
            data["Damage"] = ArgoUtil.Stat(Master, "Damage", 9f);
            data["Damagemulti"] = Param("Lance_Multi", 2.5f);
            Fire(ArgoData.Lance, from, dir, speed * 1.2f, range * Param("Lance_Range", 2f), targetY, 999f, data);
        }
        else
            Fire(ArgoData.HarpoonShot, from, dir, speed, range + 0.5f, targetY, ArgoUtil.Stat(Master, "Pierce", 3f), data);
    }

    // Every hit this tower's projectiles land (Entity_Master.Interact fires on the attacker before the target takes it).
    void OnHit(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.To == null || hit.Data == null || hit.Data.ContainsKey("Argodot"))
                return;
            Entity_Master to = hit.To;
            if (ArgoUtil.Team(to) == ArgoUtil.Team(Master))
                return;
            if (IsScatter)
            {
                float slow = Param("Slow_Base", 0f);
                if (slow > 0f)
                    ArgoUtil.Slow(Master, to, slow, Param("Duration_Base", 1.5f), true);
                float stagger = Param("Stagger_Base", 0f);
                if (stagger > 0f)
                    ArgoUtil.Root(Master, to, stagger);
                return;
            }
            // Harpoon.
            float whale = Param("Whaler_Base", 0f);
            float multi;
            if (whale > 0f && hit.Data.TryGetValue("Damagemulti", out multi))
            {
                // A copy: the projectile hands the same dictionary to every enemy it pierces.
                hit.Data = new Dictionary<string, float>(hit.Data);
                hit.Data["Damagemulti"] = multi * (1f + Mathf.Clamp01(ArgoUtil.MaxHealth(to) / whale));
            }
            float shot;
            if (hit.Data.TryGetValue("Argoshot", out shot) && anchorShots.Remove(shot))
                ArgoUtil.Root(Master, to, Param("Root_Base", 1f));
            int ticks = Mathf.RoundToInt(Param("Bleedticks_Base", 0f));
            if (ticks > 0)
                StartCoroutine(Bleed(to, ticks, ArgoUtil.Stat(Master, "Damage", 9f) * Param("Bleed_Multi", 0.2f)));
        }
        catch (Exception e)
        {
            ArgoLog.Warn(Def.Id + " hit: " + e.Message);
        }
    }

    IEnumerator Bleed(Entity_Master target, int ticks, float damage)
    {
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(1f);
            if (target == null || !ArgoUtil.Alive(target) || Master == null)
                yield break;
            Dictionary<string, float> d = ArgoUtil.Hit(damage, 1f);
            d["Argodot"] = 1f;
            Helper_Entitytools.Interacthurt(Master, target, d);
            ArgoUtil.Vis(Master, "Argo_Bleed", ArgoUtil.Pos(target), 0);
        }
    }

    // ---------------------------------------------------------------- Diving Bell and the crew

    public int CrewSize { get { return Mathf.Clamp(Mathf.RoundToInt(ArgoUtil.Stat(Master, "Units", 2f)), 0, ArgoData.Nauts.Length); } }

    void EnsureCrewCommands(bool late)
    {
        int want = CrewSize;
        for (int i = crewCommands; i < want; i++)
        {
            ArgoData.Crew c = ArgoData.Nauts[i];
            if (!ArgoContent.Commands.ContainsKey(c.Link))
                continue;
            Helper_Entitytools.Createcommand(Master, c.Command, c.Link, false, true);
            Helper_Entitytools.Updatecommand(Master, c.Link, "", !late, true);
            respawnAt[i] = Time.time + (i == 0 ? 0.5f : 1.2f + i * 0.6f);
        }
        crewCommands = Mathf.Max(crewCommands, want);
    }

    void KeepCrew()
    {
        int size = CrewSize;
        for (int i = 0; i < size; i++)
        {
            if (Crew[i] != null && Crew[i].Master != null)
                continue;
            Crew[i] = null;
            if (Time.time < respawnAt[i] && !diving[i])
                continue;
            Spawn(i, diving[i] ? Posts[i] : null);
            diving[i] = false;
        }
    }

    // Spawns crew member i at the bell, or (Full Crew's dive) next to the tower it was sent to.
    void Spawn(int i, Entity_Master at)
    {
        Beingobject being;
        if (!ArgoContent.Beings.TryGetValue(ArgoData.Nauts[i].Being, out being))
            return;
        Genitem block = Master.Beingserver.Currentblock;
        if (at != null && at.Beingserver != null && at.Beingserver.Currentblock != null)
        {
            // A free tile next to the tower, checked for the Naut that dove (falls back to the tower's own tile, where
            // the game's unit towers spawn their units too).
            Genitem near = null;
            try { near = Helper_Gridtools.Checkifstandablenearest(at.Beingserver.Currentblock, divers[i] ?? Master.Beingserver, 3); } catch (Exception) { }
            block = near ?? at.Beingserver.Currentblock;
        }
        divers[i] = null;
        Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(Master.Beingserver);
        spawn.Genadd = block;
        spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        spawn.Being = being;
        ArgoServer.PendingBell = this;
        ArgoServer.PendingPost = Posts[i];
        Link_Being b = Helper_Entitytools.Spawn(spawn, block.Position, Master.Beingserver, true, null);
        ArgoServer.PendingBell = null;
        ArgoServer.PendingPost = null;
        respawnAt[i] = Time.time + 2f;
        if (b == null || b.Worldobject == null)
            return;
        ArgoNautBrain naut = b.Worldobject.GetComponent<ArgoNautBrain>();
        if (naut != null)
        {
            naut.Index = i;
            Crew[i] = naut;
        }
        ArgoUtil.Vis(Master, at != null ? "Argo_Dive" : "Argo_Naut", block.Position, i);
    }

    public void CrewGone(ArgoNautBrain naut, bool killed)
    {
        int i = Array.IndexOf(Crew, naut);
        if (i < 0)
            return;
        Crew[i] = null;
        if (killed)
        {
            respawnAt[i] = Time.time + ArgoUtil.Stat(Master, "Respawn", ArgoData.NautRespawn);
            ArgoUtil.Warn(Master, 700 + i);   // "{0} is swimming back to the Diving Bell"
        }
    }

    // Full Crew: the Naut sinks out of sight and surfaces next to its tower.
    public void Dive(ArgoNautBrain naut)
    {
        int i = Array.IndexOf(Crew, naut);
        if (i < 0 || naut.Master == null || naut.Master.Beingserver == null)
            return;
        diving[i] = true;
        divers[i] = naut.Master.Beingserver;
        ArgoUtil.Vis(Master, "Argo_Dive", ArgoUtil.Pos(naut.Master), i);
        Crew[i] = null;
        Helper_Entitytools.Despawn(naut.Master.Beingserver, false, "Dive");
    }

    public void ClearPost(int i)
    {
        if (i >= 0 && i < Posts.Length)
            Posts[i] = null;
    }

    // PORT / STARBOARD / BOSUN: {"Ref":<tower>} sends that Naut to the tower; the bell itself calls it home.
    void CrewOrder(int i, string json)
    {
        if (i >= CrewSize)
            return;
        ulong id = ArgoUtil.JsonRef(json);
        if (id == 0)
            return;
        Entity_Master target = ArgoUtil.ById(id);
        ArgoNautBrain naut = Crew[i];
        if (target == null || target == Master)
        {
            Posts[i] = null;
            if (naut != null) naut.SendTo(null);
            ArgoUtil.Warn(Master, 600 + i);
            return;
        }
        if (!ArgoUtil.Alive(target) || !ArgoUtil.HasTag(target, "Tower"))
        {
            ArgoUtil.Warn(Master, 4);
            return;
        }
        if (ArgoUtil.Team(target) != ArgoUtil.Team(Master))
        {
            ArgoUtil.Warn(Master, 8);
            return;
        }
        if (Posts[i] == target)
        {
            ArgoUtil.Warn(Master, 300 + i);
            return;
        }
        // One Naut per tower: whoever is there now heads home.
        ArgoNautBrain other = ArgoServer.TunerOf(target);
        if (other != null && other != naut && other.Bell != null)
        {
            other.Bell.ClearPost(other.Index);
            other.SendTo(null);
        }
        Posts[i] = target;
        if (naut != null) naut.SendTo(target);
        ArgoUtil.Warn(Master, 500 + i);
    }

    // ---------------------------------------------------------------- Deep Drill

    void DrillTick()
    {
        Vector3 pos = ArgoUtil.Pos(Master);
        // Mining: the game's own ore damage, exactly as the Sno Box siphons (the ore pays out when it cracks).
        float aspd = ArgoUtil.Stat(Master, "Attackspeed", 0.25f);
        if (aspd > 0f && Time.fixedTime - lastAttack >= 1f / aspd)
        {
            lastAttack = Time.fixedTime;
            Entity_Master ore = Ore(pos);
            if (ore != null)
                Bite(ore, 1f);
            else if (Time.time - dryWarned > 60f)
            {
                dryWarned = Time.time;
                ArgoUtil.Vis(Master, "Argo_Dry", pos, 0);
            }
        }
        // Tremors: a light slow on every enemy inside, refreshed a little longer than each tick.
        if (Time.time >= nextTremor)
        {
            nextTremor = Time.time + ArgoData.TremorTick;
            float radius = TremorRadius, slow = ArgoUtil.Stat(Master, "Slow", 0.2f);
            bool any = false;
            foreach (Entity_Master e in ArgoUtil.Area(pos, radius, ArgoUtil.Enemies(Master)))
            {
                if (e == null || !ArgoUtil.Alive(e) || !ArgoUtil.HasTag(e, "Mob"))
                    continue;
                any = true;
                float last;
                bool vfx = !slowVfx.TryGetValue(e, out last) || Time.time - last > 1.5f;
                if (vfx) slowVfx[e] = Time.time;
                ArgoUtil.Slow(Master, e, slow, ArgoData.TremorTick + 0.25f, vfx);
            }
            if (slowVfx.Count > 64)
                slowVfx.Clear();
            // Mantle Tap: the first enemy into the Tremors each Tide sets off a quake.
            if (any && Param("Mantle_Base", 0f) > 0f && mantleTide != ArgoServer.Tide)
            {
                mantleTide = ArgoServer.Tide;
                Quake(Param("Mantle_Base", 2f));
            }
        }
        float pulse = Param("Pulse_Base", 0f);
        if (pulse > 0f && Time.time >= nextPulse)
        {
            nextPulse = Time.time + ArgoData.AftershockEvery;
            bool hitAny = false;
            foreach (Entity_Master e in ArgoUtil.Area(pos, TremorRadius, ArgoUtil.Enemies(Master)))
                if (e != null && ArgoUtil.Alive(e) && ArgoUtil.HasTag(e, "Mob"))
                {
                    Dictionary<string, float> d = ArgoUtil.Hit(pulse, 1f);
                    d["Argodot"] = 1f;
                    Helper_Entitytools.Interacthurt(Master, e, d);
                    hitAny = true;
                }
            if (hitAny)
            {
                Pulses++;
                ArgoUtil.Vis(Master, "Argo_Pulse", pos, TremorRadius);
            }
        }
    }

    public Entity_Master Ore(Vector3 pos)
    {
        Entity_Master best = null;
        float bestD = float.MaxValue;
        foreach (Entity_Master o in ArgoUtil.Area(pos, ArgoData.OreReach + 0.5f, ArgoUtil.Tagged("Resource")))
        {
            if (o == null || !ArgoUtil.Alive(o))
                continue;
            float d = ArgoUtil.Flat(ArgoUtil.Pos(o), pos);
            if (d <= ArgoData.OreReach && d < bestD)
            {
                best = o;
                bestD = d;
            }
        }
        return best;
    }

    void Bite(Entity_Master ore, float multi)
    {
        Dictionary<string, float> d = new Dictionary<string, float>();
        d["Damage"] = ArgoUtil.Stat(Master, "Damage", 0.5f);
        d["Damagemulti"] = multi;
        Helper_Entitytools.Interacthurt(Master, ore, d);
        Bites++;
        ArgoUtil.Vis(Master, "Argo_Mine", ArgoUtil.Pos(ore), Fathom);
    }

    // SONAR's quake (and Mantle Tap's): everything inside the Tremors is held in place.
    public void Quake(float seconds)
    {
        if (Master == null)
            return;
        Quakes++;
        Vector3 pos = ArgoUtil.Pos(Master);
        float radius = TremorRadius;
        foreach (Entity_Master e in ArgoUtil.Area(pos, radius, ArgoUtil.Enemies(Master)))
            if (e != null && ArgoUtil.Alive(e) && ArgoUtil.HasTag(e, "Mob"))
                ArgoUtil.Root(Master, e, seconds);
        ArgoUtil.Vis(Master, "Argo_Quake", pos, radius);
        // The game's own camera shake, felt by players near the drill.
        Master.Radio.Vfxshake_ClientRpc(pos, Vector3.up, 0.3f, radius + 4f);
    }

    void ApplyFathom(bool announce)
    {
        foreach (Link_Modifier m in fathomMods)
            ArgoUtil.Unmod(m);
        fathomMods.Clear();
        int below = Fathom - 1;
        if (below > 0)
        {
            fathomMods.Add(Helper_Entitytools.Addmod(Master, "Damage", ArgoData.FathomYield * below, Helper_Entitytools.Modtypes.Multi, 0f));
            fathomMods.Add(Helper_Entitytools.Addmod(Master, "Range", ArgoData.FathomTremor * below, Helper_Entitytools.Modtypes.Add, 0f));
        }
        ArgoUtil.SetCounter(Master, fathomCounter, "Fathom", Fathom);
        ArgoUtil.Skin(Master, Fathom > 1 ? "Fathom" + Mathf.Min(Fathom, 5) : "");
        if (announce)
            ArgoUtil.Vis(Master, "Argo_Fathom", ArgoUtil.Pos(Master), Fathom);
    }

    public void SinkTo(int fathom)
    {
        Fathom = Mathf.Clamp(fathom, 1, Mathf.Max(FathomMax, 1));
        ApplyFathom(true);
    }

    public void OnTide(int tide)
    {
        if (Master == null || !Started || !IsDrill)
            return;
        // Gusher: a deep drill throws up a burst of ore as the Tide turns.
        int gush = Mathf.RoundToInt(Param("Gusher_Base", 0f));
        if (gush > 0 && Fathom >= 3)
        {
            Entity_Master ore = Ore(ArgoUtil.Pos(Master));
            if (ore != null)
            {
                for (int i = 0; i < gush; i++)
                    Bite(ore, 1f);
                LastGusher = Time.time;
                ArgoUtil.Vis(Master, "Argo_Gusher", ArgoUtil.Pos(ore), gush);
            }
        }
        if (Fathom < FathomMax)
            SinkTo(Fathom + 1);
    }

    // ---------------------------------------------------------------- upgrades (mirrors Entity_Ai_General_Upgrade)

    void OnCommand(string command, string json, bool server)
    {
        if (IsBell && command.StartsWith("Crew_"))
        {
            for (int i = 0; i < ArgoData.Nauts.Length; i++)
                if (ArgoData.Nauts[i].Link == command)
                {
                    try { CrewOrder(i, json); }
                    catch (Exception e) { ArgoLog.Warn("crew order: " + e.Message); }
                }
            return;
        }
        if (command != "Upgrade" || string.IsNullOrEmpty(json))
            return;
        try
        {
            // {"Upgrade":"<name>","Upgrader":<session>} (Entity_Ai_General_Upgrade.Upgradedata). JsonUtility isn't
            // available to mod code, so read the two fields directly.
            string upgradeName = ArgoUtil.JsonString(json, "Upgrade");
            long upgrader = (long)ArgoUtil.JsonNumber(json, "Upgrader");
            Upgradeproperties up = Bl_Address.Getload("Upgrade", upgradeName) as Upgradeproperties;
            if (up == null)
                return;
            if (!server)
            {
                List<Upgradeproperties> available = new List<Upgradeproperties>(Master.Beingserver.Entity.Upgrades);
                foreach (Upgradeproperties applied in Master.Upgradelistserver.Values)
                    if (applied.Priority == Master.Entitydata.Value.Level)
                        available.AddRange(applied.Upgrades);
                if (!available.Contains(up) || Time.time - upgradeThrottle <= 0.05f)
                    return;
                upgradeThrottle = Time.time;
            }
            float scale = upgradeCommand != null ? upgradeCommand.Goalvalue : 1f;
            if (!Helper_Resources.Checkresources(up.Costs, upgrader, scale))
                return;
            Helper_Resources.Useresources(up.Costs, upgrader, scale);
            Helper_Entitytools.Upgrade(Master, up, server ? Helper_Entitytools.Upgradecheck.Forced : Helper_Entitytools.Upgradecheck.Normal, null);
            Master.Radio.Vfxoneshotcheck("Upgradeborder", ArgoUtil.Pos(Master), Vector3.forward);
            ArgoUtil.Vis(Master, "Argo_Upgrade", ArgoUtil.Pos(Master), up.Priority);
            // End-of-match stats, as Entity_Ai_General_Upgrade records them.
            GameObject player;
            Tds_Clientinfo info;
            if (Blm_Server.Instance != null && Blm_Server.Instance.Sessionids.TryGetValue(ArgoUtil.Owner(Master), out player)
                && player != null && player.TryGetComponent(out info) && info.Activeplayerdata != null)
                info.Activeplayerdata.Upgraded++;
        }
        catch (Exception e)
        {
            ArgoLog.Warn("upgrade command: " + e.Message);
        }
    }

    void OnUpgrade(Upgradeproperties up)
    {
        ArgoData.Upg def;
        if (up == null || !ArgoContent.UpgradeDefs.TryGetValue(up.Objectname, out def) || keys.Contains(def.Key))
            return;
        keys.Add(def.Key);
        if (up.Defaultparams == null)
            return;
        foreach (KeyValuePair<string, float> kv in up.Defaultparams)
        {
            int cut = kv.Key.LastIndexOf('_');
            string stat = cut > 0 ? kv.Key.Substring(0, cut) : kv.Key;
            string type = cut > 0 ? kv.Key.Substring(cut + 1) : "";
            // A stat this tower has (Damage, Range, Projcount, Spread, Pierce, Slow, Units...) is a modifier like the game's;
            // anything else is a rule parameter the brain reads.
            if (Master.Statsserver.ContainsKey(stat) && (type == "Add" || type == "Multi" || type == "Pmulti"))
            {
                Helper_Entitytools.Modtypes mt = type == "Add" ? Helper_Entitytools.Modtypes.Add : type == "Multi" ? Helper_Entitytools.Modtypes.Multi : Helper_Entitytools.Modtypes.Pmulti;
                Helper_Entitytools.Addmod(Master, stat, kv.Value, mt, 0f);
            }
            else
            {
                float old;
                param[kv.Key] = param.TryGetValue(kv.Key, out old) && !kv.Key.EndsWith("_Base") && !kv.Key.EndsWith("_Every") && !kv.Key.EndsWith("_Count") ? old + kv.Value : kv.Value;
            }
        }
        if (Started && IsBell)
        {
            EnsureCrewCommands(true);
            foreach (ArgoNautBrain n in Crew)
                if (n != null) n.Refit();
        }
        if (Started && IsDrill && Fathom > FathomMax)
            SinkTo(FathomMax);
    }
}
