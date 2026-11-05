// Claude's Directions for any vessel (docs/scope/11 (Any), built 2026-11-05; compiled, not yet run in game), host only, in
// a campaign map. RWDirections holds their sheets (16_Directions.cs); this reads each player's ranks four times a second
// and puts them on that player's own things, found by owner (kit docs/17 §11.1 rule 2). Hits a Direction adds itself carry
// Extra in their data, so none of these counts them as the vessel's own (Chain Shot can't feed Notched Oar). Argo's Cleave
// is the same kind of extra hit, dealt from the vessel: `ExtraHit` skips it too (Dimitri, 2026-11-05).
//   Wound Spring   the holder's towers: Attackspeed Multi +6 / 12 / 20%                          (Sweep, twice a second)
//   Fine Sieve     each break after a Tide: 3 / 5 / 8 x the Tide's number of the colour held least, paid to the holder
//   Spare Hands    the holder's unit buildings: Units +1 / +1 / +2 (the base vessels' own count message after it), Boutman's
//                  barrels Vacantcurrent too; rank II and III: Respawn -25 / -40% on them and on the Bouthouse (Boutbro)
//   Galley Kettle  each vessel attack: +4% Attackspeed, up to 20 / 35 / 50%, gone 3 s after the last; rank III: +10% Damage at the top
//   Chain Shot     every 4th (rank III 3rd) vessel attack jumps 1 / 2 / 3 times to the nearest foe within 2 tiles of the last,
//                  for half the hit; with Fire Ship each jump bursts for 10% of the struck foe's max health on the foes
//                  within 0.75 tiles of it
//   Salt Barrel    the vessel: Health Pmulti +15 / 30 / 45%; rank II and III: mends 0.5 / 1 a second while hurt
//   Fog Horn       foes within 2 / 2 / 3 tiles of the vessel: Speed Multi -10 / 15 / 25% (the strongest holder's), four
//                  times a second, gone as they leave
//   Clam Shell     the vessel's hit hook, after the other mods' hooks and before the game's own: a killing blow leaves it at
//                  1 health, once a map (rank III twice); rank II and III: nothing hurts it for 3 s after
//   Notched Oar    every 10th / 7th / 5th vessel attack: Damagemulti x2 on each foe it strikes (the attacker's side)
//   Lamp Oil       a foe the vessel drowns (it is the foe's Lasthit, set by the killing blow) bursts: 15 / 25 / 35% of its max health on the foes
//                  within 1 / 1.25 / 1.5 tiles, credited to the vessel, so a burst's kills burst too
//   Fish Hook      15 / 25 / 35% of vessel hits snag: half the hit again, in six ticks over 3 s; at most 3 / 3 / 5 of the
//                  holder's on one foe
//   Spare Compass  RWDirections.Roll offers 4 / 4 / 5; rank II and III: one redraw a map (RWVote's pick, RedrawPick)
//   Magpie Nest    the vessel and the holder's towers: Damage Multi +1 / 1.5 / 2% for each Trinket stack and each Cardinal
//   Tide Pool      in RWDirections.TideEffects (Tidal Mend's own pass)
// An attack is one hit for Galley Kettle, Chain Shot and Notched Oar however many foes it strikes: its hits land in one
// frame. Fish Hook rolls for each foe struck. No look of their own yet: a stock debuff look shows only on maps whose groups
// hold it (pitfall #114).
public static class RWAny
{
    // Every Direction this file and Tide Pool build: the flow self-test leaves them to the gaps part "any".
    public static readonly string[] Names = { "Wound", "Sieve", "Hands", "Kettle", "Chain", "Salt", "Fog", "Clam", "Oar", "Lamp", "Hook", "Compass", "Magpie", "Fireship", "Pool" };
    public const string Extra = "RW_Extra";
    // Argo's Cleave: the swing's second foe, dealt from the vessel with this in its data (Argo/Code/06_HeroBrain.cs). Not
    // the vessel's own hit for Notched Oar, Fish Hook, Gutting Knife or the attack counts.
    public const string ArgoCleave = "Argocleave";

