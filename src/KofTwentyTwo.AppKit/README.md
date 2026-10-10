# KofTwentyTwo.AppKit

UI-framework-free foundation for KofTwentyTwo Windows apps.

- **`AppInfo`**: the app's identity (id, name, repository, credits, brand), declared once.
- **`AppPaths`**: per-user data under `%LOCALAPPDATA%\<id>`, relocatable with `<ID>_DATA_DIR`.
- **`BuildVersion`**: "1.2.0-beta.3 (9ac6c2b1f)" from the informational version, plus prerelease detection.
- **`FileActivityLog`** / **`LogTail`**: crash-safe daily log files with retention, bounded tail/filter helpers, and an `ILogger` adapter for structured events.
- **`SettingsStore<T>`** / **`ShellSettings`**: never-throwing JSON settings with atomic saves, source-generated for trimming.
- **`CredentialManagerVault`**: secrets in the Windows Credential Manager, never on disk in plain text.
- **`IUserPrompter`**: the dialog seam shared flows use; the WinUI and WPF packages implement it.

Source, samples, and docs: https://github.com/KofTwentyTwo/AppKit

## Structured logging

Keep one `ILogger` from `activityLog.AsLogger()` and use static message templates.
Prefer source-generated events, as demonstrated in the sample apps:

```csharp
using KofTwentyTwo.AppKit.Logging;
using Microsoft.Extensions.Logging;

ILogger logger = activityLog.AsLogger();
AppEvents.LogSynced(logger, repository);

partial class AppEvents
{
   [LoggerMessage(1, LogLevel.Information, "Synced {Repository}")]
   public static partial void LogSynced(ILogger logger, string repository);
}
```

Each structured entry is a JSON object after the existing timestamp/level prefix.
`MessageTemplate` retains the original format; `Message` contains rendered text;
`Properties` retains named values. `EventId`, `EventName`, `Level`, and `Scopes` are
recorded separately. Exceptions retain their full continuation text. Daily rollover,
retention, thread safety, and nonthrowing writes use the existing file sink.

Nulls, booleans, integers, decimals, and finite floating-point values retain JSON
types. Nonfinite numbers and other objects use invariant strings; pass a
`JsonElement` for an explicit nested JSON value. Members of arbitrary objects are
never inspected. Scopes flow across async calls on the same adapter and disappear
when disposed. Do not put credentials or personal data in any field or scope.

The existing `Info`, `Warning`, and `Error` methods still write their original text
format, and existing `IActivityLog` implementations require no changes. The adapter
passes complete JSON records to those implementations. This capability requires
AppKit 0.2.0; 0.1.0 packages retain their published behavior.
