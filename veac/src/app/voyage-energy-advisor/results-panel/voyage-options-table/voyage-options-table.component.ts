import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, HostListener, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { VoyageOption } from '../../../models/entities/voyage-option.model';
import { EaEnergyValuePipe } from '../../../shared/pipes/ea-energy-value-pipe';
import { VoyageRouteAnalysisComponent } from '../voyage-route-analysis/voyage-route-analysis.component';
import { VoyageOptionTooltipComponent } from './voyage-option-tooltip/voyage-option-tooltip.component';
import { Subscription } from 'rxjs';
import { VoyageService, DisplayFormat } from '../../../services/state/voyage-scheduler.service';

@Component({
  selector: 'app-voyage-options-table',
  standalone: true,
  imports: [CommonModule, FormsModule, EaEnergyValuePipe, VoyageRouteAnalysisComponent, VoyageOptionTooltipComponent],
  templateUrl: './voyage-options-table.component.html',
  styleUrls: ['./voyage-options-table.component.css']
})
export class VoyageOptionsTableComponent implements OnInit, OnDestroy, OnChanges {
  @Input() voyageOptions: VoyageOption[] = [];
  @Input() etdTimes: number[] = [];
  @Input() etaTimes: number[] = [];
  @Input() selectedVoyageOption: VoyageOption | null = null;
  @Input() validationMessage?: string;
  @Output() voyageOptionSelected = new EventEmitter<VoyageOption>();
  
  public optionClasses: Record<string, string[]> = {};
  public isMapExpanded: boolean = false;
  public showOverlayControls: boolean = false;
  public currentDisplayFormat: DisplayFormat = 'energy';

  // Tooltip properties
  public showTooltip: boolean = false;
  public tooltipVoyageOption: VoyageOption | null = null;
  public tooltipX: number = 0;
  public tooltipY: number = 0;

  private voyageOptionCache: Map<string, VoyageOption> = new Map();
  private subscriptions: Subscription[] = [];

  constructor(private voyageService: VoyageService) {}

