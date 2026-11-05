// Server-side base for every Uloe buildable. Replaces the template's own AIs (which hard-code Smacky's objects). The
// game's own Entity_Ai_General_Upgrade sits on each of them (UloeContent.BuildTower) and runs the upgrade panel: the
// Upgrade/Upgradeoption commands, throttle, costs, Upgradeborder effect, the owner's Upgraded end-screen stat and Voice in
// a Bottle's price cut. This brain hears each upgrade through Master.Upgrade: a parameter on a stat the entity has becomes
// a modifier; the rest are read by the subclass through Param(). Every building has a Light stat
// (UloeLight feeds it) and shows its band as a skin.
public abstract class UloeBuilding : MonoBehaviour
{
    public Entity_Master Master;
    public UloeData.Build Def;
    public bool Started;
    public int LookBand;

    readonly Dictionary<string, float> param = new Dictionary<string, float>();
    readonly List<string> keys = new List<string>();   // no HashSet: System.Core isn't referenced by the game's compiler
    float nextLook, lookFrom;

    public bool Has(string key) { return keys.Contains(key); }
    public int UpgradeCount { get { return keys.Count; } }

    public float Param(string name, float fallback)
    {
        float v;
        return param.TryGetValue(name, out v) ? v : fallback;
    }

    public float Stat(string name, float fallback) { return ModKitUtil.Stat(Master, name, fallback); }
    public long Owner { get { return ModKitUtil.Owner(Master); } }
    public int Team { get { return ModKitUtil.Team(Master); } }
    public Vector3 Pos { get { return ModKitUtil.Pos(Master); } }

    // The Light this building counts: what it receives, up to the cap.
    public float Light { get { return Mathf.Min(Stat(UloeData.LightStat, 0f), UloeData.LightCap); } }

    void Update()
    {
        if (Master == null)
        {
            Destroy(this);
            return;
        }
        // A despawned building lives to the end of its frame (Game_Entity.Despawn destroys it last): a Bank due a Photon
        // then made one its Despawn no longer reached, which never went (MAX ULO-4).
        if (!ModKitUtil.IsServer || (Master.Beingserver != null && Master.Beingserver.Dead))
            return;
        try
        {
            if (!Started)
            {
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || Master.Radio == null)
                    return;
                Begin();
            }
            Tick();
            if (Time.time >= nextLook)
            {
                nextLook = Time.time + 0.5f;
                ShowLight();
            }
        }
        catch (Exception e)
        {
            UloeLog.Warn(Def.Id + " brain: " + e.Message);
            enabled = false;
        }
    }

    void Begin()
    {
        Started = true;
        // Light as a stat (fit plan 10, RogueLab R3): a modifier on a stat that isn't there never applies.
        Link_Stats light = new Link_Stats();
        light.Name = UloeData.LightStat;
        light.Value_Base = 0f;
        light.Value_Type = Statproperties.Stattype.Fixed;
        light.Display_Type = Statproperties.Displaytypes.Float;
        light.Display_Exposed = true;
        Helper_Entitytools.Addstat(Master, light, 0f);
        Master.Command += OnCommand;
        Master.Upgrade += OnUpgrade;
        foreach (Upgradeproperties u in new List<Upgradeproperties>(Master.Upgradelistserver.Values))
            OnUpgrade(u);
        Started2();
        ModKitUtil.Vis(Master, "Uloe_Built", Pos, 0);
    }

    protected virtual void Started2() { }
    protected abstract void Tick();
    // Called by UloeServer as the entity despawns (killed, or Reason "Sell", "Place"...).
    public virtual void Gone(bool killed, string reason) { }
    protected virtual void Command(string command, string json, bool server) { }
    protected virtual void Upgraded(UloeData.Upg def) { }

    // "Upgrade" belongs to the stock Entity_Ai_General_Upgrade; every other command goes to the subclass.
    void OnCommand(string command, string json, bool server)
    {
        if (command == "Upgrade")
            return;
        try { Command(command, json, server); }
        catch (Exception e) { UloeLog.Warn(Def.Id + " command " + command + ": " + e.Message); }
    }

    void OnUpgrade(Upgradeproperties up)
    {
        UloeData.Upg def;
        if (up == null || !UloeContent.UpgradeDefs.TryGetValue(up.Objectname, out def) || keys.Contains(def.Key))
            return;
        keys.Add(def.Key);
        if (up.Defaultparams != null)
            foreach (KeyValuePair<string, float> kv in up.Defaultparams)
            {
                int cut = kv.Key.LastIndexOf('_');
                string stat = cut > 0 ? kv.Key.Substring(0, cut) : kv.Key;
                string type = cut > 0 ? kv.Key.Substring(cut + 1) : "";
                bool own = Master.Statsserver.ContainsKey(stat);
                if (own && (type == "Add" || type == "Multi" || type == "Pmulti"))
                {
                    Helper_Entitytools.Modtypes mt = type == "Add" ? Helper_Entitytools.Modtypes.Add : type == "Multi" ? Helper_Entitytools.Modtypes.Multi : Helper_Entitytools.Modtypes.Pmulti;
                    Helper_Entitytools.Addmod(Master, stat, kv.Value, mt, 0f);
                }
                else
                {
                    float old;
                    param[kv.Key] = param.TryGetValue(kv.Key, out old) && !kv.Key.EndsWith("_Base") && !kv.Key.EndsWith("_Max") ? old + kv.Value : kv.Value;
                }
            }
        if (Started)
        {
            ModKitUtil.Vis(Master, "Uloe_Upgrade", Pos, def.Tier);
            Upgraded(def);
        }
    }

    // Light's look: a skin per band, changed at most every LookHold seconds, since a skin change redraws the tower.
    void ShowLight()
    {
        float raw = Stat(UloeData.LightStat, 0f);
        int band = 0;
        for (int i = 0; i < UloeData.LookBands.Length; i++)
            if (raw > UloeData.LookBands[i] || (i > 0 && raw >= UloeData.LookBands[i]))
                band = i + 1;
        if (band == LookBand || Time.time < lookFrom)
            return;
        LookBand = band;
        lookFrom = Time.time + UloeData.LookHold;
        ModKitUtil.Skin(Master, band > 0 ? "Lit" + band : "");
    }

    // Foes near this building, nearest first.
    protected List<Entity_Master> Foes(float range) { return ModKitUtil.FoesNear(Master, Pos, range); }

    public UloePlayer Player { get { return UloeServer.Player(Owner); } }

    // Owner-only message, sent through the owner's vessel when it is around (a building that is going can't carry it).
    protected void Tell(int code)
    {
        UloeHeroBrain hero = UloeServer.HeroOf(Owner);
        UloeUtil.Warn(hero != null && hero.Master != null ? hero.Master : Master, code);
    }
}

