# KofTwentyTwo.AppKit.WinUI

WinUI 3 shell pieces for KofTwentyTwo Windows apps, built in code so they work in packaged
and unpackaged (Velopack) apps alike.

- **`SplashOverlay.ShowOver(rootGrid, app, settings)`**: branded splash as an in-window overlay.
- **`AboutDialog.ShowAsync(app, xamlRoot)`**: identity, version, license, attributions.
- **`LogWindow.ShowSingle(log, app)`**: live, filterable activity-log viewer.
- **`DialogGuard`** / **`ContentDialogPrompter`**: one ContentDialog at a time, no stowed-exception crashes.
- **`WinUIShell`**: crash safety net and theme; **`WindowExtensions`**: DPI-aware sizing and the app icon.

The host app references `Microsoft.WindowsAppSDK` itself and copies its brand assets to the
output directory. Source, samples, and docs: https://github.com/KofTwentyTwo/AppKit
