import { Component, OnDestroy, OnInit, HostListener } from '@angular/core';
import { Subscription } from 'rxjs';
import { CommonModule } from '@angular/common';
import { VoyageService } from '../../services/state/voyage-scheduler.service';
import { voyageEnergyAdvisorResponse } from '../../models/api/voyage-energy-advisor-response.model';
import { VoyageOption } from '../../models/entities/voyage-option.model';
import { RouteSegment } from '../../models/entities/route-segment.model';
import { VoyageOriginalRequest } from '../../models/api/voyage-original-request.model';

// Import child components
import { VoyagePowerChartComponent } from './voyage-power-chart/voyage-power-chart.component';
import { SegmentDetailsComponent } from './segment-details/segment-details.component';
import { VoyageOptionsTableComponent } from './voyage-options-table/voyage-options-table.component';
import { VoyageDetailsComponent } from './voyage-details/voyage-details.component';
import { NoResultsComponent } from './no-results/no-results.component';
import { VoyageRouteAnalysisComponent } from './voyage-route-analysis/voyage-route-analysis.component';
import { EaEnergyValuePipe } from '../../shared/pipes/ea-energy-value-pipe';

@Component({
  selector: 'app-voyage-energy-advisor-results-panel',
  standalone: true,
  imports: [
    CommonModule,
    VoyagePowerChartComponent,
    SegmentDetailsComponent,
    VoyageOptionsTableComponent,
    VoyageDetailsComponent,
    NoResultsComponent,
    VoyageRouteAnalysisComponent,
    EaEnergyValuePipe
  ],
  templateUrl: './voyage-energy-advisor-results-panel.component.html',
  styleUrls: ['./voyage-energy-advisor-results-panel.component.css'],
})
export class VoyageEnergyAdvisorResultsPanelComponent implements OnInit, OnDestroy {
  public voyageOptions: VoyageOption[] = [];
  public durationText!: string;
  public selectedSegment: RouteSegment | null = null;
  public etdTimes: number[] = [];
  public etaTimes: number[] = [];
  public originalRequestData!: VoyageOriginalRequest;
  public validationMessage?: string;
  public optimalVoyageOption: VoyageOption | null = null;
  public isOptimalLoading = false;
  public optimalError: string | null = null;
  private noAvailableOptions!: boolean;
  private responseReceivedSubscription!: Subscription;
  private subscriptions: Subscription[] = [];

  constructor(
    private voyageService: VoyageService
  ) { }

  ngOnInit(): void {
    this.responseReceivedSubscription = this.voyageService.responseReceived$
      .subscribe((response: voyageEnergyAdvisorResponse | null) => {
        this.onResponseReceived(response);
      });

    this.subscriptions.push(
      this.voyageService.optimalVoyageOption$.subscribe(option => {
        this.optimalVoyageOption = option;
      }),
      this.voyageService.optimalLoading$.subscribe(isLoading => {
        this.isOptimalLoading = isLoading;
      }),
      this.voyageService.optimalError$.subscribe(error => {
        this.optimalError = error;
      }),
      this.voyageService.selectedSegment$.subscribe(selection => {
        if (!selection || !this.selectedVoyageOption?.routeSegments) return;
        this.selectedSegment = this.selectedVoyageOption.routeSegments[selection.segmentIndex] ?? null;
      })
    );

      const displayFormatSub = this.voyageService.displayFormat$.subscribe(() => {
        if (this.voyageOptions.length > 0) {
          this.findOptimalOptionForCurrentMode();
        }
      });
    this.subscriptions.push(displayFormatSub);
  }

private findOptimalOptionForCurrentMode(): void {
  const availableOptions = this.availableVoyageOptions;
  if (availableOptions.length === 0) return;

  const currentFormat = this.voyageService.getDisplayFormat();
  let minValue: number;
  
  if (currentFormat === 'cost') {
    // Find minimum cost
    minValue = Math.min(
      ...availableOptions.map(option => {
        const fuel = option.totalResistanceFuelConsumption || 0;
        return fuel * this.voyageService.getFuelPricePerKg();
      })
    );
  } else if (currentFormat === 'fuel') {
    // Find minimum fuel
    minValue = Math.min(
      ...availableOptions.map(option => option.totalResistanceFuelConsumption)
    );
  } else {
    // Find minimum energy
    minValue = Math.min(
      ...availableOptions.map(option => option.totalEnergyConsumption)
    );
  }

  const optimalOption = availableOptions.find(option => {
    let optionValue: number;
    
    if (currentFormat === 'cost') {
      const fuel = option.totalResistanceFuelConsumption || 0;
      optionValue = fuel * this.voyageService.getFuelPricePerKg();
    } else if (currentFormat === 'fuel') {
      optionValue = option.totalResistanceFuelConsumption;
    } else {
      optionValue = option.totalEnergyConsumption;
    }
    
    return optionValue === minValue;
  });

  if (optimalOption && optimalOption !== this.selectedVoyageOption) {
    const keepModalHidden = !this.showVoyageOptionsModal;
    this.voyageService.selectVoyageOption(optimalOption, keepModalHidden);
    this.onSelectedVoyageOptionChanged();
  }
}
  