  ngOnInit(): void {
    console.log("voyageOptions", this.voyageOptions)
    // Subscribe to display format changes
    const displayFormatSub = this.voyageService.displayFormat$.subscribe(format => {
      this.currentDisplayFormat = format;
      this.updateVoyageOptionCache();
      this.updateOptionClasses();
    });

    this.subscriptions.push(displayFormatSub);
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

   public hasValidOptions(): boolean {
    return this.voyageOptions.some(option => option.isValid);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['voyageOptions'] || changes['selectedVoyageOption']) 
        && this.voyageOptions) {
      this.updateVoyageOptionCache();
      this.updateOptionClasses();
    }
  }

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

  public get consumptionPropertyName(): keyof VoyageOption {
    switch (this.currentDisplayFormat) {
      case 'fuel': return 'totalResistanceFuelConsumption';
      case 'cost': return 'totalResistanceCost';  
      default: return 'totalEnergyConsumption';
    }
  }

  public get relativeConsumptionPropertyName(): keyof VoyageOption {
    switch (this.currentDisplayFormat) {
      case 'fuel': return 'fuelConsumptionRelative';
      case 'cost': return 'costRelative';
      default: return 'energyConsumptionRelative';
    }
  }
  
  private updateVoyageOptionCache(): void {
    this.voyageOptionCache.clear();
    for (const option of this.voyageOptions) {
      const key = `${option.etd}-${option.eta}`;
      this.voyageOptionCache.set(key, option);
    }
  }

  public voyageOptionClick(etd: number, eta: number): void {
    if (this.isVoyageOptionAvailable(etd, eta)) {
      const voyageOption = this.getVoyageOption(etd, eta);
      this.selectedVoyageOption = voyageOption;
      this.voyageOptionSelected.emit(voyageOption);
      this.updateOptionClasses();
      this.expandMap();
    }
  }

  public expandMap(): void {
    this.isMapExpanded = true;
  }

  public collapseMap(): void {
    this.isMapExpanded = false;
    this.showOverlayControls = false;
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscapeKey(event: KeyboardEvent): void {
    if (this.isMapExpanded) {
      this.collapseMap();
    }
  }

  public showMoreOptions() {
    this.isMapExpanded = false;
  }

  public getVoyageOption(etd: number, eta: number): VoyageOption {
    const key = `${etd}-${eta}`;
    return this.voyageOptionCache.get(key) || this.createDefaultVoyageOption();
  }

  private createDefaultVoyageOption(): VoyageOption {
    return {
      etd: 0,
      eta: 0,
      isValid: false,
      averageSpeed: 0,
      durationInSeconds: 0,
      totalWindEnergyConsumption: 0,
      totalWaveEnergyConsumption: 0,
      totalCurrentEnergyConsumption: 0,
      totalSailEnergyConsumption: 0,
      totalEnergyConsumption: 0,
      totalCalmWaterResistanceEnergyConsumption: 0,
      relativeWindEnergyConsumption: 0,
      relativeWaveEnergyConsumption: 0,
      relativeCurrentEnergyConsumption: 0,
      relativeSailEnergyConsumption: 0,
      routeOptionNumber: 0,
      averagePower: 0,
      energyConsumptionRelative: 0,
      routeSegments: [],
      totalResistanceFuelConsumption: 0,
      totalCalmWaterResistanceFuelConsumption: 0,
      totalWindFuelConsumption: 0,
      totalWaveFuelConsumption: 0,
      totalCurrentFuelConsumption: 0,
      totalSailFuelConsumption: 0,
      relativeWindFuelConsumption: 0,
      relativeWaveFuelConsumption: 0,
      relativeCurrentFuelConsumption: 0,
      relativeSailFuelConsumption: 0,
      averageFuelConsumptionRate: 0,
      fuelConsumptionRelative: 0,
      totalResistanceCost: 0,  // CHANGED from totalCost
      costRelative: 0,
    };
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

  public isVoyageOptionAvailable(etd: number, eta: number): boolean {
    const voyageOption = this.getVoyageOption(etd, eta);
    return !!voyageOption && voyageOption.isValid;
  }

  public isVoyageOptionOptimal(etd: number, eta: number): boolean {
    if (!this.isVoyageOptionAvailable(etd, eta)) {
      return false;
    }

    const validOptions = this.voyageOptions.filter(option => option.isValid);
    if (validOptions.length === 0) return false;

    const minConsumption = Math.min(
      ...validOptions.map(option => this.getConsumptionValue(option))
    );
    
    const voyageOption = this.getVoyageOption(etd, eta);
    const currentConsumption = this.getConsumptionValue(voyageOption);
    return currentConsumption === minConsumption;
  }

  public isVoyageOptionSelected(etd: number, eta: number): boolean {
    if (!this.isVoyageOptionAvailable(etd, eta)) {
      return false;
    }
    const voyageOption = this.getVoyageOption(etd, eta);
    return voyageOption === this.selectedVoyageOption;
  }

  private updateOptionClasses(): void {
    this.optionClasses = {};

    const validOptions = this.voyageOptions.filter(option => option.isValid);
    if (validOptions.length === 0) return;
    
    const minConsumption = Math.min(
      ...validOptions.map(option => this.getConsumptionValue(option))
    );

    for (const option of this.voyageOptions) {
      const key = `${option.etd}-${option.eta}`;
      const isAvailable = !!option && option.isValid;
      const currentConsumption = this.getConsumptionValue(option);
      const isOptimal = isAvailable && currentConsumption === minConsumption;
      const isSelected = this.isVoyageOptionSelected(option.etd, option.eta);
      
      const classes = [];
      
      if (!isAvailable) {
        classes.push('ea-vec-voyage-option-unavailable');
      } else {
        if (isOptimal) {
          classes.push('ea-vec-voyage-option-optimal');
        }
        if (isSelected) {
          classes.push('ea-vec-voyage-option-selected');
        }
      }
      
      this.optionClasses[key] = classes;
    }
  }

  public formatDuration(durationInSeconds: number): string {
    if (!durationInSeconds) return '';
    const days = Math.floor(durationInSeconds / 86400);
    const hours = Math.floor((durationInSeconds % 86400) / 3600);
    const minutes = Math.floor((durationInSeconds % 3600) / 60);

    if (days > 0) {
      return `${days}d ${hours}h`;
    } else if (hours > 0) {
      return `${hours}h ${minutes}m`;
    } else {
      return `${minutes}m`;
    }
  }
  
  public onCellMouseEnter(event: MouseEvent, etd: number, eta: number): void {
    if (this.isVoyageOptionAvailable(etd, eta)) {
      this.tooltipVoyageOption = this.getVoyageOption(etd, eta);
      this.showTooltip = true;
      this.updateTooltipPosition(event);
    }
  }

  public onCellMouseMove(event: MouseEvent): void {
    if (this.showTooltip) {
      this.updateTooltipPosition(event);
    }
  }

  public onCellMouseLeave(): void {
    this.showTooltip = false;
    this.tooltipVoyageOption = null;
  }

  private updateTooltipPosition(event: MouseEvent): void {
    this.tooltipX = event.clientX;
    this.tooltipY = event.clientY;
  }
}