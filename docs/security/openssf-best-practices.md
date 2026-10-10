# OpenSSF Best Practices assessment

Prepared 2026-10-10 for AppKit's first release. This is an evidence worksheet, not
a badge or completed certification. Use the current
[passing criteria](https://www.bestpractices.dev/en/criteria/0) when submitting.

## Register the project

The owner signs in at [bestpractices.dev](https://www.bestpractices.dev/en/projects/new)
and registers **AppKit** with repository and project URL
`https://github.com/KofTwentyTwo/AppKit`, language C#, and MIT license. No credential
belongs in this repository. After registration, keep the public project URL here
and add the site's generated badge to README. An in-progress entry is not a passing
badge; mark only evidenced criteria as met.

## Evidence worksheet

`Ready` means repository evidence is available after these changes merge. `Owner`
requires a truthful maintainer attestation or interpretation of applicability;
it must not be inferred from documentation alone. `Pending` needs more work.
Criterion names below match the passing-level form; check its details for each entry.

| Criteria | Status | Evidence / next step |
| --- | --- | --- |
| `description_good`, `interact`, `documentation_basics`, `documentation_interface` | Ready | [README](../../README.md), package READMEs, XML API comments, runnable WinUI/WPF samples. |
| `contribution`, `contribution_requirements`, `english` | Ready | [CONTRIBUTING](../../CONTRIBUTING.md), PR template, English issues and documentation. |
| `floss_license`, `floss_license_osi`, `license_location` | Ready | Root MIT [LICENSE](../../LICENSE) and SPDX headers; NuGet manifests carry MIT. |
| `sites_https`, `discussion` | Ready | HTTPS GitHub repository, issues, PR discussions, and nuget.org downloads. |
| `maintained` | Ready | First release 2026-10-10, active maintainer and support policy. The badge criterion differs from Scorecard's 90-day heuristic. |
| `repo_public`, `repo_track`, `repo_interim`, `repo_distributed` | Ready | Public Git repository, signed history, topic branches and PRs between tagged releases. |
| `version_unique`, `version_semver`, `version_tags` | Ready | Signed `v0.1.0`, four versioned packages, [release procedure](../RELEASING.md). |
| `release_notes` | Pending | [CHANGELOG](../../CHANGELOG.md) describes the initial release. Publish it, then link/include those notes in the v0.1.0 GitHub release; existing generated notes mainly describe standards adoption. |
| `release_notes_vulns` | Owner | Confirm whether any publicly known vulnerability was fixed; select N/A only if there were none. Include advisory IDs in future affected releases. |
| `report_process`, `report_tracker`, `report_archive` | Ready | Public GitHub Issues, bug template, searchable issue responses. |
| `report_responses`, `enhancement_responses` | Owner | No 2–12 month reporting history exists yet. Apply the form's details honestly; maintain timely acknowledgements instead of inventing historical reports. |
| `vulnerability_report_process`, `vulnerability_report_private` | Ready | [SECURITY](../../SECURITY.md) links GitHub private vulnerability reporting. |
| `vulnerability_report_response` | Owner | Policy targets seven-day acknowledgement. Confirm actual report history; a target is not proof of every response time. |
| `build`, `build_common_tools` | Ready | .NET SDK and MSBuild; locked restore and zero-warning build documented and run in CI. |
| `build_floss_tools` | Owner | .NET/MSBuild are open source, but full WinUI builds require Windows SDK/platform tooling. Verify licenses and applicability; explain a justified SHOULD exception if necessary. |
| `test`, `test_invocation`, `test_most`, `test_continuous_integration` | Ready | xUnit/Coverlet/FlaUI, standard `dotnet test`, required CI, 100% core/update line coverage. UI coverage is separately scoped by ADR-0003. |
| `test_policy`, `tests_are_added`, `tests_documented_added` | Ready | CONTRIBUTING requires behavior/regression tests; recent source/tests and generated-input properties provide evidence. |
| `warnings`, `warnings_fixed`, `warnings_strict` | Ready | TreatWarningsAsErrors, .editorconfig, .NET and Meziantou analyzers, format gate; local solution build has zero warnings. |
| `know_secure_design`, `know_common_errors` | Owner | Read the criteria details and personally attest knowledge. [Threat model](threat-model.md) documents boundaries, hostile inputs, dependency risks and mitigations, but cannot attest a person's knowledge. |
| `crypto_published`, `crypto_call`, `crypto_working`, `crypto_weaknesses` | Owner | AppKit delegates secrets to Windows Credential Manager and delivery to HTTPS/Velopack. Review upstream algorithms/defaults; it implements no cryptographic algorithm. |
| `crypto_floss`, `crypto_keylength`, `crypto_pfs`, `crypto_random` | Owner | Clarify applicability of OS-managed DPAPI and TLS. The Windows credential backend is platform-specific; do not assert a FLOSS equivalent or configurable key lengths without evidence. |
| `crypto_password_storage` | Owner | AppKit stores host-provided secrets, not passwords to authenticate external users. Review the criterion and use N/A with that explanation if applicable. |
| `delivery_mitm`, `delivery_unsigned` | Ready | GitHub/NuGet HTTPS, signed tags, SHA256SUMS and verified build provenance. README documents attestation verification. |
| `vulnerabilities_fixed_60_days`, `vulnerabilities_critical_fixed` | Owner | Confirm all publicly known runtime vulnerabilities and their age; public alerts here concern process controls. Recheck advisories/dependency scans before submitting. |
| `no_leaked_credentials` | Owner | Secret scanning and read-only workflow tokens provide controls. Review scan results/history before attesting no valid credential leak. |
| `static_analysis`, `static_analysis_common_vulnerabilities`, `static_analysis_often` | Ready | Required CodeQL security-extended C#/Actions scans, .NET/Meziantou analyzers, security workflow on PRs/main and weekly. |
| `static_analysis_fixed` | Owner | Confirm no unresolved medium/high exploitable static findings. Record Scorecard process exceptions separately in [scorecard.md](scorecard.md). |
| `dynamic_analysis`, `dynamic_analysis_enable_assertions` | Ready | xUnit/FlaUI and four FsCheck properties with 500 generated cases each; assertions, shrinking, and replay seed on failure. |
| `dynamic_analysis_unsafe` | Owner | Managed C# production code includes native Windows interop and Velopack dependencies. Review whether this criterion applies to project-produced code before choosing N/A. |
| `dynamic_analysis_fixed` | Owner | Confirm findings and remediation history; a green test run alone cannot establish the absence of every vulnerability. |

## Submission and verification

Complete the owner rows, resolve pending release notes, and provide public evidence
URLs from merged `main` for the form's required explanations. After submitting,
record the public project ID and actual badge level here. Rerun Scorecard on `main`
and inspect alert #5. Do not disable the check or claim passing status from this draft.
