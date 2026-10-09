# ADR-0002: Isolate Velopack in KofTwentyTwo.AppKit.Updates

- **Status:** Accepted
- **Date:** 2026-10-09
- **Pull request:** recorded retroactively for the initial design

## Context

Self-update through Velopack is right for per-user desktop apps, but not every
KofTwentyTwo app should carry it: a Windows service is installed by an MSI and updated
through winget or the MSI, and a library consumer may want identity and logging
without any update machinery.

## Options considered

### Option A: Velopack inside KofTwentyTwo.AppKit

- Pros: one package fewer.
- Cons: every consumer, including services and CLIs, pulls in Velopack and its native
  update hooks.

### Option B: a separate KofTwentyTwo.AppKit.Updates package

- Pros: the core package stays dependency-free; update logic (`UpdateCoordinator`) is
  still shared by WinUI and WPF; apps that do not self-update simply do not reference it.
- Cons: one more package to version (all four release together, so this costs little).

## Decision

Option B. Velopack and the update flow live in `KofTwentyTwo.AppKit.Updates`; the UI
packages reference it because their apps self-update.

## Consequences

- `KofTwentyTwo.AppKit` has no third-party runtime dependency.
- The Velopack pass-through is isolated behind `IUpdateBackend`, so every decision
  around it is unit-tested and only the thin adapter is excluded from coverage.
- Security: the update feed is an attack surface only for apps that reference this
  package (see the threat model, T1–T3).
