import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';

import { VoyageOption } from '../../../models/entities/voyage-option.model';
import { VoyageOptionSet } from '../../../models/entities/voyage-option-set.model';
import { EaEnergyValuePipe } from '../../../shared/pipes/ea-energy-value-pipe';
import { VoyageService, DisplayFormat } from '../../../services/state/voyage-scheduler.service';

/**
 * One departure/arrival slot, flattened for display. The row answers "which slot do I take"; expanding it
 * answers "how do I sail it".
 */
export interface VoyageOptionRow {
  key: string;
  constantPower: VoyageOption;
  variableSpeed: VoyageOption | null;
  variableSpeedUnavailableReason?: string | null;

  /** Consumption of the constant-speed option in the active display format. The ranking axis. */
  consumption: number;

  /** How much worse than the cheapest slot, in percent. Comes from the backend. */
  relativeToBest: number;

  /** Variable speed against constant speed for this same slot. Negative means it saves. Null if absent. */
  variableSpeedDelta: number | null;
}

@Component({
  selector: 'app-voyage-options-list',
  standalone: true,
  imports: [CommonModule, EaEnergyValuePipe],
  templateUrl: './voyage-options-list.component.html',
  styleUrls: ['./voyage-options-list.component.css']
})
export class VoyageOptionsListComponent implements OnInit, OnDestroy, OnChanges {
  @Input() voyageOptionSets: VoyageOptionSet[] = [];
  @Input() selectedVoyageOption: VoyageOption | null = null;
  @Input() validationMessage?: string;
  @Output() voyageOptionSelected = new EventEmitter<VoyageOption>();

  public rows: VoyageOptionRow[] = [];
  public currentDisplayFormat: DisplayFormat = 'energy';

  /** Rows the user has opened. Several can stay open so two slots can be compared side by side. */
  private expandedKeys = new Set<string>();
  private subscriptions: Subscription[] = [];

  constructor(private voyageService: VoyageService) {}

