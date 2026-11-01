# Native CI verification

AppKit is a .NET solution whose tests need a Windows host. Its native verification is **centralized**: this repository
carries no workflow that selects a host or a runner label.

- The owner-level `native.yaml` in the KofTwentyTwo CI policy repository dispatches the shared `dotnet` profile for
  `KofTwentyTwo/AppKit` with inputs declared centrally (solution, test projects, coverage packages, platform).
- It runs on pushes to the protected branches, on manual dispatch, and on pull requests opened from a branch of this same
  repository. A pull request from a fork never runs on a host: a Linux `fork-deny` job fails it instead.
- The `native-result` check aggregates the native job. It fails unless the AppKit native job succeeded, so a skipped,
  failed or cancelled native run cannot pass.
- The primary `ci-result` check (source/config lint and secrets scan) continues to run on every pull request.

Both `ci-result` and `native-result` are required checks for `main`. Changing what runs, where, or with which inputs is
done in the CI policy repository, never in a pull request to this repository.
