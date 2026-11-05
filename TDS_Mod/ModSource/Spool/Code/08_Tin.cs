// Repurposed Cookie Tin: stitches Crafted from the Yarn Stash's Thread, one every Respawn seconds while fewer than its
// Units are alive, and sends each to the spot its TARGET button picked (the Tin itself until one is picked). A pin
// marker every player sees stands on the current spot. Crafted already out keep the spot they were sent to.
// Directions its owner's vessel holds (docs/scope/11, 2026-11-05):
//   Double Needle  stitch time -15 / 30 / 30%; rank III: every third stitch makes one more Crafted for no Thread (it may
//                  stand one over the Tin's Units until one falls).
//   Button Jar     each stitch makes two smaller Crafted for one's Thread (each costs half for selling, its refund
//                  rounded down on its own), each with 55 / 65 / 75% of the health and damage (a Pmulti: it scales the
//                  Tin's Woolen Lining too); Units doubled. With one slot free it makes one, for half the Thread.
//   Pattern Book   once a Tide, the first 1 / 2 / 3 of its Crafted to fall are stitched again for free (no Thread back
//                  when sold), in the fallen one's place even past the Tin's Units; ranks I and II on the usual stitch
//                  time, rank III at once.
public class SpoolTinBrain : SpoolBuilding
{
    public bool HasSpot;
    public Vector3 Spot;
    public Entity_Master Pin;
    public int Stitched, Spent, Targets, Doubled, Restitched;
    public string LastTarget = "";
    float nextStitch;
    bool shortWarned;
    int needleCount, bookWave = -1, bookUsed;
    readonly List<string> again = new List<string>();   // Pattern Book: the kinds of the fallen to stitch again

    public int Slots { get { return Mathf.Max(1, Mathf.RoundToInt(Stat("Units", 4f))) * (Player.Buttons > 0 ? SpoolData.ButtonPair : 1); } }
    public float StitchTime
    {
        get
        {
            int needle = Player.Needle;
            return Mathf.Max(1f, Stat("Respawn", 4f) * (1f - (needle > 0 ? SpoolData.NeedleCut[needle - 1] : 0f)));
        }
    }
    public int FreeWaiting { get { return again.Count; } }
    public float Reach { get { return Stat("Reach", 8f); } }
    public string Kind { get { return Has("Design") ? SpoolData.Dart : SpoolData.Stitched; } }
    public int Alive { get { return SpoolServer.UnitsOf(this); } }

    public int Cost(string kind)
    {
        if (kind == SpoolData.Stitched && Has("Discounted"))
            return Mathf.RoundToInt(Param("Stitchedcost_Base", 8f));
        SpoolData.Unit u = SpoolData.UnitFor(kind);
        return u != null ? u.Thread : 10;
    }

    protected override void Started2()
    {
        if (SpoolContent.Target != null)
        {
            Helper_Entitytools.Createcommand(Master, SpoolData.TargetCommand, SpoolData.TargetLink, false, true);
            Helper_Entitytools.Updatecommand(Master, SpoolData.TargetLink, "", false, false);
        }
        nextStitch = Time.time + 1f;
    }

    protected override void Tick()
    {
        // Pattern Book: a fallen one stitched again for free in its place (rank III at once, else on the usual stitch
        // time). One that can't be made waits a second, as a paid one does.
        if (again.Count > 0 && (Player.Pattern >= 3 || Time.time >= nextStitch))
        {
            if (Make(again[0], 0) == null)
            {
                nextStitch = Time.time + 1f;
                return;
            }
            again.RemoveAt(0);
            Restitched++;
            if (Player.Pattern < 3)
                nextStitch = Time.time + StitchTime;
            return;
        }
        if (Time.time < nextStitch || Alive >= Slots)
            return;
        string kind = Kind;
        int cost = Cost(kind);
        if (!SpoolServer.SpendStash(Owner, cost))
        {
            if (!shortWarned)
                SpoolUtil.Warn(Master, Player.Home == null ? SpoolData.WarnNoStash : SpoolData.WarnShort);
            shortWarned = true;
            nextStitch = Time.time + 1f;
            return;
        }
        shortWarned = false;
        // Button Jar: two for one's Thread, the cost split between them for selling; as many as there is room for, each
        // paying its share (what isn't made is refunded below).
        int pair = Player.Buttons > 0 ? SpoolData.ButtonPair : 1, room = Mathf.Min(pair, Slots - Alive), paid = 0, made = 0;
        for (int i = 0; i < room; i++)
        {
            int share = i < pair - 1 ? cost / pair : cost - cost / pair * (pair - 1);
            if (Make(kind, share) != null)
            {
                paid += share;
                made++;
            }
        }
        if (made == 0)
        {
            SpoolServer.AddStash(Owner, cost);
            nextStitch = Time.time + 1f;
            return;
        }
        if (paid < cost)
            SpoolServer.AddStash(Owner, cost - paid);
        Spent += paid;
        Stitched++;
        // Double Needle III: every third stitch, one more for no Thread.
        if (Player.Needle >= 3 && ++needleCount % SpoolData.NeedleEvery == 0 && Make(kind, 0) != null)
            Doubled++;
        nextStitch = Time.time + StitchTime;
    }

