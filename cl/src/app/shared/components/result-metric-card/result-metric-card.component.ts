import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

export interface MetricComparison {
  before: number;
  after: number;
  label?: string;
}

export interface BreakdownItem {
  title: string;
  before: number;
  after: number;
}

@Component({
  selector: 'app-result-metric-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatTooltipModule
  ],
  templateUrl: './result-metric-card.component.html',
  styleUrl: './result-metric-card.component.css'
})
export class ResultMetricCardComponent {
  // Header
  @Input({ required: true }) title!: string;
  @Input({ required: true }) icon!: string;
  @Input() tooltip?: string;
  
  // Primary value
  @Input() primaryValue?: string | number;
  @Input() primaryUnit?: string;
  @Input() primaryFormat?: 'number' | 'currency' | 'percent' = 'number';
  @Input() primaryDigits?: string = '1.0-2';
  
  // Secondary value (for savings cards)
  @Input() secondaryValue?: string | number;
  @Input() secondaryUnit?: string;
  @Input() secondaryFormat?: 'number' | 'currency' = 'currency';
  
  // Percentage (for savings cards)
  @Input() percentage?: string | number;
  
  // Description (for simple cards)
  @Input() description?: string;
  
  // Comparison data (for savings cards)
  @Input() comparison?: MetricComparison;
  @Input() comparisonUnit: string = 'tons';
  @Input() comparisonDigits?: string = '1.0-0';
  
  // Breakdown data (for breakdown card)
  @Input() breakdownItems?: BreakdownItem[];
  @Input() breakdownUnit: string = 'tons';
  @Input() breakdownDigits?: string = '1.0-0';
  
  // Currency (for currency formatting)
  @Input() currency: string = 'USD';
  
  // Styling
  @Input() cardClass?: string;
  @Input() valueClass?: string;
  
  get hasComparison(): boolean {
    return !!this.comparison;
  }
  
  get hasBreakdown(): boolean {
    return !!this.breakdownItems && this.breakdownItems.length > 0;
  }
  
  get isSavingsCard(): boolean {
    return this.hasComparison || !!this.percentage;
  }

  getIconClass(): string {
    if (this.cardClass) {
      return this.cardClass.replace('-card', '-icon');
    }
    return 'neutral-icon';
  }

  formatValue(value: string | number | undefined, format?: string, digits?: string): string {
    if (value === undefined || value === null) return '';
    
    if (typeof value !== 'number') return String(value);
    
    switch (format) {
      case 'currency':
        return this.formatCurrency(value);
      case 'percent':
        return this.formatPercent(value);
      default:
        return this.formatNumber(value, digits || this.primaryDigits || '1.0-2');
    }
  }

  formatPercentage(value: string | number | undefined): string {
    if (typeof value === 'number') {
      const percentValue = value > 1 ? value / 100 : value;
      return (percentValue * 100).toFixed(1) + '%';
    }
    return String(value);
  }

  private formatNumber(value: number, digits: string): string {
    const [minInt, fractionPart] = digits.split('.');
    const [minFrac, maxFrac] = fractionPart ? fractionPart.split('-').map(Number) : [0, 0];
    return value.toLocaleString('en-US', {
      minimumFractionDigits: minFrac,
      maximumFractionDigits: maxFrac
    });
  }

  private formatCurrency(value: number): string {
    return value.toLocaleString('en-US', {
      style: 'currency',
      currency: this.currency,
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    });
  }

  private formatPercent(value: number): string {
    const percentValue = value > 1 ? value / 100 : value;
    return (percentValue * 100).toFixed(1) + '%';
  }
}

