import { Component, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { VesselInputFormComponent } from '../vessel-input/vessel-input-form/vessel-input-form.component';
import { ProfileManagerComponent, SaveRequestEvent } from '../profiles/profile-manager.component';
import { SavedProfile } from '../../core/profile.types';
import { PowerDemandsPanelComponent } from '../results-display/power-demands-panel/power-demands-panel.component';
import { BaselinePanelComponent } from '../results-display/baseline-panel/baseline-panel.component';
import { BatteryContributionPanelComponent } from '../results-display/battery-contribution-panel/battery-contribution-panel.component';
import { VariantDetailPanelComponent } from '../results-display/variant-detail-panel/variant-detail-panel.component';
import { ValidationWarningsComponent } from '../results-display/validation-warnings/validation-warnings.component';
import { SavingsChartComponent } from '../charts/savings-chart/savings-chart.component';
import { TierComparisonComponent } from '../results-display/tier-comparison/tier-comparison.component';
import { SailIndicatorComponent } from '../results-display/sail-indicator/sail-indicator.component';
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
    SailIndicatorComponent,
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
  @ViewChild(VesselInputFormComponent) vesselInputForm!: VesselInputFormComponent;
  @ViewChild(ProfileManagerComponent) profileManager!: ProfileManagerComponent;

  // Separate results for each integration level
  advancedResult: VariantResult | null = null;
  proResult: VariantResult | null = null;
  premiumResult: VariantResult | null = null;
  sailContribution: SailContributionResult | null = null;
  private selectedBaselineIndex: number | undefined = undefined;
  // Keep allResults for template compatibility
  get allResults() {
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
  advancedExpanded = false;
  proExpanded = false;
  premiumExpanded = true;
  allExpanded = false;

  private _allVariantsResult: AllVariantsCalculationResult | null = null;
  private pendingLoadedBaselineIndex: number | null = null;

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

  constructor(
    private calculatorService: CalculatorService,
    private cdr: ChangeDetectorRef,
    private dialog: MatDialog
  ) {}

  // ─── CLIENT REPORT ──────────────────────────────────────────────────────────

  openReportDialog(): void {
    const reportData = this.buildReportData();
    if (!reportData) return;
    this.dialog.open(ReportDialogComponent, { width: '460px', data: reportData });
  }

  /**
   * Maps existing page state to the report model. Tiers: advanced = Level 1,
   * pro = Level 2, premium = Level 3. KPIs/payback come from the recommended tier.
   */
  private buildReportData(): ReportData | null {
    const results = this._allVariantsResult;
    if (!results || !this.currentInput) return null;

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
      baselineEngineConfig: (() => {
        const l1 = results.level1Details;
        if (!l1) return undefined;
        return [
          l1.baselineMeCount > 0 ? `${l1.baselineMeCount} ME` : null,
          l1.baselineSgEnabled ? '1 SG' : null,
          l1.baselineAeCount > 0 ? `${l1.baselineAeCount} AE` : null,
        ].filter(Boolean).join(' + ') || undefined;
      })()
    };
  }

  // ─── PROFILE MANAGER WIRING ─────────────────────────────────────────────────

  onSaveRequest(event: SaveRequestEvent): void {
    if (!this.profileManager) return;
    const vesselSection = this.vesselInputForm?.vesselConfigSection;
    const vesselLabel = vesselSection?.selectionLabel || (this.vesselInputForm?.vesselTypeName ?? '');
    const category = vesselSection?.selectedCategoryName ?? '';
    const size = vesselSection?.selectedSize ?? 0;
    const speed = vesselSection?.selectedSpeed ?? 0;
    const inputToSave = this.vesselInputForm
      ? this.vesselInputForm.getCurrentInputSnapshot(this.selectedBaselineIndex)
      : (this.currentInput ? { ...this.currentInput, baselineIndex: this.selectedBaselineIndex } : null);

    if (!inputToSave) return;
    this.profileManager.confirmSaveWithData(inputToSave, vesselLabel, category, size, speed);
  }

  onProfileLoadRequested(profile: SavedProfile): void {
    this.pendingLoadedBaselineIndex = profile.input.baselineIndex ?? null;
    if (this.vesselInputForm) {
      this.vesselInputForm.loadProfile(profile);
    }
  }

  // ─────────────────────────────────────────────────────────────────────────────

  onFormChange(input: CalculatorInput): void {
    this.currentInput = input;
    if (this.pendingLoadedBaselineIndex !== null) {
      this.selectedBaselineIndex = this.pendingLoadedBaselineIndex;
      this.pendingLoadedBaselineIndex = null;
    } else {
      this.selectedBaselineIndex = undefined; // Reset on new user input
    }

    // Add validation: only calculate if all required fields are > 0
    if (
      input &&
      input.propulsionPower > 0 &&
      input.hotelLoad > 0 &&
      input.meCapacityPerEngine > 0 &&
      input.aeCapacityPerEngine > 0
    ) {
      this.calculate(input);
    } else {
      // Validation failed - missing required fields
    }
  }

  onBaselineIndexChanged(index: number): void {
    if (!this.currentInput) return;
    this.selectedBaselineIndex = index;
    this.recalculateBaseline(this.currentInput);
  }

  private calculate(input: CalculatorInput): void {
    const apiInput = { ...input, baselineIndex: this.selectedBaselineIndex };
    this.isCalculating = true;
    this.error = null;
    this.validationErrors = [];
    this.validationWarnings = [];
    
    // Reset results
    this.advancedResult = null;
    this.proResult = null;
    this.premiumResult = null;

    // Call new endpoint that calculates all variants at once
    this.calculatorService.calculateAllVariants(apiInput).subscribe({
      next: (results) => {
        this.applyResults(results, input);
        this.updateRecommendation();
        this.advancedExpanded = this.recommendedTier === 'Advanced';
        this.proExpanded = this.recommendedTier === 'Pro';
        this.premiumExpanded = this.recommendedTier === 'Premium';

        this.isCalculating = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        if (err.status === 400 && err.error?.errors?.length) {
          this.validationErrors = err.error.errors;
          this.error = null;
        } else {
          this.error = 'Calculation failed. Please check your inputs and try again.';
          this.validationErrors = [];
        }
        this.isCalculating = false;
        this.cdr.markForCheck();
      }
    });
  }

  recommendedTier: string = 'Premium';

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
    const apiInput = { ...input, baselineIndex: this.selectedBaselineIndex };
    this.calculatorService.calculateAllVariants(apiInput).subscribe({
      next: (results) => {
        this.applyResults(results, input);
        this.updateRecommendation();
        this.cdr.markForCheck();
      },
      error: () => {
        // Silently ignore — the old results remain visible
        this.cdr.markForCheck();
      }
    });
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

  isRecommended(variant: string): boolean {
    return this.recommendedTier === variant;
  }

  toggleAllPanels(): void {
    this.allExpanded = !this.allExpanded;
    this.advancedExpanded = this.allExpanded;
    this.proExpanded = this.allExpanded;
    this.premiumExpanded = this.allExpanded;
    this.cdr.markForCheck();
  }
}