    public static bool ExtraHit(Dictionary<string, float> data) { return data.ContainsKey(Extra) || data.ContainsKey(ArgoCleave); }
    public static readonly float[] WoundSpeed = { 0f, 0.06f, 0.12f, 0.2f };
    public static readonly int[] SieveTimes = { 0, 3, 5, 8 };
    public static readonly int[] HandsUnits = { 0, 1, 1, 2 };
    public static readonly float[] HandsRespawn = { 0f, 0f, 0.25f, 0.4f };
    public const float KettleStep = 0.04f, KettleLasts = 3f, KettleTopDamage = 0.1f;
    public static readonly float[] KettleCap = { 0f, 0.2f, 0.35f, 0.5f };
    public static readonly int[] ChainEvery = { 0, 4, 4, 3 };
    public static readonly int[] ChainJumps = { 0, 1, 2, 3 };
    public const float ChainReach = 2f, ChainShare = 0.5f;
    public static readonly float[] SaltHealth = { 0f, 0.15f, 0.3f, 0.45f };
    public static readonly float[] SaltMend = { 0f, 0f, 0.5f, 1f };
    public static readonly float[] FogSlow = { 0f, 0.1f, 0.15f, 0.25f };
    public static readonly float[] FogReach = { 0f, 2f, 2f, 3f };
    public static readonly int[] ClamSaves = { 0, 1, 1, 2 };
    public const float ClamSafe = 3f;
    public static readonly int[] OarEvery = { 0, 10, 7, 5 };
    public static readonly float[] LampBurst = { 0f, 0.15f, 0.25f, 0.35f };
    public static readonly float[] LampReach = { 0f, 1f, 1.25f, 1.5f };
    public const float LampFresh = 0.25f;
    public static readonly float[] HookChance = { 0f, 0.15f, 0.25f, 0.35f };
    public static readonly int[] HookBleeds = { 0, 3, 3, 5 };
    public const float HookShare = 0.5f, HookLasts = 3f;
    public const int HookTicks = 6;
    public static readonly int[] CompassMore = { 0, 1, 1, 2 };
    public const int CompassRedrawRank = 2;
    public static readonly float[] MagpieEach = { 0f, 0.01f, 0.015f, 0.02f };
    public const float FireShipBurst = 0.1f, FireShipReach = 0.75f;

    // One player's ranks and what their Directions count on this map.
    public sealed class Crew
    {
        public RWPlayer Player;
        public Entity_Master Vessel;
        public int Wound, Sieve, Hands, Kettle, Chain, Salt, Fog, Clam, Oar, Lamp, Hook, Compass, Magpie, Fireship;
        public int KettleHits, ChainCount, OarCount, Saves, Hoard, AttackFrame = -1;
        public bool OarNow, ChainNow;
        public float KettleLast = -99f, SafeUntil, NextMend;
        public bool Listens { get { return Kettle + Chain + Oar + Hook > 0; } }
    }

    sealed class Jump { public Entity_Master From, First; public Vector3 At; public float Damage; public int Left; public bool Fire; }
    sealed class Burst { public Entity_Master From, Skip; public Vector3 At; public float Damage, Reach; }
    sealed class Bleed { public Entity_Master From, Foe; public long Owner; public float Each, Next; public int Left; }
    sealed class Fogged { public Link_Modifier Mod; public float Amount; }

    static readonly Dictionary<long, Crew> crews = new Dictionary<long, Crew>();
    static readonly List<Jump> jumps = new List<Jump>();
    static readonly List<Burst> bursts = new List<Burst>();
    static readonly List<Bleed> bleeds = new List<Bleed>();
    static readonly Dictionary<Entity_Master, Fogged> fogged = new Dictionary<Entity_Master, Fogged>();
    static readonly Dictionary<Entity_Master, float> fogWant = new Dictionary<Entity_Master, float>();
    static readonly List<string> redrawn = new List<string>();
    static readonly Dictionary<string, float> extra = new Dictionary<string, float> { { Extra, 1f } };
    static readonly float[] purse = new float[3];   // Fine Sieve's read, in ModKitUtil.Auras' order
    static readonly Entity_Master.Interacts dealt = OnDealt;
    static readonly Entity_Master.Oninteracts hurt = OnHurt;
    static float nextRanks, nextHooks, nextSweep, nextFog, nextPrune;
    public static bool ForceSnag;       // self-test: every hit snags
    public static int Doubled, Jumped, Burned, Snags, Saved, Swept;
    public static float Bled;

