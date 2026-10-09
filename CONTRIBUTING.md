# Contributing to AppKit

Thanks for helping. AppKit follows the
[KofTwentyTwo standards](https://github.com/KofTwentyTwo/standards); this page is the
short version of what they ask of a contribution. AppKit sits under every KofTwentyTwo
Windows app, so a change here reaches all of them: changes are small, tested, and
documented.

## Before you start

For anything beyond a small fix, open an issue first so the approach can be agreed
before you invest time. Questions and proposals are welcome in
[Issues](https://github.com/KofTwentyTwo/AppKit/issues).

## Setup

Prerequisites: Windows 10 1809 or later, the .NET 10 SDK (`global.json` pins the feature
band), and optionally Visual Studio 2026 or Rider. The commands below are exactly what
CI runs; run them before pushing.

```powershell
dotnet restore AppKit.slnx -p:Platform=x64 --locked-mode
dotnet build AppKit.slnx -p:Platform=x64 -warnaserror

# Unit tests and the 100% line-coverage gate (KofTwentyTwo.AppKit, KofTwentyTwo.AppKit.Updates)
dotnet test tests/KofTwentyTwo.AppKit.Tests --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
./build/Assert-Coverage.ps1 -ResultsDirectory TestResults -Packages KofTwentyTwo.AppKit,KofTwentyTwo.AppKit.Updates

# End-to-end UI tests: FlaUI launches both sample apps (needs an interactive desktop)
dotnet test tests/KofTwentyTwo.AppKit.UiTests

# Style gate (Kingsrook layout, analyzers, naming)
dotnet format AppKit.slnx --verify-no-changes --severity warn --no-restore
```

`dotnet format` (without `--verify-no-changes`) fixes layout, `var` use, usings, and
line endings. Two Kingsrook rules it cannot apply are checked in review: three blank
lines between members and a header comment on every type and method (Rider and
ReSharper apply the blank lines from `.editorconfig`). Changing a package version means
updating `Directory.Packages.props` and committing the regenerated `packages.lock.json`
files (`dotnet restore AppKit.slnx -p:Platform=x64 --force-evaluate`).

Install the git hooks once per clone. They run the same secret, workflow, and
commit-message checks CI does:

```sh
pre-commit install --hook-type pre-commit --hook-type commit-msg
```

## Workflow

1. Branch from `main` with a short-lived topic branch named `<type>/<short-description>`,
   for example `feat/tray-icon` or `fix/42-splash-never-dismisses`. There is no `dev`
   branch.
2. Commit with [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/)
   (`feat: ...`, `fix(updates): ...`). Every commit is **signed** and **signed off**:
   `git commit -S -s` (see *Sign-off* below).
3. Open a pull request against `main`. Its title must itself be a Conventional Commit
   header: pull requests are **squash-merged**, so the title becomes the commit
   message on `main`.
4. Every required check must pass, and every review thread must be resolved, before
   the pull request merges.

## What makes a contribution acceptable

- The build has **zero warnings**, and the format gate is clean.
- New behavior has tests; a bug fix has a test that fails without the fix.
- Line coverage of `KofTwentyTwo.AppKit` and `KofTwentyTwo.AppKit.Updates` stays at
  **100%** ([ADR-0003](docs/adr/0003-coverage-scope.md)). Logic goes in those packages; the
  WinUI and WPF packages hold views and framework adapters only.
- A shell feature added to the WinUI package gets the same shape in the WPF package and a
  UI test in both samples.
- Public API changes are deliberate: a breaking change needs a major version and a note
  in the pull request.
- User-visible changes update the docs in the same pull request; attack-surface changes
  update the [threat model](docs/security/threat-model.md).
- No secrets, credentials, or personal data, ever.
- New dependencies are justified in the pull request description.

## Checks that gate a merge

| Check | What it verifies |
| --- | --- |
| `pr / title` | The PR title is a Conventional Commit header |
| `pr / dco` | Every commit is signed off |
| `pr / dependency-review` | Added dependencies have no known vulnerabilities and an allowed license |
| `security / secrets`, `security / sca`, `security / workflows` | No secrets, no vulnerable or malicious dependencies, safe workflows |
| `codeql / analyze (csharp)`, `codeql / analyze (actions)` | No high-severity static analysis findings |
| `ci / build-test` | Locked restore, zero-warning build, unit tests, 100% coverage gate, packages pack |
| `ci / format` | `dotnet format --verify-no-changes --severity warn` |
| `ci / ui-tests` | FlaUI end-to-end tests of both sample apps |

## Sign-off (Developer Certificate of Origin)

By signing off a commit you certify the
[Developer Certificate of Origin 1.1](https://developercertificate.org/): that you
wrote the change, or otherwise have the right to submit it under the project's
license. `git commit -s` appends the sign-off:

```text
Signed-off-by: Your Name <your.email@example.com>
```

The name and email must match the commit author. To add missing sign-offs to a
branch, run `git rebase --signoff main` and force-push the branch.

## AI-assisted contributions

AI coding tools are welcome. You remain the author: you must understand, test, and be
able to explain every line you submit, and you sign it off as your own. Note
substantial AI assistance in the pull request description.

## License

By contributing, you agree that your contributions are licensed under the project's
[MIT License](LICENSE).
