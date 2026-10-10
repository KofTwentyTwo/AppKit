# AppKit

Shared foundation packages for KofTwentyTwo Windows apps (WinUI 3 and WPF, .NET 10).
Every KofTwentyTwo app depends on this, so treat the public API as a contract. The repo
follows the KofTwentyTwo standards (github.com/KofTwentyTwo/standards; local clone
`../standards`).

## Commands

```powershell
dotnet restore AppKit.slnx -p:Platform=x64 --locked-mode
dotnet build AppKit.slnx -p:Platform=x64 -warnaserror
dotnet test tests/KofTwentyTwo.AppKit.Tests --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
./build/Assert-Coverage.ps1 -ResultsDirectory TestResults -Packages KofTwentyTwo.AppKit,KofTwentyTwo.AppKit.Updates
dotnet test tests/KofTwentyTwo.AppKit.UiTests        # launches both sample apps on the desktop
dotnet format AppKit.slnx --verify-no-changes --severity warn --no-restore
npm ci --ignore-scripts
npm run lint
npm test
Import-Module PSScriptAnalyzer -RequiredVersion 1.25.0
Invoke-ScriptAnalyzer -Path build -Recurse -Settings ./PSScriptAnalyzerSettings.psd1 -EnableExit
pwsh ../standards/tools/Test-RepoConformance.ps1 -LocalPath . -StaticOnly
```

## Rules

- GitHub Flow: topic branch `<type>/<desc>` → PR to `main` → squash merge. No `dev` branch.
  Conventional Commit titles, `git commit -S -s` (signature and DCO). Agents never merge,
  tag, release, or change repository settings.
- Zero warnings; 100% line coverage for `KofTwentyTwo.AppKit` and `KofTwentyTwo.AppKit.Updates`.
- Testable logic lives in those two packages; WinUI/WPF packages hold views and adapters only.
- UI packages are code-built (no XAML) so they work unpackaged; keep WinUI and WPF in step,
  with a UI test for both samples.
- Kingsrook style (`.editorconfig`): 3-space indent, Allman braces, `if(`, LF line endings,
  three blank lines between members, a `///` header comment on every type and method,
  flower-box comments inside bodies, SPDX header on every file, one type per file.
- Package versions live only in `Directory.Packages.props`; every project commits its
  `packages.lock.json`, and CI restores in locked mode.
- `samples/Shared/*.cs` are linked into both sample projects: run `dotnet format` on the
  solution only after checking it did not write merge-conflict markers into them.
- Attack-surface changes update `docs/security/threat-model.md`; expensive-to-reverse
  decisions get an ADR in `docs/adr/`.
- Significant AI contributions include a model-naming `Co-Authored-By` trailer and
  disclosure in the PR. The maintainer acknowledges the current-commit automated
  review before merging; follow `docs/security/scorecard.md`.