// Flood Light: plugs into the nearest node in its Range, strikes it like the Mining Hut's pick (the stock payout to its
// owner) and gives Light. AIM picks one of your buildings within Aimrange; with nothing picked it aims at your nearest.
// Wide Flood lights all around it and can no longer be aimed; Long Throw's light runs in a straight line toward its aim and
// lights every building on it. Its aim is a light path: Photons near the line follow it to the far end.
//   Colour Filter   a COLOUR button: each payout of its strikes is traded, for its owner, into the picked colour at 70%.
//   Searchlight     foes in its Light are Illuminated and slowed, one untimed slow a Flood Light, off as they leave it.
//   Stadium Lights  a full ring of its Light, AIM back on top of it, and a second node struck as well.
//   Lighthouse      a beam that turns all the way round, lighting buildings for a while and marking foes; no AIM.
public class UloeFloodBrain : UloeBuilding
{
    public Entity_Master Node, Node2, Picked, Aimed;
    public int Strikes, Aims, Refused, Payouts, Filtered, Swept, Searched;
    public string LastAim = "", Colour = "All";
    public float SweepAngle;
    readonly UloeLight light = new UloeLight(), ring = new UloeLight(), beam = new UloeLight();
    readonly Dictionary<Entity_Master, Link_Modifier> slows = new Dictionary<Entity_Master, Link_Modifier>();
    readonly Dictionary<Entity_Master, float> crossed = new Dictionary<Entity_Master, float>();
    readonly List<Entity_Master> lit = new List<Entity_Master>(), swept = new List<Entity_Master>();
    static readonly List<Entity_Master> drop = new List<Entity_Master>();
    Link_Command colourCommand;
    float lastStrike, nextFeed, nextLink, nextSweep;
    bool noNodeTold, rayShown, sweepShown;

