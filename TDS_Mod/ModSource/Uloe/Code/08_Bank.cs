// Power Bank: keeps its Units of Photons, one per Respawn seconds (quicker in Light), and is one of the two things PLUG IN
// goes into: its upgrades set the Light it gives. Short Circuit makes a falling Photon burst.
public class UloeBankBrain : UloeBuilding
{
    public int Spawned;
    float nextSpawn;
    bool waiting;

    public int Slots { get { return Mathf.Max(1, Mathf.RoundToInt(Stat("Units", 2f))) + UloeData.BatteryUnits[Player.Batteries]; } }
    public int Alive { get { return UloeServer.PhotonsOf(this); } }

    // Seconds to the next Photon, as it would be right now.
    public float RespawnNow
    {
        get
        {
            return Stat("Respawn", 8f) * UloeData.BatteryRespawn[Player.Batteries] / (1f + UloeData.BankPerLight * Light);
        }
    }

    protected override void Started2()
    {
        nextSpawn = Time.time + 1f;
        waiting = true;
    }

    // Like the shipped unit buildings: a Photon short, it waits the Respawn seconds, then makes one.
    protected override void Tick()
    {
        if (Alive >= Slots)
        {
            waiting = false;
            return;
        }
        if (!waiting)
        {
            waiting = true;
            nextSpawn = Time.time + Mathf.Max(0.5f, RespawnNow);
            return;
        }
        if (Time.time < nextSpawn)
            return;
        waiting = false;
        Spawn();
    }

    // A Photon on the Bank's tile, handed to its brain with this Bank as its home; it goes with the Bank.
    void Spawn()
    {
        Beingobject being;
        if (!UloeContent.Beings.TryGetValue(UloeData.Photon, out being) || Master.Beingserver == null || Master.Beingserver.Currentblock == null)
            return;
        Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(Master.Beingserver);
        spawn.Genadd = Master.Beingserver.Currentblock;
        spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        spawn.Being = being;
        spawn.Destroyonparent = true;
        UloeServer.PendingHome = this;
        Link_Being b = Helper_Entitytools.Spawn(spawn, Master.Position.Value, Master.Beingserver, true, null);
        UloeServer.PendingHome = null;
        if (b != null && b.Worldobject != null)
            Spawned++;
    }
}

// Server-side brain for a Photon. Fighting is the game's own Entity_Ai_Mob_Combat (it picks a fight while the unit is Idle);
// this brain only decides where it walks, and its hits mark what they land on. No game AI uses randomness (fit plan 10), so
// the wandering is here: every few seconds a walkable tile within its leash, or half the time one of your buildings, which
// it walks to and stands by (Kippy's visit-and-go-on, so its Light goes from tower to tower). Near a Flood Light's line it
// follows the line to the far end and wanders there until the line goes.
public class UloePhotonBrain : MonoBehaviour
{
    public Entity_Master Master;
    public UloeBankBrain Home;
    public UloeFloodBrain Following;
    public Entity_Master Target;
    public bool Started;
    public int Walks, Visits, Marked, Fights, Followed;
    public float Travelled;
    readonly UloeLight light = new UloeLight();
    Entity_Master.Interacts dealt;
    float nextThink, nextFeed, nextWander;
    Vector3 lastPos, spawnPos;

    public long Owner { get { return ModKitUtil.Owner(Master); } }
    public Vector3 Pos { get { return ModKitUtil.Pos(Master); } }

    // Where its leash is measured from: the building at the end of the line it follows, else its Power Bank.
    public Vector3 Anchor
    {
        get
        {
            if (Following != null && Following.Directed)
                return ModKitUtil.Pos(Following.Aimed);
            return Home != null && Home.Master != null ? Home.Pos : spawnPos;
        }
    }

