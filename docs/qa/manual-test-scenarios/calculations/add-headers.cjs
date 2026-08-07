#!/usr/bin/env node
/**
 * Injects the standard header block at the top of every scenario card, then run build-pdf.cjs.
 *
 *   node add-headers.cjs && node build-pdf.cjs
 *
 * Idempotent: an existing block is replaced, never stacked. Edit the tables below, re-run.
 *
 * The block exists so that a single PDF is self-sufficient. A reader who opens
 * 04-dp-redundancy-reserve.pdf alone should not have to find another document to learn that
 * Level 1 runs per mode, or why a reserve row is covered 1:1.
 */
const fs = require('fs');
const path = require('path');

const DIR = __dirname;

// ─── The mechanics, written once and reused ──────────────────────────────────

const M = {
  MODES:
    'Every active mode runs its **own** Level 1 — own demand, own combinations, own baseline, own ' +
    'optimum, own t/h. There is no single "tonnes per hour" for the vessel; the year is ' +
    '`Σ (mode t/h × mode hours)`.',
  L2L3:
    'Level 2 and Level 3 are computed for **Transit only** (decision D4/Q5 — no workbook counterpart ' +
    'elsewhere). Other modes get an empty result, and the pinned-baseline radio does not reach them.',
  TABLE:
    'The **Assumed Configuration** table shows Transit\'s combinations only, in t/h. The Fuel ' +
    'Consumption figure above it is the annual total across **all** modes.',
  CANCEL:
    'Savings are a **difference**, so a mode whose baseline equals its optimum contributes zero — ' +
    'it is present on both sides and cancels, not excluded.',
  CASCADE:
    'Cascade per row: `H` = what it wants · `I = min(remaining budget, H)` · `J = I × CoverageFactor` ' +
    '(covered) · `L = H − J` (left to the gensets). Priority: DpReserve → DpDemand → Mission → ' +
    'Propulsion → Hotel. Invariant `ΣJ + ΣL = ΣH` — the sea sets the swing, the battery moves the split.',
  FACTORS:
    'Peak-shaving `H` = average × VariationFactor (propulsion 5 %, hotel 2 %). CoverageFactor is a ' +
    'modelling assumption about how much of a swing a battery can realistically catch — propulsion ' +
    '0.35, hotel 0.05. Both live in `appsettings.json`, not in code.',
  RESERVE:
    'A **Reserve** row is not a swing: `H` = the full requirement, `CoverageFactor` = 1.00 (kW for kW), ' +
    'and it counts toward Spinning Reserve but **not** toward Peak Shaving. A covered reserve is ' +
    'readiness, so it never appears in the demand.',
  MISSION:
    'A **Mission** row takes `H` = the heavy consumer\'s full rating — a crane can start at any moment, ' +
    'so its whole draw is a potential peak.',
  DEMAND:
    'Only the **uncovered** part rejoins the demand: `propulsion\' = propulsion + L`, ' +
    '`hotel\' = hotel + L`. Covered power (`J`) is never subtracted from anything.',
  SGFIRST:
    'The shaft generator is filled before any auxiliary starts (the main engine is already turning), ' +
    'and its output is a load **on** that main engine — which is why the ME figure exceeds propulsion. ' +
    'SG capacity scales with the number of running MEs.',
  CURVE:
    'SFOC (g/kWh) depends on **load**: a large 2-stroke burns ~167 at 63 % and over 230 at 5 %. That ' +
    'curve, not the arithmetic, is why running spare engines at low load is expensive.',
  BASELINE:
    'Baseline rule: **no battery → the worst combination** (`count − 1`); **battery active → the third ' +
    'from worst** (`Math.Max(0, count − 3)`). It models what the ship is assumed to do today.',
  CLAMP:
    '`Math.Max(0, …)` bites on short lists: with only two combinations, `2 − 3` clamps to **0** — the ' +
    'optimum itself — so that mode reports **zero** Level 1 savings. A battery in a mode can therefore ' +
    'suppress that mode\'s Level 1 savings; the value moves to the Battery Benefit badge instead.',
  WORLDS:
    'Battery Benefit runs the pipeline **twice**: world A (demand = average + uncovered `L`) and ' +
    'world B (demand = average + the full swing `H`). `Benefit = (FOC_B − FOC_A) × hours`. Both are ' +
    '**optima**; a pinned baseline is ignored. Unticking "Enable Battery" in the UI gives a *third* ' +
    'world — raw demand, swing carried by nobody — which burns less than A and is not the comparison.',
  WEIGHTED:
    'With several modes the Power Demands tables gain one row per mode, and the header is an ' +
    '**hours-weighted average** (total energy ÷ total hours), not a sum.',
  FUELS:
    'ME and AE each use **their own fuel\'s** CO2 factor (MGO/MDO 3.93267 · HFO 3.114 · LNG 2.753 · ' +
    'Ammonia 0.35154). The two per-engine cards must sum to the panel total.',
  SAIL:
    'Sail thrust is subtracted from propulsion **before** the cascade runs, so it shrinks the swing ' +
    'itself — not just the average load.',
  PTI:
    'PTI is the shaft motor: the battery pushing power through the main engine\'s shaft. A gate checks ' +
    'that the installed PTI capacity can carry the propulsion band the battery is asked to shave; if ' +
    'not, the whole calculation is refused with a 400.',
  VALIDATION:
    'Validation runs **before** Level 1. A plant that cannot carry its load is rejected there, with a ' +
    'different message and a different code path from the Level 1 rejections.',
  LOOKUP:
    'When the Level 3 variation field is empty the backend looks it up from the vessel type ' +
    '(Bulk 250 · Container 1 500 · LNG 1 000 · otherwise the 500 default), all from `appsettings.json`.',
  SATURATION:
    'Past saturation extra battery power buys nothing: once `ΣH` is fully taken, the remaining budget ' +
    'has no swing left to cover.',
  CAPACITY:
    'Only `powerKw` enters the fuel calculation. `capacityKwh` drives one plausibility warning — can ' +
    'it sustain that power for 30 minutes — and changes no number on the results panel.',
  L2WORK:
    'Level 2 redistributes the hotel load between the shaft generator and the auxiliaries, looking for ' +
    'a cheaper split. It can only find one when there is something to redistribute — with the SG at its ' +
    'ceiling and a single aux, it returns zero.',
  L3WORK:
    'Level 3 (DRC) damps the hotel/mission swing: `variation × 0.8`, minus whatever the battery already ' +
    'shaved, so the same kilowatt is never counted twice.',
  ZEROGUARD:
    'A battery assigned to a mode with zero hours must change nothing and say nothing — no panel, no ' +
    'warning, no effect on the result.',
};

