// Server-side brain for the vessel: build options (the Tree's slot offers the Tree, then the Bonsai, then nothing),
// regeneration, the fertilizer counter and FERTILIZE. It plays the part of a vessel's own AI (Entity_Ai_Hero_<Name>).
// Attacking is left to the stock Entity_Ai_Mob_Ranged, exactly like Sno Sno: the brain only names its shot (the green
// laser); movement, building and selling are the engine's generic hero AIs.
public class YosseleHeroBrain : MonoBehaviour
{
    public Entity_Master Master;
    public YosselePlayer Player;
    public int Rank, Fertilizes, Healed, DandelionHidden, DandelionShown, BonsaiShown;
    public float Regenerated;
    public bool Armed;
    bool started;
    float nextCheck, nextRegen, guardUntil;
    readonly YosseleFaults faults = new YosseleFaults();

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
                Begin();
            }
            if (!Armed)
                Arm();
            if (Time.time >= nextRegen)
            {
                nextRegen = Time.time + 1f;
                Regen();
            }
            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + 0.5f;
                CheckDirections();
                CheckDandelion();
                CheckFertilize();
            }
        }
        catch (Exception e)
        {
            if (faults.Fail("hero brain", e))
                enabled = false;
        }
    }

    void Begin()
    {
        started = true;
        YosseleServer.Heroes.Add(this);
        for (int i = 0; i < YosseleData.Towers.Length; i++)
            BuildOption(i);
        Player = YosseleServer.Player(ModKitUtil.Owner(Master));
        Player.Hero = this;
        CheckDirections();
        CheckDandelion();
        CheckFertilize();
        // Subscribed after the Updatecommand calls above, which run each command once.
        Master.Command += OnCommand;
        YosseleLog.Info("Yossele landed for player " + ModKitUtil.Owner(Master));
    }

    void BuildOption(int i) { BuildOption("Buildoption" + (i + 1), YosseleData.Towers[i]); }

    void BuildOption(string link, string tower)
    {
        Link_Command c = Helper_Entitytools.Createcommand(Master, "Command_" + link, link, false, true);
        c.Goalstring = tower;
        c.Goalvalue = 1f;
        Helper_Entitytools.Updatecommand(Master, link, "", true, true);
    }

    // Entity_Ai_Mob_Ranged creates the Attack command when it spawns; give it Yossele's laser (Hero_Snosno.Starting
    // does the same with its snowball). The laser's hits carry no Damage, so the game fills in Yossele's own.
    void Arm()
    {
        Link_Command attack;
        if (Master.Commandlistserver == null || !Master.Commandlistserver.TryGetValue("Attack", out attack) || attack == null)
            return;
        attack.Goalstring = YosseleContent.LaserReady ? YosseleData.Laser : YosseleData.LaserFallback;
        Entity_Ai_Mob_Ranged ranged = Master.GetComponent<Entity_Ai_Mob_Ranged>();
        if (ranged != null && ranged.Hurtdict != null)
            ranged.Hurtdict[YosseleData.LaserTag] = 1f;
        Armed = true;
    }

    // "More health regen" (scope): no base vessel regenerates, so the brain mends Yossele once a second while hurt.
    void Regen()
    {
        if (!ModKitUtil.Alive(Master))
            return;
        float missing = ModKitUtil.MaxHealth(Master) - ModKitUtil.Health(Master);
        if (missing <= 0.01f)
            return;
        float amount = Mathf.Min(missing, YosseleData.Regen);
        ModKitUtil.Heal(Master, Master, amount);
        Healed++;
        Regenerated += amount;
    }

    // ---------------------------------------------------------------- the Tree's build slot (both limit 1)

    // The slot offers the Tree while this player has none, the Bonsai while the Tree has its Bonsai upgrade and no
    // Bonsai stands, and nothing otherwise (Argo's Miner's Base, after Entity_Ai_Hero_Boutman.Checkbouthouses). The
    // server checks build commands against the vessel's own build options, so a removed button can't be built from
    // either. A button that must change is removed first and made again on the next check, half a second later.
    public string Slot = "";

    // The Tree's Directions this vessel holds, for what its player's Trees do and the slot's limit (kit 1.7).
    void CheckDirections()
    {
        Player.Season = YosseleServer.DirectionRank(Master, YosseleData.Seasons[0].Name);
        Player.Mushrooms = YosseleServer.DirectionRank(Master, YosseleData.Mushboomkits[0].Name);
    }

    public void CheckDandelion()
    {
        if (!started || Master == null || Master.Commandlistserver == null)
            return;
        string want = YosseleServer.TreeSlot(ModKitUtil.Owner(Master));
        Link_Command shown;
        Master.Commandlistserver.TryGetValue(YosseleData.DandelionOption, out shown);
        if (shown != null && shown.Goalstring != want)
        {
            Helper_Entitytools.Removecommand(Master, YosseleData.DandelionOption);
            DandelionHidden++;
            Slot = "";
        }
        else if (shown == null && want != "")
        {
            BuildOption(YosseleData.DandelionOption, want);
            if (want == YosseleData.Bonsai) BonsaiShown++;
            else DandelionShown++;
            Slot = want;
        }
    }

    // ---------------------------------------------------------------- FERTILIZE and fertilizer

    // The button doesn't exist until a Compost Bin is held (scope); then the fertilizer counter shows on the vessel.
    void CheckFertilize()
    {
        Rank = YosseleServer.CompostRank(Master);
        bool shown = Master.Commandlistserver.ContainsKey(YosseleData.FertilizeLink);
        if (Rank > 0 && !shown && YosseleContent.Fertilize != null)
        {
            Helper_Entitytools.Createcommand(Master, YosseleData.FertilizeCommand, YosseleData.FertilizeLink, false, true);
            Helper_Entitytools.Updatecommand(Master, YosseleData.FertilizeLink, "", false, false);
            if (!Player.Unlocked)
            {
                Player.Unlocked = true;
                YosseleUtil.Warn(Master, YosseleData.WarnUnlocked);
            }
            ShowFertilizer();
        }
        else if (Rank == 0 && shown)
            Helper_Entitytools.Removecommand(Master, YosseleData.FertilizeLink);
    }

    public void ShowFertilizer()
    {
        if (Master != null && Player != null && (Player.Unlocked || Rank > 0))
            ModKitCounter.Set(Master, YosseleData.FertilizerStat, Player.Fertilizer);
    }

    void OnCommand(string command, string json, bool server)
    {
        if (command != YosseleData.FertilizeLink)
            return;
        try { Fertilize(Picked(json)); }
        catch (Exception e) { YosseleLog.Warn("fertilize: " + e.Message); }
    }

    // The building the player clicked (YosseleFertilizeSelect sends its NetworkObjectId as {"Ref":id}); null if none.
    static Entity_Master Picked(string json)
    {
        int at = json != null ? json.IndexOf("\"Ref\":") : -1;
        if (at < 0)
            return null;
        int i = at + 6, j = i;
        while (j < json.Length && char.IsDigit(json[j])) j++;
        ulong id;
        if (j == i || !ulong.TryParse(json.Substring(i, j - i), out id))
            return null;
        return ModKitUtil.ById(id);
    }

    // Spends 1 fertilizer for +1 Growth on one of this player's Yossele buildings. From the hotkey with nothing picked:
    // the nearest of theirs that can still grow, within reach.
    public bool Fertilize(Entity_Master picked)
    {
        if (Rank <= 0 || Time.time < guardUntil)
            return false;
        long owner = ModKitUtil.Owner(Master);
        YosseleBuilding target = picked != null ? picked.GetComponent<YosseleBuilding>() : null;
        if (picked != null && (target == null || target.Owner != owner))
        {
            YosseleUtil.Warn(Master, YosseleData.WarnNotYours);
            return false;
        }
        if (target == null)
        {
            float best = YosseleData.FertilizeReach;
            foreach (YosseleBuilding b in YosseleServer.Buildings)
            {
                if (b == null || !b.Started || b.Owner != owner || b.FullGrown || !ModKitUtil.Alive(b.Master))
                    continue;
                float d = ModKitUtil.Flat(b.Pos, ModKitUtil.Pos(Master));
                if (d <= best)
                {
                    best = d;
                    target = b;
                }
            }
        }
        // A picked building not begun yet, or felled, can't grow: Grow refused and the fertilizer was spent (MAX YOS-5).
        if (target == null || !target.Started || !ModKitUtil.Alive(target.Master))
        {
            YosseleUtil.Warn(Master, YosseleData.WarnNoTarget);
            return false;
        }
        if (target.FullGrown)
        {
            YosseleUtil.Warn(Master, YosseleData.WarnFullGrown);
            return false;
        }
        if (Player.Fertilizer <= 0)
        {
            YosseleUtil.Warn(Master, YosseleData.WarnNoFertilizer);
            return false;
        }
        guardUntil = Time.time + YosseleData.FertilizeGuard;
        YosseleServer.AddFertilizer(owner, -1);
        Player.Spent++;
        target.Grow(1, true);
        Fertilizes++;
        Vector3 dir = target.Pos - ModKitUtil.Pos(Master);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            Master.Entitydirection.Value = dir.normalized;
        Master.Radio.Vis_ClientRpc("Yossele_Fertilize", target.Pos, (float)target.Master.NetworkObjectId);
        return true;
    }
}
