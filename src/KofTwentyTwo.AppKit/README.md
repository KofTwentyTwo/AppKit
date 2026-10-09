# KofTwentyTwo.AppKit

UI-framework-free foundation for KofTwentyTwo Windows apps.

- **`AppInfo`**: the app's identity (id, name, repository, credits, brand), declared once.
- **`AppPaths`**: per-user data under `%LOCALAPPDATA%\<id>`, relocatable with `<ID>_DATA_DIR`.
- **`BuildVersion`**: "1.2.0-beta.3 (9ac6c2b1f)" from the informational version, plus prerelease detection.
- **`FileActivityLog`** / **`LogTail`**: crash-safe daily log files with retention, and tail/filter helpers for log viewers.
- **`SettingsStore<T>`** / **`ShellSettings`**: never-throwing JSON settings with atomic saves, source-generated for trimming.
- **`CredentialManagerVault`**: secrets in the Windows Credential Manager, never on disk in plain text.
- **`IUserPrompter`**: the dialog seam shared flows use; the WinUI and WPF packages implement it.

Source, samples, and docs: https://github.com/KofTwentyTwo/AppKit