    public bool Wide { get { return Has("Wide"); } }
    public bool Long { get { return Has("Long"); } }
    public bool Stadium { get { return Has("Stadium"); } }
    public bool Lighthouse { get { return Has("Lighthouse"); } }
    // Wide Flood takes AIM away and Stadium Lights gives it back; Lighthouse takes it for good.
    public bool CanAim { get { return (!Wide || Stadium) && !Lighthouse; } }
    public bool Directed { get { return Node != null && CanAim && Aimed != null; } }
    public float AimReach { get { return Long ? Param("Linereach_Base", 8f) : Stat("Aimrange", 6f); } }
    public float RingRadius { get { return Stadium ? Param("Stadiumradius_Base", 5f) : Param("Wideradius_Base", 3.5f); } }

    // What it puts out, before Wide Flood's or Long Throw's share: more from a richer node.
    public float Output
    {
        get
        {
            if (Node == null)
                return 0f;
            return Stat("Lightout", 2f) + Mathf.Min(UloeData.FloodNodeMax, UloeData.FloodNodePerAura * UloeUtil.NodeAura(Node));
        }
    }

    // The far end of its light path: what it aims at, or with Long Throw the end of the line through it.
    public Vector3 LineEnd
    {
        get
        {
            if (Aimed == null)
                return Pos;
            Vector3 to = ModKitUtil.Pos(Aimed);
            if (!Long)
                return to;
            Vector3 d = to - Pos;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? Pos + d.normalized * Param("Linereach_Base", 8f) : to;
        }
    }

    // Lighthouse's beam end now.
    public Vector3 SweepEnd
    {
        get { return Pos + new Vector3(Mathf.Cos(SweepAngle), 0f, Mathf.Sin(SweepAngle)) * Param("Lighthousereach_Base", 9f); }
    }

    protected override void Started2()
    {
        ShowAim();
        nextFeed = Time.time + 0.2f;
    }

    protected override void Upgraded(UloeData.Upg def)
    {
        // "Can no longer be directed": the button goes; Stadium Lights puts it back.
        if (!CanAim)
        {
            Picked = null;
            if (Master.Commandlistserver.ContainsKey(UloeData.AimLink))
                Helper_Entitytools.Removecommand(Master, UloeData.AimLink);
        }
        else if (def.Key == "Stadium")
            ShowAim();
        if (def.Key == "Filter" && colourCommand == null && UloeContent.Colour != null)
        {
            colourCommand = Helper_Entitytools.Createcommand(Master, UloeData.ColourCommand, UloeData.ColourLink, false, true);
            SyncColour();
        }
        nextFeed = 0f;
        nextLink = 0f;
    }

    void ShowAim()
    {
        if (UloeContent.Aim == null || !CanAim || Master.Commandlistserver.ContainsKey(UloeData.AimLink))
            return;
        Helper_Entitytools.Createcommand(Master, UloeData.AimCommand, UloeData.AimLink, false, true);
        Helper_Entitytools.Updatecommand(Master, UloeData.AimLink, "", false, false);
    }

    protected override void Tick()
    {
        Strike();
        if (Time.time >= nextFeed)
        {
            nextFeed = Time.time + UloeData.LightTick;
            FindNode();
            Feed();
            Search();
            swept.Clear();
        }
        if (Lighthouse && Time.time >= nextSweep)
        {
            nextSweep = Time.time + UloeData.LighthouseStep;
            Sweep();
        }
        if (Time.time >= nextLink)
            ShowLinks();
    }

    void FindNode()
    {
        float range = Stat("Range", 2.5f);
        if (!UloeUtil.IsNode(Node) || ModKitUtil.Flat(ModKitUtil.Pos(Node), Pos) > range + 0.1f)
        {
            // The node it held is gone: take its cord (and a ring) off every screen now, not 12 s later (MAX ULO-10).
            if (!ReferenceEquals(Node, null) && Master.Radio != null)
            {
                ModKitUtil.Vis(Master, "Uloe_Nodecord", Pos, -1f);
                if (Wide || Stadium) ModKitUtil.Vis(Master, "Uloe_Ring", Pos, -1f);
            }
            Node = UloeUtil.NearestNode(Pos, range);
            if (Node != null)
            {
                noNodeTold = false;
                ShowLinks();
            }
            else if (!noNodeTold)
            {
                noNodeTold = true;
                Tell(UloeData.WarnNoNode);
            }
        }
        // Stadium Lights strikes a second node in reach as well.
        if (!Stadium || Node == null)
            Node2 = null;
        else if (!UloeUtil.IsNode(Node2) || Node2 == Node || ModKitUtil.Flat(ModKitUtil.Pos(Node2), Pos) > range + 0.1f)
            Node2 = UloeUtil.NearestNode(Pos, range, Node);
    }