    // One Crafted of this Tin, sent to its spot with the Tin's upgrades (Button Jar's share when its owner holds it).
    SpoolUnitBrain Make(string kind, int cost)
    {
        SpoolUnitBrain u = SpawnUnit(kind);
        if (u == null)
            return null;
        u.Spot = HasSpot ? Spot : Pos;
        u.Cost = cost;
        u.Recycle = Has("Recycle") ? Param("Recycle_Base", 0.5f) : 0f;
        u.Burst = Has("Surprise") ? Param("Burst_Times", 3f) : 0f;
        u.HealthMulti = Param("Craftedhealth_Multi", 0f);
        u.SpeedMulti = kind == SpoolData.Stitched && Has("Discounted") ? Param("Stitchedspeed_Multi", SpoolData.StitchedSpeedUp) : 0f;
        int buttons = Player.Buttons;
        u.Small = buttons > 0 ? SpoolData.ButtonShare[buttons - 1] : 0f;
        ModKitUtil.Vis(Master, "Spool_Stitch", Pos, 0f);
        return u;
    }

    // Pattern Book: one of this Tin's Crafted fell (killed, not sold). The first ones of each Tide come back.
    public void Fell(SpoolUnitBrain u)
    {
        int rank = Player.Pattern;
        if (rank <= 0 || u == null || u.Def == null)
            return;
        int wave = ModKitTides.Wave >= 0 ? ModKitTides.Wave : (Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Wave.Value : 0);
        if (wave != bookWave)
        {
            bookWave = wave;
            bookUsed = 0;
        }
        if (bookUsed >= SpoolData.PatternFree[rank - 1])
            return;
        bookUsed++;
        again.Add(u.Def.Id);
    }

    // TARGET: the stock Relocate picker sends the clicked tile. It must be within Reach and a Crafted must be able to
    // stand there and walk to it (checked with a live Crafted of this Tin, when there is one).
    protected override void Command(string command, string json, bool server)
    {
        Vector3 at;
        if (command != SpoolData.TargetLink || !ModKitUtil.JsonPosition(json, out at))
            return;
        Targets++;
        string why;
        Genitem spot = Reachable(at, out why);
        LastTarget = why;
        if (spot == null)
        {
            SpoolUtil.Warn(Master, why == "far" ? SpoolData.WarnFar : SpoolData.WarnUnreachable);
            return;
        }
        HasSpot = true;
        Spot = spot.Position;
        MovePin(spot);
        SpoolUtil.Warn(Master, SpoolData.WarnSpot);
        ModKitUtil.Vis(Master, "Spool_Target", Spot, 0f);
    }

    public Genitem Reachable(Vector3 at, out string why)
    {
        if (ModKitUtil.Flat(at, Pos) > Reach + 0.5f)
        {
            why = "far";
            return null;
        }
        Genitem tile = ModKitUtil.Tile(at);
        if (tile == null)
        {
            why = "no tile";
            return null;
        }
        foreach (SpoolUnitBrain u in SpoolServer.Units)
        {
            if (u == null || u.Master == null || u.Home != this || u.Master.Beingserver == null || u.Master.Beingserver.Currentblock == null)
                continue;
            Link_Being being = u.Master.Beingserver;
            Genitem stand = null;
            try { stand = Helper_Gridtools.Checkifstandable(tile, being); } catch (Exception) { }
            if (stand == null)
            {
                why = "not standable";
                return null;
            }
            Link_Path path = Helper_Navtools.Getpath(being.Currentblock, stand, being, false);
            if (!path.Valid)
            {
                why = "no path";
                return null;
            }
            why = "ok, path " + path.Length;
            return stand;
        }
        why = "ok, unchecked";
        return tile;
    }

