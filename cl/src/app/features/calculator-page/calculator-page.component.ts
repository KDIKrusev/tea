import { Component, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { Subject, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { VesselInputFormComponent, FormChangeEvent } from '../vessel-input/vessel-input-form/vessel-input-form.component';
import { ProfileManagerComponent } from '../profiles/profile-manager.component';
import { SavedProfile } from '../../core/profile.types';
import { PowerDemandsPanelComponent } from '../results-display/power-demands-panel/power-demands-panel.component';
import { BaselinePanelComponent } from '../results-display/baseline-panel/baseline-panel.component';
import { BatteryContributionPanelComponent } from '../results-display/battery-contribution-panel/battery-contribution-panel.component';
import { VariantDetailPanelComponent } from '../results-display/variant-detail-panel/variant-detail-panel.component';
import { ValidationWarningsComponent } from '../results-display/validation-warnings/validation-warnings.component';
import { SavingsChartComponent } from '../charts/savings-chart/savings-chart.component';
import { TierComparisonComponent } from '../results-display/tier-comparison/tier-comparison.component';
import { CalculatorService } from '../../calculations/calculator.service';
import {
  CalculatorInput,
  VariantResult,
  AllVariantsCalculationResult,
  BaselineData,
  PowerDemands,
  ValidationWarning,
  SailContributionResult,
  BatteryDetails
} from '../../calculations/calculator.types';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ReportDialogComponent } from '../report/report-dialog.component';
import { ReportData } from '../report/report.service';

/** The three integration levels, as data. */
export type TierKey = 'Advanced' | 'Pro' | 'Premium';

/**
 * Everything that differs between the three integration-level panels.
 *
 * Story C-G: the panels were 120 lines of near-identical markup. Six values differ; the table below
 * is those six values, and the template renders it three times. Adding a level is now a row.
 */
export interface TierPanel {
  key: TierKey;
  name: string;
  subtitle: string;
  tooltip: string;
  icon: string;
  panelClass: string;
}

const TIER_PANELS: readonly TierPanel[] = [
  {
    key: 'Advanced',
    name: 'Integration level 1',
    subtitle: 'Optimal power system recommendation',
    tooltip: 'Integration Level 1: Entry-level iEMS with 3% fuel consumption reduction through basic power optimization and load balancing.',
    icon: 'bolt',
    panelClass: 'advanced-panel'
  },
  {
    key: 'Pro',
    name: 'Integration level 2',
    subtitle: 'Optimal power system recommendation + Continuous optimization',
    tooltip: 'Integration Level 2: Mid-tier iEMS with 4.5% fuel consumption reduction through enhanced load optimization and intelligent energy control functions.',
    icon: 'stars',
    panelClass: 'pro-panel'
  },
  {
    key: 'Premium',
    name: 'Integration level 3',
    subtitle: 'IL1 + IL2 + Energy control functions + Integration with weather forecast and mission plan',
    tooltip: 'Integration Level 3: Premium iEMS with 6% fuel consumption reduction through AI-driven continuous optimization integrated with weather forecast and mission planning.',
    icon: 'diamond',
    panelClass: 'premium-panel'
  }
];

/** Everything one queued calculation needs in order to be applied when its answer arrives. */
interface CalculationRequest {
  /** The form values this request was built from (used to render the panels). */
  input: CalculatorInput;
  /** What actually goes on the wire — baseline index captured at request time. */
  apiInput: CalculatorInput;
  /** Baseline re-selection: keep the panels and the spinner untouched. */
  silent: boolean;
}

@Component({
  selector: 'app-calculator-page',
  standalone: true,
  imports: [
    CommonModule,
    VesselInputFormComponent,
    PowerDemandsPanelComponent,
    BaselinePanelComponent,
    BatteryContributionPanelComponent,
    VariantDetailPanelComponent,
    ValidationWarningsComponent,
    SavingsChartComponent,
    TierComparisonComponent,
    MatProgressSpinnerModule,
    MatCardModule,
    MatExpansionModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatDialogModule,
    ProfileManagerComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './calculator-page.component.html',
  styleUrl: './calculator-page.component.css'
})
export class CalculatorPageComponent {
  private calculatorService = inject(CalculatorService);
  private cdr = inject(ChangeDetectorRef);
  private dialog = inject(MatDialog);

  @ViewChild(VesselInputFormComponent) vesselInputForm!: VesselInputFormComponent;
  @ViewChild(ProfileManagerComponent) profileManager!: ProfileManagerComponent;

  // Separate results for each integration level
  advancedResult: VariantResult | null = null;
  proResult: VariantResult | null = null;
  premiumResult: VariantResult | null = null;
  sailContribution: SailContributionResult | null = null;
  private selectedBaselineIndex: number | undefined = undefined;
  // Keep allResults for template compatibility
  get allResults(): Record<'Premium' | 'Pro' | 'Advanced', VariantResult | null> {
    return {
      Premium: this.premiumResult,
      Pro: this.proResult,
      Advanced: this.advancedResult
    };
  }
  
  baselineData: BaselineData | null = null;
  powerDemands: PowerDemands | null = null;
  validationWarnings: ValidationWarning[] = [];
  currentInput: CalculatorInput | null = null;
  isCalculating = false;
  error: string | null = null;
  validationErrors: string[] = [];

  // Smart collapse state
  /** Which level panels are open. One map instead of three fields — toggleAllPanels always
   *  treated them as a set anyway. Level 3 starts open, as it did before. */
  private readonly tierExpanded: Record<TierKey, boolean> = {
    Advanced: false,
    Pro: false,
    Premium: true
  };
  allExpanded = false;

  readonly tierPanels = TIER_PANELS;

  private _allVariantsResult: AllVariantsCalculationResult | null = null;

  /**
   * Calculation requests, serialised through switchMap: a newer request cancels the one in
   * flight, so the panels can never show a mix of two responses (which is how a stale
   * default-price result used to end up next to a fresh one).
   */
  private readonly calculationRequests$ = new Subject<CalculationRequest>();

  get allVariantsResult(): AllVariantsCalculationResult | null {
    return this._allVariantsResult;
  }

  /** Battery contribution from the last calculation (null when battery inactive) */
  get batteryDetails(): BatteryDetails | null {
    return this._allVariantsResult?.batteryDetails ?? null;
  }

  get hasResults(): boolean {
    return this._allVariantsResult !== null;
  }

  constructor() {
    this.calculationRequests$
      .pipe(
        switchMap(request =>
          this.calculatorService.calculateAllVariants(request.apiInput).pipe(
            map(results => ({ request, results, error: null as unknown })),
            // Keep the stream alive: one failed calculation must not stop the next one
            catchError(error => of({ request, results: null, error }))
          )
        ),
        takeUntilDestroyed()
      )
      .subscribe(({ request, results, error }) => {
        if (results) {
          this.onCalculationSucceeded(request, results);
        } else {
          this.onCalculationFailed(request, error);
        }
        this.cdr.markForCheck();
      });
  }

  // ─── CLIENT REPORT ──────────────────────────────────────────────────────────

  openReportDialog(): void {
    const reportData = this.buildReportData();
    if (!reportData) {return;}
    this.dialog.open(ReportDialogComponent, { width: '460px', data: reportData });
  }

  /**
   * Maps existing page state to the report model. Tiers: advanced = Level 1,
   * pro = Level 2, premium = Level 3. KPIs/payback come from the recommended tier.
   */
  private buildReportData(): ReportData | null {
    const results = this._allVariantsResult;
    if (!results || !this.currentInput) {return null;}

    const tiers = [
      { key: 'Advanced', variant: results.advanced, name: 'Level 1', description: 'Optimal power system recommendation' },
      { key: 'Pro', variant: results.pro, name: 'Level 2', description: '+ Continuous optimization' },
      { key: 'Premium', variant: results.premium, name: 'Level 3', description: '+ Energy control functions, weather forecast & mission plan integration' }
    ];
    const recommended = tiers.find(t => t.key === this.recommendedTier) ?? tiers[tiers.length - 1];

    const vesselSection = this.vesselInputForm?.vesselConfigSection;
    const vesselLabel = vesselSection?.selectionLabel || this.vesselInputForm?.vesselTypeName || 'Vessel';

    const mainFuel = this.currentInput.mainFuelType ?? 'MGO';
    const auxFuel = this.currentInput.auxFuelType ?? 'MGO';
    const fuelLabel = mainFuel === auxFuel ? mainFuel : `ME ${mainFuel} · AE ${auxFuel}`;

    return {
      clientName: null,
      vesselLabel,
      generatedAt: new Date(),
      baselineFocTonPerYear: results.baselineFOC,
      baselineCo2TonPerYear: results.baselineCO2,
      fuelPriceUsdPerTon: this.currentInput.fuelPrice,
      fuelLabel,
      levels: tiers.map(t => ({
        name: t.name,
        description: t.description,
        fuelSavingsTonPerYear: t.variant.fuelSavings,
        costSavingsPerYear: t.variant.annualCostSavings,
        recommended: t.key === recommended.key
      })),
      recommendedLevelName: recommended.name,
      recommendedFuelSavings: recommended.variant.fuelSavings,
      recommendedCo2Reduction: recommended.variant.co2Reduction,
      recommendedCostSavings: recommended.variant.annualCostSavings,
      recommendedInvestment: recommended.variant.totalInvestment,
      recommendedPaybackYears: recommended.variant.paybackPeriod,
      operationalModes: (results.powerDemands?.modeBreakdowns ?? [])
        .filter(m => m.hours > 0)
        .map(m => ({
          name: m.mode,
          hours: m.hours,
          energyKwh: m.energyMainEngineKwh + m.energyAuxGenKwh
        })),
      baselineEngineConfig: ((): string | undefined => {
        const l1 = results.level1Details;
        if (!l1) {return undefined;}
        return [
          l1.baselineMeCount > 0 ? `${l1.baselineMeCount} ME` : null,
          l1.baselineSgEnabled ? '1 SG' : null,
          l1.baselineAeCount > 0 ? `${l1.baselineAeCount} AE` : null,
        ].filter(Boolean).join(' + ') || undefined;
      })()
    };
  }

  // ─── PROFILE MANAGER WIRING ─────────────────────────────────────────────────

  onSaveRequest(): void {
    if (!this.profileManager) {return;}
    const vesselSection = this.vesselInputForm?.vesselConfigSection;
    const vesselLabel = vesselSection?.selectionLabel || (this.vesselInputForm?.vesselTypeName ?? '');
    const category = vesselSection?.selectedCategoryName ?? '';
    const size = vesselSection?.selectedSize ?? 0;
    const speed = vesselSection?.selectedSpeed ?? 0;
    const inputToSave = this.vesselInputForm
      ? this.vesselInputForm.getCurrentInputSnapshot(this.selectedBaselineIndex)
      : (this.currentInput ? { ...this.currentInput, baselineIndex: this.selectedBaselineIndex } : null);

    if (!inputToSave) {return;}
    this.profileManager.confirmSaveWithData(inputToSave, vesselLabel, category, size, speed);
  }

  onProfileLoadRequested(profile: SavedProfile): void {
    // Applied straight away: a restore emits several times (the vessel cascade patches fields
    // asynchronously), and every one of them must see the pinned baseline — not just whichever
    // arrives first.
    this.selectedBaselineIndex = profile.input.baselineIndex ?? undefined;
    if (this.vesselInputForm) {
      this.vesselInputForm.loadProfile(profile);
    }
  }

  // ─────────────────────────────────────────────────────────────────────────────

  onFormChange(event: FormChangeEvent): void {
    const input = event.input;
    this.currentInput = input;

    // A real edit invalidates a pinned baseline; the emissions of a profile restore do not.
    if (event.source === 'user') {
      this.selectedBaselineIndex = undefined;
    }

    if (input && this.describesACalculablePlant(input)) {
      this.calculate(input);
    }
    // Otherwise the plant is not described yet (a cascade still filling the form) — stay silent.
  }

  /**
   * Is there enough of a plant to ask the backend about?
   *
   * A cheap pre-check that keeps half-filled cascade states off the wire; the backend owns the
   * real validation. Main-engine capacity is required only on a MECHANICAL plant: a
   * diesel-electric vessel (meCount 0, Epic E1) legitimately sends 0 there and the auxiliaries
   * carry propulsion and hotel alike. Before that exemption this guard dropped every
   * diesel-electric calculation silently — no request, no results, no message.
   */
  private describesACalculablePlant(input: CalculatorInput): boolean {
    const mainEngineDescribed = input.meCount === 0 || input.meCapacityPerEngine > 0;

    return input.propulsionPower > 0
      && input.hotelLoad > 0
      && mainEngineDescribed
      && input.aeCapacityPerEngine > 0;
  }

  onBaselineIndexChanged(index: number): void {
    if (!this.currentInput) {return;}
    this.selectedBaselineIndex = index;
    this.recalculateBaseline(this.currentInput);
  }

  private calculate(input: CalculatorInput): void {
    this.isCalculating = true;
    this.error = null;
    this.validationErrors = [];
    this.validationWarnings = [];

    // Reset results
    this.advancedResult = null;
    this.proResult = null;
    this.premiumResult = null;

    this.requestCalculation(input, { silent: false });
  }

  /** Queues a calculation; the baseline index is captured NOW, not read when the answer lands. */
  private requestCalculation(input: CalculatorInput, options: { silent: boolean }): void {
    this.calculationRequests$.next({
      input,
      apiInput: { ...input, baselineIndex: this.selectedBaselineIndex },
      silent: options.silent
    });
  }

  private onCalculationSucceeded(request: CalculationRequest, results: AllVariantsCalculationResult): void {
    this.applyResults(results, request.input);
    this.updateRecommendation();

    if (!request.silent) {
      for (const tier of TIER_PANELS) {
        this.tierExpanded[tier.key] = this.recommendedTier === tier.key;
      }
      this.isCalculating = false;
    }
  }

  private onCalculationFailed(request: CalculationRequest, error: unknown): void {
    if (request.silent) {
      // Baseline re-selection failed — the previous results stay on screen, as before.
      return;
    }

    const httpError = error as { status?: number; error?: { errors?: string[] } };
    if (httpError?.status === 400 && httpError.error?.errors?.length) {
      this.validationErrors = httpError.error.errors;
      this.error = null;
    } else {
      this.error = 'Calculation failed. Please check your inputs and try again.';
      this.validationErrors = [];
    }
    this.isCalculating = false;
  }

  recommendedTier = 'Premium';

  private applyResults(results: AllVariantsCalculationResult, input: CalculatorInput): void {
    this.advancedResult = results.advanced;
    this.proResult = results.pro;
    this.premiumResult = results.premium;
    this.sailContribution = results.sailContribution ?? null;
    this.baselineData = {
      totalDemandKw: results.powerDemands.totalDemand,
      meInstalledKw: results.powerDemands.meInstalled,
      aeInstalledKw: results.powerDemands.aeInstalled,
      sgInstalledKw: input.sgCapacityPerEngine * input.meCount,
      meLoadPercent: results.level1Details?.baselineMeLoadPercent ?? 0,
      aeLoadPercent: results.level1Details?.baselineAeLoadPercent ?? 0,
      totalFuelConsumptionTons: results.baselineFOC,
      totalCO2EmissionsTons: results.baselineCO2
    };
    this.powerDemands = results.powerDemands;
    this.validationWarnings = results.warnings || [];
    this._allVariantsResult = results;
  }

  /** Re-call API for baseline change without resetting panels or showing spinner */
  private recalculateBaseline(input: CalculatorInput): void {
    this.requestCalculation(input, { silent: true });
  }

  private updateRecommendation(): void {
    if (!this.allResults.Advanced || !this.allResults.Pro || !this.allResults.Premium) {
      this.recommendedTier = 'Premium';
      return;
    }
    const tiers = [
      { name: 'Advanced', payback: this.allResults.Advanced.paybackPeriod, savings: this.allResults.Advanced.fuelSavings },
      { name: 'Pro', payback: this.allResults.Pro.paybackPeriod, savings: this.allResults.Pro.fuelSavings },
      { name: 'Premium', payback: this.allResults.Premium.paybackPeriod, savings: this.allResults.Premium.fuelSavings },
    ];
    const viable = tiers.filter(t => t.payback > 0 && isFinite(t.payback) && t.payback < 10);
    if (viable.length === 0) { this.recommendedTier = 'Advanced'; return; }
    const best = viable.sort((a, b) => a.payback - b.payback)[0];
    const similar = viable.filter(t => Math.abs(t.payback - best.payback) < 1);
    this.recommendedTier = similar.sort((a, b) => b.savings - a.savings)[0].name;
  }

  isTierExpanded(key: TierKey): boolean {
    return this.tierExpanded[key];
  }

  setTierExpanded(key: TierKey, expanded: boolean): void {
    this.tierExpanded[key] = expanded;
  }

  trackByTierKey(_position: number, tier: TierPanel): string {
    return tier.key;
  }

  isRecommended(variant: string): boolean {
    return this.recommendedTier === variant;
  }

  toggleAllPanels(): void {
    this.allExpanded = !this.allExpanded;
    for (const tier of TIER_PANELS) {
      this.tierExpanded[tier.key] = this.allExpanded;
    }
    this.cdr.markForCheck();
  }

  /** Validation messages are plain strings, so the value is its own identity. */
  trackByValue(_position: number, value: string): string {
    return value;
  }
}