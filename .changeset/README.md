# Changesets

Squad.SDK.NET uses a `.changeset/` folder to track release-worthy changes on `dev`.

Each release-relevant PR should add a markdown file with frontmatter targeting the NuGet package:

```md
---
"Squad.SDK.NET": minor
---

Short human-readable summary of the change.
```

Supported bump types are `patch`, `minor`, and `major`.

For this single-package .NET repo, changesets are applied when `dev` is promoted to `preview`:

1. Pending changesets are read.
2. The SDK version is bumped in `src/Squad.SDK.NET/Squad.SDK.NET.csproj`.
3. `CHANGELOG.md` gets a new release entry.
4. Applied changeset files are removed from the promoted branch.

`main` then tags and publishes that release automatically.
