<!-- PRs target the `dev` branch (see CONTRIBUTING.md). -->

## What

<!-- What does this PR change? -->

## Why

<!-- Why is it needed? Link the issue, e.g. "Fixes #12". -->

## Checklist

- [ ] `dotnet build AppKit.slnx -p:Platform=x64 -warnaserror` succeeds with **0 warnings**
- [ ] Unit tests pass and coverage of `KofTwentyTwo.AppKit` / `KofTwentyTwo.AppKit.Updates` is still **100%**
- [ ] UI changes have the same shape in WinUI **and** WPF, with a UI test in both samples
- [ ] `dotnet format AppKit.slnx --verify-no-changes --severity error` is clean
- [ ] Public API changes are intentional; breaking changes are called out for a major bump
- [ ] Docs updated (`README.md`, package READMEs) if behavior changed
