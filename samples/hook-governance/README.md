# hook-governance

> **Difficulty**: Intermediate

Demonstrates the four governance hooks built into the Squad SDK: file-write guards, PII scrubbing, reviewer lockout, and ask-user rate limiting. No `GITHUB_TOKEN` required.

## Quick start

```bash
cd samples/hook-governance
dotnet run
```

## What you'll learn

- `PolicyConfig.AllowedWritePaths` — block writes outside approved paths
- `PiiScrubberHook.ScrubPii()` — redact emails, phone numbers, SSNs
- `ReviewerLockoutHook` — prevent a rejected reviewer from re-editing a file
- `PolicyConfig.MaxAskUserPerSession` — cap user-prompt calls per session

## Expected output

```
  Demo 1 - File-Write Guards
  Write to src/utils/helper.cs                      allow [OK]
  Write to /etc/passwd                              block [X]
             => Write to '/etc/passwd' is not within allowed paths.

  Demo 2 - PII Scrubbing
  Before: Deploy fix by brady@example.com
  After:  Deploy fix by [EMAIL REDACTED]

  Demo 3 - Reviewer Lockout
  backend    edits src/auth.cs            block [X]
  frontend   edits src/auth.cs            allow [OK]

  Demo 4 - Ask-User Rate Limiter
    Ask #1: allow [OK]
    Ask #2: allow [OK]
    Ask #3: allow [OK]
    Ask #4: block [X]
    Ask #5: block [X]
```

## Next steps

- [skill-discovery](../skill-discovery/README.md) — team knowledge sharing