    // A map starts (RWVote.MapStart): the last map's things go, and each holder's once-a-map uses come back.
    public static void Reset()
    {
        crews.Clear();
        jumps.Clear();
        bursts.Clear();
        bleeds.Clear();
        fogged.Clear();
        redrawn.Clear();
        RWHold.Clear();
    }

    public static Crew CrewOf(long session)
    {
        Crew c;
        return crews.TryGetValue(session, out c) ? c : null;
    }

    static void Refresh(Crew c, RWPlayer p)
    {
        c.Player = p;
        c.Wound = Top(p, "Wound"); c.Sieve = Top(p, "Sieve"); c.Hands = Top(p, "Hands"); c.Kettle = Top(p, "Kettle");
        c.Chain = Top(p, "Chain"); c.Salt = Top(p, "Salt"); c.Fog = Top(p, "Fog"); c.Clam = Top(p, "Clam"); c.Oar = Top(p, "Oar");
        c.Lamp = Top(p, "Lamp"); c.Hook = Top(p, "Hook"); c.Compass = Top(p, "Compass"); c.Magpie = Top(p, "Magpie");
        c.Fireship = Top(p, "Fireship");
    }

    static int Top(RWPlayer p, string name) { return Top(p, "Any", name); }

    public static int Top(RWPlayer p, string vessel, string name) { return Mathf.Clamp(RWDirections.Top(p, vessel, name), 0, 3); }

    // ---------------------------------------------------------------- the offers (Spare Compass)

    public static int MoreOffers(RWPlayer p) { return p != null ? CompassMore[Top(p, "Compass")] : 0; }

    public static bool CanRedraw(RWPlayer p) { return p != null && Top(p, "Compass") >= CompassRedrawRank && !redrawn.Contains(p.Key); }

    public static void Redrew(RWPlayer p) { if (p != null && !redrawn.Contains(p.Key)) redrawn.Add(p.Key); }

    // ---------------------------------------------------------------- every frame (host)

    public static void Update()
    {
        if (!RWRun.InCampaignMap || !ModKitUtil.IsServer || Game_Entity.Instance == null)
            return;
        float now = Time.time;
        if (now >= nextRanks)
        {
            nextRanks = now + 0.25f;
            foreach (long s in RWRun.Sessions())
            {
                Crew c;
                if (!crews.TryGetValue(s, out c))
                    crews[s] = c = new Crew();
                Refresh(c, RWRun.For(s));
                c.Vessel = RWRun.Vessel(s);
            }
        }
        if (now >= nextHooks)
        {
            nextHooks = now + 0.1f;
            foreach (Crew c in crews.Values)
                Hooks(c, now);
        }
        if (now >= nextSweep)
        {
            nextSweep = now + 0.5f;
            Sweep();
        }
        if (now >= nextFog)
        {
            nextFog = now + 0.25f;
            Fog();
        }
        if (now >= nextPrune)
        {
            nextPrune = now + 5f;
            RWHold.Prune();
        }
        Jumps();
        Bursts();
        Bleeds(now);
    }

    // The hit hooks go on again ten times a second (an upgrade landing rebuilds both chains, kit docs/17 §2.3), and the
    // vessel's Galley Kettle and Salt Barrel mends follow its hits.
    static void Hooks(Crew c, float now)
    {
        Entity_Master v = c.Vessel;
        if (!ModKitUtil.Alive(v))
            return;
        if (c.Listens)
            ModKitUtil.Listen(v, dealt);
        if (c.Clam > 0)
            AfterFronts(v, hurt);
        if (c.Kettle > 0)
        {
            if (now - c.KettleLast > KettleLasts)
                c.KettleHits = 0;
            float speed = Mathf.Min(KettleCap[c.Kettle], KettleStep * c.KettleHits);
            RWHold.Set(v, "Kettle", "Attackspeed", Helper_Entitytools.Modtypes.Multi, speed);
            bool top = c.Kettle >= 3 && speed >= KettleCap[c.Kettle] - 0.0001f;
            RWHold.Set(v, "KettleTop", "Damage", Helper_Entitytools.Modtypes.Multi, top ? KettleTopDamage : 0f);
        }
        if (c.Salt > 0 && SaltMend[c.Salt] > 0f && now >= c.NextMend)
        {
            c.NextMend = now + 1f;
            float missing = ModKitUtil.MaxHealth(v) - ModKitUtil.Health(v);
            if (missing > 0.001f)
                ModKitUtil.Heal(v, v, Mathf.Min(missing, SaltMend[c.Salt]));
        }
    }

