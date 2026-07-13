import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { VariantResult, CalculatorInput, Level1Details, SailContributionResult } from '../../../calculations/calculator.types';
import { CO2_EMISSION_FACTOR } from '../../../shared/constants';

@Component({
  selector: 'app-variant-detail-panel',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatChipsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './variant-detail-panel.component.html',
  styleUrl: './variant-detail-panel.component.css'
})
export class VariantDetailPanelComponent {
  @Input() variant!: string;
  @Input() result!: VariantResult;
  @Input() currentInput!: CalculatorInput;
  @Input() level1Details?: Level1Details;
  @Input() sailContribution?: SailContributionResult;
  @Input() isRecommended: boolean = false;
  readonly co2Factor = CO2_EMISSION_FACTOR;

  /**
   * Get ME load percentage for this variant from backend calculation
   */
  get meLoadPercentage(): number {
    return this.result?.mainEngineLoadPercent || 0;
  }

  /**
   * Get AE load percentage for this variant from backend calculation
   */
  get aeLoadPercentage(): number {
    return this.result?.auxiliaryEngineLoadPercent || 0;
  }

  /**
   * Check if AE is running - display only
   */
  get aeIsRunning(): boolean {
    if (!this.currentInput) return false;
    const sgCapacityTotal = this.currentInput.sgCapacityPerEngine * this.currentInput.meCount;
    return this.currentInput.hotelLoad > sgCapacityTotal;
  }

  /**
   * Calculate fuel cost - display only (simple multiplication)
   */
  get fuelCost(): number {
    if (!this.result || !this.currentInput) return 0;
    return this.result.optimizedFOC * this.currentInput.fuelPrice;
  }

  /**
   * Format payback period for display
   */
  formatPayback(years: number): string {
    if (years < 1) {
      return `${Math.round(years * 12)} months`;
    }
    return `${years.toFixed(1)} years`;
  }

  getVariantColor(): string {
    const colors: {[key: string]: string} = {
      'Premium': 'variant-premium',
      'Pro': 'variant-pro',
      'Advanced': 'variant-advanced'
    };
    return colors[this.variant] || '';
  }

  getVariantIcon(): string {
    const icons: {[key: string]: string} = {
      'Premium': 'diamond',
      'Pro': 'stars',
      'Advanced': 'bolt'
    };
    return icons[this.variant] || 'settings';
  }

}
