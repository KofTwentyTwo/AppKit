# ADR-0003: Hold the UI-framework-free packages at 100% line coverage

- **Status:** Accepted
- **Date:** 2026-10-09
- **Pull request:** recorded retroactively for the initial design

## Context

The KofTwentyTwo testing standard requires 100% line coverage for libraries published
for others to consume and for UI-free core logic
([K22-TEST-10](https://github.com/KofTwentyTwo/standards/blob/main/standards/testing.md#coverage)).
WinUI and WPF views need a live compositor and a desktop session, which unit tests do
not have.

## Options considered

### Option A: measure every package

- Pros: one number.
- Cons: the view packages cannot reach 100% without a desktop, so the gate would be
  lowered for everyone or riddled with exclusions.

### Option B: measure KofTwentyTwo.AppKit and KofTwentyTwo.AppKit.Updates at 100%

- Pros: the gate is meaningful and cheap to keep; it pushes logic out of the views and
  into tested code.
- Cons: the view packages rely on the FlaUI end-to-end tests instead of a number.

## Decision

Option B. `coverage.runsettings` measures only the two framework-free packages, and
`build/Assert-Coverage.ps1` fails CI below 100%. Exclusions use
`[ExcludeFromCodeCoverage(Justification = ...)]` only for thin pass-throughs that need
an installed app or the network.

## Consequences

- New logic belongs in the framework-free packages; views stay thin.
- The UI packages are verified by the sample apps' UI tests in CI.
- Security: none directly; tested code paths include the secret vault and the update
  guarding.
