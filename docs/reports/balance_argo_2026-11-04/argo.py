"""Argo in the balance simulator (2026-11-04, ../ARGO_REVIEW_2026-11-04.md).

Read from the built Argo 2.0 (TDS_Mod/ModSource/Argo/Code, 02_Data.cs and the brains) and docs/scope/09. No game code
runs here. Every number is a tune knob, so the as-built kit, the kit as its text says, and the review's proposals all
run from one file:

- `slow_stack` (default True = as coded): Deep Water re-applies its slow every 0.25 s for 0.5 s and the Tremor every
  0.5 s for 0.75 s (11_Water.cs, 07_TowerBrain.cs), and Speed Multi mods sum, so a foe carries two water slows and on
  average 1.5 Tremor slows at once. False = one slow per source (what the text says).
- `ore_excess` (default False = as coded): the Drill's bite strikes ore with min(Damage, 1) (Helper_Damagetools), so
  Fathom yield past Damage 1 and Mantle Tap's x2 mine nothing. True = the excess goes in as Damageresource, as the
  Sapling, the Loom and Uloe already do.
- The proposals (`prop` list) are named in policies.ARGO_PROPS.

The lane model is sim.py's: a foe walks x tiles to the core; zones are stretches of x. Pools fill the merged stretch
first, then go up every lane; the Tremor is aimed at the merge (its circle covers the stretch on every lane there).
"""
import math

BOSS_HP = 600.0      # ArgoData.BossHealth: never reeled or pinned

def has(b, u):
    return b is not None and b.alive and u in b.up

def props(run):
    return run.tune.get('prop', [])

# ------------------------------------------------------------------------------------------------- make
def make(kind, cl, run, Bld, **kw):
    T = run.tune; P = props(run)
    if kind == 'scatter':
        b = Bld(kind, cl, 40, dict(dmg=1.25, aspd=0.6, rng=4.5, n=6, spread=50.0, pierce=1, bonus=T.get('scatter_bonus', 0.5),
                                   pinbonus=T.get('scatter_pinbonus', T.get('scatter_bonus', 0.5)), volleys=0, stored=0, idle=0.0))
    elif kind == 'harpoon':
        b = Bld(kind, cl, 40, dict(dmg=T.get('harpoon_dmg', 5.0), aspd=T.get('harpoon_aspd', 0.35), rng=6.5, pierce=3, shots=0))
    elif kind == 'water':
        b = Bld(kind, cl, 30, dict(slow=T.get('water_slow', 0.3), pools=1, noshoot=True))
    elif kind == 'drill':
        b = Bld(kind, cl, 40, dict(aspd=0.25, dmg=0.5, tremor=2.0, slow=T.get('tremor_slow', 0.2), lane=kw.get('lane', True),
                                   noshoot=kw.get('far', False), quaked=-1))
        b.growth = kw.get('fathom', 1)
    elif kind == 'base':
        b = Bld(kind, cl, 50, dict(nauts=2, tuned=[]))
    elif kind == 'vessel_a':
        b = Bld('vessel', cl, 65 + T.get('argo_hp', 0), dict(dmg=4.0, aspd=0.4, rng=1.75, respawn=10.0, cleave=0.6,
                                                             spot=kw.get('spot', run.m['merge'][cl])), unit=True)
        b.reach = 2.5
    else:
        raise KeyError(kind)
    return b

KINDS = ('scatter', 'harpoon', 'water', 'drill', 'base', 'vessel_a')

# ------------------------------------------------------------------------------------------------- the crew
def tuned(run, b):
    """Naut tuning on this tower: (attack speed multiplier, Old Salts on, Calibrate on)."""
    if run.tune.get('naut_off'): return 1.0, False, False
    for base in run.alive('base'):
        if b in base.p['tuned']:
            boost = 0.2 + (0.2 if 'Fullcrew' in base.up else 0.0)
            return 1.0 + boost, 'Salts' in base.up, 'Calibrate' in base.up
    return 1.0, False, False

