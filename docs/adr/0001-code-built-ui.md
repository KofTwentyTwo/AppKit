# ADR-0001: Build the WinUI and WPF views in code, not XAML

- **Status:** Accepted
- **Date:** 2026-10-09
- **Pull request:** recorded retroactively for the initial design

## Context

The splash, About, and log viewer screens are shared by every KofTwentyTwo app through
NuGet packages. Most of those apps ship unpackaged through Velopack, not as MSIX. A
WinUI XAML library compiles its markup into `.xbf` and `.pri` resources that the host
app must merge into its own resource index; that merge is reliable for packaged apps
and fragile for unpackaged ones (missing-resource crashes at runtime that no build
step catches).

## Options considered

### Option A: XAML user controls in the libraries

- Pros: familiar authoring, designer support.
- Cons: resource merging in unpackaged hosts; two build pipelines (WinUI and WPF XAML
  compilers) inside libraries; harder to keep the WinUI and WPF versions identical.

### Option B: views built in C#

- Pros: a plain assembly with no resources to merge, so it works the same packaged or
  not; WinUI and WPF versions can mirror each other line for line.
- Cons: layout is more verbose to read and write.

## Decision

Option B. The UI packages construct their visual trees in C#. Host apps still use XAML
for their own windows.

## Consequences

- The packages work in packaged and unpackaged apps without extra setup.
- Views are a little harder to restyle; theme resources are looked up at runtime.
- Security: none.
- The FlaUI tests drive the sample apps to prove the views render and behave.
