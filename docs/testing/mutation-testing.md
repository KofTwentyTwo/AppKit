# Library mutation testing

Run `./build/Invoke-MutationTests.ps1` on Windows with the repository's .NET SDK.
The local tool manifest pins Stryker.NET 5.0.0 (Apache-2.0, development only).
Both complete library projects use Standard mutations, two workers and per-test
coverage. No repository mutation exclusions or score overrides are configured.
Initial test failures and tool errors fail the run. HTML/JSON reports remain under
`artifacts/mutation/`; the release workflow repeats runs and retains evidence.

Review survivors before a minor release under K22-TEST-11. Successful execution
does not approve survivors. Default score colors are advisory, with no adopted
numeric gate. Reassess decisions whenever the affected behavior changes.

## Audit on 2026-10-10

| Library | Initial score | Final score | Killed | Survived | Compile errors | Ignored |
| --- | --- | --- | --- | --- | --- | --- |
| Core | 90.17% | 96.27% | 284 | 11 | 38 | 75 |
| Updates | 77.92% | 89.61% | 69 | 8 | 2 | 34 |

Final runs have no uncovered mutations or timeouts. Core's final run used 160
tests; updates used 161 after an additional backend-context case. The complete
unit suite passes 161 tests and retains 100% core/updates line coverage. Compilation
errors cannot exercise behavior; ignored counts come from Stryker's redundancy
filters and existing coverage exclusions, including the live GitHub/Velopack adapter.
Scores are calculated from tested mutations, not every generated mutation.

New or strengthened tests killed 27 former survivors: settings sanitized before
writing, shared-writer log reads, removed-folder recovery, disabled logger state,
single-line warnings, identity diagnostics/version metadata, empty secret keys,
native error context, backend caching, conflicting update outcomes, and asynchronous
UI/backend context affinity. These additions verify behavior without changing
production code solely to improve a score.

## Remaining survivor decisions

IDs identify mutations in this run, not permanent identifiers. All 19 are accounted
for below. These are technical review decisions; release-owner review is still required.

| File and IDs | Decision and evidence |
| --- | --- |
| `AppInfo.cs`: 4 | Equivalent under supported construction: required `Id` always invokes the validated initializer and overwrites the backing field before use. |
| `ArgbColor.cs`: 49, 51, 53 | Equivalent: `value` is unsigned, so `>>` and `>>>` produce identical channel bytes. |
| `BuildVersion.cs`: 84 | Equivalent: slicing a nine-character hash to nine characters returns the same hash; changing `>` to `>=` has no effect. |
| `LogTail.cs`: 260 | Equivalent for supported encodings: twice the preamble length is divisible by the code-unit width, so the changed modular alignment is identical. |
| `LogTail.cs`: 282 | Equivalent: UTF-32's `byteOrderMark` flag changes emitted preambles, not decoding; this reader skips the known four-byte header explicitly. |
| `CredentialManagerVault.cs`: 334, 335, 352 | Retain cleanup. Wiping the pinned plaintext array, freeing its pin and freeing native credentials are security/resource requirements. Functional native tests cannot prove memory wiping or allocator release. Reviewed the unconditional `finally` cleanup, including error paths; record this as a mutation-test limitation, not equivalence. Do not remove these calls. |
| `CredentialManagerVault.cs`: 346 | Retain the defensive OR guard for either zero pointer or zero size. Real Credential Manager tests exercise valid native buffers, not fabricated malformed pointers. Native-boundary limitation; not a claim that AND is safe. |
| `UpdateCoordinator.cs`: 12, 19, 24, 27, 41, 53 | Tail awaits perform no later UI work on their continuation; changing capture has no observable effect in these paths. Retain explicit capture for consistent UI orchestration. Asynchronous tests kill the capture changes before subsequent prompts/downloads. |
| `VelopackUpdateService.cs`: 85 | Tool-repaired equivalence: deleting the getter's catch-body return still returns null through Stryker's injected `return default(string)`. The throwing-backend test proves the public fail-closed behavior. |
| `VelopackUpdateService.cs`: 92 | Retain context-free check processing. This continuation only updates private state and returns its result; caller continuation affinity is controlled by the caller's await. Backend application and UI prompt affinity are explicitly tested. |

Stryker's return repair is implemented by its
[EndingReturnEngine](https://github.com/stryker-mutator/stryker-net/blob/dotnet-stryker%405.0.0/src/Stryker.Core/Stryker.Core/Instrumentation/EndingReturnEngine.cs).
See the [configuration reference](https://stryker-mutator.io/docs/stryker-net/configuration/)
for filter and score semantics. Keep exact-run reports with the release checklist;
rerun at the reviewed/tagged commit before claiming release acceptance.
