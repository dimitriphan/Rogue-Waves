// Server-side brain for a Naut (Port, Starboard, Bosun). It walks with the stock Entity_Ai_Mob_Movement, given a goal
// entity the way the Mining Hut's miners are sent to ore (Move command, Goalobject = the tower), and tunes the tower it
// was sent to: faster attacks and a slow repair, plus the Miner's Base's upgrades (range, armour, double tuning).
public class ArgoNautBrain : MonoBehaviour
{
    public Entity_Master Master;
    public ArgoData.Crew Crew;
    public ArgoTowerBrain Bell;
    public Entity_Master Post;
    public int Index = -1;
    public bool Tuning;

    bool started;
    float lastOrder, closest, closestAt, nextHeal;
    Entity_Master tunedTower;
    Link_Modifier speedMod, rangeMod, nautSpeedMod, saltMod;
    readonly ArgoFaults faults = new ArgoFaults();

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
            if (!started)
            {
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || Master.Radio == null)
                    return;
                started = true;
                if (Bell != null && Index < 0)
                    Index = Array.IndexOf(ArgoData.Nauts, Crew);
                Refit();
                Moved();
            }
            if (Bell == null || Bell.Master == null || !ModKitUtil.Alive(Bell.Master))
            {
                StopTune();
                if (Master.Beingserver != null && !Master.Beingserver.Dead)
                    Helper_Entitytools.Despawn(Master.Beingserver, false, "Bell lost");
                Master = null;
                return;
            }
            if (Post != null && !ModKitUtil.Alive(Post))
                PostLost();
            Vector3 pos = ModKitUtil.Pos(Master);
            if (Post != null)
            {
                float d = ModKitUtil.Flat(pos, ModKitUtil.Pos(Post));
                if (d <= ArgoData.TuneReach)
                {
                    if (!Tuning) StartTune();
                    Work(pos);
                    return;
                }
                if (Tuning && d > ArgoData.TuneReach + 0.6f)
                    StopTune();
                // Full Crew: dive instead of walking any distance.
                if (!Tuning && Bell.Param("Divebell_Base", 0f) > 0f && d > 2.5f && Time.time - lastOrder > 0.5f)
                {
                    Bell.Dive(this);
                    return;
                }
                Travel(Post, d);
            }
            else
            {
                StopTune();
                float d = ModKitUtil.Flat(pos, ModKitUtil.Pos(Bell.Master));
                if (d > 1.6f)
                    Travel(Bell.Master, d);
            }
        }
        catch (Exception e)
        {
            if (faults.Fail(Crew.Name + " brain", e))
                enabled = false;
        }
    }

    // ---------------------------------------------------------------- orders

    public void SendTo(Entity_Master tower)
    {
        if (tower == Post)
            return;
        StopTune();
        Post = tower;
        Moved();
        if (Master != null)
            ModKitUtil.Vis(Master, "Argo_Naut", ModKitUtil.Pos(Master), Index);
    }

    public void PostLost()
    {
        StopTune();
        if (Bell != null)
            Bell.ClearPost(Index);
        Post = null;
        Moved();
    }

    public void Gone(bool killed)
    {
        StopTune();
        if (Bell != null)
            Bell.CrewGone(this, killed);
    }

    void Moved()
    {
        lastOrder = -99f;
        closest = float.MaxValue;
        closestAt = Time.time;
    }

    // The stock movement AI walks to the nearest standable tile next to the goal entity.
    void Travel(Entity_Master goal, float distance)
    {
        if (distance < closest - 0.2f)
        {
            closest = distance;
            closestAt = Time.time;
        }
        else if (Post != null && Time.time - closestAt > 6f)
        {
            // No progress in six seconds: the tower can't be reached from here.
            ArgoUtil.Warn(Bell.Master, 200 + Index);
            PostLost();
            return;
        }
        string state = Master.State.Value.ToString();
        if (Time.time - lastOrder < (state == "Move" ? 3f : 0.75f))
            return;
        Link_Command move;
        if (!Master.Commandlistserver.TryGetValue("Move", out move) || move == null)
            return;
        lastOrder = Time.time;
        move.Goalobject = goal;
        move.Goalposition = Vector3.one * -1f;
        move.Goalstring = "Idle";
        Helper_Entitytools.Updatecommand(Master, "Move", "", true, true);
    }

    // ---------------------------------------------------------------- tuning

    void StartTune()
    {
        if (Post == null)
            return;
        Tuning = true;
        tunedTower = Post;
        float boost = ArgoData.TuneAttackspeed + Bell.Param("Tuneboost_Base", 0f);
        if (Post.Statsserver.ContainsKey("Attackspeed"))
            speedMod = Helper_Entitytools.Addmod(Post, "Attackspeed", boost, Helper_Entitytools.Modtypes.Multi, 0f);
        float reach = Bell.Param("Tunerange_Base", 0f);
        if (reach > 0f && Post.Statsserver.ContainsKey("Range"))
            rangeMod = Helper_Entitytools.Addmod(Post, "Range", reach, Helper_Entitytools.Modtypes.Add, 0f);
        float armor = Bell.Param("Bulkhead_Base", 0f);
        if (armor > 0f)
            ArgoHooks.Armor(Post, this, armor);
        // Old Salts: each kind of Argo tower is tuned its own way (other towers get the attack speed only).
        if (Bell.Has("Salts"))
        {
            string being = ModKitUtil.BeingName(Post);
            if (being == ArgoData.Scatter && Post.Statsserver.ContainsKey("Projcount"))
                saltMod = Helper_Entitytools.Addmod(Post, "Projcount", ArgoData.SaltsPellets, Helper_Entitytools.Modtypes.Add, 0f);
            else if (being == ArgoData.Harpoon && Post.Statsserver.ContainsKey("Pierce"))
            {
                saltMod = Helper_Entitytools.Addmod(Post, "Pierce", ArgoData.SaltsPierce, Helper_Entitytools.Modtypes.Add, 0f);
                ArgoTowerBrain harpoon = ArgoServer.TowerOf(Post);
                if (harpoon != null) harpoon.TunedReel = ArgoData.SaltsReel;
            }
            else if (being == ArgoData.Drill && Post.Statsserver.ContainsKey("Slow"))
                saltMod = Helper_Entitytools.Addmod(Post, "Slow", ArgoData.SaltsSlow, Helper_Entitytools.Modtypes.Add, 0f);
        }
        ModKitUtil.Skin(Master, "Tune");
        ModKitUtil.Vis(Master, "Argo_Tune", ModKitUtil.Pos(Post), Index);
        nextHeal = Time.time + 1f;
    }

    public void StopTune()
    {
        if (!Tuning)
            return;
        Tuning = false;
        ModKitUtil.Unmod(speedMod);
        ModKitUtil.Unmod(rangeMod);
        ModKitUtil.Unmod(saltMod);
        speedMod = rangeMod = saltMod = null;
        if (tunedTower != null)
        {
            ArgoHooks.Unarmor(tunedTower, this);
            ArgoTowerBrain t = ArgoServer.TowerOf(tunedTower);
            if (t != null) t.TunedReel = 0f;
        }
        tunedTower = null;
        if (Master != null)
            ModKitUtil.Skin(Master, "");
    }

    void Work(Vector3 pos)
    {
        Vector3 face = ModKitUtil.Pos(Post) - pos;
        face.y = 0f;
        if (face.sqrMagnitude > 0.01f)
            Master.Entitydirection.Value = face.normalized;
        if (Time.time < nextHeal)
            return;
        nextHeal = Time.time + 1f;
        float heal = ArgoData.TuneHeal + Bell.Param("Repair_Base", 0f);
        Link_Stats hp;
        if (heal > 0f && Post.Statsserver.TryGetValue("Health", out hp) && hp.Value < hp.Value_Calc - 0.01f)
        {
            Helper_Entitytools.Interact(Master, Post, "Heal", ArgoUtil.Hit(heal, 1f));
            ModKitUtil.Vis(Master, "Argo_Repair", ModKitUtil.Pos(Post), Index);
        }
    }

    // The bell's crew upgrade (Flippers), and a re-tune when the bell's tuning upgrades change.
    public void Refit()
    {
        if (Master == null || Bell == null || !started)
            return;
        ModKitUtil.Unmod(nautSpeedMod);
        nautSpeedMod = null;
        float speed = Bell.Param("Nautspeed_Multi", 0f);
        if (speed > 0f && Master.Statsserver.ContainsKey("Speed"))
            nautSpeedMod = Helper_Entitytools.Addmod(Master, "Speed", speed, Helper_Entitytools.Modtypes.Multi, 0f);
        if (Tuning)
        {
            StopTune();
            StartTune();
        }
    }

    void OnDestroy()
    {
        StopTune();
    }
}
