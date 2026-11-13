// Claude's Trinkets and Cardinals (docs/scope/12 [Cardinal and Trinket List], built 2026-11-05; compiled, not yet run in game).
// Their sheets are RWCardinals.Mine (17_Cardinals.cs): scriptless upgrades, on the buyer's vessel's panel or on the Basin and
// in the Cardinal row. This does what each says, host only, in a campaign map, from the stacks each player holds
// (RWCardinals.Held) and the run holds (RWCardinals.Stacked), read four times a second. Hits this adds carry RWAny.Extra.
//   Trinkets, one player's
//   Brass Spyglass  the vessel: Range Add +0.5 tile if it shoots (it has a Projspeed), +0.25 if it fights in melee
//   Gutting Knife   the vessel's own hits on a foe above half its health: Damagemulti x (1 + 20% a stack)
//   Oil Can         the holder's towers: Attackspeed Multi +5% a stack
//   Tar Bucket      the holder's towers: 1% of their health a second a stack, once 4 s pass with no hit
//   Rubber Duck     the vessel's death: the Basin's Respawn Multi -20% a stack (at most -60%) for that death alone, on before
//                   the Basin's own handler reads it (first on the vessel's Despawn) and off after (last; kit docs/17 §2.4)
//   Spare Plank     the vessel's (Defense) build button costs nothing while no plank of theirs stands; the first wall built
//                   at no price is the plank (it sells for nothing, as a free build does); when it falls or is sold the button
//                   is free again, a new map starts it again. The Basin's Processor Jr button is the team's and the HUD has four
//                   build slots, so the free build is the vessel's own button, under Onetimebuild's rule (kit docs/15 §18.1).
//                   Never offered to a vessel with no wall (Buster; Argo, whose Drill Rig is no wall)
//   Punch Card      RWCardinals.PriceFor: Cardinals and Trinkets 20% off, rounded down; pairs with Pearl Oyster
//   Pearl Oyster    each Tide's pay: +1 Pure Aura while the holder has bought nothing at VENDCINT on this map since holding it
//   Bottled Storm   each stack is one storm of 5 Tides, one storm at a time: the vessel takes 25% more damage; as it passes, a
//                   random Direction below rank III goes up a rank, or Pure Aura 20 if none can (RWPlayer.StormLeft, StormsDone)
//   Oilskin Coat    a hit on the vessel does nothing, at 1 - 0.9^stacks (10%, 19%, 27%...)
//   Gull Feather    an elite (a foe with a stock elite's or the pack's modifier) the vessel drowns: +20% Damage and +15%
//                   Speed Multi for 8 s, each later elite 8 s more
//   Cardinals, everyone's
//   Cork Lifebuoy   the Basin's hit hook, just after the game's own last stand (Entity_Ai_Main_Core): a second killing blow
//                   leaves it at 1 health, once a map
//   Signal Flare    every 20 s, as soon as a foe is within 8 tiles of the Basin: the Aura Gavel's bolt at the one with the most
//                   health, 15 damage a stack (a straight hit if the bolt isn't loaded)
//   Cargo Crate     RWCardinals.ShelfFor: one more card on the shelf each break a stack (6 stacks, 9 cards)
//   Tin Whistle     once all of a Tide's foes have come (its table row's count) and 3 or fewer stand: each of them -30%
//                   Speed and +25% damage taken, until it drowns
//   Stock Trinkets (RWCardinals.StockTrinketIds; the rest go on the buyer's things through RWCardinals.Grant and Reach)
//   the four Coins  the Coin's own numbers paid to the holder alone: its start sum once a map (on the map's first look with
//                   every vessel up, or at once when bought mid-map), then each break its Tide sum, Add x Tide x (30 /
//                   Tides, a whole-number factor) as Entity_Ai_Artifact_Coin, split by the players as the game splits it (Dimitri
//                   2026-11-05: "Pay only share")
//   Badge, Bendigo, Kippy, Fish Bait  the unit their spawner (on the holder's vessel, RWCardinals.GrantStock) makes is the
//                   holder's, one at a time: a vessel that drowns and comes back starts its copied spawner again, and the
//                   unit the drowned vessel made goes once the new one stands (Companions)
//   Aura Gavel      one of the holder's own units (a Mob, the vessel too) drowns: the Gavel's bolt flies from it at the
//                   nearest foe within the Gavel's reach, damage by the unit's level (Damage_Base, Damage2_Base..Damage5_Base)
// No look of their own yet, as the Directions (pitfall #114).
public static partial class RWTrinkets
{
    public const float SpyglassRanged = 0.5f, SpyglassMelee = 0.25f, KnifeEach = 0.2f, KnifeAbove = 0.5f, OilcanEach = 0.05f;
    public const float TarEach = 0.01f, TarAfter = 4f, DuckEach = 0.2f, DuckMost = 0.6f, PunchOff = 0.2f;
    public const int OysterPay = 1, StormPure = 20;   // StormTides: 15_Limits.cs
    public const float StormTaken = 0.25f, OilskinEach = 0.1f, GullDamage = 0.2f, GullSpeed = 0.15f, GullLasts = 8f;
    public const float FlareEvery = 20f, FlareDamage = 15f, FlareReach = 8f;
    public const int CrateMost = 6, WhistleLast = 3;
    public const float WhistleSlow = 0.3f, WhistleTaken = 0.25f;
    public const string FlareBolt = "Artifact_Auragavel";

