import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { BaselineData, CalculatorInput, Level1Details, ValidCombinationDto } from '../../../calculations/calculator.types';
import { CO2_EMISSION_FACTOR } from '../../../shared/constants';

@Component({
  selector: 'app-baseline-panel',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './baseline-panel.component.html',
  styleUrl: './baseline-panel.component.css'
})
export class BaselinePanelComponent {
  @Input() baseline!: BaselineData;
  @Input() baselineME: number = 0;
  @Input() baselineAE: number = 0;
  @Input() currentInput!: CalculatorInput;
  @Input() level1Details?: Level1Details;
  @Output() baselineIndexChanged = new EventEmitter<number>();
  readonly co2Factor = CO2_EMISSION_FACTOR;

  get meLoadPercentage(): number {
    return this.level1Details?.baselineMeLoadPercent ?? 0;
  }

  get aeLoadPercentage(): number {
    return this.level1Details?.baselineAeLoadPercent ?? 0;
  }

  get aeIsRunning(): boolean {
    return (this.level1Details?.baselineAeCount ?? 0) > 0;
  }

  get fuelCost(): number {
    if (!this.baseline || !this.currentInput) return 0;
    return this.baseline.totalFuelConsumptionTons * this.currentInput.fuelPrice;
  }

  get selectedBaselineIndex(): number {
    return this.level1Details?.selectedBaselineIndex ?? 0;
  }

  get validCombinations(): ValidCombinationDto[] {
    return this.level1Details?.validCombinations ?? [];
  }

  get isDefaultBaseline(): boolean {
    if (!this.level1Details) return true;
    const defaultIndex = this.level1Details.validCombinationsCount - 1;
    return this.selectedBaselineIndex === defaultIndex;
  }

  formatComboLabel(combo: ValidCombinationDto): string {
    const parts: string[] = [];
    if (combo.activeMeCount > 0) parts.push(`${combo.activeMeCount}×ME`);
    if (combo.sgEnabled) parts.push('SG');
    if (combo.activeAeCount > 0) parts.push(`${combo.activeAeCount}×AE`);
    return parts.join(' + ') || 'None';
  }

  onBaselineSelectionChange(index: number): void {
    this.baselineIndexChanged.emit(index);
  }
}