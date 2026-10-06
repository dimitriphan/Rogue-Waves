# 07 · Online play

TDS uses Unity Netcode for GameObjects over Steam, host + clients. A vessel mod works online when every player builds
the **same content under the same names**, and the **host decides** everything while clients only draw. Sprigg 1.1
was played online by the user and a friend and worked; Argo and Mummer use the same architecture.

## 1. Why a mod vessel can work online at all

- Every entity spawns from **one generic network prefab**; the network carries only the being's name
  (`Entity_Master.Being`), and each machine looks it up in its own `Bl_Address.Groupsname`. No new network prefab or
  registration is needed, only identical data on every machine.
- Vessel choice travels as text (`Tds_Clientinfo.Hero`); the host's `Changehero_ServerRpc` only checks the hero exists.
- Being modded doesn't block lobbies (it only disables leaderboard validation).

## 2. The rules

1. **Same mod, same version, enabled, on every machine** (host included). Say so in the player README.
2. **Deterministic content build.** Everything registered at boot is built from data the same way on every machine: no
   random numbers, no time- or machine-dependent choices, no iteration over unordered sources that could differ.
   Every id carries the vessel name and is ≤ 60 characters.
3. **The host is the authority.** Brains are added in `Onspawn` only when `Game_Entity.Instance.IsServer`; all rules,
   timers, resources and decisions run there.
4. **Change state only through networked channels**: spawn/despawn beings, stat modifiers, skins (`Master.Skin`),
   upgrades, commands (`Commandlist`), debuff looks (`Vfxdebuffcheck`), Vis RPCs. A field on a brain is invisible to
   clients.
5. **Clients draw.** `<Name>Client` (runs everywhere, host too) only reacts to Vis events and synced values: sounds,
   particles, lines, animations, warnings. It never changes stats, spawns, or decides anything.
6. **Client input = a command to the host.** Buttons and pickers send `Remote.Command_ServerRpc(link, json)`; the host
   validates everything (owner, team, range, cooldown, cost) and may refuse with an owner-only warning.
7. **What a client's UI must show comes from synced data**: a button's state in the command's `Goalstring`/`Goalvalue`
   (synced through `Commandlist` with `Updatecommand(m, link, "", false, false)`), a resource as a Counter stat, a mode
   as a skin. Clients see `Taglist`, not `Taglistserver`.
8. **Persistent things in the world are networked beings** (leftovers, ground effects, units), not client-side models.
9. **Status looks use the game's debuff system** with `target.Beingref.Value`: attached on every client, timed by the
   game. (With `Beingrefparallel`, nothing appeared on any client.)
10. **Per-player state** is keyed by the owner's session id (`Entitydata.Value.Owner`). Two players may pick the same
    vessel; one player's resource or unlocks must never leak to the other.
11. **Owner-only messages**: send a Vis code; the client shows it only if `OwnedLocally(master)`.
12. **No timing assumptions across machines**: `Time.time` differs per machine; the host's timers decide, clients
    animate with their own clocks.
13. **Teammates**: decide whether abilities affect teammates' entities (Argo's Nauts tune any tower on the team;
    Mummer's Scraps go to the owner of the best Loom in range). Make it explicit in the design.

## 3. Checklist before shipping

- [ ] Every id, tag, icon, effect and sound name contains the vessel name; ids ≤ 60 chars.
- [ ] `grep` the code for `Random` in the content build: none.
- [ ] Every brain and every state change is behind `IsServer`.
- [ ] Every visual a player relies on is a Vis event, a debuff look, a skin, a stat or a networked being.
- [ ] Every command handler validates owner, team, range, cooldown and cost on the host.
- [ ] Button state is in the command's Goalstring/Goalvalue, synced with `Updatecommand(..., false, false)`.
- [ ] Per-player dictionaries keyed by owner; nothing global per vessel type.
- [ ] Debuffs use `Beingref.Value`.
- [ ] The self-test passes (it runs as host, which also runs the client code).
- [ ] If the vessel adds a new kind of sync (a new channel or a client-driven picker), ask the user to try a match with
      a friend and send both Player.log files.

## 4. Testing online

The automated self-test runs solo as host: it proves the host logic and the host's client code. It can't prove a
remote client sees the same; the rules above are what make that true. For a real check, the user hosts, a friend
joins, both pick (ideally both the new vessel), play a few Tides, and send both `Player.log` files. Look for the
vessel's `[<Name>]` lines on the client, `Could not retrieve` warnings (a name the client doesn't have) and exceptions.
