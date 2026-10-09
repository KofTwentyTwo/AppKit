# Security Policy

## Supported versions

Only the **latest release** of the AppKit packages receives security fixes. Apps built on
AppKit pick up a fix by updating their package reference (Dependabot opens that PR).

## Reporting a vulnerability

**Please do not open a public issue for security problems.**

This repository has GitHub **private vulnerability reporting** enabled:

1. Go to the repository's [Security tab](https://github.com/KofTwentyTwo/AppKit/security).
2. Click **Report a vulnerability**.
3. Describe the issue, how to reproduce it, and the impact you see.

That is the only reporting channel. AppKit is maintained by one person, so responses are
best-effort: normally an acknowledgment within **7 days**.

## Security-relevant design

- **Secrets** (tokens, keys) are stored only through `ISecretVault`. The Windows
  implementation uses the per-user Credential Manager (DPAPI-protected) and clears its
  plaintext buffer after writing. Secrets never reach settings files or logs.
- **Updates** come only from the app's own GitHub Releases over HTTPS via Velopack, and
  each package's integrity is checked by Velopack before it is applied.
- **Releases** of AppKit carry SLSA build-provenance attestations and a `SHA256SUMS` file.
  Verify a package with:

  ```powershell
  gh attestation verify KofTwentyTwo.AppKit.<version>.nupkg --repo KofTwentyTwo/AppKit
  ```
