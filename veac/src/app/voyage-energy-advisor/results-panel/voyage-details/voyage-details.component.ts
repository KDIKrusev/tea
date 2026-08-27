import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { VoyageOption } from '../../../models/entities/voyage-option.model';
import { RouteSegment } from '../../../models/entities/route-segment.model';
import { EaEnergyValuePipe } from '../../../shared/pipes/ea-energy-value-pipe';
import { EaPowerValuePipe } from '../../../shared/pipes/ea-power-value-pipe';
import { VoyageService, DisplayFormat } from '../../../services/state/voyage-scheduler.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-voyage-details',
  standalone: true,
  imports: [CommonModule, EaEnergyValuePipe, EaPowerValuePipe],
  templateUrl: './voyage-details.component.html',
  styleUrls: ['./voyage-details.component.css']
})
export class VoyageDetailsComponent implements OnInit, OnDestroy {
  @Input() selectedVoyageOption!: VoyageOption;
  @Input() durationText!: string;
  @Input() selectedVoyageOptionIndex?: number;
  @Input() selectedSegment: RouteSegment | null = null;
  @Input() isVariableSpeedOption = false;
  
  private subscriptions: Subscription[] = [];
  public currentDisplayFormat: DisplayFormat = 'energy';

  constructor(private voyageService: VoyageService) {}

