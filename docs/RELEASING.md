# Releasing AppKit

Releases follow the
[KofTwentyTwo release standard](https://github.com/KofTwentyTwo/standards/blob/main/standards/releases.md).
Pushing a protected `v*` tag on `main` runs [`.github/workflows/release.yml`](../.github/workflows/release.yml),
which calls the shared `release-nuget.yml` workflow in KofTwentyTwo/standards. All four
packages always release together under one version.

> **Status:** the shared `release-nuget.yml` workflow is pinned to the
> `feat/dotnet-workflows` branch of KofTwentyTwo/standards while that pull request is
> under review. Repin `release.yml` (and `ci.yml`) to the merged commit before cutting
> the first release.

## Versioning

The version is the tag without the leading `v`, in strict [semver](https://semver.org).

| Tag | Version | GitHub Release | nuget.org |
| --- | --- | --- | --- |
| `v1.2.3` | `1.2.3` | stable | stable |
| `v1.2.3-beta.1` | `1.2.3-beta.1` | prerelease | prerelease |

AppKit is a library every app depends on, so semver is a promise:

- **major**: any breaking change to a public API
- **minor**: new capabilities, backward compatible
- **patch**: fixes only

`Directory.Build.props` holds `VersionPrefix`, the next expected release; local builds
are `<prefix>-dev`. Bump the prefix right after each release, in a pull request.

## What a release does

The shared workflow (SLSA Build L3: the build definition lives in KofTwentyTwo/standards,
not here):

1. Validates the tag and re-runs every gate that guards `main`: locked restore,
   zero-warning build, unit tests, and the 100% coverage gate.
2. Packs the four packages and their symbol packages.
3. Generates a CycloneDX 1.6 SBOM per package (Syft, from `packages.lock.json`) and
   attests it.
4. Writes `SHA256SUMS` over every asset and attests build provenance.
5. Creates a draft GitHub Release with the assets and generated notes, then publishes it
   (releases are immutable).

Then the `publish` job in this repository's `release.yml` downloads the packages,
verifies their attestations were signed by the shared workflow, and pushes them to
nuget.org through **trusted publishing** (OIDC; no API key is stored anywhere).

## One-time setup (owner)

1. **nuget.org trusted publishing policy:** nuget.org → your profile → *Trusted
   Publishing* → *Add policy*:
   - Repository owner: `KofTwentyTwo`
   - Repository: `AppKit`
   - Workflow file: `release.yml`
   - Environment: `release`
2. **Repository variable** `NUGET_USER` (Settings → Secrets and variables → Actions →
   Variables): your nuget.org profile name. Without it the `publish` job is skipped and
   the release still succeeds on GitHub.
3. **Environment** `release` (Settings → Environments) with a deployment rule that allows
   only tags matching `v*`.

## Cutting a release

1. Make sure `main` is green and the conformance check passes:
   `pwsh ../standards/tools/Test-RepoConformance.ps1 -Repository KofTwentyTwo/AppKit -LocalPath .`
2. For a minor or major release, open a release checklist issue from the standards
   template and complete it (security assessment, threat model review, scan results).
3. Tag `main` and push the tag:

   ```powershell
   git switch main
   git pull
   git tag -s v0.1.0 -m "v0.1.0"
   git push origin v0.1.0
   ```

4. Watch the **release** workflow, then verify the release as described in the
   [README](../README.md#verifying-a-release).
5. Open a pull request that bumps `VersionPrefix` in `Directory.Build.props`.

## When something fails

- **Before the release is published:** nothing is public. Delete the draft
  (`gh release delete v0.1.0 --yes`), fix the cause, and re-run the workflow.
- **At `publish (nuget.org)`:** the GitHub release is published and immutable; re-run
  only the failed `publish` job from the Actions page.
- **A published release is broken:** never reuse its version. Fix forward with a new
  patch release, and unlist the bad version on nuget.org.
