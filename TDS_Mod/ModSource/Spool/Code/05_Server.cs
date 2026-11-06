// Host-only rules. Everything that changes the game state happens here or in the brains, and reaches other players
// through the engine's own networking (spawns, stats, upgrades, commands, Vis RPCs).
//
// Thread lives per player on the host, in two places: the Yarn Stash (what the Cookie Tins and PATCH spend) and carried
// (Spool and each Thread Head). Ground Thread is not a networked entity (the sheet: "make sure it doesn't lag the
// game"): the host keeps a list of drops, and clients draw a tuft from one Vis event and fly it away on another.
public class SpoolPlayer
{
    public long Owner;
    public int Stash, Carried;
    public bool Placed;          // the map's first Stash got its starting Thread
    // Direction ranks the vessel holds: its brain refreshes them, they stay while it is down.
    public int Recycler, Sewing, Needle, Buttons, Pattern, Merit, Socks;
    public SpoolHeroBrain Hero;
    public SpoolStashBrain Home;

    public int Cap { get { return Home != null ? Home.Cap : (int)SpoolData.StashCap; } }
    public int Room { get { return Home != null ? Mathf.Max(0, Cap - Stash) : 0; } }

    // Roomy Pockets: Spool carries more and picks Thread up from farther away.
    public int CarryCap { get { return Mathf.RoundToInt(Upg("Pockets", "Carry_Base", SpoolData.CarryHero)); } }
    public float Reach { get { return Upg("Pockets", "Pick_Base", SpoolData.PickHero); } }

    // This player's Yarn Stash while it stands and holds the upgrade, else null. Several of its upgrades land on Spool
    // (like Boutman's Bouthouse): they last as long as the Stash does, and go when it is sold or destroyed.
    public SpoolStashBrain Holds(string key) { return Home != null && Home.Has(key) ? Home : null; }

    // One number of a Stash upgrade while it is held, else 'fallback'.
    public float Upg(string key, string name, float fallback)
    {
        SpoolStashBrain s = Holds(key);
        return s != null ? s.Param(name, fallback) : fallback;
    }
}

public class SpoolDrop
{
    public int Id, Amount;
    public Vector3 Pos;
    public float Until;
}

public static class SpoolServer
{
    public static readonly List<SpoolHeroBrain> Heroes = new List<SpoolHeroBrain>();
    public static readonly List<SpoolBuilding> Buildings = new List<SpoolBuilding>();
    public static readonly List<SpoolUnitBrain> Units = new List<SpoolUnitBrain>();
    public static readonly List<SpoolDrop> Drops = new List<SpoolDrop>();
    static readonly Dictionary<long, SpoolPlayer> players = new Dictionary<long, SpoolPlayer>();
    static readonly List<Entity_Master> tokens = new List<Entity_Master>();
    static readonly List<Comeback> comebacks = new List<Comeback>();
    static Game_Entity hookedEntity;
    static bool subscribed;
    static int nextDrop = 1;
    static float nextSweep;
    public static int Dropped, Picked, Delivered, Spilled, Refunded, Swapped, Returned, StashRefused;

    // Set just before a brain spawns one of Spool's units; read by OnSpawn (Onspawn fires synchronously inside Spawn).
    public static SpoolBuilding PendingHome;
    public static Comeback PendingHead;

    // A Thread Head on its way back: where it guards, what it had.
    public class Comeback
    {
        public long Owner;
        public Vector3 Spot;
        public float At, Sellvalue = 1f;
        public bool Mending;
        public List<Upgradeproperties> Upgrades = new List<Upgradeproperties>();
    }