  ngOnInit(): void {
    this.voyageService.displayFormat$.subscribe(format => {
      this.currentDisplayFormat = format;
    });
    
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  // Backward compatibility getter
  public get showFuelConsumption(): boolean {
    return this.currentDisplayFormat === 'fuel';
  }

  public get showCost(): boolean {
    return this.currentDisplayFormat === 'cost';
  }

  public get consumptionUnit(): string {
    switch (this.currentDisplayFormat) {
      case 'cost': return '$';
      case 'fuel': return 'kg';
      default: return 'kWh';
    }
  }

  public get sectionTitle(): string {
    switch (this.currentDisplayFormat) {
      case 'cost': return 'Cost breakdown';
      case 'fuel': return 'Fuel consumption';
      default: return 'Energy requirements';
    }
  }

  public getCalmWaterConsumption(): number {
    if (this.currentDisplayFormat === 'cost') {
      return this.selectedVoyageOption.totalCalmWaterResistanceCost ?? 0;
    } else if (this.currentDisplayFormat === 'fuel') {
      return this.selectedVoyageOption.totalCalmWaterResistanceFuelConsumption ?? 0;
    } else {
      return this.selectedVoyageOption.totalCalmWaterResistanceEnergyConsumption;
    }
  }

  public getWindConsumption(): number {
    if (this.currentDisplayFormat === 'cost') {
      return this.selectedVoyageOption.totalWindCost ?? 0;
    } else if (this.currentDisplayFormat === 'fuel') {
      return this.selectedVoyageOption.totalWindFuelConsumption ?? 0;
    } else {
      return this.selectedVoyageOption.totalWindEnergyConsumption;
    }
  }

  public getCurrentConsumption(): number {
    if (this.currentDisplayFormat === 'cost') {
      return this.selectedVoyageOption.totalCurrentCost ?? 0;
    } else if (this.currentDisplayFormat === 'fuel') {
      return this.selectedVoyageOption.totalCurrentFuelConsumption ?? 0;
    } else {
      return this.selectedVoyageOption.totalCurrentEnergyConsumption;
    }
  }

  public getWaveConsumption(): number {
    if (this.currentDisplayFormat === 'cost') {
      return this.selectedVoyageOption.totalWaveCost ?? 0;
    } else if (this.currentDisplayFormat === 'fuel') {
      return this.selectedVoyageOption.totalWaveFuelConsumption ?? 0;
    } else {
      return this.selectedVoyageOption.totalWaveEnergyConsumption;
    }
  }

  public getSailConsumption(): number {
    if (this.currentDisplayFormat === 'cost') {
      return this.selectedVoyageOption.totalSailCost ?? 0;
    } else if (this.currentDisplayFormat === 'fuel') {
      return this.selectedVoyageOption.totalSailFuelConsumption ?? 0;
    } else {
      return this.selectedVoyageOption.totalSailEnergyConsumption;
    }
  }

  public getTotalConsumption(): number {
    if (this.currentDisplayFormat === 'cost') {
      return this.selectedVoyageOption.totalResistanceCost ?? 0;
    } else if (this.currentDisplayFormat === 'fuel') {
      return this.selectedVoyageOption.totalResistanceFuelConsumption ?? 0;
    } else {
      return this.selectedVoyageOption.totalEnergyConsumption;
    }
  }

  public getRelativeWind(): number {
    if (this.currentDisplayFormat === 'cost') {
      return this.selectedVoyageOption.relativeWindCost ?? 
             this.selectedVoyageOption.relativeWindFuelConsumption ?? 0;
    } else if (this.currentDisplayFormat === 'fuel') {
      return this.selectedVoyageOption.relativeWindFuelConsumption ?? 0;
    } else {
      return this.selectedVoyageOption.relativeWindEnergyConsumption;
    }
  }

  public getRelativeCurrent(): number {
    if (this.currentDisplayFormat === 'cost') {
      return this.selectedVoyageOption.relativeCurrentCost ?? 
             this.selectedVoyageOption.relativeCurrentFuelConsumption ?? 0;
    } else if (this.currentDisplayFormat === 'fuel') {
      return this.selectedVoyageOption.relativeCurrentFuelConsumption ?? 0;
    } else {
      return this.selectedVoyageOption.relativeCurrentEnergyConsumption;
    }
  }

  public getRelativeWave(): number {
    if (this.currentDisplayFormat === 'cost') {
      return this.selectedVoyageOption.relativeWaveCost ?? 
             this.selectedVoyageOption.relativeWaveFuelConsumption ?? 0;
    } else if (this.currentDisplayFormat === 'fuel') {
      return this.selectedVoyageOption.relativeWaveFuelConsumption ?? 0;
    } else {
      return this.selectedVoyageOption.relativeWaveEnergyConsumption;
    }
  }

  public getRelativeSail(): number {
    if (this.currentDisplayFormat === 'cost') {
      return this.selectedVoyageOption.relativeSailCost ?? 
             this.selectedVoyageOption.relativeSailFuelConsumption ?? 0;
    } else if (this.currentDisplayFormat === 'fuel') {
      return this.selectedVoyageOption.relativeSailFuelConsumption ?? 0;
    } else {
      return this.selectedVoyageOption.relativeSailEnergyConsumption;
    }
  }

  public formatConsumption(value: number): string {
    if (this.currentDisplayFormat === 'cost') {
      if (value >= 1000) {
        return `$${(value / 1000).toFixed(2)}K`;
      }
      return `$${value.toFixed(2)}`;
    } else if (this.currentDisplayFormat === 'fuel') {
      // Fuel: Convert kg to tons if value is large
      if (value >= 1000) {
        return `${(value / 1000).toFixed(1)} t`;
      }
      return `${value.toFixed(1)} kg`;
    } else {
      // Energy: kWh or MWh
      if (value >= 1000) {
        return `${(value / 1000).toFixed(1)} MWh`;
      }
      return `${value.toFixed(1)} kWh`;
    }
  }

  public isVoyageOptionOptimal(voyageOption: VoyageOption): boolean {
    if (this.currentDisplayFormat === 'cost') {
      return (voyageOption.costRelative === 0);
    } else if (this.currentDisplayFormat === 'fuel') {
      return (voyageOption.fuelConsumptionRelative === 0);
    } else {
      return (voyageOption.energyConsumptionRelative === 0);
    }
  }

  public getRouteOptionNumber(): number {
    if (this.selectedVoyageOption.routeOptionNumber !== undefined && 
        this.selectedVoyageOption.routeOptionNumber !== null) {
      return this.selectedVoyageOption.routeOptionNumber;
    }
            
    if (this.selectedVoyageOptionIndex !== undefined) {
      return this.selectedVoyageOptionIndex + 1;
    }
            
    return 1;
  }

  public formatDuration(durationInSeconds: number): string {
    if (!durationInSeconds || durationInSeconds <= 0) return '';
        
    const totalMinutes = Math.floor(durationInSeconds / 60);
    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;
        
    if (hours > 0) {
      return `${hours}h${minutes > 0 ? ` ${minutes}m` : ''}`;
    } else {
      return `${minutes}m`;
    }
  }

  public isResistance(value: number): boolean {
    return value > 0; 
  }

  public hasResistances(): boolean {
    return this.getRelativeWind() > 0 || 
           this.getRelativeWave() > 0 || 
           this.getRelativeCurrent() > 0 ||
           this.getRelativeSail() > 0;
  }

  public hasContributions(): boolean {
    return this.getRelativeWind() < 0 || 
           this.getRelativeWave() < 0 || 
           this.getRelativeCurrent() < 0 ||
           this.getRelativeSail() < 0;
  }

  public shouldShowVariableSpeedSegmentDetails(): boolean {
    return this.isVariableSpeedOption && !!this.selectedSegment;
  }

  public formatPercentage(value: number): string {
    if (value === 0) return '0%';
    return (value > 0 ? '+' : '') + value.toFixed(0) + '%';
  }
}