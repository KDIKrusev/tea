import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { AllVariantsCalculationResult, BaselineData } from '../../../calculations/calculator.types';

@Component({
  selector: 'app-tier-comparison',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="tier-comparison" *ngIf="results">
      <h3 class="comparison-title">
        <mat-icon>compare_arrows</mat-icon>
        Integration Level Comparison
      </h3>

      <div class="comparison-grid">
        <!-- Header Row -->
        <div class="grid-cell header-cell"></div>
        <div class="grid-cell header-cell tier-header baseline-header">Baseline</div>
        <div class="grid-cell header-cell tier-header" [class.recommended-header]="bestTier === 'Advanced'">Level 1
        </div>
        <div class="grid-cell header-cell tier-header" [class.recommended-header]="bestTier === 'Pro'">Level 2
        </div>
        <div class="grid-cell header-cell tier-header" [class.recommended-header]="bestTier === 'Premium'">Level 3
        </div>

        <!-- Fuel Consumption -->
        <div class="grid-cell row-label">Fuel Consumption</div>
        <div class="grid-cell baseline-col">{{ baseline?.totalFuelConsumptionTons | number:'1.0-1' }} ton/yr</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Advanced'">{{ results.advanced.optimizedFOC | number:'1.0-1' }} ton/yr</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Pro'">{{ results.pro.optimizedFOC | number:'1.0-1' }} ton/yr</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Premium'">{{ results.premium.optimizedFOC | number:'1.0-1' }} ton/yr</div>

        <!-- Fuel Savings -->
        <div class="grid-cell row-label">Fuel Savings</div>
        <div class="grid-cell baseline-col">—</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Advanced'">{{ results.advanced.fuelSavings | number:'1.0-1' }} ton/yr</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Pro'">{{ results.pro.fuelSavings | number:'1.0-1' }} ton/yr</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Premium'">{{ results.premium.fuelSavings | number:'1.0-1' }} ton/yr</div>

        <!-- CO2 Emissions / Reduction -->
        <div class="grid-cell row-label">CO2 Emissions</div>
        <div class="grid-cell baseline-col">{{ baseline?.totalCO2EmissionsTons | number:'1.0-1' }} ton/yr</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Advanced'">{{ results.advanced.optimizedCO2 | number:'1.0-1' }} ton/yr</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Pro'">{{ results.pro.optimizedCO2 | number:'1.0-1' }} ton/yr</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Premium'">{{ results.premium.optimizedCO2 | number:'1.0-1' }} ton/yr</div>

        <!-- CO2 Reduction -->
        <div class="grid-cell row-label">CO2 Reduction</div>
        <div class="grid-cell baseline-col">—</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Advanced'">{{ results.advanced.co2Reduction | number:'1.0-1' }} ton/yr</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Pro'">{{ results.pro.co2Reduction | number:'1.0-1' }} ton/yr</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Premium'">{{ results.premium.co2Reduction | number:'1.0-1' }} ton/yr</div>

        <!-- Cost Savings -->
        <div class="grid-cell row-label">Cost Savings</div>
        <div class="grid-cell baseline-col">—</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Advanced'"><span>$</span>{{ results.advanced.annualCostSavings | number:'1.0-0' }}/yr</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Pro'"><span>$</span>{{ results.pro.annualCostSavings | number:'1.0-0' }}/yr</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Premium'"><span>$</span>{{ results.premium.annualCostSavings | number:'1.0-0' }}/yr</div>

        <!-- Investment -->
        <div class="grid-cell row-label">Investment</div>
        <div class="grid-cell baseline-col">—</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Advanced'"><span>$</span>{{ results.advanced.totalInvestment | number:'1.0-0' }}</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Pro'"><span>$</span>{{ results.pro.totalInvestment | number:'1.0-0' }}</div>
        <div class="grid-cell" [class.recommended-col]="bestTier === 'Premium'"><span>$</span>{{ results.premium.totalInvestment | number:'1.0-0' }}</div>


      </div>
    </div>
  `,
  styles: [`
    .tier-comparison {
      margin-bottom: 16px;
      padding: 20px;
      background: white;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.06);
    }

    .comparison-title {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 16px;
      font-weight: 600;
      color: #1e293b;
      margin: 0 0 16px 0;
    }

    .comparison-title mat-icon {
      color: #3b82f6;
    }

    .comparison-grid {
      display: grid;
      grid-template-columns: 160px 1fr 1fr 1fr 1fr;
      gap: 0;
    }

    .grid-cell {
      padding: 8px 12px;
      font-size: 13px;
      color: #475569;
      border-bottom: 1px solid #f1f5f9;
      display: flex;
      align-items: center;
    }

    .header-cell {
      font-weight: 600;
      color: #1e293b;
      background: #f8fafc;
      border-bottom: 2px solid #e2e8f0;
      font-size: 14px;
    }

    .tier-header {
      justify-content: center;
    }

    .recommended-header {
      background: #eff6ff;
      color: #1d4ed8;
      flex-direction: column;
      gap: 2px;
    }

    .recommended-badge {
      font-size: 10px;
      font-weight: 500;
      background: #3b82f6;
      color: white;
      padding: 1px 6px;
      border-radius: 10px;
    }

    .recommended-col {
      background: #f0f7ff;
    }

    .row-label {
      font-weight: 500;
      color: #334155;
    }

    .baseline-header {
      color: #64748b;
      background: #f8fafc;
    }

    .baseline-col {
      color: #64748b;
      background: #fafafa;
    }

    .sub-label {
      padding-left: 20px;
      font-weight: 400;
      font-size: 12px;
    }

    .section-divider {
      border-bottom: 2px solid #e2e8f0;
      font-weight: 600;
      font-size: 12px;
      color: #64748b;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      padding-top: 12px;
    }

    .check-cell {
      justify-content: center;
      text-align: center;
    }

    /* Responsive: stack on small screens */
    @media (max-width: 640px) {
      .comparison-grid {
        grid-template-columns: 1fr;
      }

      .grid-cell {
        padding: 6px 12px;
      }

      .header-cell:first-child {
        display: none;
      }

      .row-label {
        background: #f8fafc;
        font-weight: 600;
        margin-top: 8px;
      }
    }
  `]
})
export class TierComparisonComponent {
  @Input() results!: AllVariantsCalculationResult;
  @Input() baseline: BaselineData | null = null;
  @Input() recommendedTier = 'Premium';

  get bestTier(): string {
    if (!this.results) {return '';}
    const tiers = [
      { name: 'Advanced', foc: this.results.advanced.optimizedFOC },
      { name: 'Pro', foc: this.results.pro.optimizedFOC },
      { name: 'Premium', foc: this.results.premium.optimizedFOC },
    ];
    return tiers.reduce((a, b) => a.foc <= b.foc ? a : b).name;
  }

  formatPayback(years: number): string {
    if (years < 1) {
      return `${Math.round(years * 12)} months`;
    }
    return `${years.toFixed(1)} years`;
  }
}