    public static void Update()
    {
        if (!subscribed)
        {
            subscribed = true;
            ModKitTides.Tide += OnTide;
        }
        Game_Entity ge = Game_Entity.Instance;
        if (ge != hookedEntity)
        {
            hookedEntity = ge;
            Heroes.Clear();
            Buildings.Clear();
            Units.Clear();
            Drops.Clear();
            players.Clear();
            tokens.Clear();
            comebacks.Clear();
            ModKitUtil.ForgetLooks(true);
            if (ge != null)
            {
                ge.Onspawn += OnSpawn;
                ge.Ondespawn += OnDespawn;
            }
        }
        if (ge == null || !ge.IsServer)
            return;
        Heroes.RemoveAll(h => h == null);
        Buildings.RemoveAll(b => b == null);
        Units.RemoveAll(u => u == null);
        try
        {
            if (tokens.Count > 0)
                SwapTokens();
            if (comebacks.Count > 0)
                Comebacks();
            if (Time.time >= nextSweep)
            {
                nextSweep = Time.time + 0.5f;
                Sweep();
            }
        }
        catch (Exception e)
        {
            SpoolLog.Warn("server: " + e.Message);
        }
    }

    // ---------------------------------------------------------------- players and Thread

    public static SpoolPlayer Player(long owner)
    {
        SpoolPlayer p;
        if (!players.TryGetValue(owner, out p))
        {
            p = new SpoolPlayer { Owner = owner };
            players[owner] = p;
        }
        return p;
    }

    public static IEnumerable<SpoolPlayer> Players { get { return players.Values; } }

    // Puts Thread in a player's Stash up to its cap; returns what didn't fit.
    public static int AddStash(long owner, int amount)
    {
        SpoolPlayer p = Player(owner);
        int put = Mathf.Clamp(amount, 0, p.Room);
        p.Stash += put;
        if (put != 0)
            Show(p);
        return amount - put;
    }

    public static bool SpendStash(long owner, int amount)
    {
        SpoolPlayer p = Player(owner);
        if (p.Home == null || p.Stash < amount)
            return false;
        p.Stash -= amount;
        Show(p);
        return true;
    }

    // The counters: Spool's Carried and Stash, the Yarn Stash's Thread.
    public static void Show(SpoolPlayer p)
    {
        if (p.Hero != null && p.Hero.Master != null)
        {
            ModKitCounter.Set(p.Hero.Master, "Carried", p.Carried);
            ModKitCounter.Set(p.Hero.Master, "Stash", p.Stash);
        }
        if (p.Home != null && p.Home.Master != null)
            ModKitCounter.Set(p.Home.Master, "Thread", p.Stash);
    }

    // A carrier at its Stash (or at a Golden Thread Loom) hands over what fits; the rest stays carried. The Thread goes
    // into the Stash either way; the look flies to the nearer of the two, or always to the Stash with 'straight' (Head of
    // the Class).
    public static int Deliver(long owner, Entity_Master carrier, int carried, bool straight = false)
    {
        SpoolPlayer p = Player(owner);
        if (p.Home == null || p.Home.Master == null || carried <= 0)
            return 0;
        int put = Mathf.Min(carried, p.Room);
        if (put <= 0)
            return 0;
        p.Stash += put;
        Delivered += put;
        Show(p);
        Vector3 to = ModKitUtil.Pos(p.Home.Master), from = carrier != null ? ModKitUtil.Pos(carrier) : to;
        SpoolLoomBrain golden = straight ? null : GoldenNear(owner, from, ModKitUtil.Flat(to, from));
        if (golden != null && ModKitUtil.Flat(golden.Pos, from) < ModKitUtil.Flat(to, from))
        {
            to = golden.Pos;
            GoldenDrops++;
            GoldenThread += put;
        }
        ModKitUtil.Vis(carrier, "Spool_Deliver", to, put);
        return put;
    }

    // Within 'reach' of the player's Yarn Stash, or of one of their Looms with Golden Thread. With no Stash standing there
    // is nowhere for the Thread to go, so a Golden Thread Loom is no drop-off point either.
    public static bool AtStash(long owner, Vector3 at, float reach)
    {
        SpoolPlayer p = Player(owner);
        if (p.Home == null || p.Home.Master == null)
            return false;
        return ModKitUtil.Flat(ModKitUtil.Pos(p.Home.Master), at) <= reach || GoldenNear(owner, at, reach) != null;
    }