    // What is held while standing: on the vessel, and on each of the holder's towers (re-made for a rebuilt one, and for a
    // stat that appears later, such as a wall's Damage after Spikes: RWHold skips a stat the thing lacks, kit docs/17 §2.1).
    static void Sweep()
    {
        bool towers = false;
        foreach (Crew c in crews.Values)
        {
            Entity_Master v = c.Vessel;
            c.Hoard = c.Magpie > 0 ? Hoard(c.Player) : 0;
            if (ModKitUtil.Alive(v))
            {
                if (c.Salt > 0) RWHold.Set(v, "Salt", "Health", Helper_Entitytools.Modtypes.Pmulti, SaltHealth[c.Salt]);
                if (c.Magpie > 0) RWHold.Set(v, "Magpie", "Damage", Helper_Entitytools.Modtypes.Multi, MagpieEach[c.Magpie] * c.Hoard);
            }
            if (c.Wound > 0 || c.Magpie > 0 || c.Hands > 0) towers = true;
        }
        if (!towers)
            return;
        foreach (string tag in SweptTags)
            foreach (Entity_Master e in Owned(tag))
            {
                Crew c;
                if (!crews.TryGetValue(ModKitUtil.Owner(e), out c))
                    continue;
                List<string> tags = e.Beingserver.Entity.Tags;
                bool house = tags.Contains("Bouthouse");
                if (!house)
                {
                    if (c.Wound > 0) RWHold.Set(e, "Wound", "Attackspeed", Helper_Entitytools.Modtypes.Multi, WoundSpeed[c.Wound]);
                    if (c.Magpie > 0) RWHold.Set(e, "Magpie", "Damage", Helper_Entitytools.Modtypes.Multi, MagpieEach[c.Magpie] * c.Hoard);
                }
                if (c.Hands > 0) Hands(e, c, house, tags);
                Swept++;
            }
    }

    static readonly string[] SweptTags = { "Tower", "Bouthouse" };   // Boutbro is no Tower, so not here
    static readonly Dictionary<string, List<Entity_Master>> owned = new Dictionary<string, List<Entity_Master>>();
    static readonly Dictionary<string, int> ownedFrame = new Dictionary<string, int>();
    static readonly Predicate<Entity_Master> fallen = e => !ModKitUtil.Alive(e);

    // A tag's things as the sweeps read them (RWDirections.Reach's filter): standing, with stats, no vessel and no Blurry
    // Photo copy, whoever owns them. One list a tag a frame (RWAny's and RWTrinkets' sweeps and Tar Bucket fall on the
    // same frames), kept for reuse; a second look in the frame drops what fell since.
    public static IReadOnlyList<Entity_Master> Owned(string tag)
    {
        List<Entity_Master> l, all;
        int frame;
        if (!owned.TryGetValue(tag, out l))
            owned[tag] = l = new List<Entity_Master>();
        if (ownedFrame.TryGetValue(tag, out frame) && frame == Time.frameCount)
        {
            l.RemoveAll(fallen);
            return l;
        }
        ownedFrame[tag] = Time.frameCount;
        l.Clear();
        if (!Game_Entity.Instance.Entityclienttags.TryGetValue(tag, out all))
            return l;
        foreach (Entity_Master e in all)
        {
            if (!ModKitUtil.Alive(e) || e.Beingserver.Entity == null || e.Statsserver == null)
                continue;
            if (!e.Beingserver.Entity.Tags.Contains("Hero") && !RWRun.IsCopy(e))
                l.Add(e);
        }
        return l;
    }

    // Every Trinket stack the player holds and every Cardinal the run holds, the lobby's picks too.
    public static int Hoard(RWPlayer p) { return (p != null ? p.Trinkets.Count : 0) + RWCardinals.HeldCount(); }

