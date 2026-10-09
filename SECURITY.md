# Security Policy

## Reporting a vulnerability

**Do not open a public issue for a security problem.**

Report it privately through GitHub's
[private vulnerability reporting](https://github.com/KofTwentyTwo/AppKit/security/advisories/new)
(the repository's **Security** tab, then **Report a vulnerability**). That is the only
reporting channel.

Security contact: **James Maes** ([@KofTwentyTwo](https://github.com/KofTwentyTwo)),
maintainer.

Please include the affected package and version, how to reproduce it, and the impact
you see. AppKit's attack surface is described in its
[threat model](docs/security/threat-model.md).

## What to expect

KofTwentyTwo follows coordinated vulnerability disclosure. Timeframes are counted
from the day the report arrives and come from the
[KofTwentyTwo security program](https://github.com/KofTwentyTwo/standards/blob/main/policies/security-program.md)
(targets until the standards reach v1.0.0, commitments from then on):

| Step | Target |
| --- | --- |
| Acknowledge the report | 7 days |
| Assess it: confirm or reject, with a severity | 14 days |
| Release a fix for a CRITICAL vulnerability | 7 days |
| Release a fix for a HIGH vulnerability | 30 days |
| Release a fix for a MEDIUM vulnerability | 90 days |
| Release a fix for a LOW vulnerability | The next release |
| Publish the advisory (GitHub Security Advisory, CVE where applicable) | When the fix ships, or 90 days after the report, whichever comes first |

Reporters are credited in the advisory unless they ask not to be.

## Supported versions

| Version | Security fixes |
| --- | --- |
| Latest minor release (`0.1.x` once published) | Yes |
| Older | No; upgrade to the latest release |

All four packages release together under one version. Only the latest minor release
receives fixes; a line stops receiving security fixes when the next minor version is
released. After a new major version, the previous major's last minor receives security
fixes for 6 months.

## Published vulnerabilities

Fixed vulnerabilities are published as
[GitHub Security Advisories](https://github.com/KofTwentyTwo/AppKit/security/advisories).