    // Strikes its node like the Mining Hut's pick, faster in Light. Ore takes min(Damage, 1) + Damageresource per strike
    // (Helper_Damagetools.Dodamage) and pays out when emptied.
    void Strike()
    {
        float aspd = Stat("Attackspeed", 0.4f) * (1f + UloeData.FloodPerLight * Light);
        if (aspd <= 0f || Node == null || !ModKitUtil.Alive(Node) || Time.time - lastStrike < 1f / aspd)
            return;
        lastStrike = Time.time;
        StrikeOne(Node);
        if (Node2 != null && ModKitUtil.Alive(Node2))
            StrikeOne(Node2);
        Strikes++;
    }

    void StrikeOne(Entity_Master node)
    {
        float hit = 1f + Mathf.Max(0f, Stat("Damage", 1f) - 1f);
        Dictionary<string, float> d = new Dictionary<string, float> { { "Damage", 1f }, { "Damagemulti", 1f }, { "Damageresource", hit - 1f } };
        // What a payout gives, read before the strike: a node that pays once is gone after it.
        float before = ModKitUtil.Stat(node, "Healthresource", -1f);
        float red = ModKitUtil.Stat(node, "Redauramine", 0f), blue = ModKitUtil.Stat(node, "Blueauramine", 0f), yellow = ModKitUtil.Stat(node, "Yellowauramine", 0f);
        Vector3 at = ModKitUtil.Pos(node);
        Helper_Entitytools.Interact(Master, node, "Hurt", d);
        Master.Radio.Vis_ClientRpc("Uloe_Strike", at, 0f);
        if (!UloeUtil.Paid(before, ModKitUtil.Stat(node, "Healthresource", -1f), hit, ModKitUtil.Alive(node)))
            return;
        Payouts++;
        if (Has("Filter") && Colour != "All")
            Filter(red, blue, yellow);
    }

    // Colour Filter: the node just split its payout among the active players (Entity_Ai_Resource_Mine, Styles.Split). The
    // owner's share is taken back colour by colour (the game's own Give, with a negative amount, so its Gained stat nets
    // out) and paid as 70% of the share's total in the picked colour. Teammates keep theirs as the node paid it.
    public void Filter(float red, float blue, float yellow)
    {
        int players = UloeUtil.ActivePlayers();
        float total = red + blue + yellow;
        if (players <= 0 || total <= 0f)
            return;
        long owner = Owner;
        if (red > 0f) Give("Redaura", -red / players, owner);
        if (blue > 0f) Give("Blueaura", -blue / players, owner);
        if (yellow > 0f) Give("Yellowaura", -yellow / players, owner);
        Give(Colour, Param("Filter_Base", 0.7f) * total / players, owner);
        Filtered++;
    }

    static void Give(string colour, float amount, long owner)
    {
        Link_Resource r = default(Link_Resource);
        r.Name = colour;
        r.Amount = amount;
        Helper_Resources.Giveresource(r, owner, Helper_Resources.Styles.Default);
    }

    // The aim: what the player picked while it stands in reach, else the nearest of the owner's buildings in reach.
    Entity_Master CurrentAim()
    {
        if (ModKitUtil.Alive(Picked) && ModKitUtil.Flat(ModKitUtil.Pos(Picked), Pos) <= AimReach + 0.5f)
            return Picked;
        Picked = null;
        UloeBuilding near = UloeServer.NearestBuilding(Owner, Pos, AimReach, Master);
        return near != null ? near.Master : null;
    }

    void Feed()
    {
        if (Node == null)
        {
            if (Aimed != null) nextLink = 0f;
            Aimed = null;
            return;
        }
        if (Wide || Stadium)
        {
            float share = Stadium ? Param("Stadium_Base", 1f) : Param("Wide_Base", 0.6f);
            UloeServer.Shine(ring, Master, Pos, RingRadius, Output * share, Master);
        }
        if (!CanAim)
        {
            if (Aimed != null) nextLink = 0f;
            Aimed = null;
            return;
        }
        Entity_Master aim = CurrentAim();
        if (aim != Aimed)
        {
            Aimed = aim;
            nextLink = 0f;
        }
        if (aim == null)
            return;
        long owner = Owner;
        if (!Long)
        {
            light.Feed(aim, Output, owner);
            light.Sweep();
            return;
        }
        Vector3 end = LineEnd;
        float width = Param("Linewidth_Base", 0.6f), amount = Output * Param("Line_Base", 0.8f);
        foreach (UloeBuilding b in UloeServer.Buildings)
            if (b != null && b.Started && b != this && b.Master != null && b.Team == Team && ModKitUtil.Alive(b.Master) && UloeUtil.SegmentDistance(b.Pos, Pos, end) <= width)
                light.Feed(b.Master, amount, owner);
        light.Sweep();
    }

