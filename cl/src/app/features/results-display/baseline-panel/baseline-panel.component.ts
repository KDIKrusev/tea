import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { BaselineData, CalculatorInput, Level1Details, ValidCombinationDto } from '../../../calculations/calculator.types';

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
  @Input() baselineME = 0;
  @Input() baselineAE = 0;
  /** Per-engine CO2 from the API (each engine's own fuel factor) — never recomputed here. */
  @Input() baselineMeCO2 = 0;
  @Input() baselineAeCO2 = 0;
  @Input() currentInput!: CalculatorInput;
  @Input() level1Details?: Level1Details;
  @Output() baselineIndexChanged = new EventEmitter<number>();

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
    if (!this.baseline || !this.currentInput) {return 0;}
    return this.baseline.totalFuelConsumptionTons * this.currentInput.fuelPrice;
  }

  get selectedBaselineIndex(): number {
    return this.level1Details?.selectedBaselineIndex ?? 0;
  }

  get validCombinations(): ValidCombinationDto[] {
    return this.level1Details?.validCombinations ?? [];
  }

  get isDefaultBaseline(): boolean {
    if (!this.level1Details) {return true;}
    const defaultIndex = this.level1Details.validCombinationsCount - 1;
    return this.selectedBaselineIndex === defaultIndex;
  }

  formatComboLabel(combo: ValidCombinationDto): string {
    const parts: string[] = [];
    if (combo.activeMeCount > 0) {parts.push(`${combo.activeMeCount}×ME`);}
    if (combo.sgEnabled) {parts.push('SG');}
    if (combo.activeAeCount > 0) {parts.push(`${combo.activeAeCount}×AE`);}
    return parts.join(' + ') || 'None';
  }

  onBaselineSelectionChange(index: number): void {
    this.baselineIndexChanged.emit(index);
  }

  /** The combination's own index — stable across recalculations, unlike the loop position. */
  trackByComboIndex(_position: number, combo: ValidCombinationDto): number {
    return combo.index;
  }
}