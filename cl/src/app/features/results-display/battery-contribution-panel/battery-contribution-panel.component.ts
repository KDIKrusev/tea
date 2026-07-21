import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BatteryDetails } from '../../../calculations/calculator.types';

/**
 * Battery Contribution panel body: computed Spinning Reserve / Peak Shaving, the dual-scenario
 * Battery Benefit, and the per-mode allocation tables. Rendered inside the results accordion's
 * expansion panel (shell stays in calculator-page, same pattern as baseline-panel).
 */
@Component({
  selector: 'app-battery-contribution-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  templateUrl: './battery-contribution-panel.component.html',
  styleUrl: './battery-contribution-panel.component.css'
})
export class BatteryContributionPanelComponent {
  @Input({ required: true }) batteryDetails!: BatteryDetails;

  functionLabel(functionName: string): string {
    return functionName === 'PeakShaving' ? 'Peak Shaving' : 'Reserve';
  }
}
