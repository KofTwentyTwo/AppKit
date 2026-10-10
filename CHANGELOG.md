# Changelog

Release versions follow [Semantic Versioning](https://semver.org/). All four AppKit
packages release together. Unreleased changes are listed before tagging; published
versions retain their original package contents.

## Unreleased — 0.2.0

- Bound each log-viewer refresh to 1 MiB of trailing input plus the encoding header,
  including huge single lines. Preserve supported BOM encodings and show a notice
  when older input is omitted. Existing two-argument log-reader calls remain compatible.
- Add an `ILogger` adapter for activity logs with original message templates, typed
  scalar fields, event IDs, and asynchronous scopes stored as JSON. Existing
  `IActivityLog` methods and third-party implementations remain compatible. Foundation
  update/crash events and samples use source-generated log methods. The official
  `Microsoft.Extensions.Logging.Abstractions` package is a new runtime dependency.

## 0.1.0 — 2026-10-10

Initial public release for .NET 10 Windows desktop applications:

- Core package: app identity and paths, build version, settings persistence and repair,
  activity logs, credential storage, credits, and user interaction abstractions.
- Updates package: Velopack startup, GitHub release updates, and a shared update
  coordinator with errors returned to the caller.
- WinUI and WPF packages: code-built splash, About, and log views; framework adapters
  for dialogs, theming, window behavior, and crash handling.
- Runnable WinUI/WPF samples, unit and UI automation tests, full core/update line
  coverage, and enforced build, formatting, dependency, and security checks.
- GitHub release assets include NuGet/symbol packages, per-package SBOMs, SHA-256
  checksums, and signed build provenance. NuGet publishing uses trusted publishing.

This is the first release, so there is no earlier AppKit version to upgrade from.
The UI packages and Windows credential backend require Windows; see README for
package-specific target frameworks and setup.

[GitHub release](https://github.com/KofTwentyTwo/AppKit/releases/tag/v0.1.0)