    void MovePin(Genitem tile)
    {
        if (Pin != null && Pin.Beingserver != null)
            Helper_Entitytools.Despawn(Pin.Beingserver, false, "Move");
        Pin = null;
        Beingobject being;
        if (!SpoolContent.Beings.TryGetValue(SpoolData.Pin, out being) || Master.Beingserver == null)
            return;
        Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(Master.Beingserver);
        spawn.Genadd = tile;
        spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        spawn.Being = being;
        spawn.Destroyonparent = true;
        Link_Being b = Helper_Entitytools.Spawn(spawn, tile.Position, Master.Beingserver, true, null);
        Pin = b != null ? b.Worldobject : null;
    }

    public override void Gone(bool killed, string reason)
    {
        if (Pin != null && Pin.Beingserver != null)
            Helper_Entitytools.Despawn(Pin.Beingserver, false, "Parent");
        Pin = null;
    }
}

// Server-side brain for one of Spool's units: a Tin's Crafted (Stitched, Dart), a Loom's Seam, a Pin Cushion's Mangled.
// Fighting is the game's own Entity_Ai_Mob_Combat / Entity_Ai_Mob_Ranged (they pick a fight while the unit is Idle);
// this brain only decides where it walks, the way the shipped units' AIs do it (a Move command with a Goalobject or a
// Goalposition). Crafted run to their spot ignoring foes on the way, then guard it within their leash.
// Directions its owner's vessel holds (docs/scope/11, 2026-11-05):
//   Merit Badge  +3% Damage and Health (Multi) for each foe it drowns (lands the last hit on), up to +15 / 30 / 45%.
//   Odd Socks    +8 / 15 / 25% Damage while a Crafted of another kind and its team stands within 2 tiles; rank III:
//                also 10% less damage taken (a handler at the front of its hit chain).
public class SpoolUnitBrain : MonoBehaviour
{
    public Entity_Master Master;
    public SpoolData.Unit Def;
    public SpoolBuilding Home;
    public Vector3 Spot;
    public bool Started, Arrived, Holding;
    public int Cost, Orders, Drowns;
    public float Recycle, Burst, HealthMulti, SpeedMulti, Leap, Small, MeritBonus, SocksBonus;
    public Entity_Master Target, LeapAt;
    public bool Guarded;

    float nextThink, nextFront;
    bool armed;
    Link_Modifier meritDamage, meritHealth, socksDamage;
    static readonly Entity_Master.Oninteracts guard = OnGuarded;