def assign_nauts(run):
    """The player keeps each Naut on the tower doing the most work (re-picked each second; a Naut walks over)."""
    for base in run.alive('base'):
        n = base.p['nauts'] + (1 if 'Thirdmate' in base.up else 0)
        towers = [b for b in run.blds if b.alive and b.kind in ('scatter', 'harpoon')]
        towers.sort(key=lambda b: -b.dmg_done)
        drills = [b for b in run.blds if b.alive and b.kind == 'drill' and b.p['lane']]
        pick = towers[:n]
        if 'Salts' in base.up and drills and len(pick) == n and run.tune.get('naut_drill', False):
            pick[-1] = drills[0]
        base.p['tuned'] = pick

# ------------------------------------------------------------------------------------------------- zones
def lanes_into(run, cl):
    return sum(1 for ec in run.m['ecores'] for g in ec if g == cl) or 1

def water_zone(run, cl):
    """(lo, hi, lead pump) of the cluster's Deep Water: pools fill the merged stretch, then go up every lane."""
    pumps = [b for b in run.alive('water') if b.cl == cl]
    if not pumps: return None
    lead = max(pumps, key=lambda b: len(b.up))
    k = sum(b.p['pools'] for b in pumps)
    mrg = run.m['merge'][cl]
    if k <= mrg + 0.5:
        return (mrg + 0.5 - k, mrg + 0.5, lead)
    extra = (k - (mrg + 0.5)) / lanes_into(run, cl)
    return (0.0, mrg + 0.5 + extra, lead)

def fathom_max(b):
    return 7 if 'Deepcore' in b.up else 5

def drill_stats(run, b):
    F = min(b.growth, fathom_max(b)); below = F - 1; P = props(run)
    y = run.tune.get('fathom_yield', 0.75); sp = run.tune.get('fathom_speed', 0.1)
    dmg = 0.5 * (1 + y * below) * (2.0 if 'Mantle' in b.up and 'mantle_speed' not in P else 1.0)
    aspd = 0.25 * (1 + sp * below) * (run.tune.get('mantle_mult', 2.0) if 'Mantle' in b.up and 'mantle_speed' in P else 1.0)
    r = 2.0 + 0.4 * below + (0.5 if 'Seismic' in b.up else 0.0)
    s = b.p['slow'] + 0.04 * below + (0.1 if 'Seismic' in b.up else 0.0)
    _, salts, _ = tuned(run, b)
    if salts: s += 0.1
    return F, dmg, aspd, r, min(0.6, s)

def tremor_zone(run, b):
    if not b.p['lane']: return None
    F, dmg, aspd, r, s = drill_stats(run, b)
    mrg = run.m['merge'][b.cl]
    c = mrg + run.tune.get('tremor_at', 1.0)
    return (max(0.0, c - r), c + r, s)

def zones(run):
    """Per cluster: water (lo, hi, lead) and Tremors [(lo, hi, slow, drill)], a Tremor touching the water shaking it all."""
    Z = {}
    for cl in range(run.m['cores']):
        w = water_zone(run, cl)
        ts = []
        for d in run.alive('drill'):
            if d.cl != cl: continue
            z = tremor_zone(run, d)
            if z is None: continue
            lo, hi, s = z
            if w is not None and lo <= w[1] and hi >= w[0]:
                lo, hi = min(lo, w[0]), max(hi, w[1])
            ts.append((lo, hi, s, d))
        Z[cl] = (w, ts)
    run.azones = Z

def in_water(run, f):
    w = run.azones.get(f.core, (None, []))[0]
    return w is not None and w[0] <= f.x <= w[1], (w[2] if w else None)

def tremors_on(run, f):
    return [t for t in run.azones.get(f.core, (None, []))[1] if t[0] <= f.x <= t[1]]

