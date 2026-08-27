import { Component, Input, Output, EventEmitter, ViewChild, OnInit, OnDestroy, HostListener, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { VoyageOption } from '../../../models/entities/voyage-option.model';
import { EaEnergyValuePipe } from '../../../shared/pipes/ea-energy-value-pipe';
import { VoyageMapComponent } from '../../../shared/components/map/voyage-map.component';
import { VoyageMapService } from '../../../services/ui/map/voyage-map.service';
import { VoyageService, DisplayFormat } from '../../../services/state/voyage-scheduler.service';
import { MapOverlaySettings, OverlayVisualizationMode } from '../../../services/ui/map/voyage-map-service-type';
import { CurrentPosition } from '../../../models/entities/current-vessel-position.model'; // Add this import

@Component({
  selector: 'app-voyage-route-analysis',
  standalone: true,
  imports: [CommonModule, FormsModule, EaEnergyValuePipe, VoyageMapComponent],
  templateUrl: './voyage-route-analysis.component.html',
  styleUrls: ['./voyage-route-analysis.component.css'],
  providers: [VoyageMapService]
})

export class VoyageRouteAnalysisComponent implements OnInit, OnDestroy, OnChanges {
  @Input() selectedVoyageOption: VoyageOption | null = null;
  
  // Add live mode inputs
  @Input() isLiveMode: boolean = false;
  @Input() currentVesselPosition: CurrentPosition | null = null;
  @Input() autoFollow: boolean = false;
  
  @Output() closeRequested = new EventEmitter<void>();
  @Output() overlaySettingsChanged = new EventEmitter<MapOverlaySettings>();

  public currentDisplayFormat: DisplayFormat = 'energy';
  // Map-related state
  public isMapMovedInline: boolean = false;

  public overlaySettings: MapOverlaySettings = {
    vesselCourse: false,
    trueWind: false,
    trueCurrent: false,
    weatherData: false,
    trueWaves: false,
    routeSegments: true,
    showLabels: false,
    activeOverlay: 'none'
  };

  public visualizationMode: OverlayVisualizationMode = {
    vesselCourse: 'simplified',
    trueWind: 'vectors',
    trueCurrent: 'vectors',
    trueWaves: 'vectors'
  };

  constructor(
    private voyageMapService: VoyageMapService ,
    private voyageService: VoyageService
  ) {}

  ngOnInit(): void {

     this.voyageService.displayFormat$.subscribe(format => {
      this.currentDisplayFormat = format;
    });

    this.isMapMovedInline = true;
    if (this.selectedVoyageOption?.routeSegments) {
      this.voyageMapService.setRouteSegments(this.selectedVoyageOption.routeSegments);
    }


  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedVoyageOption'] && !changes['selectedVoyageOption'].firstChange) {
      if (this.selectedVoyageOption?.routeSegments) {
        this.voyageMapService.setRouteSegments(this.selectedVoyageOption.routeSegments);
        
        setTimeout(() => {
          if (this.overlaySettings.activeOverlay !== 'none') {
            const activeOverlay = this.overlaySettings.activeOverlay;
            this.hideAllOverlays();
            
            setTimeout(() => {
              this.showOverlay(activeOverlay as keyof MapOverlaySettings);
            }, 50);
          }
        }, 100);
      }
    }

    if (changes['currentVesselPosition'] && this.isLiveMode && this.currentVesselPosition) {
      this.updateVesselPositionOnMap();
    }
  }

  ngOnDestroy(): void {
  }

  private updateVesselPositionOnMap(): void {
    if (this.isLiveMode && this.currentVesselPosition) {
    }
  }

  // === MAP OVERLAY METHODS ===

  public onOverlayToggle(overlayType: keyof MapOverlaySettings, event: any): void {
    const isChecked = event.target.checked;
    
    this.clearAllOverlays();
    
    if (isChecked) {
      // Set the selected overlay
      (this.overlaySettings as any)[overlayType] = true;
      this.overlaySettings.activeOverlay = overlayType as any;
      
      // Tell the service to show this overlay
      this.showOverlay(overlayType);
    } else {
      // Turn off all overlays
      this.overlaySettings.activeOverlay = 'none';
      this.hideAllOverlays();
    }
    
    this.overlaySettingsChanged.emit(this.overlaySettings);
  }

  private hideAllOverlays(): void {
    this.voyageMapService.hideAllOverlays();
  }

  private showOverlay(overlayType: keyof MapOverlaySettings): void {
    // Call service method based on overlay type
    switch (overlayType) {
      case 'vesselCourse':
        this.voyageMapService.showVesselCourse();
        break;
      case 'trueWind':
        this.voyageMapService.showWindVectors();
        break;
      case 'trueCurrent':
        this.voyageMapService.showCurrentVectors();
        break;
      case 'weatherData':
        this.voyageMapService.showWeatherData();
        break;
      case 'trueWaves':
        this.voyageMapService.showWaveVectors();
        break;
      default:
        console.warn("Unknown overlay type:", overlayType);
    }
  }

   public getTotalConsumption(): number {
  if (!this.selectedVoyageOption) return 0;
  
  if (this.currentDisplayFormat === 'cost') {
    return this.selectedVoyageOption.totalResistanceCost ?? 0;
  } else if (this.currentDisplayFormat === 'fuel') {
    return this.selectedVoyageOption.totalResistanceFuelConsumption ?? 0;
  } else {
    return this.selectedVoyageOption.totalEnergyConsumption;
  }
}

  public formatConsumption(value: number): string {
    if (this.currentDisplayFormat === 'cost') {
      // Format as currency
      if (value >= 1000) {
        return `$${(value / 1000).toFixed(2)}k`;
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

  private clearAllOverlays(): void {
    this.overlaySettings.vesselCourse = false;
    this.overlaySettings.trueWind = false;
    this.overlaySettings.trueCurrent = false;
    this.overlaySettings.weatherData = false;
    this.overlaySettings.trueWaves = false;
  }

  public onLabelsToggle(event: any): void {
    this.overlaySettings.showLabels = event.target.checked;
    this.voyageMapService.setLabelsVisibility(event.target.checked);
    this.overlaySettingsChanged.emit(this.overlaySettings);
  }

  public hasActiveOverlay(): boolean {
    return this.overlaySettings.activeOverlay !== 'none' && 
           this.overlaySettings.activeOverlay !== undefined;
  }

  public getActiveOverlaysCount(): number {
    let count = 0;
    if (this.overlaySettings.vesselCourse) count++;
    if (this.overlaySettings.trueWind) count++;
    if (this.overlaySettings.trueCurrent) count++;
    if (this.overlaySettings.weatherData) count++;
    if (this.overlaySettings.trueWaves) count++;
    return count;
  }

  // === CORE DATA METHODS ===
  
  public calculateDistance(): string {
    if (this.selectedVoyageOption) {
      const hours = this.selectedVoyageOption.durationInSeconds / 3600;
      const distance = hours * this.selectedVoyageOption.averageSpeed;
      return distance.toFixed(0);
    }
    return '0';
  }

  public getPerformancePercentage(): number {
    if (!this.selectedVoyageOption) return 100;
    const relative = this.selectedVoyageOption.energyConsumptionRelative;
    if (relative === 0) return 100;
    return Math.max(20, 100 - Math.min(relative * 2, 80));
  }

  public getAverageWindSpeed(): number {
    if (!this.selectedVoyageOption?.routeSegments) return 0;
    const segments = this.selectedVoyageOption.routeSegments;
    const validSegments = segments.filter(segment => segment.trueWeather?.windSpeed != null);
    if (validSegments.length === 0) return 0;
    const total = validSegments.reduce((sum, segment) => sum + segment.trueWeather!.windSpeed, 0);
    return Math.round((total / validSegments.length) * 10) / 10;
  }

  public getAverageWaveHeight(): number {
    if (!this.selectedVoyageOption?.routeSegments) return 0;
    const segments = this.selectedVoyageOption.routeSegments;
    const validSegments = segments.filter(segment => segment.trueWeather?.waveHeight != null);
    if (validSegments.length === 0) return 0;
    const total = validSegments.reduce((sum, segment) => sum + segment.trueWeather!.waveHeight, 0);
    return Math.round((total / validSegments.length) * 10) / 10;
  }

  public getAverageCurrentSpeed(): number {
    if (!this.selectedVoyageOption?.routeSegments) return 0;
    const segments = this.selectedVoyageOption.routeSegments;
    const validSegments = segments.filter(segment => segment.trueWeather?.currentSpeed != null);
    if (validSegments.length === 0) return 0;
    const total = validSegments.reduce((sum, segment) => sum + segment.trueWeather!.currentSpeed, 0);
    return Math.round((total / validSegments.length) * 10) / 10;
  }

  public getAverageTemperature(): number {
    if (!this.selectedVoyageOption?.routeSegments) return 0;
    const segments = this.selectedVoyageOption.routeSegments;
    const validSegments = segments.filter(segment => 
      segment.trueWeather?.airTemperature != null
    );
    if (validSegments.length === 0) return 0;
    const total = validSegments.reduce((sum, segment) => 
      sum + segment.trueWeather!.airTemperature, 0
    );
    return Math.round((total / validSegments.length) * 10) / 10;
  }

  public getRouteSegmentsCount(): number {
    return this.selectedVoyageOption?.routeSegments?.length || 0;
  }

  public roundNumber(value: number): number {
    return Math.round(value);
  }

  public formatDateTime(timestamp: number): string {
    if (!timestamp) return '--:--';
    const date = new Date(timestamp);
    return date.toLocaleTimeString('en-GB', { 
      hour: '2-digit', 
      minute: '2-digit',
      timeZone: 'UTC'
    });
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

  // === EVENT HANDLERS ===

  public closePanel(): void {
    this.closeRequested.emit();
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscapeKey(event: KeyboardEvent): void {
    this.closePanel();
  }
}