    // ---------------------------------------------------------------- ground Thread

    // Any of Spool's entities carries the drop's Vis to every player (the foe that dropped it is already gone).
    static Entity_Master Radio()
    {
        foreach (SpoolHeroBrain h in Heroes)
            if (h != null && h.Master != null && h.Master.Radio != null)
                return h.Master;
        foreach (SpoolBuilding b in Buildings)
            if (b != null && b.Master != null && b.Master.Radio != null)
                return b.Master;
        return null;
    }

    // A tuft on the ground: it joins one within half a tile while the sum fits one tuft; larger amounts become several.
    // 'via': a dying building's own entity, whose messages still go out (Ondespawn runs before the game despawns it). With
    // no radio at all the Thread still lies there for a carrier, unseen, instead of vanishing (MAX SPL-10: a last Stash
    // falling while Spool was down spilled nothing). While any player's Stash holds the Remnant Bin, a new tuft and one
    // joined last the bin's longer life, and clients are told so (SpoolClient.LongTuft).
    public static void Drop(Vector3 at, int amount, Entity_Master via = null)
    {
        Entity_Master radio = Radio() ?? via;
        SpoolStashBrain bin = RemnantBin(at);
        float life = bin != null ? bin.Param("Droplife_Base", SpoolData.DropLife) : SpoolData.DropLife;
        int lasting = bin != null ? SpoolClient.LongTuft : 0;
        int left = amount, n = 0;
        while (left > 0)
        {
            Vector3 p = n == 0 ? at : at + new Vector3(Mathf.Cos(n * 2.1f), 0f, Mathf.Sin(n * 2.1f)) * 0.35f;
            SpoolDrop d = null;
            foreach (SpoolDrop o in Drops)
                if (o.Amount > 0 && o.Amount < SpoolData.TuftMax && o.Until > Time.time && ModKitUtil.Flat(o.Pos, p) <= SpoolData.Merge)
                {
                    d = o;
                    break;
                }
            if (d == null)
            {
                d = new SpoolDrop { Id = nextDrop++, Pos = p };
                if (nextDrop >= 1 << 19) nextDrop = 1;
                Drops.Add(d);
            }
            int put = Mathf.Min(left, SpoolData.TuftMax - d.Amount);
            d.Amount += put;
            d.Until = Time.time + life;
            left -= put;
            n++;
            Dropped += put;
            ModKitUtil.Vis(radio, "Spool_Tuft", d.Pos, d.Id * 16 + d.Amount + lasting);   // nothing when there is no radio
        }
    }

    // Takes up to 'room' Thread from the drops within 'reach' of a carrier; each pickup flies to it on every screen.
    public static int Collect(Entity_Master carrier, float reach, int room)
    {
        if (room <= 0 || Drops.Count == 0)
            return 0;
        Vector3 at = ModKitUtil.Pos(carrier);
        int took = 0;
        foreach (SpoolDrop d in Drops)
        {
            if (took >= room)
                break;
            if (d.Amount <= 0 || d.Until <= Time.time || ModKitUtil.Flat(d.Pos, at) > reach)
                continue;
            int n = Mathf.Min(d.Amount, room - took);
            d.Amount -= n;
            took += n;
            ModKitUtil.Vis(carrier, "Spool_Take", d.Pos, d.Id * 16 + d.Amount);
        }
        if (took > 0)
        {
            Picked += took;
            Drops.RemoveAll(d => d.Amount <= 0);
        }
        return took;
    }

