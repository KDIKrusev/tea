# Golden-Master Suite

Runs every import-ready profile in [`docs/qa/manual-test-scenarios`](../../docs/qa/manual-test-scenarios)
through the production pipeline and compares the **whole API response** with an approved snapshot.

Its job is to make refactoring safe: it answers *"did anything observable change?"* — not
*"are the numbers right?"*. Correctness was established by the manual passes against the reference
workbook and is documented per scenario in
[`calculations/`](../../docs/qa/manual-test-scenarios/calculations). The snapshots were approved
against those cards and cross-checked against a live API instance (real SQL Server, real
`appsettings.json`): all 18 matched to within 1e-9.

## Layout

| Path | Role |
|---|---|
| `GoldenMasterTests.cs` | `[Theory]` over the scenario folder + a guard that every scenario has a snapshot |
| `GoldenScenarioHost.cs` | wires the real services; mirrors `CalculatorController`'s validate → calculate → 400-on-infeasible flow |
| `JsonApproval.cs` | snapshot compare with a 1e-9 relative tolerance on numbers, reporting differences by JSON path |
| `GoldenPaths.cs` | repo-relative paths via `[CallerFilePath]` (survives `-p:BaseOutputPath` redirects) |
| `Fixtures/db-fixture.json` | the DB rows the calculator reads — engine SFOC curves, sail lookup table, integration levels |
| `Expected/*.json` | one approved response per scenario |

Only two things are stubbed: the database (→ fixture) and HTTP. Everything between input and
output is production code, so no SQL Server is needed to run the suite.

## Daily use

```bash
dotnet test KSailCalc.Tests/KSailCalc.Tests.csproj --filter "FullyQualifiedName~GoldenMasterTests"
```

A failure prints the exact JSON paths that moved:

```
04-dp-redundancy-reserve.json: 2 difference(s) from the approved result:
   $.result.baselineFOC: expected 14534.8364, got 14534.9012 (Δ 0.0648)
```

## Adding a scenario

1. Drop the exported profile in `docs/qa/manual-test-scenarios/`.
2. Write its calculation card in `calculations/` (that is where the numbers get justified).
3. `GOLDEN_UPDATE=1 dotnet test … --filter GoldenMasterTests` to generate the snapshot.
4. **Review the generated file against the card**, then commit both.

If the scenario uses an engine the fixture does not carry, the test fails with the id and a
pointer to `regenerate-fixture.ps1` — it never silently falls back to a default SFOC curve.

## When a change is intended

Re-run with `GOLDEN_UPDATE=1`, then read `git diff` on `Expected/`. Every moved number must have
an explanation; if it does not, the refactor was not behaviour-preserving. An approval is only
worth what its reviewer checked.

## Regenerating the fixture

Needs a live database — only when engine curves, the sail table or integration levels change,
or a new scenario needs an uncaptured engine:

```powershell
./regenerate-fixture.ps1                      # default ids 1,2,4,5,6,7,8
./regenerate-fixture.ps1 -EngineTypeIds 1,2,4,5,6,7,8,9
```