    // Each vessel's (Defense) building (docs/scope/07-10, 16). Buster has none; Argo's Drill Rig is Economy/Defense and his
    // own tooltip says "no walls of its own".
    static readonly string[] WallHeroes = { "Hero_Smacky", "Hero_Snosno", "Hero_Boutman", "Hero_Fie", "Hero_Yossele", "Hero_Spool", "Hero_Uloe" };
    static readonly string[] WallBeings = { "Tower_Smackywall", "Tower_Snosnowall", "Tower_Boutmanbarrel", "Tower_Fiewall", "Tower_Yosselehenge", "Tower_Spoolcushion", "Tower_Uloemirror" };
    // The game's six elites (kit docs/13 §4), and Aegis as 21_Difficulty lists it, if a map loads one; the pack's go by RWFData's prefix.
    static readonly string[] StockElites = { "Enemy_Health_Upgradeobject", "Enemy_Red_Upgradeobject", "Enemy_Hyper_Upgradeobject", "Enemy_Chaos_Upgradeobject", "Enemy_Armor_Upgradeobject", "Enemy_Explode_Upgradeobject", "Enemy_Aegis_Upgradeobject" };
    const string PackModifier = "RW_Modifier_";

    public static string WallOf(string hero)
    {
        int i = Array.IndexOf(WallHeroes, hero ?? "");
        return i >= 0 ? WallBeings[i] : "";
    }

    // One player's stacks and what their Trinkets keep on this map.
    public sealed class Kit
    {
        public RWPlayer Player;
        public Entity_Master Vessel;
        public int Spyglass, Knife, Oilcan, Tar, Duck, Plank, Oilskin, Gull;
        public bool Gavel;
        public readonly List<string> Coins = new List<string>();     // the stock Coins held
        public readonly List<string> Units = new List<string>();     // the companion Trinkets held
        public float GullUntil;
        public Entity_Master Standing;                        // the plank, while it stands
        public Link_Command Button;                           // the vessel's wall button, held at no price while the plank is free
        public float Kept = -1f;                              // the price the game (or a Direction's cut) wrote there; -1: not held
        public readonly List<Entity_Master> Built = new List<Entity_Master>();   // walls of theirs that came while it was held
    }

    sealed class Duck { public Entity_Master Vessel; public Entity_Master.Despawns First, Last; public Link_Modifier Mod; public float Cut; }

    static readonly Dictionary<long, Kit> kits = new Dictionary<long, Kit>();
    static readonly Dictionary<Entity_Master, Duck> ducks = new Dictionary<Entity_Master, Duck>();
    static readonly Dictionary<Entity_Master, RWUtil.Rest> tarred = new Dictionary<Entity_Master, RWUtil.Rest>();
    static readonly Dictionary<Entity_Master, Link_Modifier> whistled = new Dictionary<Entity_Master, Link_Modifier>();
    static readonly List<Entity_Master> corked = new List<Entity_Master>();
    static readonly List<string> coined = new List<string>();      // "<session>|<Coin id>": its start sum is paid on this map
    static readonly Dictionary<string, float> extra = new Dictionary<string, float> { { RWAny.Extra, 1f } };
    static readonly Entity_Master.Interacts dealt = OnDealt;
    static readonly Entity_Master.Oninteracts taken = OnTaken, basin = OnBasin, whistle = OnWhistled, tarHit = OnTarHit;
    static float nextKits, nextHooks, nextSweep, nextTar, nextWhistle, flareReady;
    static int spawned, totalWave = -1, total;
    static readonly List<string> rowFoes = new List<string>();   // the beings the Tide's row sends
    public static bool ForceSlide, ForceLast;       // self-test: every hit slides off; a Tide's foes have all come
    public static int Slid, Ducked, Planked, Storms, Feathered, Corked, Flares, Gavels, CompanionsCut;
    public static float TarMended, Flared, CoinPaid;

    // A map starts (RWVote.MapStart): the last map's things go, each map's once-a-map uses and planks come back.
    public static void Reset()
    {
        kits.Clear();
        ducks.Clear();
        tarred.Clear();
        whistled.Clear();
        corked.Clear();
        coined.Clear();
        totalWave = -1;
        spawned = total = 0;
        rowFoes.Clear();
        flareReady = 0f;
    }

    static void Refresh(Kit k, RWPlayer p)
    {
        k.Player = p;
        k.Spyglass = RWCardinals.Held(p, "Spyglass"); k.Knife = RWCardinals.Held(p, "Knife"); k.Oilcan = RWCardinals.Held(p, "Oilcan");
        k.Tar = RWCardinals.Held(p, "Tar"); k.Duck = RWCardinals.Held(p, "Duck"); k.Plank = RWCardinals.Held(p, "Plank");
        k.Oilskin = RWCardinals.Held(p, "Oilskin"); k.Gull = RWCardinals.Held(p, "Gull");
        k.Gavel = RWCardinals.StockHeld(p, "Gavel").Count > 0;
        k.Coins.Clear();
        k.Coins.AddRange(RWCardinals.StockHeld(p, "Coin"));
        k.Units.Clear();
        k.Units.AddRange(RWCardinals.StockHeld(p, "Unit"));
    }