    // Takes the Thread a player's Looms hold within 'reach' of a carrier.
    public static int CollectLooms(long owner, Entity_Master carrier, float reach, int room)
    {
        int took = 0;
        Vector3 at = ModKitUtil.Pos(carrier);
        foreach (SpoolBuilding b in Buildings)
        {
            SpoolLoomBrain loom = b as SpoolLoomBrain;
            if (took >= room || loom == null || !loom.Started || loom.Owner != owner || loom.Held <= 0 || ModKitUtil.Flat(loom.Pos, at) > reach)
                continue;
            int n = loom.TakeHeld(room - took);
            if (n > 0)
            {
                took += n;
                ModKitUtil.Vis(carrier, "Spool_Take", loom.Pos, 0);
            }
        }
        return took;
    }

    public static int DropsNear(Vector3 at, float reach)
    {
        int n = 0;
        foreach (SpoolDrop d in Drops)
            if (d.Amount > 0 && d.Until > Time.time && ModKitUtil.Flat(d.Pos, at) <= reach)
                n += d.Amount;
        return n;
    }

    // A foe fell: Thread on the ground where it fell, while some player is Spool (landed this map, alive or not). High
    // Thread Count's extra comes on top of the cap.
    static void FoeFell(Entity_Master foe)
    {
        if (players.Count == 0)
            return;
        int n = ModKitUtil.BaseHealth(foe) >= SpoolData.LargeHealth ? SpoolData.DropLarge : SpoolData.DropSmall;
        if (Elite(foe))
            n += SpoolData.DropElite;
        Vector3 at = ModKitUtil.Pos(foe);
        int more = CountBonus(at);
        Counted += more;
        Drop(at, Mathf.Min(n, SpoolData.DropMax) + more);
    }

    // An elite for the drop: the game's Enemy_* modifiers (the kit's reading) and the pack's own foe modifiers, which the
    // mode writes into its Tide rows (docs/scope/08 "Earned by"; Dimitri's answer 12 of 2026-11-05, B6).
    public const string PackModifier = "RW_Modifier_";

    public static bool Elite(Entity_Master foe)
    {
        if (ModKitUtil.Elite(foe))
            return true;
        if (foe == null || foe.Upgradelistserver == null)
            return false;
        foreach (string key in foe.Upgradelistserver.Keys)
            if (key.StartsWith(PackModifier))
                return true;
        return false;
    }

    // ---------------------------------------------------------------- Directions (kit 1.7, docs/scope/11)

    // The highest rank of a Direction this vessel carries (0: none): the mode grants them on the vessel.
    public static int DirectionRank(Entity_Master hero, string name)
    {
        if (hero == null || hero.Upgradelistserver == null)
            return 0;
        for (int r = 3; r >= 2; r--)
            if (hero.Upgradelistserver.ContainsKey(ModKitDirections.Id("Spool", name + r)))
                return r;
        return hero.Upgradelistserver.ContainsKey(ModKitDirections.Id("Spool", name)) ? 1 : 0;
    }

    // Heals every hurt same-team unit and building within a radius once; 'except' (a minion being sold) and Cores are left out.
    public static void MendAround(Entity_Master from, Vector3 at, float radius, float heal, Entity_Master except)
    {
        if (from == null || from.Beingserver == null || heal <= 0f)
            return;
        int team = ModKitUtil.Team(from);
        foreach (Entity_Master e in ModKitUtil.Area(at, radius, ModKitUtil.Friends(from)))
        {
            if (e != except && ModKitUtil.Alive(e) && ModKitUtil.Team(e) == team && e.Taglistserver != null && !e.Taglistserver.Contains("Core"))
                Mend(from, e, heal);
        }
    }

    // One mend: nothing at full health; the game's own heal, so it counts as healing, and the mend look on every screen.
    static bool Mend(Entity_Master from, Entity_Master e, float heal)
    {
        if (ModKitUtil.Health(e) >= ModKitUtil.MaxHealth(e) - 0.01f)
            return false;
        Helper_Entitytools.Interactheal(from, e, new Dictionary<string, float> { { "Damage", heal }, { "Damagemulti", 1f } });
        ModKitUtil.Vis(from, "Spool_Mend", ModKitUtil.Pos(e), 0f);
        return true;
    }

