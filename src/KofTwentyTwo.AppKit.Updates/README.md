# KofTwentyTwo.AppKit.Updates

Velopack self-update for KofTwentyTwo Windows apps, fed by the app's GitHub Releases.

- **`VelopackStartup.Run()`**: the first line of `Main`; handles install/update/uninstall hooks.
- **`VelopackUpdateService`**: never-throwing check/download/apply. Prerelease builds follow the
  `dev` channel (GitHub prereleases), stable builds the `stable` channel.
- **`UpdateCoordinator`**: the complete "Check for updates" flow (interactive and quiet startup
  variants) written against `IUserPrompter`, so WinUI and WPF apps behave identically.

Source, samples, and docs: https://github.com/KofTwentyTwo/AppKit
