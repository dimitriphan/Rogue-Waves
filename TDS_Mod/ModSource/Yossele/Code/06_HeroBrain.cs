// Server-side brain for the vessel: build options (the Dandelion Tree's button goes while one stands), regeneration,
// the fertilizer counter and FERTILIZE. It plays the part of a vessel's own AI (Entity_Ai_Hero_<Name>). Attacking is
// left to the stock Entity_Ai_Mob_Combat, exactly like Smacky; movement, building and selling are the engine's generic
// hero AIs.
public class YosseleHeroBrain : MonoBehaviour
{
    public Entity_Master Master;
    public YosselePlayer Player;
    public int Rank, Fertilizes, Healed, DandelionHidden, DandelionShown;
    public float Regenerated;
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
        if (!YosseleUtil.IsServer)
            return;
        try
        {
            if (!started)
            {
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || Master.Radio == null)
                    return;
                Begin();
            }
            if (Time.time >= nextRegen)
            {
                nextRegen = Time.time + 1f;
                Regen();
            }
            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + 0.5f;
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
        Player = YosseleServer.Player(YosseleUtil.Owner(Master));
        Player.Hero = this;
        CheckDandelion();
        CheckFertilize();
        // Subscribed after the Updatecommand calls above, which run each command once.
        Master.Command += OnCommand;
        YosseleLog.Info("Yossele landed for player " + YosseleUtil.Owner(Master));
    }

    void BuildOption(int i)
    {
        string link = "Buildoption" + (i + 1);
        Link_Command c = Helper_Entitytools.Createcommand(Master, "Command_" + link, link, false, true);
        c.Goalstring = YosseleData.Towers[i];
        c.Goalvalue = 1f;
        Helper_Entitytools.Updatecommand(Master, link, "", true, true);
    }

    // "More health regen" (scope): no base vessel regenerates, so the brain mends Yossele once a second while hurt.
    void Regen()
    {
        if (!YosseleUtil.Alive(Master))
            return;
        float missing = YosseleUtil.MaxHealth(Master) - YosseleUtil.Health(Master);
        if (missing <= 0.01f)
            return;
        float amount = Mathf.Min(missing, YosseleData.Regen);
        YosseleUtil.Heal(Master, Master, amount);
        Healed++;
        Regenerated += amount;
    }

    // ---------------------------------------------------------------- the Dandelion Tree's limit

    // Its button goes while this player's tree stands and comes back when it is gone (Argo's Miner's Base, after
    // Entity_Ai_Hero_Boutman.Checkbouthouses). The server checks build commands against the vessel's own build options,
    // so a removed button can't be built from either.
    public void CheckDandelion()
    {
        if (!started || Master == null || Master.Commandlistserver == null)
            return;
        bool has = YosseleServer.HasDandelion(YosseleUtil.Owner(Master));
        bool shown = Master.Commandlistserver.ContainsKey(YosseleData.DandelionOption);
        if (has && shown)
        {
            Helper_Entitytools.Removecommand(Master, YosseleData.DandelionOption);
            DandelionHidden++;
        }
        else if (!has && !shown)
        {
            BuildOption(Array.IndexOf(YosseleData.Towers, YosseleData.Dandelion));
            DandelionShown++;
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
        return YosseleUtil.ById(id);
    }

    // Spends 1 fertilizer for +1 Growth on one of this player's Yossele buildings. From the hotkey with nothing picked:
    // the nearest of theirs that can still grow, within reach.
    public bool Fertilize(Entity_Master picked)
    {
        if (Rank <= 0 || Time.time < guardUntil)
            return false;
        long owner = YosseleUtil.Owner(Master);
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
                if (b == null || !b.Started || b.Owner != owner || b.FullGrown || !YosseleUtil.Alive(b.Master))
                    continue;
                float d = YosseleUtil.Flat(b.Pos, YosseleUtil.Pos(Master));
                if (d <= best)
                {
                    best = d;
                    target = b;
                }
            }
        }
        if (target == null)
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
        Vector3 dir = target.Pos - YosseleUtil.Pos(Master);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            Master.Entitydirection.Value = dir.normalized;
        Master.Radio.Vis_ClientRpc("Yossele_Fertilize", target.Pos, (float)target.Master.NetworkObjectId);
        return true;
    }
}