    // Sewing Machine: each Tide every Loom of a player holding it mends around itself. Mending Circle: see MendingCircle.
    static void OnTide(int wave)
    {
        foreach (SpoolBuilding b in Buildings.ToArray())
        {
            SpoolLoomBrain loom = b as SpoolLoomBrain;
            if (loom == null || !loom.Started || loom.Master == null || !ModKitUtil.Alive(loom.Master) || !loom.Sewing)
                continue;
            SpoolData.Sewing s = SpoolData.Sewingmachines[loom.Player.Sewing - 1];
            MendAround(loom.Master, loom.Pos, s.Radius, s.Heal, null);
        }
        try { MendingCircle(); }
        catch (Exception e) { SpoolLog.Warn("Mending Circle: " + e.Message); }
    }

    // ---------------------------------------------------------------- spawns

    static void OnSpawn(Entity_Master master)
    {
        try
        {
            if (!ModKitUtil.IsServer || master == null || master.Beingserver == null || master.Beingserver.Being == null)
                return;
            string being = master.Beingserver.Being.Objectname;
            if (being == SpoolData.Hero)
            {
                master.gameObject.AddComponent<SpoolHeroBrain>().Master = master;
                return;
            }
            if (being == SpoolData.HeadToken)
            {
                tokens.Add(master);
                return;
            }
            SpoolData.Unit unit = SpoolData.UnitFor(being);
            if (unit != null)
            {
                SpoolUnitBrain brain = master.gameObject.AddComponent<SpoolUnitBrain>();
                brain.Master = master;
                brain.Def = unit;
                brain.Home = PendingHome;
                PendingHome = null;
                Units.Add(brain);
                return;
            }
            SpoolData.Build b = SpoolData.BuildFor(being);
            if (b == null)
                return;
            SpoolBuilding building;
            if (b.Id == SpoolData.Stash) building = master.gameObject.AddComponent<SpoolStashBrain>();
            else if (b.Id == SpoolData.Tin) building = master.gameObject.AddComponent<SpoolTinBrain>();
            else if (b.Id == SpoolData.Cushion) building = master.gameObject.AddComponent<SpoolCushionBrain>();
            else if (b.Id == SpoolData.Loom) building = master.gameObject.AddComponent<SpoolLoomBrain>();
            else
            {
                SpoolHeadBrain head = master.gameObject.AddComponent<SpoolHeadBrain>();
                head.Back = PendingHead;
                PendingHead = null;
                building = head;
            }
            building.Master = master;
            building.Def = b;
            Buildings.Add(building);
            // BATCH 08:64 (Limit: 1): the Stash button goes the moment a Stash spawns, not at the next menu check.
            if (building is SpoolStashBrain)
                StashLimit(ModKitUtil.Owner(master), true);
        }
        catch (Exception e)
        {
            SpoolLog.Warn("spawn hook: " + e.Message);
        }
    }

    static void OnDespawn(Entity_Master master, bool killed, string reason)
    {
        try
        {
            if (!ModKitUtil.IsServer || master == null)
                return;
            SpoolBuilding building = master.GetComponent<SpoolBuilding>();
            if (building != null)
            {
                Buildings.Remove(building);
                building.Gone(killed, reason);
                // BATCH 08:64: and comes back the moment the last one goes (sold or destroyed).
                if (building is SpoolStashBrain)
                    StashLimit(ModKitUtil.Owner(master));
                return;
            }
            SpoolUnitBrain unit = master.GetComponent<SpoolUnitBrain>();
            if (unit != null)
            {
                Units.Remove(unit);
                unit.Gone(killed, reason);
                return;
            }
            SpoolHeroBrain hero = master.GetComponent<SpoolHeroBrain>();
            if (hero != null)
            {
                Heroes.Remove(hero);
                if (killed)
                    ModKitUtil.Vis(master, "Spool_Herodeath", ModKitUtil.Pos(master), 0);
                return;
            }
            if (killed && master.Taglistserver != null && master.Taglistserver.Contains("Enemy"))
            {
                FoeFell(master);
                // Merit Badge: the Crafted that landed the last hit drowned it.
                SpoolUnitBrain by = master.Lasthit != null ? master.Lasthit.GetComponent<SpoolUnitBrain>() : null;
                if (by != null)
                    by.Drowned();
            }
        }
        catch (Exception e)
        {
            SpoolLog.Warn("despawn hook: " + e.Message);
        }
    }