// ─── Per scenario: what it proves, and the mechanics it turns on ─────────────

const CARDS = {
  '01': { proves: 'The whole pipeline on the reference plant — every Excel "Load Demands" cell reproduced 1:1.',
          mech: ['CASCADE', 'FACTORS', 'DEMAND', 'SGFIRST', 'CURVE', 'BASELINE', 'WORLDS'] },
  '02': { proves: 'What happens when the budget runs out mid-cascade: propulsion takes all 300, the hotel row gets nothing.',
          mech: ['CASCADE', 'FACTORS', 'SATURATION'] },
  '03': { proves: 'The no-battery reference world — this scenario IS world B of 01\'s Battery Benefit, written down as its own file.',
          mech: ['WORLDS', 'BASELINE', 'SGFIRST'] },
  '04': { proves: 'RESERVE ≠ PEAK SHAVING, and two operational modes inside one calculation.',
          mech: ['RESERVE', 'MODES', 'L2L3', 'TABLE', 'CANCEL', 'CLAMP', 'WEIGHTED'] },
  '05': { proves: 'The Mission row: a crane\'s full rating is its swing, and the cascade continues below it.',
          mech: ['MISSION', 'CASCADE', 'DEMAND'] },
  '06': { proves: 'Priority devouring the whole budget — and the anti-double-counting rule between the battery and Level 3.',
          mech: ['MISSION', 'CASCADE', 'L3WORK'] },
  '07': { proves: 'Modes never overlap in time, so each one runs its own cascade with the FULL budget.',
          mech: ['MODES', 'CASCADE', 'WEIGHTED', 'L2L3'] },
  '08': { proves: 'The PTI discharge gate passing silently: enough shaft-motor capacity, no change to any result.',
          mech: ['PTI', 'CASCADE'] },
  '09': { proves: 'The PTI gate blocking: the app refuses to calculate, and the message names the missing kW.',
          mech: ['PTI', 'VALIDATION'] },
  '10': { proves: 'Capacity (kWh) drives a warning only — beyond saturation, extra battery power buys nothing.',
          mech: ['CAPACITY', 'SATURATION'] },
  '11': { proves: 'All five operational modes at once, and the only scenario where Level 3 contributes a visible figure.',
          mech: ['MODES', 'L2L3', 'CANCEL', 'CLAMP', 'WEIGHTED', 'L3WORK', 'LOOKUP'] },
  '12': { proves: 'Sail: an intervention applied BEFORE the cascade, which shrinks the swing itself.',
          mech: ['SAIL', 'CASCADE', 'WORLDS'] },
  '13': { proves: 'Per-fuel CO2: ME and AE burning different fuels must produce two splits that sum to the panel total.',
          mech: ['FUELS', 'CURVE'] },
  '14': { proves: 'The Level 3 variation looked up from the vessel type when the field is left empty.',
          mech: ['LOOKUP', 'L3WORK', 'BASELINE'] },
  '15': { proves: 'A user-pinned baseline (rule D1) — and the client bug the import revealed.',
          mech: ['BASELINE', 'TABLE', 'L2L3'] },
  '16': { proves: 'Sea margin multiplies propulsion BEFORE the swing is computed, so the whole cascade grows.',
          mech: ['CASCADE', 'FACTORS', 'DEMAND'] },
  '17': { proves: 'Validation refusing the plant before Level 1 ever runs — a different failure path from 09.',
          mech: ['VALIDATION'] },
  '18': { proves: 'The zero-effect guard: a battery assigned to a mode with no hours must change nothing, silently.',
          mech: ['ZEROGUARD', 'MODES'] },
  '19': { proves: 'The only scenario where Level 2 does visible work — and Level 3 with it.',
          mech: ['L2WORK', 'L3WORK', 'L2L3', 'SGFIRST', 'BASELINE'] },
  '20': { proves: 'Level 1 rejecting a mode because the installed engines cannot carry its demand.',
          mech: ['MODES', 'VALIDATION'] },
  '21': { proves: 'Level 1 rejecting because the auxiliaries would have to run above the 90 % limit.',
          mech: ['VALIDATION', 'SGFIRST'] },
  '22': { proves: 'Level 1 rejecting structurally: no combination can cover the hotel load at all.',
          mech: ['VALIDATION', 'SGFIRST'] },
  '23': { proves: 'HFO on both engines — one fuel factor, applied to both sides.',
          mech: ['FUELS'] },
  '24': { proves: 'Ammonia main + MGO aux: an 11× factor gap on one vessel, the strongest fuel guard in the suite.',
          mech: ['FUELS'] },
  '25': { proves: 'A Container vessel picking up its 1 500 kW Level 3 variation from the lookup table.',
          mech: ['LOOKUP', 'L3WORK'] },
  '26': { proves: 'An unrecognised vessel type falling back to the 500 kW default variation.',
          mech: ['LOOKUP', 'L3WORK'] },
  '27': { proves: 'Transit plus Anchor — a second mode with hotel load but no propulsion.',
          mech: ['MODES', 'L2L3', 'WEIGHTED', 'CANCEL'] },
  '28': { proves: 'Transit plus Maneuvering — a second mode that has both propulsion and hotel.',
          mech: ['MODES', 'L2L3', 'WEIGHTED', 'CANCEL'] },
  '29': { proves: 'A battery budget past saturation: more power, identical result.',
          mech: ['SATURATION', 'CASCADE'] },
  '30': { proves: 'A pinned baseline index outside the valid range, and how it is handled.',
          mech: ['BASELINE', 'CLAMP', 'TABLE'] },
  '31': { proves: 'Level 2 with a shaft generator in the mix.',
          mech: ['L2WORK', 'SGFIRST'] },
  '32': { proves: 'PTI assist actually engaging, inside DP mode.',
          mech: ['PTI', 'MODES', 'RESERVE'] },
  '33': { proves: 'An LNG vessel picking up its 1 000 kW Level 3 variation from the lookup.',
          mech: ['LOOKUP', 'L3WORK', 'FUELS'] },
  '34': { proves: 'Advisory warnings only — results are still computed and shown.',
          mech: ['VALIDATION'] },
  '35': { proves: 'Several validation errors reported together in one response.',
          mech: ['VALIDATION'] },
};