    void Update()
    {
        if (Master == null)
        {
            Destroy(this);
            return;
        }
        if (!ModKitUtil.IsServer)
            return;
        try
        {
            if (!Started)
            {
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || !Master.Commandlistserver.ContainsKey("Move"))
                    return;
                Begin();
            }
            if (Time.time >= nextFeed)
            {
                nextFeed = Time.time + UloeData.LightTick;
                UloeServer.Shine(light, Master, Pos, UloeData.PhotonRadius, UloeData.PhotonLight, null);
            }
            if (Time.time >= nextThink)
            {
                nextThink = Time.time + 0.4f;
                ModKitUtil.Listen(Master, dealt);
                Think();
            }
        }
        catch (Exception e)
        {
            UloeLog.Warn("photon brain: " + e.Message);
            enabled = false;
        }
    }

    void Begin()
    {
        Started = true;
        spawnPos = lastPos = Pos;
        dealt = OnDealt;
        ModKitUtil.Listen(Master, dealt);
        ModKitUtil.Vis(Master, "Uloe_Pop", Pos, 0f);
        nextWander = Time.time + 0.3f;
    }

    // Every hit it lands marks the foe (Merchant's Mob_Merchant.Interact does the same): very little damage, but it counts.
    void OnDealt(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || !ModKitUtil.IsFoe(hit.To, ModKitUtil.Team(Master)))
                return;
            Entity_Master radio = Radio();
            UloeMarks.Blind(radio, hit.To, UloeData.PhotonBlind);
            UloeMarks.Illuminate(radio, hit.To, UloeData.PhotonLit);
            Marked++;
        }
        catch (Exception e)
        {
            UloeLog.Warn("photon hit: " + e.Message);
        }
    }

    Entity_Master Radio()
    {
        if (Home != null && Home.Master != null && Home.Master.Radio != null)
            return Home.Master;
        UloeHeroBrain hero = UloeServer.HeroOf(Owner);
        return hero != null && hero.Master != null ? hero.Master : Master;
    }

    void Think()
    {
        Vector3 pos = Pos;
        Travelled += ModKitUtil.Flat(pos, lastPos);
        lastPos = pos;
        CheckPath(pos);
        if (Fight(pos))
            return;
        string state = ModKitUtil.State(Master);
        if (state == "Move" || state == "Attack" || Time.time < nextWander)
            return;
        nextWander = Time.time + UnityEngine.Random.Range(UloeData.WanderMin, UloeData.WanderMax);
        Wander(pos);
    }

    // A line of light close by takes it along; a line that went away lets it go back to its Bank.
    void CheckPath(Vector3 pos)
    {
        if (Following != null && (Following.Master == null || !Following.Directed))
            Following = null;
        if (Following != null)
            return;
        UloeFloodBrain f = UloeServer.PathNear(Owner, pos);
        if (f == null || f.Aimed == null)
            return;
        Following = f;
        Followed++;
        Walk(f.Aimed, Vector3.zero, 1f);
        nextWander = Time.time + UloeData.WanderMax;
    }

    // A foe within reach of it and its leash draws it to fight; the stock AI swings at what is in Range when it stands still.
    bool Fight(Vector3 pos)
    {
        Vector3 anchor = Anchor;
        float range = ModKitUtil.Stat(Master, "Range", 1.5f), leash = UloeData.PhotonLeash;
        if (ModKitUtil.Flat(pos, anchor) > leash + 1f)
        {
            Target = null;
            if (ModKitUtil.State(Master) != "Move")
                Walk(null, anchor, 0.5f);
            return true;
        }
        List<Entity_Master> foes = ModKitUtil.FoesNear(Master, pos, UloeData.PhotonEngage);
        foes.RemoveAll(f => ModKitUtil.Flat(ModKitUtil.Pos(f), anchor) > leash + range);
        Entity_Master target = foes.Count > 0 ? foes[0] : null;
        if (target == null)
        {
            Target = null;
            return false;
        }
        string state = ModKitUtil.State(Master);
        if (ModKitUtil.Flat(ModKitUtil.Pos(target), pos) > range - 0.1f && (Target != target || state != "Move"))
        {
            Walk(target, Vector3.zero, 0f);
            Fights++;
        }
        Target = target;
        return true;
    }

    void Wander(Vector3 pos)
    {
        Vector3 anchor = Anchor;
        if (UnityEngine.Random.value < UloeData.PhotonVisit)
        {
            List<UloeBuilding> near = new List<UloeBuilding>();
            foreach (UloeBuilding b in UloeServer.Buildings)
                if (b != null && b.Started && b.Master != null && ModKitUtil.Team(b.Master) == ModKitUtil.Team(Master) && ModKitUtil.Flat(b.Pos, anchor) <= UloeData.PhotonLeash && ModKitUtil.Flat(b.Pos, pos) > 1.2f)
                    near.Add(b);
            if (near.Count > 0)
            {
                Walk(near[UnityEngine.Random.Range(0, near.Count)].Master, Vector3.zero, 1f);
                Visits++;
                return;
            }
        }
        for (int tries = 0; tries < 4; tries++)
        {
            float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f), d = UnityEngine.Random.Range(1f, UloeData.PhotonLeash);
            Genitem tile = ModKitUtil.Tile(anchor + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d);
            Link_Being being = Master.Beingserver;
            if (tile == null || being == null || being.Currentblock == null)
                continue;
            Genitem stand = null;
            try { stand = Helper_Gridtools.Checkifstandable(tile, being); } catch (Exception) { }
            if (stand == null || !Helper_Navtools.Getpath(being.Currentblock, stand, being, false).Valid)
                continue;
            Walk(null, stand.Position, 0f);
            return;
        }
    }

    void Walk(Entity_Master target, Vector3 position, float stop)
    {
        ModKitUtil.MoveTo(Master, target, position, stop);
        Walks++;
    }

    // Killed, with Short Circuit: it bursts on the foes around it. The Bank takes the kill credit.
    public void Gone(bool killed, string reason)
    {
        Vector3 pos = Pos;
        Entity_Master radio = Radio();
        if (killed && Home != null && Home.Master != null && Home.Has("Short"))
        {
            float damage = Home.Param("Burstdamage_Base", 3f), radius = Home.Param("Burstradius_Base", 1.5f);
            foreach (Entity_Master foe in ModKitUtil.FoesNear(radio, pos, radius))
            {
                ModKitUtil.Hurt(radio, foe, damage);
                if (ModKitUtil.Alive(foe))
                {
                    UloeMarks.Blind(radio, foe, UloeData.BurstBlind);
                    UloeMarks.Illuminate(radio, foe, UloeData.BurstLit);
                }
            }
            UloeServer.Bursts++;
            ModKitUtil.Vis(radio, "Uloe_Burst", pos, radius);
        }
        else if (killed)
            ModKitUtil.Vis(radio, "Uloe_Down", pos, 0f);
    }
}
