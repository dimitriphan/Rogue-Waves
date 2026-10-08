// Server-side brain for every Argo buildable. Replaces the template tower AIs (which hard-code Smacky's projectiles and
// upgrades) and Entity_Ai_General_Upgrade (mirrored exactly, so the game's upgrade panel works unchanged).
//   Scatter Cannon  volleys of real pellets (Projectile_Ai_Simple) fanned across a cone; each pellet hits what it meets.
//   Harpoon Tower   one piercing harpoon down the line that holds the most enemies (the Heavy Bow's native Pierce);
//                   the first enemy each harpoon hits is reeled toward the tower (1.4).
//   Diving Bell     keeps its Nauts alive and takes the PORT / STARBOARD / BOSUN orders.
//   Deep Drill      bites the nearest ore (the game's own mining, like the Sno Box), sinks a Fathom each Tide, and
//                   slows enemies inside its Tremors. Argo's guns inside a Drill's Tremors are loaded by its depth
//                   (Pressure, 1.4).
public class ArgoTowerBrain : MonoBehaviour
{
    public Entity_Master Master;
    public ArgoData.Build Def;
    public bool Started;
    public int Shots, Volleys, Bites, Quakes, Pulses, Fathom = 1;
    public float LastGusher;
    // 1.4: Pressure on a gun from the deepest Drill around it; Double Barrel's stored volley; the reel; Pay Dirt.
    public int Pressure, Stored, Reels, BossHits, PaydirtBites;
    public float TunedReel, LastReelDone;