// Scenarios sharing the reference "Excel plant": propulsion 11 463 · hotel 3 800 · SM 0 ·
// ME 2×24 000 · SG 3 250 · AE 4×4 000 · Transit 5 000 h.
const EXCEL_PLANT = new Set(['01','02','03','04','05','06','07','08','09','10','12','13','14','15','16','17','18']);

const READ_AFTER = {
  '02': '01', '03': '01', '04': '03', '05': '04', '06': '05', '07': '01', '08': '01', '09': '08',
  '10': '01', '11': '04', '12': '01', '13': '01', '14': '11', '15': '01', '16': '01', '17': '09',
  '18': '01', '19': '11', '20': '17', '21': '20', '22': '20', '23': '13', '24': '13', '25': '14',
  '26': '14', '27': '07', '28': '07', '29': '10', '30': '15', '31': '19', '32': '08', '33': '14',
  '34': '17', '35': '17',
};

const MARKER = '<!-- header:auto -->';

function build(num, sections) {
  const card = CARDS[num];
  const out = [MARKER, ''];

  out.push(`> **Proves** · ${card.proves}`);
  out.push('>');
  out.push('> **Mechanics this scenario turns on**');
  for (const key of card.mech) {
    out.push(`> - ${M[key]}`);
  }
  out.push('>');
  out.push(`> **Panels described below** · ${sections.join(' · ')}`);
  out.push('>');

  if (num === '01') {
    out.push('> **This is the reference card.** The other Excel-plant scenarios (02–10, 12–18) describe ' +
             'only what they change and refer back to this one for the rest.');
  } else if (EXCEL_PLANT.has(num)) {
    out.push('> **Anything not described here** behaves exactly as in `01-excel-baseline` — same plant, ' +
             'same hours; only what this scenario changes is worked through below.');
  } else {
    out.push('> **Anything not described here** — the mechanics above name the step that produced it; ' +
             '`00-ORIENTATION` Part 6 has the full number-to-step index.');
  }

  out.push('>');
  out.push(Number(num) <= 18
    ? '> **Trust** · verified against the reference workbook. These figures are proof.'
    : '> **Trust** · characterisation snapshot, generated from the code. It detects change; it does ' +
      'NOT prove correctness. Figures marked *pending reference verification* have never been checked ' +
      'against anything outside the application.');

  if (READ_AFTER[num]) {
    out.push('>');
    out.push(`> **Read after** · scenario ${READ_AFTER[num]}.`);
  }

  out.push('');
  return out.join('\n');
}

let done = 0;
for (const file of fs.readdirSync(DIR).filter(f => /^\d\d-.*\.md$/.test(f)).sort()) {
  const num = file.slice(0, 2);
  if (!CARDS[num]) continue;

  const full = path.join(DIR, file);
  let text = fs.readFileSync(full, 'utf8');
  text = text.replace(new RegExp(`${MARKER}\\n\\n(?:>.*\\n|\\n)*?(?=\\n?[^>\\n])`), '');

  const sections = [...text.matchAll(/^## (.+)$/gm)].map(m => m[1].replace(/\s*\(.*?\)\s*$/, '').trim());
  const nl = text.indexOf('\n');
  const h1 = text.slice(0, nl);
  const body = text.slice(nl + 1).replace(/^\n+/, '');

  fs.writeFileSync(full, `${h1}\n\n${build(num, sections)}\n${body}`, 'utf8');
  done++;
}
console.log(`${done} cards updated`);