# ------------------------------------------------------------------------------------------------- foes each step
def foe_step(run, f, dt):
    """Slows, holds and damage over time on one foe. Returns the speed multiplier (0 = held)."""
    t = run.t; T = run.tune; P = props(run)
    wet, lead = in_water(run, f)
    trem = tremors_on(run, f)
    total = 0.0
    stack = T.get('slow_stack', True)
    if wet:
        s = min(0.6, lead.p['slow'])
        total += s * (2.0 if stack else 1.0)
        f.wet_t = t + 0.3
        # Kelp Bed: caught on entering (again after 3 s out of the water)
        if 'Kelp' in lead.up and t - f.last_wet > 3.0 and f.mhp < BOSS_HP:
            hold = 0.75
            if 'kelp_pins' in P:
                pin(run, f, 0.5, stun=False)
            else:
                f.root_t = max(f.root_t, t + hold)
        f.last_wet = t
        brine = 0.5 if 'Brine' in lead.up else 0.0
        if 'Salt' in lead.up and t < f.pin_t:
            brine = (brine if brine > 0 else 0.5) * 2.0
        if brine > 0: run.hurt(f, brine * dt, lead, 'dot')
        if 'Trench' in lead.up and t >= lead.p.get('next_drag', 0.0):
            lead.p['next_drag'] = t + (6.0 if 'trench_more' not in P else 3.0)
            if f.mhp < BOSS_HP:
                if 'kelp_pins' in P: pin(run, f, 1.0, stun=False)
                else: f.root_t = max(f.root_t, t + 1.0)
    for (lo, hi, s, d) in trem:
        total += s * (1.5 if stack else 1.0)
        if 'Bedrock' in d.up and t < f.pin_t:
            run.hurt(f, T.get('bedrock_dps', 1.5) * dt, d, 'dot')
        # Mantle Tap's quake: once a Tide (or every 20 s with the proposal) when 5+ foes are in the Tremor
    for (until, amt) in f.a_slows:
        if until > t: total += amt
    f.a_slows = [x for x in f.a_slows if x[0] > t]
    f.a_slowed = total > 0
    if t < f.root_t or t < f.pin_t: return 0.0
    return max(0.1, 1.0 - total)

def drill_quakes(run):
    t = run.t; P = props(run)
    for d in run.alive('drill'):
        if 'Mantle' not in d.up: continue
        z = None
        for (lo, hi, s, dd) in run.azones.get(d.cl, (None, []))[1]:
            if dd is d: z = (lo, hi)
        if z is None: continue
        inside = [f for f in run.foes if f.hp > 0 and f.state != 'gone' and f.core == d.cl and z[0] <= f.x <= z[1]]
        every = 20.0 if 'mantle_every' in P else None
        ready = (d.p['quaked'] != run.tide) if every is None else (t >= d.p.get('next_quake', 0.0))
        if ready and len(inside) >= 5:
            d.p['quaked'] = run.tide; d.p['next_quake'] = t + (every or 0)
            for f in inside:
                if f.mhp < BOSS_HP: pin(run, f, 2.0, stun=False)

def debuffed(run, f):
    return f.a_slowed or run.t < f.pin_t

def pin(run, f, secs, stun):
    if f.mhp >= BOSS_HP: return
    if run.t < f.wet_t: secs *= 2.0
    f.pin_t = max(f.pin_t, run.t + secs)
    if stun: f.stun_t = max(f.stun_t, run.t + secs)

def crush(run, f):
    if run.t < f.wet_t:
        w = run.azones.get(f.core, (None, []))[0]
        if w is not None and 'Crushing' in w[2].up: return 1.15
    return 1.0

def slow_for(run, f, amt, secs):
    f.a_slows.append((run.t + secs, amt))

# ------------------------------------------------------------------------------------------------- buildings
def cover(run, b, rng):
    return run.cover(b.cl, rng)