    // A base vessel's unit building counts its units only when told to (kit docs/17 §11.1 rule 10, verified); a pack one
    // reads its Units stat live. Boutman's barrels keep a vacancy count beside their cap, as their own upgrades raise both.
    static void Hands(Entity_Master e, Crew c, bool house, List<string> tags)
    {
        bool units = !house && e.Statsserver.ContainsKey("Units");
        if (units && RWHold.Set(e, "Hands", "Units", Helper_Entitytools.Modtypes.Add, HandsUnits[c.Hands]))
        {
            if (tags.Contains("Boutbarrel"))
                RWHold.Set(e, "HandsVacant", "Vacantcurrent", Helper_Entitytools.Modtypes.Add, HandsUnits[c.Hands]);
            if (e.Ais != null)
                foreach (Entity_Ai_Base ai in e.Ais)
                    if (ai != null && ai.GetType().Name.StartsWith("Entity_Ai_Tower_"))
                    {
                        foreach (string call in new[] { "Addressminercount", "Addressmobcount", "Addressdogcount" })
                            ai.SendMessage(call, SendMessageOptions.DontRequireReceiver);
                        break;
                    }
        }
        if ((units || house) && HandsRespawn[c.Hands] > 0f)
            RWHold.Set(e, "HandsRespawn", "Respawn", Helper_Entitytools.Modtypes.Multi, -HandsRespawn[c.Hands]);
    }

    // ---------------------------------------------------------------- the vessel's hits (the attacker's side, kit docs/17 §2.3)

