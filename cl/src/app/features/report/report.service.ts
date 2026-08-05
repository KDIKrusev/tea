import { Injectable } from '@angular/core';

/** One integration level row in the savings ladder */
export interface ReportLevel {
  name: string;
  description: string;
  fuelSavingsTonPerYear: number;
  costSavingsPerYear: number;
  recommended: boolean;
}

/** Single operational mode entry for the power-profile chart */
export interface ReportOperationalMode {
  /** "Transit" | "Port" | "Anchor" | "Maneuvering" | "DP" */
  name: string;
  /** Annual hours in this mode */
  hours: number;
  /** Total annual energy demand (ME + AE) in kWh */
  energyKwh: number;
}

/** Everything the client report needs — collected from existing calculator-page state */
export interface ReportData {
  clientName: string | null;
  vesselLabel: string;
  generatedAt: Date;
  baselineFocTonPerYear: number;
  baselineCo2TonPerYear: number;
  fuelPriceUsdPerTon: number;
  /** Chosen fuel(s): single value (e.g. "MGO") or "ME LNG · AE MGO" when they differ */
  fuelLabel: string;
  levels: ReportLevel[];
  /** KPIs and payback come from the recommended level */
  recommendedLevelName: string;
  recommendedFuelSavings: number;
  recommendedCo2Reduction: number;
  recommendedCostSavings: number;
  recommendedInvestment: number;
  recommendedPaybackYears: number;
  /** Operational mode breakdown for the power-profile donut chart */
  operationalModes?: ReportOperationalMode[];
  /** Baseline engine config label, e.g. "1 ME + 3 AE" */
  baselineEngineConfig?: string;
}

/**
 * Builds the self-contained "Energy Savings Assessment" document (approved mockup
 * docs/design/report-mockup.html) and opens it in a new tab for print / save-as-PDF.
 * Intentionally NOT an in-app component: the standalone document keeps print styling
 * deterministic and fully isolated from application CSS (Epic 2 architecture decision).
 */
@Injectable({ providedIn: 'root' })
export class ReportService {

  /** Returns false when the popup was blocked — caller informs the user. */
  open(data: ReportData): boolean {
    const reportWindow = window.open('', '_blank');
    if (!reportWindow) {
      return false;
    }
    reportWindow.document.open();
    reportWindow.document.write(this.buildReportHtml(data));
    reportWindow.document.close();
    return true;
  }