    public long Owner { get { return ModKitUtil.Owner(Master); } }
    bool Crafted { get { return Def.Sellable; } }

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
            if (Def.Ranged && !armed)
                Arm();
            // Updateinteract rebuilds the hit chain when an upgrade lands: Odd Socks' guard goes back to the front.
            if (Guarded && Time.time >= nextFront)
            {
                nextFront = Time.time + 0.1f;
                ModKitUtil.Front(Master, guard);
            }
            if (Time.time >= nextThink)
            {
                nextThink = Time.time + 0.4f;
                Think();
            }
        }
        catch (Exception e)
        {
            SpoolLog.Warn(Def.Id + " brain: " + e.Message);
            enabled = false;
        }
    }

    void Begin()
    {
        Started = true;
        if (Spot == Vector3.zero)
            Spot = Home != null && Home.Master != null ? Home.Pos : ModKitUtil.Pos(Master);
        if (HealthMulti != 0f)
        {
            Helper_Entitytools.Addmod(Master, "Health", HealthMulti, Helper_Entitytools.Modtypes.Multi, 0f);
            Fill();
        }
        if (SpeedMulti != 0f)
            Helper_Entitytools.Addmod(Master, "Speed", SpeedMulti, Helper_Entitytools.Modtypes.Multi, 0f);
        // Button Jar: a share of a whole one's health and damage (a Pmulti multiplies with Woolen Lining's Multi).
        if (Small > 0f)
        {
            Helper_Entitytools.Addmod(Master, "Health", Small - 1f, Helper_Entitytools.Modtypes.Pmulti, 0f);
            Helper_Entitytools.Addmod(Master, "Damage", Small - 1f, Helper_Entitytools.Modtypes.Pmulti, 0f);
            Fill();
        }
        ModKitUtil.Vis(Master, "Spool_Pop", ModKitUtil.Pos(Master), 0f);
        if (Crafted)
            Walk(null, Spot, 0f);
        else if (Leap > 0f)
            LeapIn();
    }

    // A raised maximum starts full.
    void Fill()
    {
        Link_Stats s;
        if (Master.Statsserver.TryGetValue("Health", out s))
            s.Value = s.Value_Calc;
    }

    // Entity_Ai_Mob_Ranged creates the Attack command when it spawns; its Goalstring names the projectile it throws
    // (Sno Sno's AI names its snowball the same way): the Dart's needle.
    void Arm()
    {
        Link_Command attack;
        if (!Master.Commandlistserver.TryGetValue("Attack", out attack) || attack == null)
            return;
        attack.Goalstring = SpoolData.Needle;
        armed = true;
    }

    Vector3 Anchor
    {
        get
        {
            if (Crafted || Holding)
                return Spot;
            return Home != null && Home.Master != null ? Home.Pos : Spot;
        }
    }

    void Think()
    {
        if (Crafted)
            Socks();
        Vector3 pos = ModKitUtil.Pos(Master);
        if (Crafted && !Arrived)
        {
            if (ModKitUtil.Flat(pos, Spot) <= 0.9f)
                Arrived = true;
            else
            {
                if (ModKitUtil.State(Master) != "Move")
                    Walk(null, Spot, 0f);
                return;
            }
        }
        if (Leap > 0f && !Holding)
        {
            if (ModKitUtil.Flat(pos, Spot) <= 0.6f || ModKitUtil.State(Master) != "Move")
            {
                Holding = true;
                Spot = pos;
            }
            return;
        }
        Entity_Master target = Guard();
        SpoolCushionBrain cushion = Home as SpoolCushionBrain;
        if (target != null && cushion != null && cushion.Has("Burst") && ModKitUtil.Flat(ModKitUtil.Pos(target), pos) <= ModKitUtil.Stat(Master, "Range", 1.5f) + 0.2f)
            BurstOn(cushion);
        else if (target == null && Def.Id == SpoolData.Seam)
            Mine(pos);
    }

    // Fights what comes within its leash of its anchor (spot, Loom or Cushion), else goes back to it.
    Entity_Master Guard()
    {
        Vector3 pos = ModKitUtil.Pos(Master);
        Vector3 anchor = Anchor;
        float leash = Def.Leash;
        float range = ModKitUtil.Stat(Master, "Range", 1.5f);
        List<Entity_Master> foes = ModKitUtil.FoesNear(Master, pos, Mathf.Max(SpoolData.CraftedEngage, range + 0.5f));
        foes.RemoveAll(f => ModKitUtil.Flat(ModKitUtil.Pos(f), anchor) > leash + range);
        Entity_Master target = foes.Count > 0 ? foes[0] : null;
        string state = ModKitUtil.State(Master);
        if (ModKitUtil.Flat(pos, anchor) > leash + 1f)
            target = null;   // strayed past its leash: back first
        if (target != null)
        {
            float d = ModKitUtil.Flat(ModKitUtil.Pos(target), pos);
            if (Holding)
            {
                // Holds its tile: the stock AI swings at what is in reach.
            }
            else if (Def.Ranged)
            {
                if (d > range - 0.3f)
                    Walk(null, ModKitUtil.Pos(target) + (pos - ModKitUtil.Pos(target)).normalized * Mathf.Max(1f, range - 1f), 0f);
                else if (state == "Move")
                    ModKitUtil.Stop(Master);
            }
            else if (d > range - 0.1f && (Target != target || state != "Move"))
                Walk(target, Vector3.zero, 0f);
            Target = target;
            return target;
        }
        Target = null;
        if (!Holding && ModKitUtil.Flat(pos, anchor) > (Crafted ? 0.9f : leash) && state != "Move" && state != "Attack" && Def.Id != SpoolData.Seam)
            Walk(null, anchor + Offset(), 0f);
        return null;
    }

    // Spread around the anchor so a crowd doesn't stand on one tile.
    Vector3 Offset()
    {
        if (Crafted)
            return Vector3.zero;
        float a = (Master.NetworkObjectId % 7) * 0.9f;
        return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.9f;
    }

    // A Seam with nothing to fight stands at the nearest ore in reach of its Loom; idle there, the stock melee AI
    // strikes it (Smacky's miners mine the same way), and the ore pays the Seam's owner.
    void Mine(Vector3 pos)
    {
        string state = ModKitUtil.State(Master);
        if (state == "Move" || state == "Attack")
            return;
        Vector3 loom = Anchor;
        Entity_Master ore = Helper_Entityquerytools.Entitynearest(loom, Def.Leash + 1f, ModKitUtil.Tagged("Resource"));
        if (ore == null)
        {
            if (ModKitUtil.Flat(pos, loom) > Def.Leash)
                Walk(null, loom + Offset(), 0f);
            return;
        }
        if (ModKitUtil.Flat(ModKitUtil.Pos(ore), pos) > ModKitUtil.Stat(Master, "Range", 1.5f))
            Walk(ore, Vector3.zero, 1f);
    }

    void Walk(Entity_Master target, Vector3 position, float stop)
    {
        ModKitUtil.MoveTo(Master, target, position, stop);
        Orders++;
    }

    // Overstuffed: the Mangled leaps up to its Leap toward the foe that hit its Cushion, and holds that tile.
    void LeapIn()
    {
        Vector3 from = ModKitUtil.Pos(Master);
        Vector3 to = LeapAt != null && ModKitUtil.Alive(LeapAt) ? ModKitUtil.Pos(LeapAt) : from;
        Vector3 dir = to - from;
        dir.y = 0f;
        float far = Mathf.Min(Leap, dir.magnitude - 0.8f);
        if (far < 0.5f)
        {
            Holding = true;
            Spot = from;
            return;
        }
        Genitem tile = ModKitUtil.Tile(from + dir.normalized * far);
        Spot = tile != null ? tile.Position : from + dir.normalized * far;
        Helper_Entitytools.Addmod(Master, "Speed", 2f, Helper_Entitytools.Modtypes.Multi, 1.2f);
        Walk(null, Spot, 0f);
        ModKitUtil.Vis(Master, "Spool_Leap", Spot, 0f);
    }

    // Burst Seams: a Mangled that reaches a foe bursts on every foe within the radius.
    void BurstOn(SpoolCushionBrain cushion)
    {
        Vector3 pos = ModKitUtil.Pos(Master);
        float damage = cushion.Param("Seams_Damage", 5f), radius = cushion.Param("Seams_Radius", 1.25f);
        foreach (Entity_Master foe in ModKitUtil.FoesNear(Master, pos, radius))
            ModKitUtil.Hurt(cushion.Master, foe, damage);
        cushion.Bursts++;
        ModKitUtil.Vis(cushion.Master, "Spool_Burst", pos, radius);
        Helper_Entitytools.Despawn(Master.Beingserver, true, "Burst");
    }

    // ---------------------------------------------------------------- Merit Badge and Odd Socks

    // A foe this Crafted landed the last hit on fell (SpoolServer.OnDespawn).
    public void Drowned()
    {
        int rank = SpoolServer.Player(Owner).Merit;
        if (!Crafted || rank <= 0 || !ModKitUtil.Alive(Master))
            return;
        Drowns++;
        float bonus = Mathf.Min(SpoolData.MeritEach * Drowns, SpoolData.MeritCap[rank - 1]);
        if (bonus <= MeritBonus + 0.0001f)
            return;
        MeritBonus = bonus;
        meritDamage = ModKitUtil.Keep(Master, meritDamage, "Damage", Helper_Entitytools.Modtypes.Multi, bonus);
        // Made anew rather than kept: Keep fills the stat, and Health is a Variable stat, which keeps its share of the
        // maximum when the maximum moves.
        ModKitUtil.Unmod(meritHealth);
        meritHealth = Helper_Entitytools.Addmod(Master, "Health", bonus, Helper_Entitytools.Modtypes.Multi, 0f);
        ModKitUtil.Vis(Radio(), "Spool_Merit", ModKitUtil.Pos(Master), Drowns);
    }

    void Socks()
    {
        int rank = SpoolServer.Player(Owner).Socks;
        bool beside = rank > 0 && SpoolServer.OtherKindNear(this, SpoolData.SocksRange);
        float bonus = beside ? SpoolData.SocksBonus[rank - 1] : 0f;
        if (!Mathf.Approximately(bonus, SocksBonus))
        {
            socksDamage = ModKitUtil.Keep(Master, socksDamage, "Damage", Helper_Entitytools.Modtypes.Multi, bonus);
            SocksBonus = bonus;
        }
        bool guarded = beside && rank >= 3;
        if (guarded == Guarded)
            return;
        Guarded = guarded;
        if (guarded)
            ModKitUtil.Front(Master, guard);
        else
            ModKitUtil.Unfront(Master, guard);
    }

    // Odd Socks III: 10% less from each hit while it stands beside another kind.
    static void OnGuarded(Link_Interact hit)
    {
        try
        {
            float multi;
            if (hit == null || hit.Type != "Hurt" || hit.To == null || hit.Data == null || !hit.Data.TryGetValue("Damagemulti", out multi))
                return;
            SpoolUnitBrain u = hit.To.GetComponent<SpoolUnitBrain>();
            if (u == null || !u.Guarded)
                return;
            // A copy: some AIs reuse one dictionary for several hits (pitfall #30).
            hit.Data = new Dictionary<string, float>(hit.Data);
            hit.Data["Damagemulti"] = multi * (1f - SpoolData.SocksGuard);
            Socked++;
        }
        catch (Exception e)
        {
            SpoolLog.Warn("Odd Socks: " + e.Message);
        }
    }

    public static int Socked;

    Entity_Master Radio()
    {
        if (Home != null && Home.Master != null && Home.Master.Radio != null)
            return Home.Master;
        SpoolHeroBrain hero = SpoolServer.HeroOf(Owner);
        return hero != null && hero.Master != null ? hero.Master : Master;
    }

    // Sold: half its Thread back, rounded down (what doesn't fit drops beside it), or Efficient Recycler's share, whose
    // rank III also mends around it. Killed: Reuse Reduce Recycle drops part of its cost, One Last Surprise bursts on the
    // foes in its reach.
    public void Gone(bool killed, string reason)
    {
        Vector3 pos = ModKitUtil.Pos(Master);
        Entity_Master radio = Radio();
        if (reason == "Sell" && Crafted)
        {
            int rank = SpoolServer.Player(Owner).Recycler;
            SpoolData.Recycler recycler = rank > 0 ? SpoolData.Recyclers[rank - 1] : null;
            int back = recycler != null ? Cost * recycler.Percent / 100 : Cost / 2;
            int over = SpoolServer.AddStash(Owner, back);
            if (over > 0)
                SpoolServer.Drop(pos, over);
            SpoolServer.Refunded += back;
            ModKitUtil.Vis(radio, "Spool_Unstitch", pos, back);
            if (recycler != null)
                SpoolServer.MendAround(radio, pos, recycler.Radius, recycler.Heal, Master);
            return;
        }
        if (!killed)
            return;
        SpoolTinBrain tin = Home as SpoolTinBrain;
        if (Crafted && tin != null && tin.Master != null)
            tin.Fell(this);
        if (Recycle > 0f && Cost > 0)
            SpoolServer.Drop(pos, Mathf.CeilToInt(Cost * Recycle));
        if (Burst > 0f && radio != Master)
        {
            float damage = ModKitUtil.Stat(Master, "Damage", 2f) * Burst, reach = ModKitUtil.Stat(Master, "Range", 1.5f);
            foreach (Entity_Master foe in ModKitUtil.FoesNear(radio, pos, reach))
                ModKitUtil.Hurt(radio, foe, damage);
            ModKitUtil.Vis(radio, "Spool_Burst", pos, reach);
        }
        ModKitUtil.Vis(radio, "Spool_Down", pos, 0f);
    }
}