    static void OnDealt(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.Data == null || hit.From == null || hit.To == null || ExtraHit(hit.Data))
                return;
            Crew c;
            if (!crews.TryGetValue(ModKitUtil.Owner(hit.From), out c) || c.Vessel != hit.From || !ModKitUtil.IsFoe(hit.To, ModKitUtil.Team(hit.From)))
                return;
            float damage, multi;
            if (!hit.Data.TryGetValue("Damage", out damage) || !hit.Data.TryGetValue("Damagemulti", out multi))
                return;
            // A new attack: the first of its hits counts it.
            if (Time.frameCount != c.AttackFrame)
            {
                c.AttackFrame = Time.frameCount;
                if (c.Kettle > 0)
                {
                    c.KettleHits++;
                    c.KettleLast = Time.time;
                }
                c.OarNow = c.Oar > 0 && ++c.OarCount >= OarEvery[c.Oar];
                if (c.OarNow) c.OarCount = 0;
                c.ChainNow = c.Chain > 0 && ++c.ChainCount >= ChainEvery[c.Chain];
                if (c.ChainNow) c.ChainCount = 0;
            }
            if (c.OarNow)
            {
                multi *= 2f;
                ModKitCombat.SetMulti(hit, multi);
                Doubled++;
            }
            float landed = damage * multi;
            if (landed <= 0f)
                return;
            if (c.ChainNow)
            {
                // From the first foe the attack strikes.
                c.ChainNow = false;
                jumps.Add(new Jump { From = hit.From, First = hit.To, At = ModKitUtil.Pos(hit.To), Damage = landed * ChainShare, Left = ChainJumps[c.Chain], Fire = c.Fireship > 0 });
            }
            if (c.Hook > 0 && (ForceSnag || UnityEngine.Random.value < HookChance[c.Hook]))
                Snag(c, hit.From, hit.To, landed * HookShare);
        }
        catch (Exception e) { RWLog.Once("any dealt", "a vessel hit: " + e.Message); }
    }

    // ---------------------------------------------------------------- the vessel taking a hit

    // After every other mod's front hook (Uloe's SURGE cut, Made of Tougher Stuff: they come first, ModKitUtil.Front) and
    // before the first of the game's own handlers, one of which takes the health (Helper_Damagetools.Dodamage). An upgrade
    // AI on the vessel that changes the hit still acts after it. Put back ten times a second, like Front. A handler that
    // changes the blow goes before Clam Shell's, which judges it (before: ClamHook), so the order holds whichever part goes first.
    public static Entity_Master.Oninteracts ClamHook { get { return hurt; } }

    static void AfterFronts(Entity_Master m, Entity_Master.Oninteracts handler) { AfterFronts(m, handler, null); }

    public static void AfterFronts(Entity_Master m, Entity_Master.Oninteracts handler, Entity_Master.Oninteracts before)
    {
        Delegate[] list = m.Oninteract != null ? m.Oninteract.GetInvocationList() : new Delegate[0];
        List<Delegate> want = new List<Delegate>(list.Length + 1);
        foreach (Delegate d in list)
        {
            if (d.Equals(handler))
                continue;
            if ((d.Target is Entity_Ai_Base || (before != null && d.Equals(before))) && !want.Contains(handler))
                want.Add(handler);
            want.Add(d);
        }
        if (!want.Contains(handler))
            want.Add(handler);
        SetChain(m, list, want);
    }

    // A rebuilt hit chain goes back only when it differs from the one there.
    public static void SetChain(Entity_Master m, Delegate[] list, List<Delegate> want)
    {
        bool same = want.Count == list.Length;
        for (int i = 0; same && i < list.Length; i++)
            same = list[i].Equals(want[i]);
        if (!same)
            m.Oninteract = (Entity_Master.Oninteracts)Delegate.Combine(want.ToArray());
    }

    // Would this blow end the vessel? As the game deals it: armour takes any hit for 1, an overshield takes one it can hold
    // whole (a bigger one breaks it and lands in full), ore health takes at most 1 (Helper_Damagetools.Dodamage).
    public static bool Kills(Entity_Master v, float dealt)
    {
        Link_Stats s;
        Dictionary<string, Link_Stats> st = v.Statsserver;
        if (st.TryGetValue("Healtharmor", out s) && s.Value > 0f)
            return false;
        if (st.TryGetValue("Healthover", out s) && s.Value > 0f && dealt <= s.Value)
            return false;
        if (st.TryGetValue("Healthresource", out s) && s.Value > 0f)
            return false;
        return st.TryGetValue("Health", out s) && s.Value > 0f && dealt >= s.Value;
    }

    static void OnHurt(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.To == null || hit.Data == null)
                return;
            Crew c;
            if (!crews.TryGetValue(ModKitUtil.Owner(hit.To), out c) || c.Vessel != hit.To)
                return;
            float damage, multi;
            if (!hit.Data.TryGetValue("Damage", out damage) || !hit.Data.TryGetValue("Damagemulti", out multi) || damage <= 0f)
                return;
            float was = multi;
            if (Time.time < c.SafeUntil)
                multi = 0f;
            else if (c.Clam > 0 && c.Saves < ClamSaves[c.Clam])
            {
                float hp = ModKitUtil.Health(hit.To);
                if (hit.To.Statsserver != null && Kills(hit.To, damage * multi))
                {
                    multi = Mathf.Max(0f, (hp - 1f) / damage);
                    c.Saves++;
                    Saved++;
                    if (c.Clam >= 2)
                        c.SafeUntil = Time.time + ClamSafe;
                    RWLog.Info((c.Player != null ? c.Player.Name : "a vessel") + "'s Clam Shell held (" + c.Saves + " of " + ClamSaves[c.Clam] + " this map)");
                }
            }
            if (!Mathf.Approximately(multi, was))
            {
                ModKitCombat.SetMulti(hit, multi);
            }
        }
        catch (Exception e) { RWLog.Once("any hurt", "a hit on a vessel: " + e.Message); }
    }

    // ---------------------------------------------------------------- what the hits leave behind

    // Chain Shot's jumps, a frame after the hit: never inside another hit's handlers.
    static void Jumps()
    {
        if (jumps.Count == 0)
            return;
        List<Jump> now = new List<Jump>(jumps);
        jumps.Clear();
        foreach (Jump j in now)
        {
            if (j.From == null)
                continue;
            List<Entity_Master> struck = new List<Entity_Master> { j.First };
            Vector3 at = j.At;
            for (int k = 0; k < j.Left; k++)
            {
                Entity_Master next = Nearest(j.From, at, ChainReach, struck);
                if (next == null)
                    break;
                float max = ModKitUtil.MaxHealth(next);
                Vector3 there = ModKitUtil.Pos(next);
                Hit(j.From, next, j.Damage);
                Jumped++;
                if (j.Fire)
                    bursts.Add(new Burst { From = j.From, Skip = next, At = there, Damage = max * FireShipBurst, Reach = FireShipReach });
                struck.Add(next);
                at = there;
            }
        }
    }

    // FoesNear gives the nearest first.
    static Entity_Master Nearest(Entity_Master from, Vector3 at, float reach, List<Entity_Master> skip)
    {
        foreach (Entity_Master e in ModKitUtil.FoesNear(from, at, reach))
            if (!skip.Contains(e))
                return e;
        return null;
    }

    // Lamp Oil's and Fire Ship's bursts; a burst's kills queue theirs for the next frame.
    static void Bursts()
    {
        if (bursts.Count == 0)
            return;
        List<Burst> now = new List<Burst>(bursts);
        bursts.Clear();
        foreach (Burst b in now)
        {
            if (b.From == null || b.Damage <= 0f)
                continue;
            foreach (Entity_Master e in ModKitUtil.FoesNear(b.From, b.At, b.Reach))
                if (e != b.Skip)
                {
                    Hit(b.From, e, b.Damage);
                    Burned++;
                }
        }
    }

    // Game_Entity.Ondespawn (RWCardinals.OnDespawn): a foe the vessel drowned.
    public static void OnDespawn(Entity_Master master, bool killed, string reason)
    {
        if (!killed || master == null || !RWRun.InCampaignMap || !ModKitUtil.IsServer)
            return;
        Crew c = DrownedBy(master);
        if (c == null || c.Lamp <= 0)
            return;
        if (master.Taglistserver == null || !master.Taglistserver.Contains("Enemy") || ModKitUtil.Team(master) == ModKitUtil.Team(c.Vessel))
            return;
        bursts.Add(new Burst { From = c.Vessel, Skip = master, At = ModKitUtil.Pos(master), Damage = LampBurst[c.Lamp] * ModKitUtil.MaxHealth(master), Reach = LampReach[c.Lamp] });
    }

    // The player whose vessel dealt the blow that drowned this, or null. Its Lasthit, when set by the killing blow, not by an
    // older hit: one carrying its own Damage (a Sno Box, a wall's blast) never moves it (kit docs/17 §2.6), and a death comes
    // inside the blow that deals it (Helper_Damagetools.Addressdamage).
    public static Crew DrownedBy(Entity_Master master)
    {
        Entity_Master by = master.Lasthit;
        Crew c;
        return by != null && Time.fixedTime - master.Lasthittime <= LampFresh && crews.TryGetValue(ModKitUtil.Owner(by), out c) && c.Vessel == by ? c : null;
    }

    static void Snag(Crew c, Entity_Master from, Entity_Master foe, float total)
    {
        long owner = ModKitUtil.Owner(from);
        int on = 0;
        foreach (Bleed b in bleeds)
            if (b.Foe == foe && b.Owner == owner) on++;
        if (on >= HookBleeds[c.Hook] || total <= 0f)
            return;
        bleeds.Add(new Bleed { From = from, Foe = foe, Owner = owner, Each = total / HookTicks, Left = HookTicks, Next = Time.time + HookLasts / HookTicks });
        Snags++;
    }

    static void Bleeds(float now)
    {
        for (int i = bleeds.Count - 1; i >= 0; i--)
        {
            Bleed b = bleeds[i];
            if (b.From == null || !ModKitUtil.Alive(b.Foe))
            {
                bleeds.RemoveAt(i);
                continue;
            }
            if (now < b.Next)
                continue;
            b.Next += HookLasts / HookTicks;
            b.Left--;
            Hit(b.From, b.Foe, b.Each);
            Bled += b.Each;
            if (b.Left <= 0)
                bleeds.RemoveAt(i);
        }
    }

    // Credited to the vessel, so the kill is its (kit docs/17 §11.1 rule 17), and marked as no hit of its own.
    static void Hit(Entity_Master from, Entity_Master to, float damage)
    {
        if (ModKitUtil.Alive(to) && damage > 0f)
            ModKitCombat.Hurt(from, to, damage, 1f, null, extra);
    }

    // ---------------------------------------------------------------- Fog Horn

    static void Fog()
    {
        fogWant.Clear();
        foreach (Crew c in crews.Values)
        {
            if (c.Fog <= 0 || !ModKitUtil.Alive(c.Vessel))
                continue;
            foreach (Entity_Master foe in ModKitUtil.FoesNear(c.Vessel, ModKitUtil.Pos(c.Vessel), FogReach[c.Fog]))
            {
                float had;
                if (!fogWant.TryGetValue(foe, out had) || had < FogSlow[c.Fog])
                    fogWant[foe] = FogSlow[c.Fog];
            }
        }
        foreach (KeyValuePair<Entity_Master, float> kv in fogWant)
        {
            Fogged f;
            if (fogged.TryGetValue(kv.Key, out f) && f.Mod != null && !f.Mod.Dead && Mathf.Approximately(f.Amount, kv.Value))
                continue;
            if (f != null)
                ModKitUtil.Unmod(f.Mod);
            fogged[kv.Key] = new Fogged { Mod = ModKitUtil.SlowOn(kv.Key, kv.Value), Amount = kv.Value };
        }
        if (fogged.Count == fogWant.Count)
            return;
        foreach (Entity_Master foe in new List<Entity_Master>(fogged.Keys))
            if (foe == null || !fogWant.ContainsKey(foe))
            {
                ModKitUtil.Unmod(fogged[foe].Mod);
                fogged.Remove(foe);
            }
    }

    public static float FogOn(Entity_Master foe) { Fogged f; return foe != null && fogged.TryGetValue(foe, out f) && f.Mod != null ? f.Amount : 0f; }

    // ---------------------------------------------------------------- Fine Sieve (each break after a Tide)

    public static void Tide(int wave)
    {
        if (wave <= 0 || Game_Players.Instance == null)
            return;
        foreach (long s in RWRun.Sessions())
        {
            int rank = Top(RWRun.For(s), "Sieve");
            if (rank <= 0 || !ModKitUtil.Purse(s, purse))
                continue;
            string least = "";
            float low = float.MaxValue;
            for (int i = 0; i < purse.Length; i++)
                if (purse[i] < low) { low = purse[i]; least = ModKitUtil.Auras[i]; }
            int amount = SieveTimes[rank] * wave;
            Helper_Resources.Giveresources(new List<Link_Resourceset> { new Link_Resourceset { Name = least, Amount = amount } }, s, Helper_Resources.Styles.Default);
        }
    }
}

