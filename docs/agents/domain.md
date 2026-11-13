# Domain Docs

How Matt Pocock's engineering skills read this repository's domain documentation. Layout: **single-context**.

## Before exploring, read these

- **`CLAUDE.md`**, then **`docs/PLAN.md`** Part C "Now": the map and the state. They win over anything below.
- **`GLOSSARY.md`** at the repository root, if it exists.
- **`docs/adr/`**, if it exists: the ADRs that touch the area you are about to work in.
- Until those two exist, the words and decisions live here: names in `docs/STYLE.md` and `CLAUDE.md` "Rules the code
  doesn't show" (Trinkets, Cardinals), what to build in `docs/scope/`, how the game works in `TDS_Vessel_Creator/`,
  decisions in `docs/PLAN.md` Part C "Decisions".

If `GLOSSARY.md` or `docs/adr/` doesn't exist, **proceed silently**. Don't flag the absence and don't suggest creating
them upfront. The `domain-modeling` skill (reached through `grill-with-docs` and `improve-codebase-architecture`)
creates them when a term or a decision is actually resolved.

## File structure

```
/
├── GLOSSARY.md        (not there yet)
├── docs/adr/          (not there yet)
│   └── 0001-<decision>.md
└── TDS_Mod/, TDS_Vessel_Creator/, docs/ ...
```

## Use the project's vocabulary

When your output names a domain concept (an issue title, a refactor proposal, a hypothesis, a test name), use the term
as `GLOSSARY.md`, the scope and `docs/STYLE.md` define it. Don't drift to a synonym they avoid: **Trinkets**, never
"Vessel Cardinals"; **Cardinals**, never "Team Cardinals".

If the concept you need isn't defined yet, either you are inventing language the project doesn't use (reconsider) or
there is a real gap (note it for `domain-modeling`).

## Flag conflicts

If your output contradicts an ADR, the scope or a line of Part C "Decisions", say so instead of overriding it silently:

> _Contradicts ADR-0007 (<title>), but worth reopening because…_