    // ---------------------------------------------------------------- the Thread Head

    // The build menu placed a token on a walkable tile; the Thread Head takes its place, paid for and owned the same.
    static void SwapTokens()
    {
        foreach (Entity_Master token in tokens.ToArray())
        {
            if (token == null || token.Beingserver == null)
                continue;
            if (token.Beingserver.Currentblock == null || token.Statsserver == null)
                return;   // not settled yet: next frame
            Genitem block = token.Beingserver.Currentblock;
            long owner = ModKitUtil.Owner(token);
            Comeback c = new Comeback { Owner = owner, Spot = block.Position, Sellvalue = token.Entitydata.Value.Sellvalue };
            SpoolHeroBrain hero = HeroOf(owner);
            Link_Being parent = hero != null && hero.Master != null && ModKitUtil.Alive(hero.Master) ? hero.Master.Beingserver : token.Beingserver;
            bool ok = SpawnHead(c, block, parent);
            Helper_Entitytools.Despawn(token.Beingserver, false, "Place");
            if (ok) Swapped++;
            else SpoolLog.Warn("the Thread Head could not take its token's place");
        }
        tokens.Clear();
    }

    static bool SpawnHead(Comeback c, Genitem block, Link_Being parent)
    {
        Beingobject being;
        if (!SpoolContent.Beings.TryGetValue(SpoolData.Head, out being) || block == null || parent == null)
            return false;
        Link_Spawn s = Helper_Entitytools.Createlinkspawnparent(parent);
        s.Genadd = block;
        s.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        s.Being = being;
        s.Sellvalue = c.Sellvalue;
        s.Destroyonparent = false;
        s.Preupgrades = new List<Upgradeproperties>(c.Upgrades);
        PendingHead = c;
        Link_Being b = Helper_Entitytools.Spawn(s, block.Position, parent, true, null);
        PendingHead = null;
        return b != null && b.Worldobject != null;
    }

    // A fallen Thread Head comes back at its owner's Yarn Stash with the upgrades it had, then walks to its spot.
    public static void HeadFell(Comeback c) { comebacks.Add(c); }

    static void Comebacks()
    {
        for (int i = comebacks.Count - 1; i >= 0; i--)
        {
            Comeback c = comebacks[i];
            if (Time.time < c.At)
                continue;
            SpoolPlayer p = Player(c.Owner);
            if (p.Home == null || p.Home.Master == null || p.Home.Master.Beingserver == null)
            {
                c.At = Time.time + 1f;   // no Stash: wait for one
                continue;
            }
            comebacks.RemoveAt(i);
            if (SpawnHead(c, p.Home.Master.Beingserver.Currentblock, p.Home.Master.Beingserver))
                Returned++;
        }
    }

    public static int WaitingHeads(long owner)
    {
        int n = 0;
        foreach (Comeback c in comebacks)
            if (c.Owner == owner) n++;
        return n;
    }

    // ---------------------------------------------------------------- lookups

    public static SpoolHeroBrain HeroOf(long owner)
    {
        foreach (SpoolHeroBrain h in Heroes)
            if (h != null && h.Master != null && ModKitUtil.Owner(h.Master) == owner)
                return h;
        return null;
    }

