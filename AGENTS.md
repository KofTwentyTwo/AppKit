# Repository Guidelines

## Project Structure & Module Organization

AppKit provides shared .NET 10 foundations for Windows apps. `AppKit.slnx` groups four packages in `src/`: core services, Velopack updates, WinUI adapters, and WPF adapters. Keep testable logic in core or updates; UI packages contain code-built views and framework adapters.

`tests/` contains unit and UI tests. `samples/` contains runnable WinUI and WPF apps; shared code and brand assets live in `samples/Shared/`. `tools/BrandTool/` generates icons, `build/` holds the coverage gate, and `docs/` contains ADRs, release instructions, and the threat model.

## Build, Test, and Development Commands

Use Windows 10 1809+ and the .NET 10 SDK selected by `global.json`. Run from the repository root:

```powershell
# Restore the locked dependencies and build with zero warnings.
dotnet restore AppKit.slnx -p:Platform=x64 --locked-mode
dotnet build AppKit.slnx -p:Platform=x64 -warnaserror
# Run unit tests and enforce coverage.
dotnet test tests/KofTwentyTwo.AppKit.Tests --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
./build/Assert-Coverage.ps1 -ResultsDirectory TestResults -Packages KofTwentyTwo.AppKit,KofTwentyTwo.AppKit.Updates
# Drive both built samples; requires an interactive desktop.
dotnet test tests/KofTwentyTwo.AppKit.UiTests
# Verify formatting and run the WPF sample locally.
dotnet format AppKit.slnx --verify-no-changes --severity warn --no-restore
dotnet run --project samples/AppKit.Sample.Wpf -p:Platform=x64
# Produce release packages.
dotnet pack AppKit.slnx -c Release -p:Platform=x64 -o artifacts/nuget
```

## Coding Style & Naming Conventions

Follow `.editorconfig`: three-space indentation, LF endings, Allman braces, file-scoped namespaces, and `if(` spacing. YAML uses two spaces. Use PascalCase types/members, `I`-prefixed interfaces, `_camelCase` private fields, and `s_camelCase` private static fields. Use `var` only when the type is apparent. Include type/method header comments and three blank lines between members. Builds enforce style and Meziantou analyzers; check linked sample files for conflict markers after formatting.

## Testing Guidelines

Use xUnit, Coverlet for coverage, and FlaUI/UIA3 for UI automation. Name tests `Method_Scenario_Expectation`. Core and updates must retain 100% line coverage. Add regression tests for fixes; exercise shell features in both samples and keep WinUI/WPF behavior aligned.

## Commit & Pull Request Guidelines

Recent history uses `chore: ...`; follow Conventional Commits (`fix(updates): ...`). Branch from `main` as `<type>/<description>`. Sign and sign off commits with `git commit -S -s`. Target PRs at `main` with a Conventional Commit title for squash merging. Explain what changed, why, linked issues, and validation; include screenshots for UI changes. Justify dependencies, disclose substantial AI assistance, update relevant docs, and resolve checks/review threads. See `CONTRIBUTING.md` for the full checklist.

## Dependencies & Security

Manage versions in `Directory.Packages.props` and regenerate affected `packages.lock.json` files. Never commit secrets or personal data. Update `docs/security/threat-model.md` for attack-surface changes; follow `SECURITY.md` for private vulnerability reports.
