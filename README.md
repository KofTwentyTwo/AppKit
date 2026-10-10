# AppKit

**The shared foundation for KofTwentyTwo Windows apps.** Everything a desktop app needs
that has nothing to do with what the app actually does: identity, per-user data paths,
crash-safe logging, settings, secret storage, a branded splash and About screen, a live
log viewer, a crash safety net, theming, and Velopack self-update with stable/dev
channels. Extracted from [gclo](https://github.com/KofTwentyTwo/gclo), where every piece
shipped in a 1.0 release first.

[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/KofTwentyTwo/AppKit/badge)](https://scorecard.dev/viewer/?uri=github.com/KofTwentyTwo/AppKit)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
![.NET 10](https://img.shields.io/badge/.NET-10-blueviolet)
![Platform: Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)

| | |
| --- | --- |
| **Tier** | Product ([scope](https://github.com/KofTwentyTwo/standards/blob/main/policies/README.md#scope)) |
| **Standards** | Adopting [KofTwentyTwo standards](https://github.com/KofTwentyTwo/standards) (pre-v1.0); this README will say *Conforms to KofTwentyTwo standards v1.0* once the conformance checker passes |
| **Latest release** | [v0.1.0](https://github.com/KofTwentyTwo/AppKit/releases/tag/v0.1.0) |

## Packages

| Package | Target | What it gives you |
| --- | --- | --- |
| `KofTwentyTwo.AppKit` | `net10.0` | `AppInfo` identity, `AppPaths`, `BuildVersion`, `FileActivityLog` + `LogTail`, `SettingsStore<T>` + `ShellSettings`, `CredentialManagerVault`, `IUserPrompter` |
| `KofTwentyTwo.AppKit.Updates` | `net10.0` | `VelopackStartup`, `VelopackUpdateService` (GitHub Releases, stable/dev channel), `UpdateCoordinator` (the "Check for updates" flow) |
| `KofTwentyTwo.AppKit.WinUI` | `net10.0-windows10.0.19041.0` | `SplashOverlay`, `AboutDialog`, `LogWindow`, `DialogGuard`, `ContentDialogPrompter`, `WinUIShell` (crash net, theme), DPI-aware `WindowExtensions` |
| `KofTwentyTwo.AppKit.Wpf` | `net10.0-windows` | `SplashOverlay`, `AboutWindow`, `LogWindow`, `MessageBoxPrompter`, `WpfShell` (crash net, Fluent theme) |

The two UI-framework-free packages are held to **100% line coverage** in CI. The UI
packages are built in code rather than XAML, so they work identically in packaged (MSIX)
and unpackaged (Velopack) apps, and are exercised end-to-end by FlaUI tests that drive
the sample apps ([ADR-0001](docs/adr/0001-code-built-ui.md)).

## Install

The packages are published to nuget.org with the GitHub release of each version.
Reference them through central package management:

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="KofTwentyTwo.AppKit.WinUI" Version="0.1.0" />  <!-- or .Wpf -->

<!-- the app's .csproj -->
<PackageReference Include="KofTwentyTwo.AppKit.WinUI" />
<PackageReference Include="Microsoft.WindowsAppSDK" />
```

The UI packages bring in `KofTwentyTwo.AppKit` and `KofTwentyTwo.AppKit.Updates`. A
WinUI app references `Microsoft.WindowsAppSDK` itself (the WinUI package does not carry
its build targets) and copies its brand assets (`Assets/Brand/app-icon.svg`,
`app.ico`) to the output directory.

## Usage

New apps should start from the KofTwentyTwo Windows app template, which has everything
below in place; the design is the
[Windows desktop reference architecture](https://github.com/KofTwentyTwo/standards/blob/main/architecture/windows-desktop-app.md).
To add AppKit to an existing WinUI app:

```csharp
// One identity, declared once.
internal static class MyApp
{
   public static AppInfo Info { get; } = new()
   {
      Id = "myapp",                                                      // data folder, log prefix, credential prefix
      DisplayName = "My App",
      Tagline = "Does one thing well",
      RepositoryUrl = new Uri("https://github.com/KofTwentyTwo/myapp"), // also the update feed
      Author = "James Maes",
      Copyright = "Copyright (c) 2026 James Maes",
   };
   public static AppPaths Paths { get; } = new(Info);
}

// Program.Main (with DISABLE_XAML_GENERATED_MAIN): Velopack goes first.
VelopackStartup.Run();

// App constructor
WinUIShell.InstallCrashNet(this, () => Log);
WinUIShell.ApplyTheme(this, settings.ThemeKind);

// Main window
SplashOverlay.ShowOver(RootGrid, MyApp.Info, settings);
var updates = new UpdateCoordinator(
   new VelopackUpdateService(MyApp.Info.RepositoryUrl!),
   new ContentDialogPrompter(() => Content.XamlRoot),
   MyApp.Info.DisplayName, Log);

// Help menu
await updates.CheckInteractivelyAsync();
await AboutDialog.ShowAsync(MyApp.Info, Content.XamlRoot);
LogWindow.ShowSingle(Log, MyApp.Info);
```

[`samples/`](samples) has complete WinUI and WPF apps wired to every service.

## Building from source

Prerequisites:

- Windows 10 1809 or later (the UI packages and samples are Windows-only)
- .NET 10 SDK (the feature band is pinned by `global.json`)
- Optional: Visual Studio 2026 or Rider for the samples

```powershell
dotnet restore AppKit.slnx -p:Platform=x64 --locked-mode
dotnet build AppKit.slnx -p:Platform=x64 -warnaserror          # zero warnings, always
dotnet test tests/KofTwentyTwo.AppKit.Tests --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
./build/Assert-Coverage.ps1 -ResultsDirectory TestResults -Packages KofTwentyTwo.AppKit,KofTwentyTwo.AppKit.Updates
dotnet test tests/KofTwentyTwo.AppKit.UiTests                   # drives the samples; needs a desktop
dotnet format AppKit.slnx --verify-no-changes --severity warn --no-restore
dotnet pack AppKit.slnx -c Release -p:Platform=x64 -o artifacts/nuget
```

Dependencies are declared in `Directory.Packages.props` (central package management)
and locked per project in `packages.lock.json`; see [CONTRIBUTING.md](CONTRIBUTING.md)
for the full local workflow.

```text
src/        the four packages
tests/      unit tests (100% gate) and FlaUI end-to-end tests of the samples
samples/    minimal WinUI and WPF apps wired to every service
tools/      BrandTool: generates app-icon.svg and a multi-size app.ico from one definition
build/      Assert-Coverage.ps1, the coverage gate
docs/       release process, architecture decisions, threat model
```

## Verifying a release

Every release asset (`.nupkg`, `.snupkg`, and the CycloneDX SBOMs) is listed in
`SHA256SUMS` and has a build-provenance attestation signed by the shared KofTwentyTwo
release workflow:

```powershell
gh release download v0.1.0 --repo KofTwentyTwo/AppKit
sha256sum --check SHA256SUMS
gh attestation verify KofTwentyTwo.AppKit.0.1.0.nupkg --repo KofTwentyTwo/AppKit --signer-repo KofTwentyTwo/standards
```

## Support

Only the latest released minor version receives fixes, including security fixes; see
[SECURITY.md](SECURITY.md#supported-versions).

## Security

Report vulnerabilities privately; see [SECURITY.md](SECURITY.md). The attack surface is
described in the [threat model](docs/security/threat-model.md).

Repository-practice findings and the single-maintainer review policy are documented
in [the Scorecard assessment](docs/security/scorecard.md). The
[OpenSSF Best Practices worksheet](docs/security/openssf-best-practices.md) tracks
evidence and outstanding owner attestations; no certification is claimed yet.

Unreleased 0.2.0 adds [structured logging](src/KofTwentyTwo.AppKit/README.md#structured-logging)
and bounded log reads; these capabilities are not part of the published 0.1.0 packages.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Releases: [docs/RELEASING.md](docs/RELEASING.md).

## License

[MIT](LICENSE) © 2026 James Maes
