# AppKit

**The shared foundation for KofTwentyTwo Windows apps.** Everything a desktop app needs
that has nothing to do with what the app actually does: identity, per-user data paths,
crash-safe logging, settings, secret storage, a branded splash and About screen, a live
log viewer, a crash safety net, theming, and Velopack self-update with stable/dev
channels. Extracted from [gclo](https://github.com/KofTwentyTwo/gclo), where every piece
shipped in a 1.0 release first.

[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
![.NET 10](https://img.shields.io/badge/.NET-10-blueviolet)
![Platform: Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)

## Packages

| Package | Target | What it gives you |
| --- | --- | --- |
| `KofTwentyTwo.AppKit` | `net10.0` | `AppInfo` identity, `AppPaths`, `BuildVersion`, `FileActivityLog` + `LogTail`, `SettingsStore<T>` + `ShellSettings`, `CredentialManagerVault`, `IUserPrompter` |
| `KofTwentyTwo.AppKit.Updates` | `net10.0` | `VelopackStartup`, `VelopackUpdateService` (GitHub Releases, stable/dev channel), `UpdateCoordinator` (the "Check for updates" flow) |
| `KofTwentyTwo.AppKit.WinUI` | `net10.0-windows10.0.19041.0` | `SplashOverlay`, `AboutDialog`, `LogWindow`, `DialogGuard`, `ContentDialogPrompter`, `WinUIShell` (crash net, theme), DPI-aware window helpers |
| `KofTwentyTwo.AppKit.Wpf` | `net10.0-windows` | `SplashOverlay`, `AboutWindow`, `LogWindow`, `MessageBoxPrompter`, `WpfShell` (crash net, Fluent theme) |

The two framework-free packages are held to **100% line coverage** in CI. The UI packages
are built in code rather than XAML, so they work identically in packaged (MSIX) and
unpackaged (Velopack) apps, and are exercised end-to-end by FlaUI tests that drive the
sample apps.

## Using it

New apps should start from the app template rather than wiring this by hand; it
references these packages and has everything below already in place. To add AppKit to
an existing WinUI app:

```csharp
// One identity, declared once.
static class MyApp
{
    public static AppInfo Info { get; } = new()
    {
        Id = "myapp",                                    // data folder, log prefix, credential prefix
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

Apps reference `Microsoft.WindowsAppSDK` themselves (the WinUI package does not carry its
build targets), and copy their brand assets (`Assets/Brand/app-icon.svg`, `app.ico`) to
the output directory. See [`samples/`](samples) for complete WinUI and WPF apps.

## Repository layout

```
src/        the four packages
tests/      unit tests (100% gate) and FlaUI end-to-end tests of the samples
samples/    minimal WinUI and WPF apps wired to every service
tools/      BrandTool: generates app-icon.svg and a multi-size app.ico from one definition
build/      scripts CI shares with every app repo (coverage gate)
```

## Building

```powershell
dotnet build AppKit.slnx -p:Platform=x64 -warnaserror          # zero warnings, always
dotnet test tests/KofTwentyTwo.AppKit.Tests --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
./build/Assert-Coverage.ps1 -ResultsDirectory TestResults -Packages KofTwentyTwo.AppKit,KofTwentyTwo.AppKit.Updates
dotnet test tests/KofTwentyTwo.AppKit.UiTests                   # drives the samples; needs a desktop
dotnet format AppKit.slnx --verify-no-changes --severity error
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for the branch model and PR expectations, and
[docs/RELEASING.md](docs/RELEASING.md) for how packages are published.

## License

[MIT](LICENSE) © 2026 James Maes