// One kept modifier per thing and key (kit docs/17 §11.1 rules 5-6): made when the thing has the stat, changed in place when
// the value changes, taken off at 0, through ModKitUtil.Keep (a Health mod made again, every other stat filled at once,
// kit docs/17 §2.1). Returns true when it changed.
public static class RWHold
{
    sealed class Kept { public Link_Modifier Mod; public float Value; }
    // Each thing's mods by key (a thing is a key here by reference: no string is built for a look-up).
    static readonly Dictionary<Entity_Master, Dictionary<string, Kept>> kept = new Dictionary<Entity_Master, Dictionary<string, Kept>>();

    public static void Clear() { kept.Clear(); }

    // Takes every thing's mod under this key off.
    public static void Drop(string key)
    {
        foreach (Dictionary<string, Kept> mods in kept.Values)
        {
            Kept h;
            if (!mods.TryGetValue(key, out h))
                continue;
            if (h.Mod != null && !h.Mod.Dead) ModKitUtil.Unmod(h.Mod);
            mods.Remove(key);
        }
    }

    public static bool Set(Entity_Master e, string key, string stat, Helper_Entitytools.Modtypes type, float value)
    {
        if (e == null || e.Statsserver == null || !e.Statsserver.ContainsKey(stat))
            return false;
        Dictionary<string, Kept> mods;
        if (!kept.TryGetValue(e, out mods))
            kept[e] = mods = new Dictionary<string, Kept>();
        Kept h;
        mods.TryGetValue(key, out h);
        bool live = h != null && h.Mod != null && !h.Mod.Dead;
        if (live && Mathf.Approximately(h.Value, value))
            return false;
        if (Mathf.Abs(value) < 0.0001f)
        {
            if (live) ModKitUtil.Unmod(h.Mod);
            mods.Remove(key);
            return live;
        }
        if (h == null)
            mods[key] = h = new Kept();
        h.Mod = ModKitUtil.Keep(e, live ? h.Mod : null, stat, type, value);
        h.Value = value;
        return true;
    }

    public static float Get(Entity_Master e, string key)
    {
        Dictionary<string, Kept> mods;
        Kept h;
        return e != null && kept.TryGetValue(e, out mods) && mods.TryGetValue(key, out h) && h.Mod != null && !h.Mod.Dead ? h.Value : 0f;
    }

    public static void Prune()
    {
        List<Entity_Master> gone = null;
        foreach (Entity_Master e in kept.Keys)
            if (e == null || e.Beingserver == null || e.Beingserver.Dead)
            {
                if (gone == null) gone = new List<Entity_Master>();
                gone.Add(e);
            }
        if (gone != null)
            foreach (Entity_Master e in gone) kept.Remove(e);
    }
}