    // Searchlight, once a second: every foe in its Light now is Illuminated and holds one slow from this Flood Light; a
    // foe that left it, or fell, loses that slow.
    void Search()
    {
        lit.Clear();
        if (Has("Searchlight") && Node != null)
        {
            if (Wide || Stadium)
                lit.AddRange(Foes(RingRadius));
            if (Directed)
            {
                if (Long)
                {
                    Vector3 end = LineEnd;
                    foreach (Entity_Master f in Foes(Param("Linereach_Base", 8f) + 0.5f))
                        if (UloeUtil.SegmentDistance(ModKitUtil.Pos(f), Pos, end) <= UloeData.SearchLine && !lit.Contains(f))
                            lit.Add(f);
                }
                else
                    foreach (Entity_Master f in ModKitUtil.FoesNear(Master, ModKitUtil.Pos(Aimed), Param("Searchradius_Base", 1.5f)))
                        if (!lit.Contains(f))
                            lit.Add(f);
            }
            foreach (Entity_Master f in swept)
                if (ModKitUtil.Alive(f) && !lit.Contains(f))
                    lit.Add(f);
        }
        int level = Output >= Param("Searchbright_Base", 5f) - 0.0001f ? UloeData.SearchLitBright : UloeData.SearchLit;
        float slow = Mathf.Clamp01(Param("Searchslow_Base", 0.2f));
        foreach (Entity_Master f in lit)
        {
            UloeMarks.Illuminate(Master, f, level);
            Link_Modifier m;
            if (slows.TryGetValue(f, out m) && m != null && !m.Dead)
                continue;
            if (ModKitUtil.Speed(f) == null)
                continue;
            slows[f] = Helper_Entitytools.Addmod(f, "Speed", -slow, Helper_Entitytools.Modtypes.Multi, 0f);
            Searched++;
        }
        drop.Clear();
        foreach (KeyValuePair<Entity_Master, Link_Modifier> kv in slows)
            if (kv.Key == null || !ModKitUtil.Alive(kv.Key) || !lit.Contains(kv.Key))
                drop.Add(kv.Key);
        foreach (Entity_Master f in drop)
        {
            Link_Modifier m;
            if (slows.TryGetValue(f, out m))
                ModKitUtil.Unmod(m);
            slows.Remove(f);
        }
    }

    // Lighthouse, ten times a second: the beam turns on; every building and foe it crossed since the last step (the wedge
    // between the two angles, so nothing slips between steps) is lit or marked. A building is lit again at most once a
    // second; what it holds lasts Lighthousehold seconds.
    void Sweep()
    {
        if (Node == null)
            return;
        float turn = Mathf.Max(0.5f, Param("Lighthouseturn_Base", 6f)), reach = Param("Lighthousereach_Base", 9f);
        float from = SweepAngle;
        SweepAngle = Mathf.Repeat(Time.time / turn * Mathf.PI * 2f, Mathf.PI * 2f);
        float span = Mathf.Repeat(SweepAngle - from, Mathf.PI * 2f);
        if (span > Mathf.PI)
            span = 0f;   // the first step, or a long stall: just the beam where it is now
        float amount = Output * Param("Lighthouse_Base", 0.6f), hold = Param("Lighthousehold_Base", 6f);
        long owner = Owner;
        foreach (UloeBuilding b in UloeServer.Buildings)
        {
            if (b == null || !b.Started || b == this || b.Master == null || b.Team != Team || !ModKitUtil.Alive(b.Master) || !Crossed(b.Pos, from, span, reach))
                continue;
            float last;
            if (crossed.TryGetValue(b.Master, out last) && Time.time - last < 1f)
                continue;
            crossed[b.Master] = Time.time;
            beam.Feed(b.Master, amount, owner, hold);
            Swept++;
        }
        foreach (Entity_Master f in Foes(reach + 0.5f))
        {
            if (!Crossed(ModKitUtil.Pos(f), from, span, reach))
                continue;
            UloeMarks.Blind(Master, f, UloeData.LighthouseMark);
            UloeMarks.Illuminate(Master, f, UloeData.LighthouseMark);
            if (!swept.Contains(f))
                swept.Add(f);
        }
        if (crossed.Count > 64)
        {
            drop.Clear();
            foreach (KeyValuePair<Entity_Master, float> kv in crossed)
                if (kv.Key == null || Time.time - kv.Value > hold)
                    drop.Add(kv.Key);
            foreach (Entity_Master e in drop)
                crossed.Remove(e);
        }
        beam.Sweep();
    }

