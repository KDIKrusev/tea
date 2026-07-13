import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { VariantResult, Level1Details, SailContributionResult } from '../../../calculations/calculator.types';

@Component({
  selector: 'app-savings-breakdown-card',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="savings-breakdown" *ngIf="result">
      <h3 class="breakdown-title">
        <mat-icon>analytics</mat-icon>
        Savings Breakdown
      </h3>

      <div class="breakdown-list">
        <!-- L1: Always shown — all tiers include Engine Setup -->
        <div class="breakdown-item">
          <span class="level-indicator l1"></span>
          <span class="breakdown-label">Optimal Engine Setup (L1):</span>
          <span class="breakdown-value">{{ result.level1SavingsTonPerYear | number:'1.0-1' }} ton/yr</span>
        </div>
        <div class="detail-info" *ngIf="level1Details">
          <div>Optimal: {{ level1Details.activeMeCount }} ME + SG {{ level1Details.sgEnabled ? 'ON' : 'OFF' }} + {{ level1Details.activeAeCount }} AE — {{ level1Details.optimalFocTonPerHour | number:'1.0-4' }} ton/h</div>
          <div>Baseline: {{ level1Details.baselineFocTonPerHour | number:'1.0-4' }} ton/h | {{ level1Details.validCombinationsCount }} combinations evaluated</div>
        </div>

        <!-- L2: Only shown for Pro and Premium -->
        <div class="breakdown-item" *ngIf="variant === 'Pro' || variant === 'Premium'">
          <span class="level-indicator l2"></span>
          <span class="breakdown-label">Load Optimization (L2):</span>
          <span class="breakdown-value">{{ result.level2SavingsTonPerYear | number:'1.0-1' }} ton/yr</span>
        </div>
        <div class="detail-info" *ngIf="(variant === 'Pro' || variant === 'Premium') && result.level2Details?.optimalSetpoints?.length">
          <div *ngFor="let sp of result.level2Details!.optimalSetpoints">
            {{ sp.generatorType === 'SG' ? 'Shaft Gen' : 'Aux Engine' }}: {{ sp.loadPercent * 100 | number:'1.0-1' }}% ({{ sp.powerKw | number:'1.0-0' }} kW) — {{ sp.sfoc | number:'1.0-1' }} g/kWh
          </div>
        </div>

        <!-- L3: Only shown for Premium -->
        <div class="breakdown-item" *ngIf="variant === 'Premium'">
          <span class="level-indicator l3"></span>
          <span class="breakdown-label">Dynamic Ramp Control (L3):</span>
          <span class="breakdown-value">{{ result.level3SavingsTonPerYear | number:'1.0-1' }} ton/yr</span>
        </div>
        <div class="detail-info" *ngIf="variant === 'Premium' && result.level3Details">
          <div>±{{ result.level3Details.variationPerGeneratorKw | number:'1.0-0' }} kW per generator → ±{{ result.level3Details.reducedVariationPerGeneratorKw | number:'1.0-0' }} kW with DRC</div>
          <div>{{ result.level3Details.activeGeneratorCount }} active generators</div>
        </div>

        <!-- Sail Contribution -->
        <div class="breakdown-item sail-item" *ngIf="sailContribution">
          <span class="level-indicator sail"></span>
          <span class="breakdown-label">Sail Contribution:</span>
          <span class="breakdown-value sail-value">-{{ sailContribution.sailPowerKw | number:'1.0-0' }} kW propulsion</span>
        </div>
        <div class="detail-info" *ngIf="sailContribution">
          <div>Propulsion: {{ sailContribution.transitPropulsionBeforeKw | number:'1.0-0' }} → {{ sailContribution.transitPropulsionAfterKw | number:'1.0-0' }} kW ({{ sailContribution.sailSavingsPercent | number:'1.0-1' }}% reduction)</div>
        </div>

        <!-- Total -->
        <div class="breakdown-item total-item">
          <span class="breakdown-label total-label">Total Fuel Savings:</span>
          <span class="breakdown-value total-value">{{ result.fuelSavings | number:'1.0-1' }} ton/year</span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .savings-breakdown {
      margin-top: 16px;
      padding: 16px;
      background: #f8fafc;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
    }

    .breakdown-title {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 14px;
      font-weight: 600;
      color: #334155;
      margin: 0 0 12px 0;
    }

    .breakdown-title mat-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
      color: #64748b;
    }

    .breakdown-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .breakdown-item {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 13px;
      color: #475569;
    }

    .level-indicator {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      flex-shrink: 0;
    }

    .level-indicator.l1 { background-color: #8b5cf6; }
    .level-indicator.l2 { background-color: #3b82f6; }
    .level-indicator.l3 { background-color: #10b981; }
    .level-indicator.sail { background-color: #14b8a6; }

    .breakdown-label {
      flex: 1;
    }

    .breakdown-value {
      font-weight: 600;
      font-variant-numeric: tabular-nums;
      min-width: 80px;
      text-align: right;
    }

    .detail-info {
      padding-left: 26px;
      font-size: 12.5px;
      color: #475569;
      line-height: 1.7;
      margin-bottom: 4px;
    }

    .sail-value {
      color: #0d9488;
    }

    .total-item {
      margin-top: 4px;
      padding-top: 8px;
      border-top: 1px solid #cbd5e1;
    }

    .total-label {
      font-weight: 600;
      color: #1e293b;
    }

    .total-value {
      font-size: 14px;
      color: #059669;
      font-weight: 700;
    }
  `]
})
export class SavingsBreakdownCardComponent {
  @Input() result!: VariantResult;
  @Input() level1Details?: Level1Details;
  @Input() sailContribution?: SailContributionResult;
  @Input() variant: string = '';
}
