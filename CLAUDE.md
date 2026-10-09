# AppKit

Shared foundation packages for KofTwentyTwo Windows apps (WinUI 3 and WPF, .NET 10).
Every KofTwentyTwo app depends on this, so treat the public API as a contract.

## Commands

```powershell
dotnet build AppKit.slnx -p:Platform=x64 -warnaserror
dotnet test tests/KofTwentyTwo.AppKit.Tests --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
./build/Assert-Coverage.ps1 -ResultsDirectory TestResults -Packages KofTwentyTwo.AppKit,KofTwentyTwo.AppKit.Updates
dotnet test tests/KofTwentyTwo.AppKit.UiTests        # launches both sample apps on the desktop
dotnet restore AppKit.slnx -p:Platform=x64; dotnet format AppKit.slnx --verify-no-changes --severity error --no-restore
```

## Rules

- PRs target `dev`; `main` is release-only; tags `v*` publish (see docs/RELEASING.md).
- Zero warnings; 100% line coverage for `KofTwentyTwo.AppKit` and `KofTwentyTwo.AppKit.Updates`.
- Testable logic lives in those two packages; WinUI/WPF packages hold views and adapters only.
- UI packages are code-built (no XAML) so they work unpackaged; keep WinUI and WPF in step,
  with a UI test for both samples.
- Package versions live only in `Directory.Packages.props` (central package management).
- Files are CRLF (`.editorconfig`, `.gitattributes`).
