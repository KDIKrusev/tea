# Battery Feature — Analysis Workspace

**Analyst:** Mary (BMAD Business Analyst) · **Created:** 2026-07-13 · **Status:** Initial deep analysis

This folder holds all analysis artifacts for the **Battery Configuration / Spinning Reserve / Peak Shaving** feature
requested for the KSailCalc iEMS Savings Calculator.

## Source materials

| Source | Location | Role |
|---|---|---|
| Task request (Teams screenshot, Krishna Kumar Nagalingam, meeting 5/22) | `docs/Screenshot 2026-07-13 094513.png` | The feature ask |
| Power plant + battery reference model | `docs/PowerPlantSetupAdvisesIncludingPTIOAndbatteries_test.xlsx` | **Primary algorithm reference** — battery peak shaving / spinning reserve / PTI-PTO / optimal setup selection |
| Legacy machinery calculation tool | `docs/MachCalcTool-20200821-v2.43 linear 1 (1).xlsm` | Secondary reference — battery efficiencies, hybrid/battery machinery alternatives, annual statistics |
| Application backend | `c:\Камен\1307calc\tea` (ASP.NET Core, .NET 10) | Current implementation |
| Application frontend | `cl/` (Angular 18) | Current implementation |
| Database | SQL Server `VoyageEnergyDB` (local, trusted connection) | Config data: `IntegrationLevel`, `EngineType`, `VesselType`, `Configurations` |

## Documents in this folder

1. **[01-task-brief.md](01-task-brief.md)** — What is being asked: interpretation of the screenshot, business context, requirements draft.
2. **[02-excel-model-analysis.md](02-excel-model-analysis.md)** — Reverse-engineering of the two Excel workbooks: the battery allocation algorithm, spinning-reserve/peak-shaving math, PTI/PTO model, optimal-setup selection.
3. **[03-gap-analysis.md](03-gap-analysis.md)** — Current system state (backend / client / DB) vs. what the feature needs; discrepancies found; proposed implementation direction.
4. **[04-open-questions.md](04-open-questions.md)** — Numbered open questions that must be answered by stakeholders before/while implementing.
5. **[05-decisions-log.md](05-decisions-log.md)** — Running log of stakeholder decisions (D1: baseline stays user-selectable; D2: Excel workbooks are the authoritative calculation reference → PTI/PTO in scope).
6. **[06-architecture-design.md](06-architecture-design.md)** — Architect's design (Winston): pipeline placement, new models/services, Level 1/3 changes, PTI gates, API contract, Angular form/results changes, build increments A–E, risks, ADRs.
7. **[07-excel-fidelity-audit.md](07-excel-fidelity-audit.md)** — Formula-by-formula audit vs the workbook + numeric cross-check of the Excel's own saved cell values against our pinned tests (verdict: battery arithmetic is a 1:1 match; deviations confined to the pre-existing plant model, all documented).

## Suggested reading order

Start with `01-task-brief.md`, then `04-open-questions.md` (decisions needed), then dive into `02` / `03` for the detail.