    float lastAttack, nextScan, upgradeThrottle, nextTremor, nextPulse, nextHookCheck, dryWarned = -99f;
    int shotSerial, mantleTide = -1;
    readonly Dictionary<string, float> param = new Dictionary<string, float>();
    readonly List<string> keys = new List<string>();   // no HashSet: System.Core isn't referenced by the game's compiler
    readonly List<Link_Modifier> fathomMods = new List<Link_Modifier>();
    readonly Dictionary<Entity_Master, float> slowVfx = new Dictionary<Entity_Master, float>();
    readonly List<float> reelShots = new List<float>();   // shots whose first hit is still to come
    // Each reel shot's nearest foe hit in the step it first hits, and how far it stood from the tower.
    readonly Dictionary<float, KeyValuePair<Entity_Master, float>> reelFirst = new Dictionary<float, KeyValuePair<Entity_Master, float>>();
    Link_Modifier fathomCounter, pressureMod;
    float nextPressure;
    Link_Command upgradeCommand;
    Entity_Master.Interacts hitHandler;
    readonly ArgoFaults faults = new ArgoFaults();

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
            if ((IsScatter || IsHarpoon) && Time.time >= nextPressure)
            {
                nextPressure = Time.time + 1f;
                CheckPressure();
            }
            if (IsScatter) Scatter();
            else if (IsHarpoon) Harpoon();
            else if (IsBell) KeepCrew();
            else if (IsDrill) DrillTick();
        }
        catch (Exception e)
        {
            if (faults.Fail(Def.Id + " brain", e))
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

    // A straight shot of a real game projectile along a 3D direction, so it flies down a slope or off a ledge straight at
    // the target. (Argo 1.0 aimed level and spread the height difference over the whole range, so shots at foes below
    // passed over their heads.) Every parameter is passed each time (see ArgoContent.BuildProjectiles).
    void Fire(string projectile, Vector3 from, Vector3 dir, float speed, float reach, float pierce, Dictionary<string, float> data)
    {
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
        float time = Mathf.Max(0.05f, reach / speed);
        Projdata p = new Projdata();
        p.Params = new Dictionary<string, float>
        {
            { "Xvelocity", dir.x * speed }, { "Zvelocity", dir.z * speed }, { "Yvelocity", dir.y * speed },
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

    // Where range is measured from: the "Detect" point of the tower's hit prefab, which is also where the game draws the
    // range when the tower is selected or placed (ArgoContent gives each Argo tower its own, centred on the tower).
    public Vector3 Eye { get { return ArgoUtil.Pos(Master) + Vector3.up * Def.Eye; } }

    void Scatter()
    {
        float range;
        if (!Ready(out range))
            return;
        Entity_Master target = Helper_Entityquerytools.Entitynearest(Eye, range, Visible());
        if (target == null)
        {
            // Double Barrel: a full reload with nothing to shoot loads a volley for later.
            int store = Mathf.RoundToInt(Param("Double_Base", 0f));
            if (store > Stored)
            {
                Stored = store;
                ArgoUtil.Vis(Master, "Argo_Loaded", ArgoUtil.Pos(Master), 0);
            }
            return;
        }
        lastAttack = Time.fixedTime;
        Volley(target, range);
        if (Stored > 0)
        {
            Stored--;
            Entity_Master first = target;
            ArgoUtil.Later(this, ArgoData.DoubleDelay, () =>
            {
                if (Master == null) return;
                float r = ArgoUtil.Stat(Master, "Range", range);
                Entity_Master t = first != null && ArgoUtil.Alive(first) ? first : Helper_Entityquerytools.Entitynearest(Eye, r, Visible());
                if (t != null) Volley(t, r);
            });
        }
    }

    void Volley(Entity_Master target, float range)
    {
        Vector3 pos = ArgoUtil.Pos(Master);
        Volleys++;
        Vector3 aim = ArgoUtil.Pos(target);
        Vector3 flat = Flat(aim - pos);
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
        Vector3 from = pos + Vector3.up * 0.12f + flat * 0.3f;
        // The cone is tipped toward the target's body (down a slope, up a ledge), then fanned around the vertical.
        Vector3 dir = broadside ? flat : (aim - from).normalized;
        for (int i = 0; i < count; i++)
        {
            // Stratified across the cone with a little jitter, so pellets cover it evenly but never line up perfectly.
            float angle = broadside ? i * 360f / count : -spread * 0.5f + spread * (i + UnityEngine.Random.Range(0.2f, 0.8f)) / count;
            Vector3 d = Quaternion.AngleAxis(angle, Vector3.up) * dir;
            Fire(ArgoData.Pellet, broadside ? pos + Vector3.up * 0.12f + d * 0.3f : from, d, speed, range + 0.4f, pierce, ShotData());
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
        Entity_Master[] found = ArgoUtil.Area(Eye, range, Visible());
        if (found.Length == 0)
            return;
        // The line through the most enemies (ties go to the nearest target): what a harpooner would pick. Lines are 3D,
        // from the harpoon's muzzle to each enemy's body, so a line down a slope counts the enemies it really passes.
        Entity_Master best = null;
        int bestCount = -1;
        float bestDist = 0f;
        foreach (Entity_Master t in found)
        {
            if (t == null || !ArgoUtil.Alive(t))
                continue;
            Vector3 from = Muzzle(ArgoUtil.Pos(t));
            Vector3 dir = (ArgoUtil.Pos(t) - from).normalized;
            int n = 0;
            foreach (Entity_Master o in found)
            {
                if (o == null) continue;
                Vector3 rel = ArgoUtil.Pos(o) - from;
                float along = Vector3.Dot(rel, dir);
                if (along > 0f && along <= range + 0.5f && (rel - dir * along).magnitude <= 0.5f) n++;
            }
            float d = Vector3.Distance(ArgoUtil.Pos(t), pos);
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
        Vector3 aimDir = aim - Muzzle(aim);
        int lanceEvery = Mathf.RoundToInt(Param("Lance_Every", 0f));
        bool lance = lanceEvery > 0 && Volleys % lanceEvery == 0;
        Throw(aimDir, range, lance);
        ArgoUtil.Vis(Master, lance ? "Argo_Lance" : "Argo_Harpoon", aim, 0);
        float twin = Param("Twin_Base", 0f);
        if (twin > 0f && !lance)
            ArgoUtil.Later(this, twin, () =>
            {
                if (Master == null) return;
                Throw(aimDir, ArgoUtil.Stat(Master, "Range", range), false);
                ArgoUtil.Vis(Master, "Argo_Harpoon", aim, 1);
            });
    }

    // The harpoon gun's mouth, turned toward a point.
    Vector3 Muzzle(Vector3 toward)
    {
        return ArgoUtil.Pos(Master) + Vector3.up * 0.3f + Flat(toward - ArgoUtil.Pos(Master)) * 0.35f;
    }

    void Throw(Vector3 dir, float range, bool lance)
    {
        Vector3 from = ArgoUtil.Pos(Master) + Vector3.up * 0.3f + Flat(dir) * 0.35f;
        float speed = ArgoUtil.Stat(Master, "Projspeed", 18f);
        Dictionary<string, float> data = ShotData();
        // Every harpoon but the lance reels the first foe it hits.
        if (!lance)
        {
            reelShots.Add(data["Argoshot"]);
            if (reelShots.Count > 8) reelShots.RemoveAt(0);
        }
        if (lance)
        {
            data["Damage"] = ArgoUtil.Stat(Master, "Damage", 5f);
            data["Damagemulti"] = Param("Lance_Multi", 2.5f);
            Fire(ArgoData.Lance, from, dir, speed * 1.2f, range * Param("Lance_Range", 2f), 999f, data);
        }
        else
            Fire(ArgoData.HarpoonShot, from, dir, speed, range + 0.5f, ArgoUtil.Stat(Master, "Pierce", 3f), data);
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
            // A hit whose data already carries Damage (the lance's) isn't recorded on the target by the game: record it,
            // so the tower gets the kill and an Aegis stops regenerating (ModKitCombat).
            ModKitCombat.Mark(to, Master);
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
            // The reel goes to the foe nearest the tower among the hits of the step that first hits: one projectile
            // step can hit several foes, and the game hands them over in any order (Projectile_Master sweeps with
            // SphereCastAll), so the eighth PC run's harpoon reeled the foe behind the first.
            float shot;
            KeyValuePair<Entity_Master, float> near;
            if (hit.Data.TryGetValue("Argoshot", out shot))
            {
                float d = ArgoUtil.Flat(ArgoUtil.Pos(to), ArgoUtil.Pos(Master));
                if (reelShots.Remove(shot))
                {
                    reelFirst[shot] = new KeyValuePair<Entity_Master, float>(to, d);
                    StartCoroutine(ReelNearest(shot));
                }
                else if (reelFirst.TryGetValue(shot, out near) && d < near.Value)
                    reelFirst[shot] = new KeyValuePair<Entity_Master, float>(to, d);
            }
            int ticks = Mathf.RoundToInt(Param("Bleedticks_Base", 0f));
            if (ticks > 0)
                StartCoroutine(Bleed(to, ticks, ArgoUtil.Stat(Master, "Damage", 5f) * Param("Bleed_Multi", 0.2f)));
        }
        catch (Exception e)
        {
            ArgoLog.Warn(Def.Id + " hit: " + e.Message);
        }
    }

    // A step's hits all land within one frame: the reel waits for the rest of them.
    IEnumerator ReelNearest(float shot)
    {
        yield return null;
        KeyValuePair<Entity_Master, float> near;
        if (!reelFirst.TryGetValue(shot, out near))
            yield break;
        reelFirst.Remove(shot);
        try
        {
            if (Master != null && near.Key != null)
                ReelIn(near.Key);
        }
        catch (Exception e)
        {
            ArgoLog.Warn(Def.Id + " reel: " + e.Message);
        }
    }

    // The harpoon's reel: the first foe a harpoon hits walks 1.5 tiles toward the tower (a tile farther when an Old
    // Salts Naut tunes it); Anchor Line pins it where it lands. A Boss-weight foe is pinned where it stands.
    void ReelIn(Entity_Master foe)
    {
        if (ArgoUtil.MaxHealth(foe) >= ArgoData.BossHealth)
            BossHits++;
        float root = Param("Root_Base", 0f);
        Action<Entity_Master> pin = f =>
        {
            LastReelDone = Time.time;
            if (root > 0f && Master != null) ArgoUtil.Root(Master, f, root);
        };
        Vector3? to = ArgoUtil.Reel(this, foe, ArgoUtil.Pos(Master), ArgoData.ReelTiles + TunedReel, ArgoData.ReelTime, ArgoData.ReelMinDist, pin);
        if (to == null)
            return;
        Reels++;
        Master.Radio.Vis_ClientRpc("Argo_Reel", to.Value, (float)foe.NetworkObjectId);
    }

    // Pressure: every second, the deepest started Deep Drill on this team whose Tremors reach this gun loads it, +1
    // pellet (Scatter) or +1 pierce (Harpoon) per Fathom below the first. The modifier is swapped when the depth changes.
    void CheckPressure()
    {
        Vector3 pos = ArgoUtil.Pos(Master);
        int team = ArgoUtil.Team(Master), depth = 0;
        foreach (ArgoTowerBrain t in ArgoServer.Towers)
            if (t != null && t.IsDrill && t.Started && t.Master != null && ArgoUtil.Team(t.Master) == team
                && ArgoUtil.Flat(ArgoUtil.Pos(t.Master), pos) <= t.TremorRadius)
                depth = Mathf.Max(depth, t.Fathom - 1);
        if (depth == Pressure)
            return;
        bool rose = depth > Pressure;
        ArgoUtil.Unmod(pressureMod);
        pressureMod = null;
        Pressure = depth;
        if (depth > 0)
            pressureMod = IsScatter
                ? Helper_Entitytools.Addmod(Master, "Projcount", depth * ArgoData.PressurePellets, Helper_Entitytools.Modtypes.Add, 0f)
                : Helper_Entitytools.Addmod(Master, "Pierce", depth * ArgoData.PressurePierce, Helper_Entitytools.Modtypes.Add, 0f);
        if (rose)
            ArgoUtil.Vis(Master, "Argo_Pressure", pos, depth);
    }

    // Pay Dirt: a foe felled inside the Tremors is dug up, PaydirtBase mining strikes on the nearest ore.
    public void PayDirt(Entity_Master foe, Vector3 at)
    {
        int n = Mathf.RoundToInt(Param("Paydirt_Base", 0f));
        if (n <= 0 || !Started || ArgoUtil.Flat(at, ArgoUtil.Pos(Master)) > TremorRadius || ArgoUtil.Team(foe) == ArgoUtil.Team(Master))
            return;
        Entity_Master ore = Ore(ArgoUtil.Pos(Master));
        if (ore == null)
            return;
        ArgoUtil.Vis(Master, "Argo_Paydirt", at, n);
        for (int i = 0; i < n; i++)
            Bite(ore, 1f);
        PaydirtBites += n;
    }

    IEnumerator Bleed(Entity_Master target, int ticks, float damage)
    {
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(1f);
            if (target == null || !ArgoUtil.Alive(target) || Master == null)
                yield break;
            ModKitCombat.Hurt(Master, target, damage, 1f, null, ArgoUtil.Data("Argodot", 1f));
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
            float radius = TremorRadius, slow = Mathf.Min(ArgoData.SlowCap, ArgoUtil.Stat(Master, "Slow", 0.2f));
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
                    ModKitCombat.Hurt(Master, e, pulse, 1f, null, ArgoUtil.Data("Argodot", 1f));
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
            fathomMods.Add(Helper_Entitytools.Addmod(Master, "Attackspeed", ArgoData.FathomSpeed * below, Helper_Entitytools.Modtypes.Multi, 0f));
            fathomMods.Add(Helper_Entitytools.Addmod(Master, "Range", ArgoData.FathomTremor * below, Helper_Entitytools.Modtypes.Add, 0f));
            fathomMods.Add(Helper_Entitytools.Addmod(Master, "Slow", ArgoData.FathomSlow * below, Helper_Entitytools.Modtypes.Add, 0f));
        }
        ArgoUtil.SetCounter(Master, fathomCounter, "Fathom", Fathom);
        // The skin carries the Fathom to every player: the pipe stack it shows, and the Tremor ring clients draw.
        ArgoUtil.Skin(Master, Fathom > 1 ? "Fathom" + Mathf.Min(Fathom, ArgoData.FathomDeepest) : "");
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
        if (Master == null || !Started)
            return;
        // The guns check their Pressure right after every Drill has sunk.
        if (IsScatter || IsHarpoon)
            nextPressure = 0f;
        if (!IsDrill)
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