    // BATCH 08:64 (scope 08 "Limit: 1"): one Yarn Stash a player, the Miner's Base and Bonsai way (Boutman's
    // Checkbouthouses): the vessel's Stash button goes while one stands and comes back when it is gone, refreshed on the
    // spot as one spawns or despawns. The first one spawned is the player's, counted from its spawn, before it starts.
    public static SpoolStashBrain FirstStash(long owner)
    {
        foreach (SpoolBuilding b in Buildings)
        {
            SpoolStashBrain s = b as SpoolStashBrain;
            if (s != null && !s.Refused && ModKitUtil.Alive(s.Master) && s.Owner == owner)
                return s;
        }
        return null;
    }

    public static bool HasStash(long owner) { return FirstStash(owner) != null; }

    public static void StashLimit(long owner, bool spawned = false)
    {
        foreach (SpoolHeroBrain h in Heroes)
            if (h != null && h.Master != null && h.Player != null && ModKitUtil.Owner(h.Master) == owner)
                h.CheckMenu(spawned);
    }

    public static List<T> Owned<T>(long owner) where T : SpoolBuilding
    {
        List<T> list = new List<T>();
        foreach (SpoolBuilding b in Buildings)
            if (b is T && b.Started && b.Master != null && b.Owner == owner)
                list.Add((T)b);
        return list;
    }

    // Odd Socks: a Crafted of another kind on the same team within 'reach' of this one.
    public static bool OtherKindNear(SpoolUnitBrain u, float reach)
    {
        Vector3 at = ModKitUtil.Pos(u.Master);
        int team = ModKitUtil.Team(u.Master);
        // The cheap tests first: most units are the same kind, or not Crafted at all. A Tacked Stitched counts as a Stitched.
        string family = SpoolData.FamilyOf(u.Def);
        foreach (SpoolUnitBrain o in Units)
            if (o != null && o.Def != null && o.Def.Sellable && SpoolData.FamilyOf(o.Def) != family && o.Master != null && ModKitUtil.Flat(ModKitUtil.Pos(o.Master), at) <= reach
                && ModKitUtil.Team(o.Master) == team && ModKitUtil.Alive(o.Master))
                return true;
        return false;
    }

    public static int UnitsOf(SpoolBuilding home)
    {
        int n = 0;
        foreach (SpoolUnitBrain u in Units)
            if (u != null && u.Master != null && u.Home == home)
                n++;
        return n;
    }

    // ---------------------------------------------------------------- the Yarn Stash's upgrades

    public static int Remnants, Circled;   // Thread the Remnant Bin brought home; buildings the Mending Circle healed

    // Ground Thread that ran out goes, every half second (the self-test calls it to time a check); the Remnant Bin brings
    // part of it home.
    public static void Sweep()
    {
        for (int i = Drops.Count - 1; i >= 0; i--)
        {
            SpoolDrop d = Drops[i];
            if (d.Amount > 0 && d.Until > Time.time)
                continue;
            Drops.RemoveAt(i);
            if (d.Amount > 0)
                Remnant(d);
        }
    }

    // The Stash nearest to 'at' of a player whose Stash holds the Remnant Bin; null while nobody's does.
    static SpoolStashBrain RemnantBin(Vector3 at)
    {
        SpoolStashBrain best = null;
        float bestD = float.MaxValue;
        foreach (SpoolPlayer p in players.Values)
        {
            SpoolStashBrain s = p.Holds("Remnant");
            if (s == null || !ModKitUtil.Alive(s.Master))
                continue;
            float d = ModKitUtil.Flat(s.Pos, at);
            if (d < bestD)
            {
                best = s;
                bestD = d;
            }
        }
        return best;
    }

    // Remnant Bin: a tuft that ran out gives its share, rounded down, to the nearest bin's Stash (what doesn't fit is
    // lost), and flies there on every screen.
    static void Remnant(SpoolDrop d)
    {
        SpoolStashBrain bin = RemnantBin(d.Pos);
        int share = bin != null ? Mathf.FloorToInt(d.Amount * bin.Param("Remnant_Base", 0.5f)) : 0;
        if (share <= 0)
            return;
        int given = share - AddStash(bin.Owner, share);
        if (given <= 0)
            return;   // a full Stash: the tuft fades where it lay, on its own clock
        Remnants += given;
        ModKitUtil.Vis(bin.Master, "Spool_Remnant", d.Pos, d.Id * 16 + given);
    }

