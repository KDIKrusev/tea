import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { SailContributionResult } from '../../../calculations/calculator.types';

@Component({
  selector: 'app-sail-indicator',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="sail-banner" *ngIf="sailResult">
      <div class="sail-header">
        <span class="sail-icon">&#x26F5;</span>
        <span class="sail-title">Sail Contribution Active &mdash; {{ sailResult.sailSavingsPercent | number:'1.0-1' }}% propulsion reduction</span>
      </div>
      <div class="sail-details">
        <span class="sail-detail">
          Wind: {{ sailResult.trueWindSpeed | number:'1.0-1' }} m/s &#64; {{ sailResult.windAngleRelVessel | number:'1.0-0' }}&deg;
          | Sail: +{{ sailResult.sailPowerKw | number:'1.0-0' }} kW
          | Propulsion: {{ sailResult.transitPropulsionBeforeKw | number:'1.0-0' }}
          &rarr; {{ sailResult.transitPropulsionAfterKw | number:'1.0-0' }} kW
        </span>
      </div>
    </div>
  `,
  styles: [`
    .sail-banner {
      background: linear-gradient(135deg, #0d9488, #14b8a6);
      color: white;
      border-radius: 8px;
      padding: 16px 20px;
      margin-bottom: 16px;
      box-shadow: 0 2px 8px rgba(20, 184, 166, 0.3);
    }

    .sail-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 8px;
    }

    .sail-icon {
      font-size: 22px;
    }

    .sail-title {
      font-size: 16px;
      font-weight: 700;
      letter-spacing: 0.01em;
    }

    .sail-details {
      display: flex;
      flex-direction: column;
      gap: 4px;
      margin-bottom: 8px;
    }

    .sail-detail {
      font-size: 13px;
      opacity: 0.95;
      font-variant-numeric: tabular-nums;
    }

  `]
})
export class SailIndicatorComponent {
  @Input() sailResult: SailContributionResult | null | undefined;
}
