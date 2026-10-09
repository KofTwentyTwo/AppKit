# Releasing AppKit

Releases are automated by [`.github/workflows/release.yml`](../.github/workflows/release.yml).
Pushing a `v*` tag builds, tests, packs, attests, and publishes all four packages
together; they always share one version.

## Versioning

The version is the tag without the leading `v`, in strict [semver](https://semver.org).

| Tag | Version | GitHub Release | nuget.org |
| --- | --- | --- | --- |
| `v1.2.3` | `1.2.3` | stable | stable |
| `v1.2.3-beta.1` | `1.2.3-beta.1` | prerelease | prerelease |

AppKit is a library every app depends on, so semver is a promise:

- **major**: any breaking change to a public API (renames, removed members, changed behavior apps rely on)
- **minor**: new capabilities, backward compatible
- **patch**: fixes only

`Directory.Build.props` holds `VersionPrefix`, the next expected release. Local builds are
`<prefix>-dev`. Bump the prefix on `dev` right after each stable release.

## What a release publishes

| Target | Condition |
| --- | --- |
| GitHub Release: the four `.nupkg` files, their `.snupkg` symbol packages, `SHA256SUMS` | always |
| Build-provenance attestation for every `.nupkg` | always |
| nuget.org | the `NUGET_API_KEY` repository secret is set |

`NUGET_API_KEY` is a nuget.org API key with **push** scope limited to the glob
`KofTwentyTwo.AppKit*`. Set it under **Settings → Secrets and variables → Actions**.
Without it, the release still succeeds and the packages are only on GitHub.

## Cutting a release

1. Merge `dev` into `main` through a PR, with every check green.
2. Tag `main` and push the tag:

   ```powershell
   git checkout main
   git pull
   git tag v0.1.0
   git push origin v0.1.0
   ```

3. Watch the **Release** workflow. It re-runs the zero-warning build, the unit tests, and
   the coverage gate, then packs, writes checksums, attests, creates a **draft** release
   with the packages and generated notes, publishes it, and pushes to nuget.org.
4. Verify: the release lists eight package files plus `SHA256SUMS`, and
   `gh attestation verify <package>.nupkg --repo KofTwentyTwo/AppKit` succeeds.
5. On `dev`, bump `VersionPrefix` in `Directory.Build.props`.

## When something fails

- **Before "Publish the release"**: nothing is public. Delete the draft
  (`gh release delete v0.1.0 --yes`), fix the cause, and re-run the workflow.
- **At "Push to nuget.org"**: the GitHub release is published and immutable, so do not
  re-run the job. Push by hand from the release's assets:

  ```powershell
  gh release download v0.1.0 --pattern "*.nupkg" --dir nupkgs
  dotnet nuget push "nupkgs/*.nupkg" --api-key <key> --source https://api.nuget.org/v3/index.json --skip-duplicate
  ```

- **A published release is broken**: never reuse its version. nuget.org versions are
  permanent (they can only be unlisted). Fix forward with a new patch release, and unlist
  the bad version on nuget.org.
