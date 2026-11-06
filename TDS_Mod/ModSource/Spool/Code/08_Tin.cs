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

    public int Slots { get { return Mathf.Max(1, Mathf.RoundToInt(Stat("Units", 6f))) * (Player.Buttons > 0 ? SpoolData.ButtonPair : 1); } }
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

    // Quilting Bee (the Yarn Stash's BEE): stitches up to the Tin's Units at once, for no Thread (sold, they give nothing
    // back). Returns how many it made.
    public int Sew()
    {
        int made = 0;
        for (int guard = 0; Alive < Slots && guard < 64; guard++)
        {
            if (Make(Kind, 0) == null)
                break;
            made++;
        }
        return made;
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
    bool armed, begun;
    static readonly SpoolFaults.Log faultLog = new SpoolFaults.Log();
    readonly SpoolFaults faults = new SpoolFaults(faultLog);
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
                begun = true;
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
            // Pitfall #80: a bad tick is skipped; a Begin that threw (Started is set on its first line) still stops it.
            if (faults.Fail(Def.Id + " brain", e) || !begun)
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
        // Button Jar: a share of a whole one's health and damage (a Pmulti multiplies with Woolen Lining's Multi), and the
        // smaller look where its kind has one.
        if (Small > 0f)
        {
            Helper_Entitytools.Addmod(Master, "Health", Small - 1f, Helper_Entitytools.Modtypes.Pmulti, 0f);
            Helper_Entitytools.Addmod(Master, "Damage", Small - 1f, Helper_Entitytools.Modtypes.Pmulti, 0f);
            Fill();
            if (Def != null && SpoolContent.HasSmallLook(Def.Id))
                ModKitUtil.Skin(Master, SpoolData.SmallSkin);
        }
        // Pitfall #158: told through its home (or Spool), which every client has hooked: the unit itself is a frame old.
        ModKitUtil.Vis(Radio(), "Spool_Pop", ModKitUtil.Pos(Master), 0f);
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
        else if (target == null && Def.Id == SpoolData.Seam && !Snare(pos))
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

    // ---------------------------------------------------------------- a Loom's Seams: Snare Line, Dyed in the Wool

    // Snare Line: the aura beast it hunts. Dyed in the Wool: the node its last hit landed on, what the node held and the
    // hit could take, and the owner's purse before it, read again in LateUpdate.
    public Entity_Master Prey;
    bool hunting, struckNoted;
    float nextSnare, nextListen, struckBefore, struckHit;
    Entity_Master struck;
    readonly float[] purseBefore = new float[3], purseAfter = new float[3];
    Entity_Master.Interacts dealt;

    // Aura beasts, by being name (the large one is an aura beast too): they wander the map and pay every player when they
    // fall (kit docs/16 section 3).
    static readonly string[] Beasts = { "Mob_Aurabeast", "Mob_Largeaurabeast" };

    static bool IsBeast(Entity_Master e) { return Array.IndexOf(Beasts, ModKitUtil.BeingName(e)) >= 0; }

    // Snare Line: with no foe within its leash of the Loom, the Seam goes after the aura beast nearest it within the
    // Loom's Snare reach and strikes it itself at its attack speed (a beast may be no foe to the stock melee AI). With
    // none left, or a foe at the Loom, it walks back, and Mine takes it to the ore from there. True while it hunts or
    // turns back (Mine waits).
    bool Snare(Vector3 pos)
    {
        SpoolLoomBrain loom = Home as SpoolLoomBrain;
        float reach = loom != null && loom.Master != null ? loom.SnareReach : 0f;
        if (reach <= 0f)
            return false;
        Vector3 anchor = loom.Pos;
        float range = ModKitUtil.Stat(Master, "Range", 1.5f);
        Entity_Master prey = FoeAtLoom(anchor, range) ? null : Beast(anchor, reach, pos);
        if (prey == null)
        {
            if (!hunting)
                return false;
            hunting = false;
            Prey = null;
            Walk(null, anchor + Offset(), 0f);
            return true;
        }
        bool fresh = !hunting || prey != Prey;
        if (fresh)
            loom.Hunts++;
        hunting = true;
        Prey = prey;
        string state = ModKitUtil.State(Master);
        if (ModKitUtil.Flat(ModKitUtil.Pos(prey), pos) > range + 0.2f)
        {
            if (fresh || state != "Move")
                Walk(prey, Vector3.zero, Mathf.Max(0f, range - 0.5f));
            return true;
        }
        if (state == "Move")
            ModKitUtil.Stop(Master);
        // When the stock melee AI is already swinging at this beast (it is a foe to it on this map), its hits are enough.
        Link_Command attack;
        bool swinging = state == "Attack" && Master.Commandlistserver.TryGetValue("Attack", out attack) && attack != null && attack.Goalobject == prey;
        if (!swinging && Time.time >= nextSnare)
        {
            nextSnare = Time.time + 1f / Mathf.Max(0.05f, ModKitUtil.Stat(Master, "Attackspeed", 0.8f));
            ModKitUtil.Hurt(Master, prey, ModKitUtil.Stat(Master, "Damage", 1f));
            loom.Snared++;
            ModKitUtil.Vis(Radio(), "Spool_Strike", ModKitUtil.Pos(prey), 0f);
        }
        return true;
    }

    // A foe other than a beast within the Seam's leash of its Loom: it guards first.
    bool FoeAtLoom(Vector3 loom, float range)
    {
        foreach (Entity_Master f in ModKitUtil.FoesNear(Master, loom, Def.Leash + range))
            if (!IsBeast(f))
                return true;
        return false;
    }

    // The aura beast nearest the Seam within 'reach' of its Loom; the one it hunts is kept while it stays in reach. The
    // query asks for Mob-tagged things or things of another team (Styles.Either, read as either test), so a beast is found
    // whichever team the map gives it.
    Entity_Master Beast(Vector3 loom, float reach, Vector3 pos)
    {
        if (hunting && ModKitUtil.Alive(Prey) && ModKitUtil.Flat(ModKitUtil.Pos(Prey), loom) <= reach)
            return Prey;
        Link_Entityquery query = new Link_Entityquery { Being = Master.Beingserver, Teamfor = false, Tags = new List<string> { "Mob" }, Style = Link_Entityquery.Styles.Either };
        int team = ModKitUtil.Team(Master);
        Entity_Master best = null;
        float nearest = float.MaxValue;
        foreach (Entity_Master e in ModKitUtil.Area(loom, reach, query))
        {
            if (!ModKitUtil.Alive(e) || ModKitUtil.Team(e) == team || !IsBeast(e) || ModKitUtil.Flat(ModKitUtil.Pos(e), loom) > reach)
                continue;
            float d = ModKitUtil.Flat(ModKitUtil.Pos(e), pos);
            if (d < nearest)
            {
                best = e;
                nearest = d;
            }
        }
        return best;
    }

    // Dyed in the Wool: a hit this Seam lands (the stock melee AI mines with it), heard on the attacker's side, which runs
    // before the node's own hit chain, where it pays. A hit on a node, for a dyed Loom, notes the node's count, what the
    // hit can take off it and the owner's purse; LateUpdate reads them again.
    void OnDealt(Link_Interact hit)
    {
        try
        {
            SpoolLoomBrain loom = Home as SpoolLoomBrain;
            if (hit == null || hit.Type != "Hurt" || loom == null || !loom.Dyed || !ModKitUtil.HasTag(hit.To, "Resource") || hit.To.Statsserver == null
                || !hit.To.Statsserver.ContainsKey("Healthresource") || !ModKitUtil.Purse(loom.Owner, purseBefore))
                return;
            float damage, extra;
            struck = hit.To;
            struckNoted = true;
            struckBefore = ModKitUtil.Stat(hit.To, "Healthresource", -1f);
            // A node takes min(Damage, 1) + Damageresource a hit (kit docs/16 section 4).
            struckHit = Mathf.Clamp01(hit.Data != null && hit.Data.TryGetValue("Damage", out damage) ? damage : 1f)
                + (hit.Data != null && hit.Data.TryGetValue("Damageresource", out extra) ? Mathf.Max(0f, extra) : 0f);
        }
        catch (Exception e)
        {
            SpoolLog.Warn("Seam hit: " + e.Message);
        }
    }

    // A Seam's hit noted this frame, read after every Update (the stock AI's hit and the node's payout among them): when
    // it paid, the owner's purse gain is traded into the Loom's colour. Another payout landing on the same frame (a
    // strike of the Loom's, a beast falling) is in that gain too, so it is traded as well.
    void LateUpdate()
    {
        if (Master == null || !Started || Def == null || Def.Id != SpoolData.Seam || !ModKitUtil.IsServer)
            return;
        try
        {
            SpoolLoomBrain loom = Home as SpoolLoomBrain;
            if (struckNoted)
            {
                struckNoted = false;
                Entity_Master ore = struck;
                struck = null;
                // A node that pays once is gone after it: Stat and Alive read it as gone, which Paid counts as paid.
                if (loom != null && loom.Dyed && SpoolLoomBrain.Paid(struckBefore, ModKitUtil.Stat(ore, "Healthresource", -1f), struckHit, ModKitUtil.Alive(ore))
                    && ModKitUtil.Purse(loom.Owner, purseAfter) && loom.Trade(purseBefore, purseAfter))
                    loom.SeamTraded++;
            }
            // Updateinteract clears the attacker's side of the hit chain too, so the handler is said again ten times a second.
            if (loom != null && loom.Dyed && Time.time >= nextListen)
            {
                nextListen = Time.time + 0.1f;
                if (dealt == null)
                    dealt = OnDealt;
                ModKitUtil.Listen(Master, dealt);
            }
        }
        catch (Exception e)
        {
            // A bad trade is skipped; the Seam's own brain carries on.
            faults.Fail(Def.Id + " dye", e);
        }
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
        ModKitUtil.Vis(Radio(), "Spool_Leap", Spot, 0f);
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
        meritHealth = ModKitUtil.Keep(Master, meritHealth, "Health", Helper_Entitytools.Modtypes.Multi, bonus);
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
            ModKitCombat.SetMulti(hit, multi * (1f - SpoolData.SocksGuard));
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
