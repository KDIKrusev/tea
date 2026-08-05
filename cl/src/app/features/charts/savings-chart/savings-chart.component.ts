import { Component, Input, AfterViewInit, OnDestroy, OnChanges, SimpleChanges, ViewChild, ElementRef, NgZone, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Chart, ChartData, TooltipItem } from 'chart.js/auto';
import { VariantResult, BaselineData } from '../../../calculations/calculator.types';

@Component({
  selector: 'app-savings-chart',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './savings-chart.component.html',
  styleUrl: './savings-chart.component.css'
})
export class SavingsChartComponent implements AfterViewInit, OnDestroy, OnChanges {
  private ngZone = inject(NgZone);

  // New inputs for all variants
  @Input() baseline: BaselineData | null = null;
  @Input() premiumResult: VariantResult | null = null;
  @Input() proResult: VariantResult | null = null;
  @Input() advancedResult: VariantResult | null = null;
  @Input() chartType: 'foc' | 'co2' | 'both' = 'both'; // New input to specify chart type
  
  // Keep legacy input for backward compatibility during transition
  @Input() result: VariantResult | null = null;

  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;

  private chart: Chart | null = null;
  private viewInitialized = false;
  private isCreatingChart = false;

  ngAfterViewInit(): void {
    this.viewInitialized = true;
    if (this.hasData() && !this.chart && !this.isCreatingChart) {
      this.isCreatingChart = true;
      this.ngZone.runOutsideAngular(() => {
        setTimeout(() => {
          this.createChart();
          this.isCreatingChart = false;
        }, 100);
      });
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['baseline'] || changes['premiumResult'] || changes['proResult'] || changes['advancedResult'] || changes['result']) && this.hasData()) {
      if (changes['baseline']?.firstChange || changes['premiumResult']?.firstChange || changes['proResult']?.firstChange || changes['advancedResult']?.firstChange || changes['result']?.firstChange) {
        return;
      }
      
      if (this.viewInitialized && this.chart) {
        this.ngZone.runOutsideAngular(() => {
          if (this.chart) {
            this.chart.data = this.prepareChartData();
            (this.chart.options.scales as Record<string, { min?: number }>)['y'].min = this.getYAxisMin();
            this.chart.update('none');
          }
        });
      } else if (this.viewInitialized && !this.chart && !this.isCreatingChart) {
        this.isCreatingChart = true;
        this.ngZone.runOutsideAngular(() => {
          setTimeout(() => {
            this.createChart();
            this.isCreatingChart = false;
          }, 100);
        });
      }
    }
  }

  private hasData(): boolean {
    // Check if we have new format (all variants) or legacy format (single result)
    return (this.baseline !== null && this.premiumResult !== null) || this.result !== null;
  }

  ngOnDestroy(): void {
    if (this.chart) {
      this.chart.destroy();
      this.chart = null;
    }
  }

  private createChart(): void {
    if (!this.chartCanvas || !this.hasData()) {
      return;
    }
    
    const ctx = this.chartCanvas.nativeElement.getContext('2d');
    if (!ctx) {
      return;
    }

    if (this.chart) {
      this.chart.destroy();
      this.chart = null;
    }

    try {
      const chartData = this.prepareChartData();
      
      this.chart = new Chart(ctx, {
        type: 'bar',
        data: chartData,
        options: {
          responsive: true,
          maintainAspectRatio: false,
          animation: false,
          plugins: {
            title: {
              display: true,
              text: this.getChartTitle(),
              font: {
                size: 16,
                weight: 'bold',
                family: 'Inter'
              },
              padding: {
                top: 10,
                bottom: 20
              }
            },
            legend: {
              display: true,
              position: 'top',
              labels: {
                usePointStyle: true,
                font: { 
                  family: 'Inter',
                  size: 12
                },
                padding: 15
              }
            },
            tooltip: {
              callbacks: {
                label: (context: TooltipItem<'bar'>): string => {
                  const label = context.dataset.label || '';
                  const value = context.parsed.y || 0;
                  const unit = this.chartType === 'co2' ? 'tons CO₂' : 'tons';
                  return `${label}: ${value.toLocaleString()} ${unit}`;
                }
              }
            }
          },
          scales: {
            y: {
              min: this.getYAxisMin(),
              title: {
                display: true,
                text: this.getYAxisLabel(),
                font: {
                  size: 12,
                  family: 'Inter'
                }
              },
              ticks: {
                callback: function(value: string | number): string {
                  return value.toLocaleString();
                }
              }
            },
            x: {
              ticks: {
                font: {
                  size: 11,
                  family: 'Inter'
                }
              }
            }
          }
        }
      });
    } catch {
      // Chart.js could not build the canvas — the panel simply stays empty rather than breaking
      // the page. Nothing actionable is recoverable from the error object here.
    }
  }

  /**
   * Prepare chart data - supports both new format (all variants) and legacy format
   */
  private prepareChartData(): ChartData<'bar'> {
    // New format: All variants comparison
    if (this.baseline && this.premiumResult && this.proResult && this.advancedResult) {
      const labels = this.getChartLabels();
      
      return {
        labels: labels,
        datasets: [
          {
            label: 'Baseline (No iEMS)',
            data: this.getDataForVariant(this.baseline, null),
            backgroundColor: '#f97316', // Orange
            borderColor: '#ea580c',
            borderWidth: 2,
            borderRadius: 6,
            hoverBackgroundColor: '#fb923c',
            hoverBorderColor: '#f97316',
            hoverBorderWidth: 3
          },
          {
            label: 'Integration level 1',
            data: this.getDataForVariant(null, this.advancedResult),
            backgroundColor: '#8b5cf6', // Purple
            borderColor: '#7c3aed',
            borderWidth: 2,
            borderRadius: 6,
            hoverBackgroundColor: '#a78bfa',
            hoverBorderColor: '#8b5cf6',
            hoverBorderWidth: 3
          },
          {
            label: 'Integration level 2',
            data: this.getDataForVariant(null, this.proResult),
            backgroundColor: '#3b82f6', // Blue
            borderColor: '#2563eb',
            borderWidth: 2,
            borderRadius: 6,
            hoverBackgroundColor: '#60a5fa',
            hoverBorderColor: '#3b82f6',
            hoverBorderWidth: 3
          },
          {
            label: 'Integration level 3',
            data: this.getDataForVariant(null, this.premiumResult),
            backgroundColor: '#10b981', // Green
            borderColor: '#059669',
            borderWidth: 2,
            borderRadius: 6,
            hoverBackgroundColor: '#34d399',
            hoverBorderColor: '#10b981',
            hoverBorderWidth: 3
          }
        ]
      };
    }

    return { labels: [], datasets: [] };
  }

  /**
   * Get chart title based on chart type
   */
  private getChartTitle(): string {
    switch (this.chartType) {
      case 'foc':
        return 'Annual Fuel Oil Consumption (FOC) - All Variants';
      case 'co2':
        return 'Annual CO₂ Emissions - All Variants';
      case 'both':
      default:
        return 'Annual FOC and CO₂ Comparison - All Variants';
    }
  }

  /**
   * Get Y-axis label based on chart type
   */
  private getYAxisLabel(): string {
    switch (this.chartType) {
      case 'foc':
        return 'Fuel Oil Consumption (tons/year)';
      case 'co2':
        return 'CO₂ Emissions (tons/year)';
      case 'both':
      default:
        return 'Consumption / Emission (tons/year)';
    }
  }

  /**
   * Get chart labels based on chart type
   */
  private getChartLabels(): string[] {
    switch (this.chartType) {
      case 'foc':
        return ['Fuel Oil Consumption'];
      case 'co2':
        return ['CO₂ Emissions'];
      case 'both':
      default:
        return ['Annual Fuel Oil Consumption (tons/year)', 'Annual CO₂ Emissions (tons/year)'];
    }
  }

  /**
   * Calculate a smart Y-axis minimum so that small savings differences (3-6%)
   * are visually distinguishable instead of all bars appearing equal height.
   * Starts the axis at min_value minus 2× the baseline-to-best-variant range,
   * but never below zero.
   */
  private getYAxisMin(): number {
    const values: number[] = [];

    if (this.baseline) {
      if (this.chartType === 'foc' || this.chartType === 'both') {values.push(this.baseline.totalFuelConsumptionTons);}
      if (this.chartType === 'co2' || this.chartType === 'both') {values.push(this.baseline.totalCO2EmissionsTons);}
    }
    if (this.advancedResult) {
      if (this.chartType === 'foc' || this.chartType === 'both') {values.push(this.advancedResult.optimizedFOC);}
      if (this.chartType === 'co2' || this.chartType === 'both') {values.push(this.advancedResult.optimizedCO2);}
    }
    if (this.proResult) {
      if (this.chartType === 'foc' || this.chartType === 'both') {values.push(this.proResult.optimizedFOC);}
      if (this.chartType === 'co2' || this.chartType === 'both') {values.push(this.proResult.optimizedCO2);}
    }
    if (this.premiumResult) {
      if (this.chartType === 'foc' || this.chartType === 'both') {values.push(this.premiumResult.optimizedFOC);}
      if (this.chartType === 'co2' || this.chartType === 'both') {values.push(this.premiumResult.optimizedCO2);}
    }

    if (values.length === 0) {return 0;}

    const minVal = Math.min(...values);
    const maxVal = Math.max(...values);
    const range = maxVal - minVal;

    // Pad by 2× the data range below the minimum so bars have visible height differences
    return Math.max(0, minVal - range * 2);
  }

  /**
   * Get data for a variant based on chart type
   */
  private getDataForVariant(baseline: BaselineData | null, result: VariantResult | null): number[] {
    if (baseline) {
      switch (this.chartType) {
        case 'foc':
          return [baseline.totalFuelConsumptionTons];
        case 'co2':
          return [baseline.totalCO2EmissionsTons];
        case 'both':
        default:
          return [baseline.totalFuelConsumptionTons, baseline.totalCO2EmissionsTons];
      }
    } else if (result) {
      switch (this.chartType) {
        case 'foc':
          return [result.optimizedFOC];
        case 'co2':
          return [result.optimizedCO2];
        case 'both':
        default:
          return [result.optimizedFOC, result.optimizedCO2];
      }
    }
    return [];
  }

  /**
   * Export chart as PNG image
   */
  exportChart(): void {
    if (!this.chart) {return;}

    try {
      // Get chart as base64 image
      const base64Image = this.chart.toBase64Image();

      // Create download link
      const link = document.createElement('a');
      link.href = base64Image;
      const date = new Date().toISOString().split('T')[0];
      link.download = `iems-savings-chart-${date}.png`;
      link.click();
    } catch {
      // The browser refused the canvas-to-PNG export (tainted canvas, or no download support).
      // The chart itself is unaffected, so there is nothing to report to the user.
    }
  }

}