    // Mending Circle: as each Tide starts, every building of a player whose Stash holds it heals a share of its maximum
    // health (its Spool buildings; Thread Heads are units).
    static void MendingCircle()
    {
        foreach (SpoolPlayer p in players.Values)
        {
            SpoolStashBrain circle = p.Holds("Circle");
            if (circle == null || !ModKitUtil.Alive(circle.Master))
                continue;
            float share = circle.Param("Circle_Base", 0.25f);
            foreach (SpoolBuilding b in Owned<SpoolBuilding>(p.Owner))
                if (!(b is SpoolHeadBrain) && ModKitUtil.Alive(b.Master) && Mend(circle.Master, b.Master, ModKitUtil.MaxHealth(b.Master) * share))
                    Circled++;
        }
    }

    // ---------------------------------------------------------------- Thread Head upgrades

    public static int Counted;   // High Thread Count's extra Thread dropped

    // The best damage bonus and the best guard (less damage taken) any of this owner's Thread Heads gives at a point:
    // Needle Point and Stitch in Time within their range, Head of the Class both within its own, whatever path the Head
    // took. Several Heads don't stack.
    public static void HeadBuffs(long owner, Vector3 at, out float damage, out float guard)
    {
        damage = 0f;
        guard = 0f;
        foreach (SpoolBuilding b in Buildings)
        {
            SpoolHeadBrain h = b as SpoolHeadBrain;
            if (h == null || !h.Started || h.Master == null || h.Owner != owner || !ModKitUtil.Alive(h.Master))
                continue;
            float d = ModKitUtil.Flat(h.Pos, at);
            if (h.Has("Class") && d <= h.Param("Classrange_Base", 4f))
            {
                damage = Mathf.Max(damage, h.Param("Classbuff_Base", 0.5f));
                guard = Mathf.Max(guard, h.Param("Classguard_Base", 0.45f));
            }
            if (h.Has("Needle") && d <= h.Param("Needlerange_Base", 3f))
                damage = Mathf.Max(damage, h.Param("Needle_Base", 0.25f));
            if (h.Has("Time") && d <= h.Param("Stitchrange_Base", 3f))
                guard = Mathf.Max(guard, h.Param("Stitchtime_Base", 0.3f));
        }
    }

    // High Thread Count: the extra Thread a foe felled here drops, once however many Heads with it stand in reach.
    public static int CountBonus(Vector3 at)
    {
        int more = 0;
        foreach (SpoolBuilding b in Buildings)
        {
            SpoolHeadBrain h = b as SpoolHeadBrain;
            if (h != null && h.Started && h.Has("Count") && ModKitUtil.Alive(h.Master) && ModKitUtil.Flat(h.Pos, at) <= h.Param("Countrange_Base", 3f))
                more = Mathf.Max(more, Mathf.RoundToInt(h.Param("Count_Base", 1f)));
        }
        return more;
    }

    // ---------------------------------------------------------------- Golden Thread

    public static int GoldenDrops, GoldenThread;   // drop-offs at a Golden Thread Loom, and the Thread they handed over

    // The player's nearest standing Loom with Golden Thread within 'reach' of a point, or null.
    public static SpoolLoomBrain GoldenNear(long owner, Vector3 at, float reach)
    {
        SpoolLoomBrain best = null;
        float nearest = reach;
        foreach (SpoolBuilding b in Buildings)
        {
            SpoolLoomBrain loom = b as SpoolLoomBrain;
            if (loom == null || !loom.Started || loom.Owner != owner || !loom.Golden || !ModKitUtil.Alive(loom.Master))
                continue;
            float d = ModKitUtil.Flat(loom.Pos, at);
            if (d <= nearest)
            {
                best = loom;
                nearest = d;
            }
        }
        return best;
    }
}