  ngOnDestroy(): void {
    if (this.responseReceivedSubscription) {
      this.responseReceivedSubscription.unsubscribe();
    }
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  public get showVoyageOptionsModal(): boolean {
    return this.voyageService.showVoyageOptionsModal;
  }

  public set showVoyageOptionsModal(value: boolean) {
    this.voyageService.showVoyageOptionsModal = value;
  }

  public get selectedVoyageOption(): VoyageOption {
    return this.voyageService.selectedVoyageOption || this.createDefaultVoyageOption();
  }

  public set selectedVoyageOption(option: VoyageOption) {
    this.voyageService.selectedVoyageOption = option;
  }

  public get hasResults(): boolean {
    return this.voyageOptions.length > 0 && !this.noAvailableOptions;
  }

  public get availableVoyageOptions(): VoyageOption[] {
    return this.voyageOptions.filter(option => 
      this.isVoyageOptionAvailable(option.etd, option.eta)
    );
  }

   public getConsumptionValue(option: VoyageOption): number {
    return this.showFuelConsumption ? 
      option.totalResistanceFuelConsumption : 
      option.totalEnergyConsumption;
  }

  public getRelativeConsumptionValue(option: VoyageOption): number {
    return this.showFuelConsumption ? 
      option.fuelConsumptionRelative : 
      option.energyConsumptionRelative;
  }

  public get showFuelConsumption(): boolean {
    return this.voyageService.showFuelConsumption;
  }


private onResponseReceived(response: voyageEnergyAdvisorResponse | null): void {
    if (!response || !response.voyageOptions || !response.voyageOptions.length) {
      this.voyageOptions = [];
      this.noAvailableOptions = true;
      this.showVoyageOptionsModal = false;
      this.validationMessage = response?.validationMessage;
    } else {
      this.noAvailableOptions = false;
      this.voyageOptions = response.voyageOptions;
      this.validationMessage = response.validationMessage;
      this.etdTimes = [...new Set(this.voyageOptions.map(option => option.etd))].sort();
      this.etaTimes = [...new Set(this.voyageOptions.map(option => option.eta))].sort();
      this.originalRequestData = this.voyageService.getOriginalRequestData();

      // Use current display mode to find optimal option
      this.findOptimalOptionForCurrentMode();
      this.showVoyageOptionsModal = true;
      this.onSelectedVoyageOptionChanged();
      console.log('VARIABLE_SPEED_TRIGGER', {
        selected: this.selectedVoyageOption,
        route: this.voyageService.getPlanningRoute(),
        originalRequest: this.originalRequestData,
        speedMin: this.originalRequestData?.speed?.min ?? this.originalRequestData?.speedMin,
        speedMax: this.originalRequestData?.speed?.max ?? this.originalRequestData?.speedMax,
        routeName: this.voyageService.getPlanningRoute()?.routeName,
        waypointCount: this.voyageService.getPlanningRoute()?.waypoints?.length ?? 0
      });
      if (!this.voyageService.wasOptimalVoyageRequestedFromSearch() && !this.isOptimalLoading && !this.optimalVoyageOption) {
        void this.calculateOptimalVoyage();
      }
    }
  }

  public onVoyageOptionSelected(voyageOption: VoyageOption): void {
    this.selectedVoyageOption = voyageOption;
    this.onSelectedVoyageOptionChanged();
  }

  // UPDATED: Use service method
  public onVoyageOptionSelectedFromModal(voyageOption: VoyageOption): void {
    // Use service method to set option and hide modal
    this.voyageService.selectVoyageOption(voyageOption, true);
    this.onSelectedVoyageOptionChanged();
  }

  public async calculateOptimalVoyage(): Promise<void> {
    const selected = this.selectedVoyageOption;
    const route = this.voyageService.getPlanningRoute();
    const originalRequest = this.originalRequestData;
    const speedMin = originalRequest?.speed?.min ?? originalRequest?.speedMin;
    const speedMax = originalRequest?.speed?.max ?? originalRequest?.speedMax;

    if (!route || !originalRequest || !selected.isValid || speedMin == null || speedMax == null) {
      this.optimalError = 'A valid speed range is required for the variable-speed calculation.';
      return;
    }

    this.isOptimalLoading = true;
    this.optimalError = null;
    console.log('VARIABLE_SPEED_REQUEST', {
      etd: selected.etd,
      eta: selected.eta,
      speedMin,
      speedMax,
      routeName: route?.routeName,
      routeWaypoints: route?.waypoints?.length ?? 0
    });
    try {
      this.optimalVoyageOption = await this.voyageService.startOptimalVoyageCalculation(
        selected.etd,
        selected.eta,
        speedMin,
        speedMax,
        route
      );
      console.log('VARIABLE_SPEED_RESPONSE_SEGMENTS', this.optimalVoyageOption?.routeSegments?.map((segment, index) => ({
        index,
        averageSpeed: segment.averageSpeed,
        totalPower: segment.avgTotalPower,
        calmWaterPower: segment.avgCalmWaterPower
      })));
    } catch (error) {
      console.error('Optimal voyage request failed', error);
      this.optimalError = 'Unable to calculate the variable-speed option.';
    } finally {
      this.isOptimalLoading = false;
    }
  }

  public selectOptimalVoyage(): void {
    if (!this.optimalVoyageOption) return;
    this.voyageService.selectVoyageOption(this.optimalVoyageOption, true);
    this.onSelectedVoyageOptionChanged();
  }

  public onMapCloseRequested(): void {
    this.showVoyageOptionsModal = true;
  }

  public closeVoyageOptionsModal(): void {
    this.showVoyageOptionsModal = false;
  }

  public onModalBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.closeVoyageOptionsModal();
    }
  }

  public onSegmentSelected(segment: RouteSegment): void {
    this.selectedSegment = segment;
  }

  public getSelectedSegmentIndex(): number | null {
    if (!this.selectedSegment || !this.selectedVoyageOption?.routeSegments) {
      return null;
    }

    const index = this.selectedVoyageOption.routeSegments.indexOf(this.selectedSegment);
    return index >= 0 ? index : null;
  }

  public isSelectedOptionVariableSpeed(): boolean {
    if (this.selectedVoyageOption?.isVariableSpeedOption === true) {
      return true;
    }

    const speeds = this.selectedVoyageOption?.routeSegments?.map(segment => segment.averageSpeed) ?? [];
    if (speeds.length < 2) {
      return false;
    }

    return Math.max(...speeds) - Math.min(...speeds) > 0.1;
  }

  private onSelectedVoyageOptionChanged(): void {
    this.durationText = this.getSelectedVoyageDurationAsText();
    this.selectedSegment = null;
  }

  // UPDATED: Use service state
  private getSelectedVoyageDurationAsText(): string {
    const selected = this.voyageService.selectedVoyageOption;
    if (!selected) return '';
    
    const days = Math.floor(selected.durationInSeconds / (24 * 60 * 60));
    const daysms = selected.durationInSeconds % (24 * 60 * 60);
    const hours = Math.floor(daysms / (60 * 60));
    const hoursms = selected.durationInSeconds % (60 * 60);
    const minutes = Math.floor(hoursms / (60));
    
    let durationAsText: string = minutes + 'm';
    if (hours > 0 || days > 0) {
      durationAsText = hours + 'h ' + durationAsText;
    }
    if (days > 0) {
      durationAsText = days + 'd ' + durationAsText;
    }
    
    return durationAsText;
  }

  public isVoyageOptionAvailable(etd: number, eta: number): boolean {
    const voyageOption = this.getVoyageOption(etd, eta);
    return !!voyageOption && voyageOption.isValid;
  }
  
  public getVoyageOption(etd: number, eta: number): VoyageOption {
    return this.voyageOptions.find(
      (option) => option.etd === etd && option.eta === eta
    ) || this.createDefaultVoyageOption();
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
    };
  }

  // UPDATED: Use service state
  public getSelectedVoyageOptionIndex(): number {
    const selected = this.voyageService.selectedVoyageOption;
    if (!selected || !this.voyageOptions || this.voyageOptions.length === 0) {
      return 0;
    }
    
    const index = this.voyageOptions.findIndex(option => 
      option.etd === selected.etd && option.eta === selected.eta
    );
    
    return index >= 0 ? index : 0;
  }

  // UPDATED: Use service state
  public getSelectedAvailableVoyageOptionIndex(): number {
    const selected = this.voyageService.selectedVoyageOption;
    if (!selected) {
      return 0;
    }
    
    const availableOptions = this.availableVoyageOptions;
    const index = availableOptions.findIndex(option => 
      option.etd === selected.etd && option.eta === selected.eta
    );
    
    return index >= 0 ? index : 0;
  }

  // UPDATED: Use service method for navigation
  public navigateToPreviousOption(): void {
    const availableOptions = this.availableVoyageOptions;
    if (availableOptions.length === 0) return;

    const currentIndex = this.getSelectedAvailableVoyageOptionIndex();
    if (currentIndex > 0) {
      const previousOption = availableOptions[currentIndex - 1];
      this.voyageService.selectVoyageOption(previousOption, true); // Keep modal hidden
      this.onSelectedVoyageOptionChanged();
    }
  }

  // UPDATED: Use service method for navigation
  public navigateToNextOption(): void {
    const availableOptions = this.availableVoyageOptions;
    if (availableOptions.length === 0) return;

    const currentIndex = this.getSelectedAvailableVoyageOptionIndex();
    if (currentIndex < availableOptions.length - 1) {
      const nextOption = availableOptions[currentIndex + 1];
      this.voyageService.selectVoyageOption(nextOption, true); // Keep modal hidden
      this.onSelectedVoyageOptionChanged();
    }
  }

  // Formatting methods for the preview panel - UPDATED to use service state
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

  public formatDateTime(timestamp: number): string {
  const date = new Date(timestamp);
  return date.toLocaleTimeString('en-GB', { 
    hour: '2-digit', 
    minute: '2-digit',
    timeZone: 'UTC'
  });
}

  // UPDATED: Use service state
  public calculateDistance(): string {
    const selected = this.voyageService.selectedVoyageOption;
    if (selected) {
      const hours = selected.durationInSeconds / 3600;
      const distance = hours * selected.averageSpeed;
      return distance.toFixed(0);
    }
    return '0';
  }

  // UPDATED: Use service state
  public getRouteSegmentsCount(): number {
    return this.voyageService.selectedVoyageOption?.routeSegments?.length || 0;
  }

  // Weather calculation methods for preview panel - UPDATED to use service state
  public getAverageWindSpeed(): number {
    const selected = this.voyageService.selectedVoyageOption;
    if (!selected?.routeSegments) return 0;
    const segments = selected.routeSegments;
    const validSegments = segments.filter(segment => segment.trueWeather?.windSpeed != null);
    if (validSegments.length === 0) return 0;
    const total = validSegments.reduce((sum, segment) => sum + segment.trueWeather!.windSpeed, 0);
    return Math.round((total / validSegments.length) * 10) / 10;
  }

  public getAverageWaveHeight(): number {
    const selected = this.voyageService.selectedVoyageOption;
    if (!selected?.routeSegments) return 0;
    const segments = selected.routeSegments;
    const validSegments = segments.filter(segment => segment.trueWeather?.waveHeight != null);
    if (validSegments.length === 0) return 0;
    const total = validSegments.reduce((sum, segment) => sum + segment.trueWeather!.waveHeight, 0);
    return Math.round((total / validSegments.length) * 10) / 10;
  }

  public getAverageCurrentSpeed(): number {
    const selected = this.voyageService.selectedVoyageOption;
    if (!selected?.routeSegments) return 0;
    const segments = selected.routeSegments;
    const validSegments = segments.filter(segment => segment.trueWeather?.currentSpeed != null);
    if (validSegments.length === 0) return 0;
    const total = validSegments.reduce((sum, segment) => sum + segment.trueWeather!.currentSpeed, 0);
    return Math.round((total / validSegments.length) * 10) / 10;
  }

  public getAverageTemperature(): number {
    const selected = this.voyageService.selectedVoyageOption;
    if (!selected?.routeSegments) return 0;
    const segments = selected.routeSegments;
    const validSegments = segments.filter(segment => 
      segment.trueWeather?.airTemperature != null
    );
    if (validSegments.length === 0) return 0;
    const total = validSegments.reduce((sum, segment) => 
      sum + segment.trueWeather!.airTemperature, 0
    );
    return Math.round((total / validSegments.length) * 10) / 10;
  }

  public canNavigateToPrevious(): boolean {
    return this.getSelectedAvailableVoyageOptionIndex() > 0;
  }

  public canNavigateToNext(): boolean {
    const availableOptions = this.availableVoyageOptions;
    return this.getSelectedAvailableVoyageOptionIndex() < availableOptions.length - 1;
  }

  public getAvailableOptionsCount(): number {
    return this.availableVoyageOptions.length;
  }

  public getCurrentAvailableOptionPosition(): number {
    return this.getSelectedAvailableVoyageOptionIndex() + 1;
  }

  @HostListener('keydown', ['$event'])
  public onKeyDown(event: KeyboardEvent): void {
    if (this.showVoyageOptionsModal) {
      return;
    }

    if (event.key === 'ArrowLeft' && this.canNavigateToPrevious()) {
      event.preventDefault();
      this.navigateToPreviousOption();
    } else if (event.key === 'ArrowRight' && this.canNavigateToNext()) {
      event.preventDefault();
      this.navigateToNextOption();
    } else if (event.key === 'Escape') {
      event.preventDefault();
      this.showVoyageOptionsModal = true;
    }
  }
}