  ngOnInit(): void {
    this.subscriptions.push(
      this.voyageService.displayFormat$.subscribe(format => {
        this.currentDisplayFormat = format;
        this.buildRows();
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['voyageOptionSets'] || changes['selectedVoyageOption']) {
      this.buildRows();
    }
  }

  public hasValidOptions(): boolean {
    return this.rows.length > 0;
  }

  public get skippedCombinationCount(): number {
    return this.voyageOptionSets.filter(set => !set.isValid).length;
  }

  // --- Row construction ---------------------------------------------------

  private buildRows(): void {
    const previousKeys = new Set(this.expandedKeys);

    this.rows = this.voyageOptionSets
      .filter(set => set.isValid && set.variablePowerOption?.isValid)
      .map(set => {
        const constantPower = set.variablePowerOption;
        const variableSpeed = set.variableSpeedOption ?? null;

        return {
          key: `${constantPower.etd}-${constantPower.eta}`,
          constantPower,
          variableSpeed,
          variableSpeedUnavailableReason: set.variableSpeedUnavailableReason,
          consumption: this.getConsumptionValue(constantPower),
          relativeToBest: this.getRelativeConsumptionValue(constantPower),
          variableSpeedDelta: this.calculateVariableSpeedDelta(constantPower, variableSpeed)
        };
      })
      .sort((a, b) => a.consumption - b.consumption);

    // Keep whatever the user had open; otherwise start with the best slot expanded so the comparison
    // is visible without a click.
    this.expandedKeys = new Set(this.rows.map(row => row.key).filter(key => previousKeys.has(key)));
    if (this.expandedKeys.size === 0 && this.rows.length > 0) {
      this.expandedKeys.add(this.rows[0].key);
    }
  }

  private calculateVariableSpeedDelta(
    constantPower: VoyageOption, variableSpeed: VoyageOption | null): number | null {
    if (!variableSpeed) return null;

    const baseline = this.getConsumptionValue(constantPower);
    if (!baseline) return null;

    return 100 * (this.getConsumptionValue(variableSpeed) / baseline - 1);
  }

  // --- Interaction --------------------------------------------------------

  public toggleRow(row: VoyageOptionRow, event: MouseEvent): void {
    event.stopPropagation();
    if (this.expandedKeys.has(row.key)) {
      this.expandedKeys.delete(row.key);
    } else {
      this.expandedKeys.add(row.key);
    }
  }

  public isExpanded(row: VoyageOptionRow): boolean {
    return this.expandedKeys.has(row.key);
  }

  /** Picking a variant is what opens the detailed analysis for that way of sailing the slot. */
  public selectVariant(option: VoyageOption | null): void {
    if (!option || !option.isValid) return;
    this.selectedVoyageOption = option;
    this.voyageOptionSelected.emit(option);
  }

  public isVariantSelected(option: VoyageOption | null): boolean {
    return !!option && option === this.selectedVoyageOption;
  }

  public isRowSelected(row: VoyageOptionRow): boolean {
    return this.isVariantSelected(row.constantPower) || this.isVariantSelected(row.variableSpeed);
  }

  // --- Formatting ---------------------------------------------------------

  public get consumptionUnit(): string {
    switch (this.currentDisplayFormat) {
      case 'fuel': return 't';
      case 'cost': return 'K';
      default: return 'MWh';
    }
  }

  public get consumptionUnitSmall(): string {
    switch (this.currentDisplayFormat) {
      case 'fuel': return 'kg';
      case 'cost': return '';
      default: return 'kWh';
    }
  }

  public get consumptionLabel(): string {
    switch (this.currentDisplayFormat) {
      case 'fuel': return 'Fuel';
      case 'cost': return 'Cost';
      default: return 'Energy';
    }
  }

  public getConsumptionValue(option: VoyageOption): number {
    switch (this.currentDisplayFormat) {
      case 'fuel':
        return (option.totalResistanceFuelConsumption ?? 0) / 1000;
      case 'cost':
        return (option.totalResistanceCost ?? 0) / 1000;
      default:
        return option.totalEnergyConsumption ?? 0;
    }
  }

  public getRelativeConsumptionValue(option: VoyageOption): number {
    switch (this.currentDisplayFormat) {
      case 'fuel':
        return option.fuelConsumptionRelative ?? 0;
      case 'cost':
        return option.costRelative ?? 0;
      default:
        return option.energyConsumptionRelative ?? 0;
    }
  }

  public absValue(value: number): number {
    return Math.abs(value);
  }

  /**
   * Constant speed shows one number; variable speed shows the span its segments actually cover, which is
   * the whole point of that option.
   */
  public formatSpeed(option: VoyageOption): string {
    const speeds = (option.routeSegments ?? [])
      .map(segment => segment.averageSpeed)
      .filter((speed): speed is number => typeof speed === 'number' && speed > 0);

    if (speeds.length === 0) {
      return `${option.averageSpeed.toFixed(1)} kn`;
    }

    const min = Math.min(...speeds);
    const max = Math.max(...speeds);

    if (max - min < 0.05) {
      return `${min.toFixed(1)} kn`;
    }

    return `${min.toFixed(1)} – ${max.toFixed(1)} kn`;
  }

  public formatDuration(durationInSeconds: number): string {
    if (!durationInSeconds) return '';
    const days = Math.floor(durationInSeconds / 86400);
    const hours = Math.floor((durationInSeconds % 86400) / 3600);
    const minutes = Math.floor((durationInSeconds % 3600) / 60);

    if (days > 0) return `${days}d ${hours}h`;
    if (hours > 0) return `${hours}h ${minutes}m`;
    return `${minutes}m`;
  }

  public formatRelative(value: number): string {
    if (Math.round(value) === 0) return 'best';
    return `+${Math.round(value)}%`;
  }
}
