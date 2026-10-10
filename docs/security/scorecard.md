# Scorecard findings and review controls

AppKit retains the single-maintainer policy in
[K22-REPO-20](https://github.com/KofTwentyTwo/standards/blob/main/standards/repository.md)
and [K22-SDLC-12 / EX-0001](https://github.com/KofTwentyTwo/standards/blob/main/policies/sdlc.md).
OpenSSF Scorecard's independent human-review criteria exceed that policy. The
following findings are tracked separately from exploitable code vulnerabilities.

## Findings assessed on 2026-10-10

Baseline: `main` at `839fb72bec464223133b405df85bb01608d25360`.
Sources: GitHub alert instances, the live `protect-main` ruleset, PR #2 review history,
repository files, and [Scorecard check definitions](https://github.com/ossf/scorecard/blob/main/docs/checks.md).

| Alert | Evidence and disposition | Revisit when |
| --- | --- | --- |
| [#4 Code-Review](https://github.com/KofTwentyTwo/AppKit/security/code-scanning/4) | No independent approval on either assessed changeset. Accepted single-maintainer limitation; AI reviews cannot satisfy Scorecard. Require an acknowledged automated review plus CodeQL instead. PR #2 had no automated review record in the inspected comments/reviews; fix the process for future PRs. | An independent maintainer joins; check review records on every PR. |
| [#3 Maintained](https://github.com/KofTwentyTwo/AppKit/security/code-scanning/3) | Created 2026-10-09; not archived. Scorecard requires over 90 days of history. Continue maintenance; no artificial commits or scanner suppression. | 2027-01-08, after 90 days have elapsed. |
| [#1 Branch-Protection](https://github.com/KofTwentyTwo/AppKit/security/code-scanning/1) | Main requires PRs, resolved threads, 11 passing checks, up-to-date branches, signed commits, linear history, and no force pushes/deletion or bypass actors. Zero human approvals and disabled CODEOWNERS/last-push approval match the one-maintainer exception. | An independent maintainer joins; enable one independent approval and last-push approval, then assess CODEOWNERS enforcement. |
| [#2 Fuzzing](https://github.com/KofTwentyTwo/AppKit/security/code-scanning/2) | Add four real FsCheck properties to the existing CI unit suite. The pinned action uses Scorecard v5.5.0, whose [C# detector](https://github.com/ossf/scorecard/blob/v5.5.0/checks/raw/fuzzing.go) recognizes `using FsCheck.Xunit;`. Local tests do not close the remote alert; rerun Scorecard after merge and verify detection. | The implementation reaches main, or parsing/input contracts change. |
| [#5 CII-Best-Practices](https://github.com/KofTwentyTwo/AppKit/security/code-scanning/5) | No registered badge project. The [assessment](openssf-best-practices.md) prepares evidence and identifies owner attestations. Do not claim a badge until the public project entry exists. | The owner registers and completes the assessment. |

No alert was dismissed or scanner setting weakened while preparing these changes.
If accepting the two review limitations in GitHub, use `won't fix` with the
single-maintainer exception and a link to this published document; do not label them
false positives. Any age-warning dismissal must say that maintenance cannot yet be
assessed and include the revisit date. Keep fuzzing and badge alerts open until verified.

## Recording an automated review

Run Codex, Claude, or Copilot against the PR diff and relevant source/tests at its
current head SHA. Resolve or answer every finding, rerun affected checks, and repeat
the review after changes. The maintainer must read the result. As `KofTwentyTwo`, post
a separate PR comment using the following format; replace placeholders with real evidence:

```markdown
<!-- appkit-automated-review:FULL_40_CHARACTER_HEAD_SHA -->
Tool: Codex
Findings: none
Summary: Describe the actual review scope, outcome, and any resolved findings.
Maintainer acknowledgement: I read the review and resolved or answered every finding.
```

Use `Findings: resolved` after addressing findings, or `Findings: unresolved` to block
the check. Include findings and their resolution below the summary. The latest
maintainer-owned record for that SHA controls the result. Editing or deleting comments
reruns validation. Other contributors and bots cannot attest for the maintainer.

The gate validates the record's author, commit, result, and acknowledgement. It
cannot prove the quality of the AI review or replace independent human review.
Keep each `Tool`, `Findings`, and `Summary` field unique; conflicting/duplicate fields
fail validation rather than allowing an old passing result to hide unresolved findings.

## Enabling the required checks

1. Review, test, and merge the bootstrap PR normally under the existing 11 checks;
   record its automated review manually. The new comment workflow is unavailable
   until it exists on the default branch.
2. On a subsequent PR, post a genuine review record and verify `review / automated`
   is attached to the **PR head**, succeeds for its SHA, and fails for an incomplete
   or unresolved record. `review / tests` must also pass.
3. Add both contexts to `protect-main`'s required checks with the GitHub Actions
   integration, preserving all existing rules and the empty bypass list.
4. Verify a new commit cannot merge with the previous record. It has no passing
   review check until a new record is posted; do not reuse the old SHA.

The workflow runs only on PR comment events and checks out `github.workflow_sha`,
the trusted default-branch workflow commit. It never checks out a PR head, executes
comment text, uses a PAT, or shares caches/artifacts with untrusted code. Its write
permission is limited to publishing check runs. Adding another maintainer requires
updating the record-author policy and the tests as well as branch protections.
