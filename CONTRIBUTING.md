# Contributing to AppKit

AppKit is the shared foundation under every KofTwentyTwo Windows app, so a change here
reaches all of them. That raises the bar: changes are small, tested, and documented.

## Prerequisites

- Windows 11, or Windows 10 version 1809 (build 17763) or later
- [.NET 10 SDK](https://dotnet.microsoft.com/) (`global.json` pins the feature band)
- Visual Studio 2026 with the Windows App SDK workload, optional, for working on the samples

## Branch model

- **`dev`** is the integration branch. **Pull requests target `dev`.**
- **`main`** is the release branch. It moves only by reviewed PRs from `dev`, and release
  tags are cut from it.

## Building and testing

```powershell
dotnet build AppKit.slnx -p:Platform=x64 -warnaserror

# Unit tests and the 100% line-coverage gate (KofTwentyTwo.AppKit, KofTwentyTwo.AppKit.Updates)
dotnet test tests/KofTwentyTwo.AppKit.Tests --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
./build/Assert-Coverage.ps1 -ResultsDirectory TestResults -Packages KofTwentyTwo.AppKit,KofTwentyTwo.AppKit.Updates

# End-to-end UI tests: FlaUI launches both sample apps (needs an interactive desktop)
dotnet test tests/KofTwentyTwo.AppKit.UiTests

dotnet format AppKit.slnx --verify-no-changes --severity error
```

## What CI enforces

| Check | Gate |
| --- | --- |
| **Build and test (x64)** | Zero-warning build (NuGetAudit included), unit tests, **100% line coverage** of the two framework-free packages, and every package still packs |
| **UI end-to-end tests (x64)** | Splash, About, crash net, log window, settings, and the update flow, in both the WinUI and WPF samples |
| **Format (style gate)** | `dotnet format --verify-no-changes --severity error` |
| **Dependency review** | New dependencies: no known vulnerabilities, permissive licenses only |
| **CodeQL** | Static security analysis on PRs to `main` and weekly |

## Design rules

- **Logic goes in the framework-free packages.** If code can be tested without a UI, it
  lives in `KofTwentyTwo.AppKit` or `KofTwentyTwo.AppKit.Updates` and is covered there.
  The WinUI and WPF packages hold views and framework adapters only.
- **UI packages are built in code, not XAML.** XAML compiled into a library has to be
  merged into the host app's resources, which is fragile for unpackaged apps.
- **Nothing optional may crash the app.** Logging, settings, and updates report failures
  as values or log entries, never as exceptions escaping to the user.
- **WinUI and WPF stay in step.** A shell feature added to one package gets the same
  shape in the other, and a UI test in both samples.
- **Never write a secret to disk, logs, or process output.** Secrets go through
  `ISecretVault`.
- **Breaking changes are deliberate.** Every app depends on this API. A breaking change
  needs a major version bump and a note in the release.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
