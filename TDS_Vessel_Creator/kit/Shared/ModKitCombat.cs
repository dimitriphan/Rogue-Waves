// Kit 1.3 (2026-10-03). Shared vessel-mod kit: hits dealt by a mod's own code.
// The game's Hurt only applies Data["Damage"] x Data["Damagemulti"]; a hit without Damage deals nothing but still flashes.
// Interacthurt fills Damage from the attacker's stat and records Lasthit/Lasthittime only when the data has no Damage, and
// throws if Damagemulti is present. Lasthit gets the kill; Lasthittime is what an Aegis watches for regen. ModKitCombat.Hurt
// always sends both and records the hit. (Copied from the PC's ModSource\Shared; header comments abridged.)
public static class ModKitCombat
{
    // credit: who gets the kill, when it isn't the attacker.
    public static void Hurt(Entity_Master from, Entity_Master to, float damage, float multi, Entity_Master credit, Dictionary<string, float> extra)
    {
        if (from == null || to == null || damage <= 0f || multi <= 0f)
            return;
        Dictionary<string, float> d = extra != null ? new Dictionary<string, float>(extra) : new Dictionary<string, float>();
        d["Damage"] = damage;
        d["Damagemulti"] = multi;
        Mark(to, credit ?? from);
        Helper_Entitytools.Interact(from, to, "Hurt", d);
    }

    public static void Hurt(Entity_Master from, Entity_Master to, float damage) { Hurt(from, to, damage, 1f, null, null); }

    public static void HurtWithStat(Entity_Master from, Entity_Master to, float multi)
    {
        Link_Stats stat;
        if (from != null && from.Statsserver != null && from.Statsserver.TryGetValue("Damage", out stat))
            Hurt(from, to, stat.Value, multi, null, null);
    }

    // Records a hit the way Interacthurt does, for hits the game delivers itself (call from the attacker's Interact handler).
    public static void Mark(Entity_Master to, Entity_Master by)
    {
        if (to == null)
            return;
        if (by != null)
            to.Lasthit = by;
        to.Lasthittime = Time.fixedTime;
    }
}