def act_scatter(run, b, dt):
    import sim
    b.cd -= dt
    if b.cd > 0: return
    sp, salts, cal = tuned(run, b)
    rng = b.p['rng'] + (1.0 if cal else 0.0)
    mrg = run.m['merge'][b.cl]
    f = sim.target_first(run, b.cl, 0.0, cover(run, b, rng), mrg)
    lvl = 1 + len(b.up)
    if f is None:
        b.p['idle'] += dt
        if 'Double' in b.up and b.p['idle'] >= 1.0 / b.p['aspd']: b.p['stored'] = 1
        return
    b.p['idle'] = 0.0
    b.cd = 1.0 / (b.p['aspd'] * sp)
    volleys = 1 + b.p['stored']; b.p['stored'] = 0
    n = b.p['n'] + (2 if 'Widebore' in b.up else 0) + (3 if 'Grapeshot' in b.up else 0) + (2 if salts else 0)
    spread = b.p['spread'] + (25 if 'Widebore' in b.up else 0)
    pierce = 1 + (1 if 'Pressure' in b.up else 0)
    dmg = 1.25 + 0.25 * (lvl - 1) + (0.5 if 'Pressure' in b.up else 0) + (0.5 if 'Broadside' in b.up else 0)
    front = run.tune.get('front', 4.0)
    d = max(1.5, math.hypot(f.x - (mrg + front * 0.5), 1.5))
    w = 2 * d * math.tan(math.radians(spread) / 2)
    lw = run.tune.get('lane_width', 1.5) * (lanes_into(run, b.cl) if f.x <= mrg else 1)
    frac = min(1.0, lw / max(w, 0.1))
    crowd = [g for g in run.foes if g.hp > 0 and g.state != 'gone' and run.near(f, g, 0.75)]
    crowd.sort(key=lambda g: g.x)
    for _ in range(volleys):
        hits = n * frac
        whole = int(hits); part = hits - whole
        if run.rng.random() < part: whole += 1
        for i in range(whole):
            live = [g for g in crowd if g.hp > 0]
            if not live: break
            g = run.rng.choice(live[:max(1, len(live))])
            for k in range(pierce):
                tgt = g if k == 0 else next((h for h in live if h.x > g.x and h.hp > 0), None)
                if tgt is None: break
                bonus = b.p['pinbonus'] if run.t < tgt.pin_t else (b.p['bonus'] if debuffed(run, tgt) else 0.0)
                run.hurt(tgt, dmg * (1 + bonus), b)
                if 'Barnacle' in b.up: slow_for(run, tgt, 0.25, 1.5)
        b.p['volleys'] += 1
        if 'Broadside' in b.up and b.p['volleys'] % 4 == 0:
            for g in run.foes:
                if g.hp > 0 and g.state != 'gone' and g.core == b.cl and g.x <= cover(run, b, rng):
                    dd = max(1.0, math.hypot(g.x - (mrg + front * 0.5), 1.5))
                    p = min(1.0, 16 * 0.6 / (2 * math.pi * dd))
                    if run.rng.random() < p:
                        run.hurt(g, dmg * (1 + (b.p['bonus'] if debuffed(run, g) else 0)), b)

def whaler(b, f):
    return (1.0 + min(1.0, f.mhp / 200.0)) if 'Whaler' in b.up else 1.0

def reel(run, b, f, tiles):
    if f.mhp >= BOSS_HP or f.hp <= 0: return
    dirn = run.tune.get('reel_dir', 1.0)
    x0 = f.x; f.x = max(0.0, f.x + tiles * dirn)
    run.reels = getattr(run, 'reels', 0) + 1
    for d in run.alive('drill'):
        if 'Faultline' in d.up and d.cl == f.core:
            for (lo, hi, s, dd) in run.azones.get(d.cl, (None, []))[1]:
                if dd is d and (min(x0, f.x) <= hi and max(x0, f.x) >= lo):
                    run.hurt(f, run.tune.get('fault_dmg', 12.0), d)