    // ---------------------------------------------------------------- every frame (host)

    public static void Update()
    {
        if (!RWRun.InCampaignMap || !ModKitUtil.IsServer || Game_Entity.Instance == null)
            return;
        float now = Time.time;
        if (now >= nextKits)
        {
            nextKits = now + 0.25f;
            foreach (long s in RWRun.Sessions())
            {
                Kit k;
                if (!kits.TryGetValue(s, out k))
                    kits[s] = k = new Kit();
                Refresh(k, RWRun.For(s));
                k.Vessel = RWRun.Vessel(s);
                CoinStart(s, k);
                Companions(s, k);
            }
        }
        if (now >= nextHooks)
        {
            nextHooks = now + 0.1f;
            foreach (Kit k in kits.Values)
                Hooks(k, now);
            Basins();
            foreach (Entity_Master foe in whistled.Keys)
                if (ModKitUtil.Alive(foe)) ModKitUtil.Front(foe, whistle);
        }
        foreach (Kit k in kits.Values)
            if (k.Plank > 0) Plank(k);
        if (now >= nextSweep)
        {
            nextSweep = now + 0.5f;
            Sweep();
        }
        if (now >= nextTar)
        {
            nextTar = now + 0.25f;
            Tar(now);
        }
        if (now >= nextWhistle)
        {
            nextWhistle = now + 0.25f;
            Whistle();
        }
        Flare(now);
    }

    // The hit hooks and Rubber Duck's go on again ten times a second (an upgrade landing rebuilds the chains), and Gull
    // Feather's lift follows its clock.
    static void Hooks(Kit k, float now)
    {
        Entity_Master v = k.Vessel;
        if (!ModKitUtil.Alive(v))
            return;
        if (k.Knife > 0)
            ModKitUtil.Listen(v, dealt);
        if (k.Oilskin > 0 || k.Player.StormLeft > 0)
            RWAny.AfterFronts(v, taken, RWAny.ClamHook);
        if (k.Duck > 0)
            Wrap(k);
        if (k.Gull > 0 || RWHold.Get(v, "Gull") > 0f)
        {
            bool lifted = k.Gull > 0 && now < k.GullUntil;
            RWHold.Set(v, "Gull", "Damage", Helper_Entitytools.Modtypes.Multi, lifted ? GullDamage : 0f);
            RWHold.Set(v, "GullSpeed", "Speed", Helper_Entitytools.Modtypes.Multi, lifted ? GullSpeed : 0f);
        }
    }

    // What is held while standing: the vessel's reach, and the holder's towers' speed (made again on a rebuilt tower).
    static void Sweep()
    {
        bool towers = false;
        foreach (Kit k in kits.Values)
        {
            Entity_Master v = k.Vessel;
            if (k.Spyglass > 0 && ModKitUtil.Alive(v))
                RWHold.Set(v, "Spyglass", "Range", Helper_Entitytools.Modtypes.Add, Ranged(v) ? SpyglassRanged : SpyglassMelee);
            if (k.Oilcan > 0) towers = true;
        }
        if (!towers)
            return;
        foreach (Entity_Master e in RWAny.Owned("Tower"))
        {
            Kit k;
            if (kits.TryGetValue(ModKitUtil.Owner(e), out k) && k.Oilcan > 0)
                RWHold.Set(e, "Oilcan", "Attackspeed", Helper_Entitytools.Modtypes.Multi, OilcanEach * k.Oilcan);
        }
    }

    // A vessel that shoots carries a Projspeed (Sno Sno, Yossele, Uloe); Boutman's fishing reach is his own (kit docs/17 §6).
    public static bool Ranged(Entity_Master v) { return v != null && v.Statsserver != null && v.Statsserver.ContainsKey("Projspeed"); }

    // ---------------------------------------------------------------- Tar Bucket

    // A hit is Mason's Trowel's (RWUtil.Hit, the hook put back here four times a second), or a drop in health while an
    // upgrade landing has the hook off; once 4 s pass with neither, the tower mends each second.
    static void Tar(float now)
    {
        bool any = false;
        foreach (Kit k in kits.Values) if (k.Tar > 0) any = true;
        if (!any)
            return;
        foreach (Entity_Master e in RWAny.Owned("Tower"))
        {
            Kit k;
            if (!kits.TryGetValue(ModKitUtil.Owner(e), out k) || k.Tar <= 0)
                continue;
            RWUtil.Rest t;
            if (!tarred.TryGetValue(e, out t))
                tarred[e] = t = new RWUtil.Rest { Health = ModKitUtil.Health(e), Hurt = now - TarAfter, Next = now };
            ModKitUtil.Front(e, tarHit);
            TarMended += RWUtil.Mend(t, e, now, TarAfter, ModKitUtil.MaxHealth(e) * TarEach * k.Tar, false);
        }
        if (tarred.Count > 256)
            foreach (Entity_Master e in new List<Entity_Master>(tarred.Keys))
                if (!ModKitUtil.Alive(e)) tarred.Remove(e);
    }

