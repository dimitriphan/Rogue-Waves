// Magnifying Glass: focuses a beam on the nearest foe in Range, and stays on it while it stays in Range. The beam is the
// stock Projectile_Ai_Beam (Buster's Totem and Fie's wall fire it): one pulse a shot, the hit taking this tower's Damage
// stat, on which Light is a held Multi (+20% per Light). Burning Point adds another held share per beam on the same target;
// a new target starts over. The lens turns to its target on every screen (UloeClient).
public class UloeGlassBrain : UloeBuilding
{
    public Entity_Master Target;
    public float Ramp;
    public int Beams, Resets;
    float nextShot, nextLook;
    Link_Modifier damageMod;

    public float Multi { get { return UloeData.GlassPerLight * Light + Ramp; } }

    protected override void Tick()
    {
        if (Time.time >= nextLook)
        {
            nextLook = Time.time + 0.25f;
            Refresh();
        }
        float aspd = Stat("Attackspeed", 0.75f);
        if (aspd <= 0f || Time.time < nextShot)
            return;
        float range = Stat("Range", 4.5f);
        Entity_Master foe = Target != null && UloeUtil.IsFoe(Target, Team) && UloeUtil.Flat(UloeUtil.Pos(Target), Pos) <= range ? Target : null;
        if (foe == null)
        {
            List<Entity_Master> foes = Foes(range);
            if (foes.Count > 0)
                foe = foes[0];
        }
        if (foe == null)
        {
            if (Ramp > 0f)
            {
                Resets++;
                Ramp = 0f;
                Refresh();
            }
            Target = null;
            return;
        }
        nextShot = Time.time + 1f / aspd;
        Fire(foe);
    }

    // Light and Burning Point on the Damage stat: one modifier, changed in place, so the panel shows the Damage it deals.
    void Refresh() { damageMod = UloeUtil.Keep(Master, damageMod, "Damage", Helper_Entitytools.Modtypes.Multi, Multi); }

    void Fire(Entity_Master foe)
    {
        if (foe != Target)
        {
            if (Ramp > 0f) Resets++;
            Ramp = 0f;
            Target = foe;
        }
        else if (Has("Burn"))
            Ramp = Mathf.Min(Param("Rampmax_Base", 1.2f), Ramp + Param("Ramp_Base", 0.12f));
        Refresh();
        Vector3 from = Pos + Vector3.up * UloeData.BeamEye;
        if (UloeContent.BeamReady)
        {
            Projdata p = new Projdata();
            // Lifetime = 1 / Tickrate: one pulse, then the beam dies. Collide 0: what the Range says counts, a hill doesn't stop it.
            p.Params = new Dictionary<string, float>
            {
                { "Lifetime", UloeData.BeamLifetime }, { "Tickrate", UloeData.BeamTick }, { "Trackspeed", 100f },
                { "Maxrange", Stat("Range", 4.5f) + 1.5f }, { "Interact", 1f }, { "Killontarget", 1f }, { "Track", 1f }, { "Collide", 0f },
            };
            p.Paramsobject = new Dictionary<string, Entity_Master> { { "Goal", foe } };
            Helper_Damagetools.Projectile(Master, UloeData.Beam, from, p, "Hurt");
        }
        else
            ModKitCombat.HurtWithStat(Master, foe, 1f);
        Beams++;
        Master.Radio.Vis_ClientRpc("Uloe_Beam", UloeUtil.Pos(foe), Multi);
    }
}