def act_harpoon(run, b, dt):
    import sim
    b.cd -= dt
    if b.cd > 0: return
    sp, salts, cal = tuned(run, b)
    rng = b.p['rng'] + (2.0 if 'Longline' in b.up else 0.0) + (1.0 if cal else 0.0)
    mrg = run.m['merge'][b.cl]
    f = sim.target_first(run, b.cl, 0.0, cover(run, b, rng), mrg)
    if f is None: return
    b.cd = 1.0 / (b.p['aspd'] * sp)
    lvl = 1 + len(b.up)
    dmg = b.p['dmg'] + 2.0 * (lvl - 1) + (4.0 if 'Whaler' in b.up else 0.0)
    pierce = b.p['pierce'] + (3 if 'Skewer' in b.up else 0) + (2 if salts else 0)
    b.p['shots'] += 1
    lance = 'Leviathan' in b.up and b.p['shots'] % 5 == 0
    if lance: dmg *= 2.5; pierce = 99
    line = sorted([g for g in run.foes if g.hp > 0 and g.state != 'gone' and g.core == f.core and g.x >= f.x
                   and (g.lane == f.lane or g.x <= mrg) and g.x <= cover(run, b, rng * (2 if lance else 1))], key=lambda g: g.x)
    hit = line[:pierce]
    for i, g in enumerate(hit):
        run.hurt(g, dmg * whaler(b, g), b)
        if 'Tether' in b.up and g.hp > 0: slow_for(run, g, 0.3, 2.0)
    if not hit: return
    first = hit[0]
    if 'Anchor' in b.up and first.hp > 0:
        pin(run, first, 1.0, stun=first.x <= mrg + 1.5)
    tiles = 1.5 + (1.0 if salts else 0.0)
    if any(u in b.up for u in ('Reel', 'Twinline', 'Undertow')) and first.hp > 0:
        reel(run, b, first, tiles)
        if 'Undertow' in b.up:
            for g in run.foes:
                if g is not first and g.hp > 0 and g.state != 'gone' and run.near(first, g, 1.5): reel(run, b, g, 1.5)
    if 'Twinline' in b.up and len(line) > 1:
        second = line[1]
        run.hurt(second, dmg * whaler(b, second), b)
        if 'Tether' in b.up and second.hp > 0: slow_for(run, second, 0.3, 2.0)
        if second.hp > 0: reel(run, b, second, tiles)

def hpp(ore_per_hit, dmg):
    """Hits to a payout: ceil(4 / ore a hit) + ceil(1 / Damage) (AURA_BASELINE §1)."""
    return math.ceil(4.0 / ore_per_hit - 1e-9) + math.ceil(1.0 / dmg - 1e-9)

def bite_value(run, b):
    """A Drill bite in Damage-1 strikes (5 to a payout)."""
    F, dmg, aspd, r, s = drill_stats(run, b)
    ore = dmg if run.tune.get('ore_excess', False) else min(1.0, dmg)
    return 5.0 / hpp(ore, dmg)

def act_drill(run, b, dt):
    import sim
    b.cd -= dt
    if b.cd > 0: return
    F, dmg, aspd, r, s = drill_stats(run, b)
    b.cd = 1.0 / aspd
    sim.mine(run, b, bite_value(run, b))

def act_base(run, b, dt):
    pass

def act_vessel(run, b, dt):
    """Argo's swing: the stock melee, and a second foe within 1.25 tiles takes 60%."""
    import sim
    b.cd -= dt
    if b.cd > 0: return
    before = {id(f): f.hp for f in run.foes}
    sim.act_melee(run, b, dt, cd_done=True)
    hitf = [f for f in run.foes if id(f) in before and f.hp < before[id(f)]]
    if hitf:
        f = hitf[0]
        for g in run.foes:
            if g is not f and g.hp > 0 and g.state != 'gone' and run.near(f, g, 1.25):
                run.hurt(g, b.p['dmg'] * b.p['cleave'], b); break

ACT = {'scatter': act_scatter, 'harpoon': act_harpoon, 'water': act_base, 'drill': act_drill, 'base': act_base}

# ------------------------------------------------------------------------------------------------- Tide hooks
def on_tide(run, n):
    P = props(run)
    for d in run.alive('drill'):
        if d.stood:
            if 'Gusher' in d.up and d.growth >= 3:
                import sim
                g = run.tune['gusher_per_fathom'] * min(d.growth, fathom_max(d)) if 'gusher_per_fathom' in run.tune else run.tune.get('gusher', 12)
                sim.mine(run, d, g * bite_value(run, d))
            d.growth = min(fathom_max(d), d.growth + 1)
    for w in run.alive('water'):
        if 'Trench' in w.up and w.stood:
            w.p['pools'] += 1 if 'trench_more' not in P else 2

def on_fell(run, f):
    """Pay Dirt: a foe felled in its Tremor is dug up, 2 bites on the drill's ore."""
    for (lo, hi, s, d) in run.azones.get(f.core, (None, []))[1]:
        if 'Paydirt' in d.up and lo <= f.x <= hi:
            import sim
            sim.mine(run, d, run.tune.get('paydirt', 2) * bite_value(run, d))
            run.paydirt = getattr(run, 'paydirt', 0) + 1