    // ---------------------------------------------------------------- the vessel's hits and the hits on it

    static void OnDealt(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.Data == null || hit.From == null || hit.To == null || RWAny.ExtraHit(hit.Data))
                return;
            Kit k;
            if (!kits.TryGetValue(ModKitUtil.Owner(hit.From), out k) || k.Vessel != hit.From || k.Knife <= 0 || !ModKitUtil.IsFoe(hit.To, ModKitUtil.Team(hit.From)))
                return;
            float multi, max = ModKitUtil.MaxHealth(hit.To);
            if (max <= 0f || ModKitUtil.Health(hit.To) <= KnifeAbove * max || !hit.Data.TryGetValue("Damagemulti", out multi))
                return;
            ModKitCombat.SetMulti(hit, multi * (1f + KnifeEach * k.Knife));
        }
        catch (Exception e) { RWLog.Once("trinket dealt", "a vessel hit: " + e.Message); }
    }

    static void OnTarHit(Link_Interact hit) { RWUtil.Hit(tarred, hit); }

    public static float SlideChance(int stacks) { return stacks > 0 ? 1f - Mathf.Pow(1f - OilskinEach, stacks) : 0f; }

    // After the other mods' fronts and before Clam Shell, which judges the blow this leaves.
    static void OnTaken(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.To == null || hit.Data == null)
                return;
            Kit k;
            if (!kits.TryGetValue(ModKitUtil.Owner(hit.To), out k) || k.Vessel != hit.To)
                return;
            float damage, multi;
            if (!hit.Data.TryGetValue("Damage", out damage) || !hit.Data.TryGetValue("Damagemulti", out multi) || damage <= 0f || multi <= 0f)
                return;
            float was = multi;
            if (k.Oilskin > 0 && (ForceSlide || UnityEngine.Random.value < SlideChance(k.Oilskin)))
            {
                multi = 0f;
                Slid++;
            }
            else if (k.Player.StormLeft > 0)
                multi *= 1f + StormTaken;
            if (!Mathf.Approximately(multi, was))
            {
                ModKitCombat.SetMulti(hit, multi);
            }
        }
        catch (Exception e) { RWLog.Once("trinket taken", "a hit on a vessel: " + e.Message); }
    }

    // ---------------------------------------------------------------- Rubber Duck

    // [First, the rest, Last] on the vessel's Despawn: the Basin's own handler, put on as the vessel spawned, reads its Respawn
    // stat in between (Entity_Ai_Main_Core.Respawn, before its first await).
    static void Wrap(Kit k)
    {
        Entity_Master v = k.Vessel;
        Duck d;
        if (!ducks.TryGetValue(v, out d))
        {
            if (ducks.Count >= 64)
                foreach (Entity_Master gone in new List<Entity_Master>(ducks.Keys))
                    if (!ModKitUtil.Alive(gone)) ducks.Remove(gone);
            d = new Duck { Vessel = v };
            Duck me = d;
            d.First = death => DuckFirst(me, death);
            d.Last = death => DuckLast(me);
            ducks[v] = d;
        }
        d.Cut = Mathf.Min(DuckMost, DuckEach * k.Duck);
        Delegate[] list = v.Despawn != null ? v.Despawn.GetInvocationList() : new Delegate[0];
        if (list.Length >= 2 && list[0].Equals(d.First) && list[list.Length - 1].Equals(d.Last))
            return;
        Delegate rest = Delegate.Remove(Delegate.Remove(v.Despawn, d.First), d.Last);
        v.Despawn = (Entity_Master.Despawns)Delegate.Combine(new Delegate[] { d.First, rest, d.Last });
    }

    static void DuckFirst(Duck d, bool death)
    {
        try
        {
            if (!death || d.Cut <= 0f)
                return;
            Entity_Master core = d.Vessel != null && ModKitUtil.HasTag(d.Vessel.Parent, "Core") ? d.Vessel.Parent : RWUtil.PlayerCore();
            if (!ModKitUtil.Alive(core) || core.Statsserver == null || !core.Statsserver.ContainsKey("Respawn"))
                return;
            d.Mod = Helper_Entitytools.Addmod(core, "Respawn", -d.Cut, Helper_Entitytools.Modtypes.Multi, 0f);
            Ducked++;
        }
        catch (Exception e) { RWLog.Once("duck", "Rubber Duck: " + e.Message); }
    }

    static void DuckLast(Duck d)
    {
        ModKitUtil.Unmod(d.Mod);
        d.Mod = null;
    }

    // ---------------------------------------------------------------- Spare Plank

    // Does Spare Plank hold this button at no price? RWDirections.Builds leaves it alone then (its cut comes back with Kept).
    public static bool Holds(Link_Command button)
    {
        if (button == null)
            return false;
        foreach (Kit k in kits.Values)
            if (ReferenceEquals(k.Button, button) && k.Kept > 0f) return true;
        return false;
    }

    // Every frame for a holder (a frame is the only window in which a second wall could land free): the button free while no
    // plank stands, its own price back once one does. Boutbro rewrites his prices on every recount (kit docs/17 §5.1): a price
    // he writes while it is held is kept and the button held again.
    static void Plank(Kit k)
    {
        Entity_Master v = k.Vessel;
        string wall = WallOf(k.Player.Hero);
        if (wall == "" || !ModKitUtil.Alive(v) || v.Commandlistserver == null)
            return;
        if (k.Standing != null && !ModKitUtil.Alive(k.Standing))
        {
            k.Standing = null;
            RWLog.Info(k.Player.Name + "'s Spare Plank fell: the next wall is free again");
        }
        // The plank stands with nothing held or waiting: nothing below would change (Holds reads the button only while held).
        if (k.Standing != null && k.Kept <= 0f && k.Built.Count == 0)
            return;
        string name = null;
        Link_Command button = null;
        foreach (KeyValuePair<string, Link_Command> c in v.Commandlistserver)
            if (c.Value != null && c.Key.StartsWith("Buildoption", StringComparison.Ordinal) && c.Value.Goalstring == wall)
            {
                name = c.Key;
                button = c.Value;
                break;
            }
        if (button == null)
            return;
        if (!ReferenceEquals(button, k.Button))
        {
            k.Button = button;      // a new vessel's own, at the game's price
            k.Kept = -1f;
        }
        // A wall that came while it was held, at no price, is the plank; one paid for (a price written before it was held again,
        // a Fie wall's own next segment) isn't.
        for (int i = k.Built.Count - 1; i >= 0; i--)
        {
            Entity_Master b = k.Built[i];
            k.Built.RemoveAt(i);
            if (k.Standing == null && ModKitUtil.Alive(b) && b.Entitydata.Value.Sellvalue < 0.001f)
            {
                k.Standing = b;
                Planked++;
                RWLog.Info(k.Player.Name + "'s Spare Plank: " + wall + " built at no price");
            }
        }
        if (k.Standing == null)
        {
            // Free already by the game's own rule (Boutman's first barrel): nothing held, so a build there isn't the plank.
            if (button.Goalvalue > 0.0001f)
            {
                k.Kept = button.Goalvalue;
                Write(v, name, button, 0f);
            }
        }
        else if (k.Kept > 0f)
        {
            if (button.Goalvalue < 0.0001f) Write(v, name, button, k.Kept);
            k.Kept = -1f;
        }
    }

    static void Write(Entity_Master v, string name, Link_Command c, float price)
    {
        c.Goalvalue = price;
        Helper_Entitytools.Updatecommand(v, name, "", false, false);
    }

    // ---------------------------------------------------------------- spawns, deaths, buys

    // Game_Entity.Onspawn (RWDirections.OnSpawn): a Tide's foes counted, a holder's wall noted for Spare Plank.
    public static void OnSpawn(Entity_Master m)
    {
        if (!ModKitUtil.IsServer || Game_Mapvars.Instance == null)
            return;
        List<string> tags = m.Beingserver.Entity.Tags;
        if (tags.Contains("Enemy") && !tags.Contains("Enemycore") && !tags.Contains("Core"))
        {
            // Only the row's own beings: a split, a summon or a nest's guard doesn't count towards the row.
            ReadRow(Game_Mapvars.Instance);
            if (rowFoes.Contains(ModKitUtil.BeingName(m))) spawned++;
            return;
        }
        Kit k;
        if (kits.TryGetValue(m.Entitydata.Value.Owner, out k) && k.Plank > 0 && k.Kept > 0f && k.Standing == null && ModKitUtil.BeingName(m) == WallOf(k.Player.Hero))
            k.Built.Add(m);
    }

    // Game_Entity.Ondespawn (RWCardinals.OnDespawn): an elite the vessel drowned (RWAny.DrownedBy, as Lamp Oil reads it).
    public static void OnDespawn(Entity_Master master, bool killed)
    {
        whistled.Remove(master);
        if (killed && master != null && RWRun.InCampaignMap && ModKitUtil.IsServer)
            Gavel(master);
        if (!killed || master == null || !RWRun.InCampaignMap || !ModKitUtil.IsServer || master.Taglistserver == null || !master.Taglistserver.Contains("Enemy"))
            return;
        RWAny.Crew c = RWAny.DrownedBy(master);
        Kit k;
        if (c == null || !kits.TryGetValue(ModKitUtil.Owner(c.Vessel), out k) || k.Vessel != c.Vessel || k.Gull <= 0 || !Elite(master))
            return;
        k.GullUntil = Mathf.Max(Time.time, k.GullUntil) + GullLasts;
        Feathered++;
    }

    public static bool Elite(Entity_Master foe)
    {
        if (foe == null || foe.Upgradelistserver == null)
            return false;
        foreach (string id in foe.Upgradelistserver.Keys)
            if (id.StartsWith(PackModifier, StringComparison.Ordinal) || Array.IndexOf(StockElites, id) >= 0) return true;
        return false;
    }

    // A buy at VENDCINT (RWCardinals.Buy, RWShop's Direction ranks): Pearl Oyster stops for this map, unless it is the Oyster
    // itself; a Bottled Storm starts if none blows.
    public static void OnBuy(RWPlayer p, string id)
    {
        if (p == null)
            return;
        if (id != "Cardinal_Oyster")
            p.Bought = true;
        OnGain(p, id);
    }

    // What a Trinket starts when a player gains it, bought or given (a Wreck's pay, RWCardinals.Give): Pearl Oyster pays
    // from the moment it is held, so a buy made before it no longer counts (a Wreck's Oyster after a buy paid nothing
    // until the next map); a Bottled Storm starts if none blows.
    public static void OnGain(RWPlayer p, string id)
    {
        if (p == null)
            return;
        if (id == "Cardinal_Oyster")
            p.Bought = false;
        else if (id == "Cardinal_Storm")
            StartStorm(p);
    }

    // Each break after a Tide (RWVote.Break, after the Tide's pay).
    public static void Tide(int wave)
    {
        if (wave <= 0)
            return;
        Game_Mapvars mv = Game_Mapvars.Instance;
        int tides = mv != null && mv.Mode != null && mv.Mode.Enemies != null ? mv.Mode.Enemies.Totalwaves : 0;
        int factor = tides > 0 ? 30 / tides : 0;     // the game's whole-number factor: nothing past 30 Tides
        foreach (long s in RWRun.Sessions())
        {
            RWPlayer p = RWRun.For(s);
            foreach (string coin in RWCardinals.StockHeld(p, "Coin"))
                Pay(s, coin, "_Add", wave * factor);    // Pay divides by the players
            if (RWCardinals.Held(p, "Oyster") > 0 && !p.Bought)
                p.Pure += OysterPay;
            if (p.StormLeft > 0 && --p.StormLeft == 0)
            {
                p.StormsDone++;
                Calm(p);
                StartStorm(p);
            }
        }
    }

    public static void StartStorm(RWPlayer p)
    {
        if (p.StormLeft > 0 || RWCardinals.Held(p, "Storm") <= p.StormsDone)
            return;
        p.StormLeft = StormTides;
        RWRejoin.Say(p.Name + "'s Bottled Storm breaks: " + StormTides + " Tides of " + Mathf.RoundToInt(StormTaken * 100f) + "% more damage taken.");
    }

    // The storm passed: a Direction below rank III one rank up, picked at random, or Pure Aura.
    static void Calm(RWPlayer p)
    {
        Storms++;
        List<Upgradeproperties> next = RWDirections.NextRanks(p.Directions);
        if (next.Count > 0)
        {
            Upgradeproperties u = next[UnityEngine.Random.Range(0, next.Count)];
            RWDirections.Take(p, u);
            RWDirections.Sweep(p);
            RWRejoin.Say(p.Name + "'s Bottled Storm passed: " + u.Display_Name + ".");
            return;
        }
        p.Pure += StormPure;
        RWRejoin.Say(p.Name + "'s Bottled Storm passed: Pure Aura " + StormPure + ".");
    }

    // ---------------------------------------------------------------- Cork Lifebuoy

    // Just after the Basin's own last stand (Entity_Ai_Main_Core.Oninteract, priority 9) and before the AI that takes the
    // health (Entity_Ai_Tower_General, 10). While the game's stand is unused it takes the blow first, so this sees none.
    static void Basins()
    {
        if (RWCardinals.Stacked("Cork") <= 0)
            return;
        foreach (Entity_Master core in RWUtil.PlayerCores())
            if (!corked.Contains(core))
                AfterStand(core);
    }

    static void AfterStand(Entity_Master m)
    {
        Delegate[] list = m.Oninteract != null ? m.Oninteract.GetInvocationList() : new Delegate[0];
        List<Delegate> want = new List<Delegate>(list.Length + 1);
        foreach (Delegate d in list)
        {
            if (d.Equals(basin))
                continue;
            want.Add(d);
            if (d.Target is Entity_Ai_Main_Core)
                want.Add(basin);
        }
        if (want.Contains(basin))     // else no stand of the game's to come after
            RWAny.SetChain(m, list, want);
    }

    static void OnBasin(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.To == null || hit.Data == null || corked.Contains(hit.To) || RWCardinals.Stacked("Cork") <= 0)
                return;
            float damage, multi;
            if (!hit.Data.TryGetValue("Damage", out damage) || !hit.Data.TryGetValue("Damagemulti", out multi) || damage * multi <= 0f || hit.To.Statsserver == null || !RWAny.Kills(hit.To, damage * multi))
                return;
            // As the game's own stand does it.
            ModKitCombat.SetMulti(hit, 0f);
            hit.Data["Damage"] = 0f;
            hit.To.Statsserver["Health"].Value = 1f;
            corked.Add(hit.To);
            Corked++;
            if (hit.To.Radio != null)
            {
                hit.To.Radio.Vfxoneshotcheck("World_Core_Oneshot", ModKitUtil.Pos(hit.To), Vector3.forward);
                hit.To.Radio.Uiwarning_ClientRpc("THE CORK LIFEBUOY HOLDS", "RogueWaves_Crest", 3f);
            }
            Helper_Entitytools.Editartifact("Cardinal_Cork", Time.fixedTime);
            RWLog.Info("Cork Lifebuoy: the Basin lived through a second killing blow");
        }
        catch (Exception e) { RWLog.Once("cork", "Cork Lifebuoy: " + e.Message); }
    }

    // ---------------------------------------------------------------- Signal Flare

    static void Flare(float now)
    {
        int stacks = RWCardinals.Stacked("Flare");
        if (stacks <= 0 || now < flareReady)
            return;
        Entity_Master core = RWUtil.PlayerCore();
        if (!ModKitUtil.Alive(core))
        {
            flareReady = now + 0.25f;   // no Basin yet (or none left): looked at as often as for a foe
            return;
        }
        Entity_Master best = null;
        float most = 0f;
        foreach (Entity_Master e in ModKitUtil.FoesNear(core, ModKitUtil.Pos(core), FlareReach))
        {
            float hp = ModKitUtil.Health(e);
            if (hp > most) { most = hp; best = e; }
        }
        if (best == null)
        {
            flareReady = now + 0.25f;   // ready, until a foe comes within reach
            return;
        }
        flareReady = now + FlareEvery;
        float damage = FlareDamage * stacks;
        Bolt(core, ModKitUtil.Pos(core), best, damage);
        Helper_Entitytools.Editartifact("Cardinal_Flare", Time.fixedTime);
        Flares++;
        Flared += damage;
    }

    // Aura Gavel's own call (Entity_Ai_Artifact_Gavel.Onitemdespawn): a homing bolt from a place at its Goal, sent by the core;
    // a straight hit if the bolt isn't loaded on this map.
    static void Bolt(Entity_Master core, Vector3 from, Entity_Master goal, float damage)
    {
        Dictionary<string, float> data = new Dictionary<string, float>(extra) { { "Damage", damage }, { "Damagemulti", 1f } };
        if (ModKitUtil.Load<Projectileobject>("Projectile", FlareBolt) != null)
        {
            Projdata p = new Projdata();
            p.Paramsobject = new Dictionary<string, Entity_Master> { { "Goal", goal } };
            p.Data = data;
            Helper_Damagetools.Projectile(core, FlareBolt, from + Vector3.up * 0.5f, p, "Hurt");
        }
        else
        {
            RWLog.Once("no gavel bolt", "the Aura Gavel's bolt isn't loaded on this map; Signal Flare and the Gavel strike straight");
            ModKitCombat.Hurt(core, goal, damage, 1f, null, extra);
        }
    }

    // ---------------------------------------------------------------- the stock Coins and Aura Gavel, as Trinkets

    // A Coin's start sum, once a map for each holder, once the Basin stands (the game's Coin pays as the core starts).
    static void CoinStart(long s, Kit k)
    {
        if (k.Coins.Count == 0 || !ModKitUtil.Alive(RWUtil.PlayerCore()) || Game_Players.Instance == null || !Game_Players.Instance.Prevsessionlist.ContainsKey(s))
            return;     // looked at again next time: the player's wallet may come after the Basin
        foreach (long other in RWRun.Sessions())
            if (!ModKitUtil.Alive(RWRun.Vessel(other)))
                return;     // every player in, so the share is split the right number of ways
        foreach (string coin in k.Coins)
        {
            string key = s + "|" + coin;
            if (coined.Contains(key))
                continue;
            coined.Add(key);
            Pay(s, coin, "_Base", 1f);
        }
    }

    // A Coin's "<aura>" + suffix numbers, times a factor, to one player: their share, the sum divided by the players as the
    // game's Coin splits it among the active players (with an owner Give doesn't split it itself, kit docs/15 §14.2).
    static void Pay(long s, string coin, string suffix, float times)
    {
        times /= Mathf.Max(1, RWRun.Sessions().Count);
        List<Link_Resourceset> pay = new List<Link_Resourceset>();
        foreach (string aura in ModKitUtil.Auras)       // "Redaura": the Coin's numbers are "Red" + suffix
        {
            float amount = RWCardinals.StockParam(coin, aura.Substring(0, aura.Length - 4) + suffix) * times;
            if (amount > 0f) pay.Add(new Link_Resourceset { Name = aura, Amount = amount });
        }
        if (pay.Count == 0 || Game_Players.Instance == null || !Game_Players.Instance.Prevsessionlist.ContainsKey(s))
            return;
        Helper_Resources.Giveresources(pay, s, Helper_Resources.Styles.Default);
        foreach (Link_Resourceset r in pay) CoinPaid += r.Amount;
    }

    // A companion holder's units of each kind, one at a time: a unit whose vessel (its spawner's holder, its parent) is gone
    // goes once the new vessel's spawner has made its own. A unit whose spawner lives is never taken, so a spawner that
    // replaces its unit (Torrid Badge, Fish Bait) never loops. Despawned here, never inside the spawn.
    static void Companions(long s, Kit k)
    {
        foreach (string id in k.Units)
        {
            float respawn;
            string being = RWCardinals.CompanionOf(id, out respawn);
            if (being == "")
                continue;
            List<Entity_Master> orphans = new List<Entity_Master>();
            bool fresh = false;
            foreach (Entity_Master e in RWAny.Owned("Mob"))
            {
                if (ModKitUtil.Owner(e) != s || ModKitUtil.BeingName(e) != being || RWRun.IsCopy(e))
                    continue;
                if (ModKitUtil.Alive(e.Parent)) fresh = true;
                else orphans.Add(e);
            }
            if (fresh)
                foreach (Entity_Master e in orphans) Despawn(e);
        }
    }

    static void Despawn(Entity_Master e)
    {
        if (!ModKitUtil.Alive(e))
            return;
        Helper_Entitytools.Despawn(e.Beingserver, false, "RogueWaves");
        CompanionsCut++;
    }

    // One of a Gavel holder's units drowned (not a copy, not a foe): its bolt at the nearest foe Mob or tower in reach.
    static void Gavel(Entity_Master dead)
    {
        Kit k;
        if (!kits.TryGetValue(ModKitUtil.Owner(dead), out k) || !k.Gavel || !ModKitUtil.HasTag(dead, "Mob") || ModKitUtil.HasTag(dead, "Enemy") || RWRun.IsCopy(dead))
            return;
        const string id = "Artifact_Gavel";
        Link_Stats lv;
        int level = Mathf.Clamp(Mathf.RoundToInt(dead.Statsserver != null && dead.Statsserver.TryGetValue("Level", out lv) ? lv.Value_Calc : 1f), 1, 5);
        float reach = RWCardinals.StockParam(id, "Range_Base"), damage = RWCardinals.StockParam(id, level == 1 ? "Damage_Base" : "Damage" + level + "_Base");
        Entity_Master core = RWUtil.PlayerCore();
        if (reach <= 0f || damage <= 0f || !ModKitUtil.Alive(core))
            return;
        Vector3 at = ModKitUtil.Pos(dead);
        foreach (Entity_Master foe in ModKitUtil.FoesNear(core, at, reach))
        {
            List<string> tags = foe.Beingserver != null && foe.Beingserver.Entity != null ? foe.Beingserver.Entity.Tags : null;
            if (tags == null || tags.Contains("Enemycore") || (!tags.Contains("Mob") && !tags.Contains("Tower")))
                continue;
            Bolt(core, at, foe, damage);
            Gavels++;
            return;
        }
    }

    // ---------------------------------------------------------------- Tin Whistle

    static readonly Link_Entityquery foes = new Link_Entityquery { Style = Link_Entityquery.Styles.Both, Tags = new List<string> { "Enemy" }, Team = 0, Teamfor = true };

    // Mode_Normal's own count of a Tide's foes (its Enemyquery): once every foe of the row has come, the last few are marked.
    static void Whistle()
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        if (RWCardinals.Stacked("Whistle") <= 0 || mv == null || (ModKitTides.InBreak && !ForceLast))
            return;
        ReadRow(mv);
        if (!ForceLast && (total <= 0 || spawned < total))
            return;
        Entity_Master[] all = Helper_Entityquerytools.Entityall(Vector3.zero, foes, false);
        List<Entity_Master> left = new List<Entity_Master>();
        if (all != null)
            foreach (Entity_Master e in all)
                if (ModKitUtil.Alive(e) && e.Taglistserver != null && !e.Taglistserver.Contains("Enemycore") && !e.Taglistserver.Contains("Core") && !e.Taglistserver.Contains("Resource"))
                    left.Add(e);
        if (left.Count == 0 || left.Count > WhistleLast)
            return;
        foreach (Entity_Master foe in left)
        {
            if (whistled.ContainsKey(foe))
                continue;
            whistled[foe] = ModKitUtil.SlowOn(foe, WhistleSlow);
            ModKitUtil.Front(foe, whistle);
        }
    }

    // Once a Tide: the foes the row playing now sends (Entry - 1, kit docs/15 §4), each Count of a content that spawns one,
    // and their beings. Mode_Normal raises Wave as the row starts, before it spawns (the pack's tables are written before).
    static void ReadRow(Game_Mapvars mv)
    {
        int wave = mv.Wave.Value;
        if (wave == totalWave)
            return;
        totalWave = wave;
        spawned = total = 0;
        rowFoes.Clear();
        List<Wavesobject.Waves> table = mv.Mode != null && mv.Mode.Enemies != null ? mv.Mode.Enemies.Wave : null;
        int row = mv.Entry.Value - 1;
        if (table == null || row < 0 || row >= table.Count || !table[row].Wave || table[row].Content == null)
            return;
        foreach (Wavesobject.Wavecontent c in table[row].Content)
            if (c.Enemy != null && c.Style != Wavesobject.Wavestyle.Wait)
            {
                total += Mathf.Max(0, c.Count);
                if (!rowFoes.Contains(c.Enemy.Objectname)) rowFoes.Add(c.Enemy.Objectname);
            }
    }

    static void OnWhistled(Link_Interact hit)
    {
        try
        {
            float multi;
            if (hit == null || hit.Type != "Hurt" || hit.To == null || hit.Data == null || !whistled.ContainsKey(hit.To) || !hit.Data.TryGetValue("Damagemulti", out multi) || multi <= 0f)
                return;
            ModKitCombat.SetMulti(hit, multi * (1f + WhistleTaken));
        }
        catch (Exception e) { RWLog.Once("whistle", "Tin Whistle: " + e.Message); }
    }

    public static bool IsWhistled(Entity_Master foe) { return foe != null && whistled.ContainsKey(foe); }
}
