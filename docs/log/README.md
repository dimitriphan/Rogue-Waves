# The Log

The project's dated history, one file per pull request (Dimitri, 2026-11-13: token use and no more merge clashes).
It was a section of `docs/PLAN.md` until then; that history is `2026-10-08_archive.md`, frozen.

- **Add:** each PR adds one new file, `<YYYY-MM-DD>_<last part of the branch name>.md`, and edits no other file here.
  First line: `- **YYYY-MM-DD: what changed** (Dimitri: "his words"): result; compiled here / not run on the PC; kit
  or pitfall used.` The detail goes under it. Two PRs never touch the same file, so the Log never conflicts.
- **Read:** never the folder or the archive whole (the archive is about 180 KB). `ls docs/log | tail` for the newest,
  `grep -rn "<word>" docs/log` for a subject, then open the one file or the lines around the hit.
- **State is not here:** what is true now lives in `docs/PLAN.md` Part C "Now".
