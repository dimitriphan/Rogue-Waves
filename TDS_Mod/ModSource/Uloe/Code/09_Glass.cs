// Magnifying Glass: focuses a beam on the nearest foe in Range, and stays on it while it stays in Range. The beam is the
// stock Projectile_Ai_Beam (Buster's Totem and Fie's wall fire it): one pulse a shot, the hit taking this tower's Damage
// stat, on which Light is a held Multi (+20% per Light, +40% with Polished Lens). Burning Point adds another held share per
// beam on the same target; a new target starts over. Death Ray's share is kept from target to target and cools only after
// a few seconds with nothing to beam. The lens turns to its target on every screen (UloeClient).
//   Prism        two more beams at the foes nearest its target, for half damage each, dealt here (the beam drawn is a look).
//   Focal Point  the beam burns every foe on its line out to Range at full damage (and Prism's side beams theirs).
//   Sunspot      its target is Illuminated by the Light the lens receives (UloeData.SunspotLight).
public class UloeGlassBrain : UloeBuilding
{
    public Entity_Master Target;
    public float Ramp, LastBeam;
    public int Beams, Resets, Sides, Pierced, Sunspots;
    float nextShot, nextLook;
    Link_Modifier damageMod;
    static readonly List<Entity_Master> burned = new List<Entity_Master>();

    public float PerLight { get { return Param("Perlight_Base", UloeData.GlassPerLight); } }
    public float Multi { get { return PerLight * Light + Ramp; } }
    public bool Deathray { get { return Has("Deathray"); } }
    public float RampMax { get { return Deathray ? Param("Deathmax_Base", 2.5f) : Param("Rampmax_Base", 1.2f); } }
    public bool FullHeat { get { return Deathray && Ramp >= RampMax - 0.0001f; } }

    // Sunspot's Illuminate level from the Light the lens receives: 0 below the first step.
    public int SunspotLevel
    {
        get
        {
            int level = 0;
            for (int i = 0; i < UloeData.SunspotLight.Length; i++)
                if (Light >= UloeData.SunspotLight[i] - 0.0001f)
                    level = i + 1;
            return level;
        }
    }

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
        Entity_Master foe = Target != null && ModKitUtil.IsFoe(Target, Team) && ModKitUtil.Flat(ModKitUtil.Pos(Target), Pos) <= range ? Target : null;
        if (foe == null)
        {
            List<Entity_Master> foes = Foes(range);
            if (foes.Count > 0)
                foe = foes[0];
        }
        if (foe == null)
        {
            // Death Ray keeps its heat through a short gap; everything else cools at once.
            if (Ramp > 0f && (!Deathray || Time.time - LastBeam >= Param("Deathidle_Base", 3f)))
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

    // Light and the ramp on the Damage stat: one modifier, changed in place, so the panel shows the Damage it deals.
    void Refresh() { damageMod = ModKitUtil.Keep(Master, damageMod, "Damage", Helper_Entitytools.Modtypes.Multi, Multi); }

    void Fire(Entity_Master foe)
    {
        if (Deathray)
            Ramp = Mathf.Min(RampMax, Ramp + Param("Deathramp_Base", 0.12f));
        else if (foe != Target)
        {
            if (Ramp > 0f) Resets++;
            Ramp = 0f;
        }
        else if (Has("Burn"))
            Ramp = Mathf.Min(RampMax, Ramp + Param("Ramp_Base", 0.12f));
        Target = foe;
        LastBeam = Time.time;
        Refresh();
        float range = Stat("Range", 4.5f);
        bool focal = Has("Focal");
        burned.Clear();
        // With Focal Point the line does the damage (the target is on it), so the beam drawn only looks.
        Beam(foe, !focal);
        if (focal)
            ModKitUtil.Vis(Master, "Uloe_Line", Line(foe, range, 1f), 2f);
        if (Has("Prism"))
            Prism(foe, range, focal);
        if (Has("Sunspot") && ModKitUtil.Alive(foe))
        {
            int level = SunspotLevel;
            if (level > 0)
            {
                UloeMarks.Illuminate(Master, foe, level);
                Sunspots++;
            }
        }
        if (FullHeat && ModKitUtil.Alive(foe))
            UloeMarks.Illuminate(Master, foe, UloeData.DeathMark);
        Beams++;
        Master.Radio.Vis_ClientRpc("Uloe_Beam", ModKitUtil.Pos(foe), Multi);
    }

    Vector3 Lens { get { return Pos + Vector3.up * UloeData.BeamEye; } }

    // The stock beam at a foe; 'hit' false: Interact 0, only its look.
    void Beam(Entity_Master foe, bool hit)
    {
        if (!UloeContent.BeamReady)
        {
            if (hit)
                ModKitCombat.HurtWithStat(Master, foe, 1f);
            return;
        }
        Projdata p = new Projdata();
        // Lifetime = 1 / Tickrate: one pulse, then the beam dies. Collide 0: what the Range says counts, a hill doesn't stop it.
        p.Params = new Dictionary<string, float>
        {
            { "Lifetime", UloeData.BeamLifetime }, { "Tickrate", UloeData.BeamTick }, { "Trackspeed", 100f },
            { "Maxrange", Stat("Range", 4.5f) + 1.5f }, { "Interact", hit ? 1f : 0f }, { "Killontarget", 1f }, { "Track", 1f }, { "Collide", 0f },
        };
        p.Paramsobject = new Dictionary<string, Entity_Master> { { "Goal", foe } };
        Helper_Damagetools.Projectile(Master, UloeData.Beam, Lens, p, "Hurt");
    }

    // Every foe within FocalWidth of the line from the lens through 'through', out to the Range, takes 'share' of the
    // Damage stat once a shot. Returns the line's far end, for its look.
    Vector3 Line(Entity_Master through, float range, float share)
    {
        Vector3 to = ModKitUtil.Pos(through), d = to - Pos;
        d.y = 0f;
        Burn(through, share);
        if (d.sqrMagnitude < 0.0001f)
            return to;
        Vector3 end = Pos + d.normalized * range;
        end.y = to.y;
        foreach (Entity_Master f in Foes(range + 0.5f))
            if (UloeUtil.SegmentDistance(ModKitUtil.Pos(f), Pos, end) <= UloeData.FocalWidth)
                Burn(f, share);
        return end;
    }

    void Burn(Entity_Master foe, float share)
    {
        if (burned.Contains(foe) || !ModKitUtil.Alive(foe))
            return;
        burned.Add(foe);
        ModKitCombat.HurtWithStat(Master, foe, share);
        Pierced++;
    }

    // The side beams: at the foes nearest the target, within Prismradius of it.
    void Prism(Entity_Master target, float range, bool pierce)
    {
        int n = Mathf.RoundToInt(Param("Prism_Base", 2f));
        float share = Param("Prismshare_Base", 0.5f);
        Vector3 at = ModKitUtil.Pos(target);
        foreach (Entity_Master f in ModKitUtil.FoesNear(Master, at, Param("Prismradius_Base", 1.5f)))
        {
            if (n <= 0)
                break;
            if (f == target || !ModKitUtil.Alive(f))
                continue;
            n--;
            Sides++;
            Vector3 end = ModKitUtil.Pos(f);
            if (pierce)
                end = Line(f, range, share);
            else if (!burned.Contains(f))
            {
                burned.Add(f);
                ModKitCombat.HurtWithStat(Master, f, share);
            }
            ModKitUtil.Vis(Master, "Uloe_Line", end, 1f);
        }
    }
}