    // Whether a point lies in the wedge the beam swept from 'from' through 'span' radians (and its width), within reach.
    bool Crossed(Vector3 p, float from, float span, float reach)
    {
        Vector3 d = p - Pos;
        d.y = 0f;
        float r = d.magnitude;
        if (r > reach || r < 0.05f)
            return false;
        float slack = UloeData.LighthouseWidth / Mathf.Max(r, 0.5f);
        float rel = Mathf.Repeat(Mathf.Atan2(d.z, d.x) - from, Mathf.PI * 2f);
        return rel <= span + slack || rel >= Mathf.PI * 2f - slack;
    }

    protected override void Command(string command, string json, bool server)
    {
        if (command == UloeData.ColourLink)
        {
            if (!Has("Filter"))
                return;
            int i = Array.IndexOf(UloeData.Colours, Colour);
            Colour = UloeData.Colours[(i + 1) % UloeData.Colours.Length];
            SyncColour();
            return;
        }
        if (command != UloeData.AimLink || !CanAim)
            return;
        Aims++;
        Entity_Master m = UloeUtil.Picked(json);
        UloeBuilding b = m != null ? m.GetComponent<UloeBuilding>() : null;
        // Only the owner's own buildings, as the client's picker shows (ULO-3, decision 18).
        if (b == null || b == this || !b.Started || b.Owner != Owner || !ModKitUtil.Alive(m))
        {
            Refused++;
            LastAim = "not a building of yours";
            Tell(UloeData.WarnBadAim);
            return;
        }
        if (ModKitUtil.Flat(b.Pos, Pos) > AimReach + 0.5f)
        {
            Refused++;
            LastAim = "too far";
            Tell(UloeData.WarnAimFar);
            return;
        }
        Picked = m;
        LastAim = ModKitUtil.BeingName(m);
        nextFeed = 0f;
        Tell(UloeData.WarnAimed);
    }

    // The button draws the colour from the command's Goalstring (synced); run: false, so it isn't pressed again.
    void SyncColour()
    {
        if (colourCommand == null)
            return;
        colourCommand.Goalstring = Colour;
        Helper_Entitytools.Updatecommand(Master, UloeData.ColourLink, "", false, false);
    }

    // The cord to the node, the ring, the ray of light and the Lighthouse's beam, told to every player on change and again
    // every few seconds.
    void ShowLinks()
    {
        nextLink = Time.time + UloeData.LinkResend;
        if (Node != null)
            ModKitUtil.Vis(Master, "Uloe_Nodecord", ModKitUtil.Pos(Node), 1f);
        if ((Wide || Stadium) && Node != null)
            ModKitUtil.Vis(Master, "Uloe_Ring", Pos, RingRadius);
        if (Directed)
        {
            rayShown = true;
            ModKitUtil.Vis(Master, "Uloe_Ray", LineEnd, Long ? 2f : 1f);
        }
        else if (rayShown)
        {
            rayShown = false;
            ModKitUtil.Vis(Master, "Uloe_Ray", Pos, -1f);
        }
        // The client turns the beam on by itself from where it is now and how long a turn takes.
        if (Lighthouse && Node != null)
        {
            sweepShown = true;
            ModKitUtil.Vis(Master, "Uloe_Sweep", SweepEnd, Mathf.Max(0.5f, Param("Lighthouseturn_Base", 6f)));
        }
        else if (sweepShown)
        {
            sweepShown = false;
            ModKitUtil.Vis(Master, "Uloe_Sweep", Pos, -1f);
        }
    }

    public override void Gone(bool killed, string reason)
    {
        foreach (Link_Modifier m in slows.Values)
            ModKitUtil.Unmod(m);
        slows.Clear();
        if (Master != null && Master.Radio != null)
        {
            ModKitUtil.Vis(Master, "Uloe_Ray", Pos, -1f);
            if (sweepShown) ModKitUtil.Vis(Master, "Uloe_Sweep", Pos, -1f);
        }
    }
}
