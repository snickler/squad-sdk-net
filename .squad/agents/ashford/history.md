## Learnings

- **Upstream sync workflows** — Created a DRY reusable workflow pattern (`sync-check.yml` + thin callers) to monitor `bradygaster/squad` dev and insiders branches. State is tracked via HTML comments in issue bodies (`<!-- upstream-sha: ... -->`), which is inspectable and requires no external storage. Sync labels are managed both in the workflow (auto-create safety net) and centrally in `sync-squad-labels.yml`.
- **Input injection safety** — Workflow inputs should be passed via `env:` block, not string-interpolated into `actions/github-script` JS. Prevents script injection if inputs contain quotes or JS syntax.
- **GitHub issues API returns PRs** — `issues.listForRepo` includes pull requests. Must filter with `!i.pull_request` when searching for actual issues.
- **Race-safe label creation** — When two workflows may create the same label concurrently, catch 422 errors on `createLabel` to avoid failures from duplicates.
- **Concurrency groups** — Added per-branch concurrency to avoid duplicate issue creation from overlapping scheduled runs.
