import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { VoyageOption } from '../../../../models/entities/voyage-option.model';
import { DisplayFormat } from '../../../../services/state/voyage-scheduler.service';

@Component({
  selector: 'app-voyage-option-tooltip',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './voyage-option-tooltip.component.html',
  styleUrls: ['./voyage-option-tooltip.component.css']
})
export class VoyageOptionTooltipComponent implements OnChanges {
  @Input() voyageOption: VoyageOption | null = null;
  @Input() show: boolean = false;
  @Input() mouseX: number = 0;
  @Input() mouseY: number = 0;
  @Input() displayFormat: DisplayFormat = 'energy'; // Add this input

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['displayFormat']) {
      console.log('Tooltip display format changed to:', this.displayFormat);
    }
  }

  // Convert values to proper units
  public get energyInMWh(): string {
    if (!this.voyageOption) return '0.00';
    return (this.voyageOption.totalEnergyConsumption / 1000).toFixed(2);
  }

  public get fuelInTons(): string {
    if (!this.voyageOption) return '0.00';
    return (this.voyageOption.totalResistanceFuelConsumption / 1000).toFixed(2);
  }

  public get costInK$(): string {
    if (!this.voyageOption) return '0.00';
    return (this.voyageOption.totalResistanceCost! / 1000).toFixed(2);
  }

  public get speedInKnots(): string {
    if (!this.voyageOption) return '0.00';
    return (this.voyageOption.averageSpeed).toFixed(2);
  }

  public get durationFormatted(): string {
    if (!this.voyageOption) return '';
    const duration = this.voyageOption.durationInSeconds;
    const days = Math.floor(duration / 86400);
    const hours = Math.floor((duration % 86400) / 3600);
    const minutes = Math.floor((duration % 3600) / 60);
    
    if (days > 0) {
      return `${days}d ${hours}h`;
    } else if (hours > 0) {
      return `${hours}h ${minutes}m`;
    } else {
      return `${minutes}m`;
    }
  }

  public get shouldShowEnergy(): boolean {
    return this.displayFormat === 'energy';
  }

  public get shouldShowFuel(): boolean {
    return this.displayFormat === 'fuel';
  }

  public get shouldShowCost(): boolean {
    return this.displayFormat === 'cost';
  }

  public get tooltipStyle(): { [key: string]: string } {
    if (!this.show) return { display: 'none' };

    const offsetX = 15;
    const offsetY = 15;

    return {
      display: 'block',
      left: `${this.mouseX + offsetX}px`,
      top: `${this.mouseY + offsetY}px`
    }
  }
}