  buildReportHtml(data: ReportData): string {
    const esc = (s: string): string => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    const ton = (n: number): string => n.toLocaleString('en-US', { minimumFractionDigits: 1, maximumFractionDigits: 1 });
    const usd = (n: number): string => '$' + Math.round(n).toLocaleString('en-US');
    const int = (n: number): string => Math.round(n).toLocaleString('en-US');

    const dateStr = data.generatedAt.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
    const refStr = `ESA-${data.generatedAt.getFullYear()}-${String(data.generatedAt.getMonth() + 1).padStart(2, '0')}${String(data.generatedAt.getDate()).padStart(2, '0')}`;

    const maxSavings = Math.max(...data.levels.map(l => l.fuelSavingsTonPerYear), 1);
    const ladderRows = data.levels.map(level => {
      const widthPct = Math.max(4, Math.round((level.fuelSavingsTonPerYear / maxSavings) * 100));
      const chip = level.recommended ? '<span class="reco-chip">Recommended</span>' : '';
      const barClass = level.recommended ? 'bar recommended' : 'bar';
      return `
      <div class="rung">
        <div class="lvl">${esc(level.name)} ${chip}<span class="d">${esc(level.description)}</span></div>
        <div class="barwrap"><div class="${barClass}" style="width:${widthPct}%"></div></div>
        <div class="val">${ton(level.fuelSavingsTonPerYear)} ton/yr<small>${usd(level.costSavingsPerYear)} / yr</small></div>
      </div>`;
    }).join('');

    // Marketing equivalences: avg EU passenger car ~4.6 t CO2/yr; mature tree ~22 kg CO2/yr
    const cars = Math.round(data.recommendedCo2Reduction / 4.6);
    const trees = Math.round(data.recommendedCo2Reduction * 1000 / 22);

    const paybackMonths = data.recommendedPaybackYears * 12;
    const paybackText = paybackMonths < 24
      ? `~${Math.max(1, Math.round(paybackMonths))} months`
      : `~${data.recommendedPaybackYears.toFixed(1)} years`;

    const clientBlock = data.clientName
      ? `<div class="meta-item"><div class="k">Prepared for</div><div class="v">${esc(data.clientName)}</div></div>`
      : '';

    // ── Power-profile donut chart ──────────────────────────────────────────────
    let profileSection = '';
    const profileModes = (data.operationalModes ?? []).filter(m => m.energyKwh > 0);
    const totalProfileEnergy = profileModes.reduce((s, m) => s + m.energyKwh, 0);
    if (profileModes.length > 0 && totalProfileEnergy > 0) {
      const cx = 90, cy = 90, r = 60, sw = 26;
      const circ = 2 * Math.PI * r;
      const modeColors: Record<string, string> = {
        Transit: '#00a3a1', Port: '#0b2540', Anchor: '#4a90c4',
        Maneuvering: '#f59e0b', DP: '#7c3aed'
      };
      const fallback = ['#00a3a1', '#0b2540', '#4a90c4', '#f59e0b', '#7c3aed', '#e84393'];
      let cumArc = 0;
      const svgSegments = profileModes.map((m, i) => {
        const arc = (m.energyKwh / totalProfileEnergy) * circ;
        const color = modeColors[m.name] ?? fallback[i % fallback.length];
        const dashOffset = circ * 0.25 - cumArc;
        cumArc += arc;
        return `<circle cx="${cx}" cy="${cy}" r="${r}" fill="none" stroke="${color}" stroke-width="${sw}" ` +
          `stroke-dasharray="${arc.toFixed(2)} ${(circ - arc).toFixed(2)}" stroke-dashoffset="${dashOffset.toFixed(2)}"/>`;
      }).join('');
      const legendRows = profileModes.map((m, i) => {
        const pct = ((m.energyKwh / totalProfileEnergy) * 100).toFixed(1);
        const color = modeColors[m.name] ?? fallback[i % fallback.length];
        return `<div class="pl-row">` +
          `<span class="pl-dot" style="background:${color}"></span>` +
          `<span class="pl-name">${esc(m.name)}</span>` +
          `<span class="pl-val">${Math.round(m.hours).toLocaleString('en-US')} h/yr</span>` +
          `<span class="pl-pct">${pct}%</span></div>`;
      }).join('');
      const baselineTag = data.baselineEngineConfig
        ? `<div class="baseline-tag">Baseline: <b>${esc(data.baselineEngineConfig)}</b></div>` : '';
      profileSection = `
  <div class="section">
    <div class="sec-title">Power profile — operational modes</div>
    <div class="sec-sub">Annual energy demand share across operating modes</div>
    <div class="profile-wrap">
      <div class="donut-wrap">
        <svg viewBox="0 0 180 180" width="150" height="150" style="display:block;">${svgSegments}</svg>
        ${baselineTag}
      </div>
      <div class="profile-legend">${legendRows}</div>
    </div>
  </div>`;
    }
    // ──────────────────────────────────────────────────────────────────────────

    return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>Energy Savings Assessment — ${esc(data.vesselLabel)}</title>
<style>
  :root {
    --navy: #0b2540; --navy-2: #133a5e; --teal: #00a3a1; --teal-light: #e0f4f4;
    --ink: #1f2937; --muted: #6b7280; --line: #e5e7eb; --good: #0e9f6e; --paper: #ffffff;
  }
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body { font-family: "Segoe UI", Arial, Helvetica, sans-serif; background: #9aa5b1; color: var(--ink);
         display: flex; flex-direction: column; align-items: center; padding: 24px; }
  .toolbar { width: 794px; display: flex; justify-content: flex-end; margin-bottom: 12px; }
  .print-btn { background: var(--navy); color: #fff; border: none; border-radius: 6px;
               padding: 10px 22px; font-size: 14px; font-weight: 600; cursor: pointer; }
  .print-btn:hover { background: var(--navy-2); }
  .sheet { width: 794px; min-height: 1123px; background: var(--paper);
           box-shadow: 0 6px 30px rgba(0,0,0,.35); display: flex; flex-direction: column; }
  @media print {
    body { background: none; padding: 0; }
    .toolbar { display: none; }
    .sheet { box-shadow: none; width: auto; min-height: auto; }
    @page { size: A4 portrait; margin: 0; }
  }
  .rpt-header { background: var(--navy); color: #fff; padding: 28px 48px 24px; }
  .brandline { display: flex; justify-content: space-between; align-items: center; }
  .wordmark { font-size: 17px; font-weight: 700; letter-spacing: .28em; }
  .wordmark span { font-weight: 300; letter-spacing: .12em; opacity: .85; }
  .doc-ref { font-size: 10.5px; opacity: .65; text-align: right; line-height: 1.5; }
  .rpt-title { margin-top: 22px; font-size: 27px; font-weight: 300; }
  .rpt-title b { font-weight: 700; }
  .meta-strip { margin-top: 18px; display: flex; gap: 36px;
                border-top: 1px solid rgba(255,255,255,.18); padding-top: 14px; }
  .meta-item .k { font-size: 9.5px; text-transform: uppercase; letter-spacing: .12em; opacity: .6; }
  .meta-item .v { font-size: 13px; font-weight: 600; margin-top: 2px; }
  .hero { padding: 30px 48px 6px; }
  .hero-label { font-size: 11px; text-transform: uppercase; letter-spacing: .14em; color: var(--muted); }
  .hero-label b { color: var(--teal); }
  .kpis { display: flex; gap: 16px; margin-top: 12px; }
  .kpi { flex: 1; border: 1px solid var(--line); border-top: 4px solid var(--teal);
         border-radius: 6px; padding: 18px 20px 16px; }
  .kpi .num { font-size: 30px; font-weight: 700; color: var(--navy); }
  .kpi .num small { font-size: 14px; font-weight: 600; color: var(--muted); margin-left: 2px; }
  .kpi .cap { font-size: 11.5px; color: var(--muted); margin-top: 4px; }
  .kpi.money { border-top-color: var(--good); }
  .kpi.money .num { color: var(--good); }
  .section { padding: 26px 48px 0; }
  .sec-title { font-size: 14px; font-weight: 700; color: var(--navy); }
  .sec-sub { font-size: 11.5px; color: var(--muted); margin-top: 2px; }
  .ladder { margin-top: 16px; display: flex; flex-direction: column; gap: 12px; }
  .rung { display: grid; grid-template-columns: 200px 1fr 150px; gap: 16px; align-items: center; }
  .rung .lvl { font-size: 12.5px; font-weight: 700; color: var(--navy); }
  .rung .lvl .d { display: block; font-size: 10.5px; font-weight: 400; color: var(--muted); margin-top: 2px; line-height: 1.35; }
  .barwrap { background: #f1f5f9; border-radius: 4px; height: 26px; overflow: hidden; }
  .bar { height: 100%; border-radius: 4px 0 0 4px; background: linear-gradient(90deg, var(--navy-2), var(--teal)); }
  .bar.recommended { background: linear-gradient(90deg, #0c7c7a, var(--teal)); }
  .rung .val { text-align: right; font-size: 13px; font-weight: 700; color: var(--navy); }
  .rung .val small { display: block; font-weight: 600; color: var(--good); font-size: 11.5px; margin-top: 1px; }
  .reco-chip { display: inline-block; background: var(--teal-light); color: #066a68; font-size: 9.5px;
               font-weight: 700; letter-spacing: .08em; text-transform: uppercase; border-radius: 3px;
               padding: 2px 7px; margin-left: 8px; vertical-align: 1px; }
  .payband { margin: 24px 48px 0; border-radius: 6px; background: var(--teal-light);
             display: flex; align-items: center; justify-content: space-between; padding: 14px 22px; }
  .payband .txt { font-size: 12.5px; color: #044f4e; }
  .payband .txt b { font-size: 14px; }
  .payband .pb { font-size: 22px; font-weight: 700; color: #066a68; white-space: nowrap; }
  .payband .pb small { font-size: 11px; font-weight: 600; display: block; text-align: right; color: #4a8f8e; }
  .equiv { margin: 18px 48px 0; display: flex; gap: 14px; }
  .eq { flex: 1; display: flex; gap: 10px; align-items: center; border: 1px dashed var(--line);
        border-radius: 6px; padding: 10px 14px; font-size: 11.5px; color: var(--muted); }
  .eq b { color: var(--ink); }
  .eq .ico { font-size: 18px; }
  .assumptions { margin-top: 8px; }
  .as-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin-top: 12px; }
  .as { border-left: 3px solid var(--line); padding-left: 10px; }
  .as .k { font-size: 9.5px; text-transform: uppercase; letter-spacing: .1em; color: var(--muted); }
  .as .v { font-size: 12px; font-weight: 600; margin-top: 2px; color: var(--ink); }
  .spacer { flex: 1; }
  .disclaimer { margin: 26px 48px 0; padding: 12px 16px; background: #f8fafc;
                border: 1px solid var(--line); border-radius: 6px; font-size: 9.8px;
                line-height: 1.55; color: var(--muted); }
  .disclaimer b { color: var(--ink); font-size: 10px; }
  .rpt-footer { margin-top: 18px; padding: 14px 48px 20px; border-top: 3px solid var(--navy);
                display: flex; justify-content: space-between; align-items: center;
                font-size: 9.5px; color: var(--muted); }
  .rpt-footer .fm { letter-spacing: .2em; font-weight: 700; color: var(--navy); }
  /* ── Power-profile chart ───────────────────────────────────────── */
  .profile-wrap { display: flex; gap: 24px; align-items: center; margin-top: 14px; }
  .donut-wrap { display: flex; flex-direction: column; align-items: center; gap: 6px; flex-shrink: 0; }
  .baseline-tag { font-size: 10px; color: var(--muted); text-align: center; }
  .baseline-tag b { color: var(--ink); }
  .profile-legend { flex: 1; display: flex; flex-direction: column; gap: 7px; }
  .pl-row { display: grid; grid-template-columns: 10px 1fr 72px 40px; gap: 8px; align-items: center; font-size: 11px; }
  .pl-dot { width: 10px; height: 10px; border-radius: 50%; }
  .pl-name { color: var(--ink); font-weight: 500; }
  .pl-val { color: var(--muted); text-align: right; }
  .pl-pct { font-weight: 700; color: var(--navy); text-align: right; }
</style>
</head>
<body>
<div class="toolbar"><button class="print-btn" onclick="window.print()">🖨 Print / Save as PDF</button></div>
<div class="sheet">
  <div class="rpt-header">
    <div class="brandline">
      <div class="wordmark">KONGSBERG <span>MARITIME</span></div>
      <div class="doc-ref">Ref: ${refStr}<br>Generated: ${dateStr}</div>
    </div>
    <div class="rpt-title">Energy Savings <b>Assessment</b></div>
    <div class="meta-strip">
      ${clientBlock}
      <div class="meta-item"><div class="k">Vessel</div><div class="v">${esc(data.vesselLabel)}</div></div>
      <div class="meta-item"><div class="k">Basis</div><div class="v">Annual operation</div></div>
      <div class="meta-item"><div class="k">System</div><div class="v">iEMS Energy Management</div></div>
    </div>
  </div>

  <div class="hero">
    <div class="hero-label">Your potential annual savings — with <b>iEMS ${esc(data.recommendedLevelName)} integration</b></div>
    <div class="kpis">
      <div class="kpi"><div class="num">${ton(data.recommendedFuelSavings)} <small>ton</small></div><div class="cap">Fuel saved per year</div></div>
      <div class="kpi"><div class="num">${ton(data.recommendedCo2Reduction)} <small>ton</small></div><div class="cap">CO₂ emissions avoided per year</div></div>
      <div class="kpi money"><div class="num">${usd(data.recommendedCostSavings)}</div><div class="cap">Operating cost saved per year</div></div>
    </div>
  </div>

  <div class="section">
    <div class="sec-title">Savings grow with every integration level</div>
    <div class="sec-sub">Each level builds on the previous one — from smart power system advice to fully integrated, weather-aware energy control.</div>
    <div class="ladder">${ladderRows}
    </div>
  </div>

  <div class="payband">
    <div class="txt">Estimated initial investment of <b>${usd(data.recommendedInvestment)}</b> is recovered through fuel savings alone.</div>
    <div class="pb">${paybackText}<small>estimated payback</small></div>
  </div>

  <div class="equiv">
    <div class="eq"><span class="ico">🚗</span><span><b>≈ ${int(cars)} passenger cars</b> taken off the road each year</span></div>
    <div class="eq"><span class="ico">🌲</span><span>CO₂ uptake of <b>≈ ${int(trees)} trees</b> growing for a year</span></div>
  </div>

  <div class="section assumptions">
    <div class="sec-title">Basis of estimate</div>
    <div class="as-grid">
      <div class="as"><div class="k">Baseline consumption</div><div class="v">${ton(data.baselineFocTonPerYear)} ton fuel / yr</div></div>
      <div class="as"><div class="k">Baseline CO₂</div><div class="v">${ton(data.baselineCo2TonPerYear)} ton / yr</div></div>
      <div class="as"><div class="k">Fuel price assumed</div><div class="v">${int(data.fuelPriceUsdPerTon)} USD / ton</div></div>
      <div class="as"><div class="k">Fuel</div><div class="v">${esc(data.fuelLabel)}</div></div>
      <div class="as"><div class="k">Operational profile</div><div class="v">Statistical, vessel class based</div></div>
    </div>
  </div>

  ${profileSection}

  <div class="spacer"></div>

  <div class="disclaimer">
    <b>Important notice.</b> This assessment is an indicative estimate produced by the Kongsberg Maritime Energy
    Savings Calculator. It is based on statistical operational profiles and reference vessel data — it is not a
    detailed engineering simulation of a specific vessel. Actual savings will vary with vessel condition, trade
    pattern, weather and operational practice. This document does not constitute a performance guarantee or a
    commercial offer. A vessel-specific study is recommended before investment decisions.
  </div>

  <div class="rpt-footer">
    <div class="fm">KONGSBERG</div>
    <div>Energy Savings Calculator</div>
    <div>Page 1 of 1</div>
  </div>
</div>
</body>
</html>`;
  }
}
