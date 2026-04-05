---
name: ci-validation-gates
description: Defensive CI/CD patterns for NuGet publishing and .NET builds
domain: ci-cd
confidence: high
source: established for Squad.SDK.NET
---

## Context

CI workflows must be defensive. These patterns ensure reliable builds and NuGet publishing.

## Patterns

### Version Validation Gate
Every publish workflow MUST validate version format before `dotnet nuget push`.

```yaml
- name: Validate semver
  run: |
    VERSION="${{ github.event.release.tag_name }}"
    VERSION="${VERSION#v}"
    if [[ ! "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9.]+)?$ ]]; then
      echo "❌ Invalid semver: $VERSION"
      exit 1
    fi
    echo "✅ Valid semver: $VERSION"
```

### NuGet API Key Verification
- Use a NuGet API key scoped to only the packages you publish
- Store as `NUGET_API_KEY` in GitHub Secrets
- Never use organization-wide keys

### Build Validation Before Publish
```yaml
- name: Build and test
  run: |
    dotnet build --configuration Release
    dotnet test --configuration Release --no-build
- name: Pack
  run: dotnet pack --configuration Release --no-build
- name: Push
  run: dotnet nuget push **/*.nupkg --source https://api.nuget.org/v3/index.json --api-key ${{ secrets.NUGET_API_KEY }}
```

### AOT Compatibility Check
```yaml
- name: Verify AOT compatibility
  run: dotnet build -p:IsAotCompatible=true --no-incremental 2>&1 | grep -c 'IL[0-9]\{4\}'
```

## Anti-Patterns
- ❌ Publishing without building and testing first
- ❌ Publishing without version validation
- ❌ Hard-coded secrets in workflows
- ❌ Skipping AOT compatibility verification
