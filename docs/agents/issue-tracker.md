# Issue tracker: GitHub

Issues and specs for this repository live as GitHub issues on `dimitriphan/Rogue-Waves`. The project's state stays in
`docs/PLAN.md` Part C: an issue holds a skill's ticket or spec, it does not replace "Now" or the Log.

A cloud thread has no `gh` CLI: use the GitHub MCP tools (`mcp__github__*`; load them with ToolSearch). A Remote
Control session on Dimitri's PC may use `gh` instead. Every comment Claude posts ends with the session's footer.

## Conventions

- **Create an issue**: `issue_write` with `method: "create"`, `title`, `body`, `labels`.
- **Read an issue**: `issue_read` (the issue, its comments, its labels, its sub-issues).
- **List issues**: `list_issues` with `state` and `labels`; ask only for the `fields` you need.
- **Make an issue a sub-issue of a parent**: `issue_write` create with `parent_issue_number`, or `sub_issue_write`
  afterwards. If that fails, put `Part of #<parent>` at the top of the child's body.
- **Comment on an issue**: `add_issue_comment`.
- **Apply or remove labels**: `issue_write` with `method: "update"` and the full `labels` list.
- **Close**: `add_issue_comment` with the reason, then `issue_write` update with `state: "closed"` and a
  `state_reason`.
- **Labels that don't exist yet**: the GitHub tools here can't create a label. Check with `get_label`; if one is
  missing, ask Dimitri to add it at `https://github.com/dimitriphan/Rogue-Waves/labels` and carry on without it.

## Pull requests as a triage surface

**PRs as a request surface: no.** _(Set to `yes` if this repository treats external PRs as feature requests;
`triage` reads this flag.)_

Issues and PRs share one number space, so a bare `#42` may be either: try `pull_request_read`, then `issue_read`.

## When a skill says "publish to the issue tracker"

Create a GitHub issue.

## When a skill says "fetch the relevant ticket"

Read it as in **Read an issue** above.

## Wayfinding operations

Used by `wayfinder`. The **map** is a single issue with **child** issues as tickets.

- **Map**: one issue labelled `wayfinder:map`, holding the Notes / Decisions-so-far / Fog body.
- **Child ticket**: a sub-issue of the map (see **Make an issue a sub-issue of a parent**), labelled
  `wayfinder:<type>` (`research`, `prototype`, `grilling`, `task`). Once claimed, it is assigned to `dimitriphan`.
- **Blocking**: a `Blocked by: #<n>, #<n>` line at the top of the child's body (the tools here don't reach GitHub's
  issue dependencies). A ticket is unblocked when every issue on that line is closed.
- **Frontier**: list the map's open children, drop any with an open blocker or an assignee; first in map order wins.
- **Claim**: `issue_write` update with `assignees: ["dimitriphan"]`, the session's first write.
- **Resolve**: `add_issue_comment` with the answer, close the issue, then append a pointer (gist and link) to the
  map's Decisions-so-far.
