import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgxSliderModule } from '@angular-slider/ngx-slider';
import { Subscription } from 'rxjs';

// Import components and services
import { VesselSelectorComponent } from '../input-panel/select-input/vessel-selector/vessel-selector.component';
import { RouteSelectorComponent } from '../input-panel/select-input/route-selector/route-selector.component';
import { DateTimeComponent } from '../../shared/components/date-time/date-time.component';
import { SpeedRangeComponent } from './speed-range/speed-range.component';
import { ResultItemComponent } from './result-buttons/result-item.component';
import { EaPanelComponent } from '../../shared/components/ea-panel/ea-panel.component';
import { VoyageService } from '../../services/state/voyage-scheduler.service';
import { TimeWindowSelectorComponent } from '../input-panel/app-time-window-selector/app-time-window-selector';
import { VoyageMapComponent } from '../../shared/components/map/voyage-map.component';

@Component({
  selector: 'app-voyage-scheduler',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule,
    NgxSliderModule,
    EaPanelComponent,
    VesselSelectorComponent,
    RouteSelectorComponent,
    DateTimeComponent,
    SpeedRangeComponent,
    ResultItemComponent,
    TimeWindowSelectorComponent,
    VoyageMapComponent
  ],
  templateUrl: './voyage-scheduler.component.html',
  styleUrls: ['./voyage-scheduler.component.css']
})
export class VoyageSchedulerComponent implements OnInit, OnDestroy {
  private resultsSubscription!: Subscription;

  // Core component state
  hasResults: boolean = false;
  updating: boolean = false;
  isInputValid: boolean = false;

  // User interaction tracking  
  private hasUserInteracted: boolean = false;
  private showValidationErrors: boolean = true; // Always show validation errors

  // Time window mode
  selectedTimeWindowMode: 'etd' | 'eta' = 'etd';
  
  // Vessel and route
  selectedRouteName: string = '';
  selectedVessel: string = '';  
  selectedVesselId: number | null = null;

  // ETD/ETA data - simplified
  etdDaysTolerance: number = 5;
  etaHoursTolerance: number = 0;
  etdHoursTolerance: number = 1;
  etaDaysTolerance: number = 0;
  etdTimeFormatted: string = 'Any time';
  etaTimeFormatted: string = 'Any time';
  etdDateFormatted: string = 'Select departure date';
  etaDateFormatted: string = 'Select arrival date';

  // Speed range
  speedMin: number = 13;
  speedMax: number = 17;

  public validationErrors: string[] = [];
  public maxDateDifferenceDays = 5;

  constructor(private voyageSchedulerService: VoyageService) {}

  async ngOnInit(): Promise<void> {
    this.voyageSchedulerService.setCurrentView('planning');
    this.resultsSubscription = this.voyageSchedulerService.resultsAvailable$.subscribe(
      hasResults => {
        this.hasResults = hasResults;
      }
    );

    this.isInputValid = false;
    this.validateInputs();
  }

  ngOnDestroy(): void {
    if (this.resultsSubscription) {
      this.resultsSubscription.unsubscribe();
    }
  }

  onTimeWindowModeChanged(mode: 'etd' | 'eta'): void {
    this.selectedTimeWindowMode = mode;
    this.markUserInteraction();
    this.validateInputs();
  }

  onSpeedChanged(event: {min: number, max: number}): void {
    this.speedMin = event.min;
    this.speedMax = event.max;

    this.markUserInteraction();
    this.validateInputs();
  }

  onDateChanged(event: { date: string, tolerance: number, type: 'etd' | 'eta' }): void {
    if (event.type === 'etd') {
      this.etdDateFormatted = event.date;
      this.etdDaysTolerance = event.tolerance;
    } else {
      this.etaDateFormatted = event.date;
      this.etaDaysTolerance = event.tolerance;
    }
    
    this.markUserInteraction();
    this.validateInputs();
  }

  /**
   * Handle time changes from DateTimeComponent
   */
  onTimeChanged(event: { time: string, tolerance: number, type: 'etd' | 'eta' }): void {
    if (event.type === 'etd') {
      this.etdTimeFormatted = event.time;
      this.etdHoursTolerance = event.tolerance;
    } else {
      this.etaTimeFormatted = event.time;
      this.etaHoursTolerance = event.tolerance;
    }
    
    this.markUserInteraction();
    this.validateInputs();
  }

  async clear(): Promise<void> {
    this.selectedRouteName = '';
    this.etdDaysTolerance = 0;
    this.etdHoursTolerance = 0;
    this.etaDaysTolerance = 0;
    this.etaHoursTolerance = 0;
    this.speedMin = 13;
    this.speedMax = 17; 
    this.etdDateFormatted = 'Select departure date';
    this.etaDateFormatted = 'Select arrival date';
    this.etdTimeFormatted = 'Any time';
    this.etaTimeFormatted = 'Any time';
    
    this.hasResults = false;
    this.hasUserInteracted = false;
    this.showValidationErrors = true;
    this.validationErrors = [];

    this.voyageSchedulerService.notifyResultsAvailable(false);
    this.validateInputs();
  }

  search(): void {
    this.markUserInteraction();
    this.showValidationErrors = true;
    this.validateInputs();
    
    if (!this.isInputValid) {
      console.error('Cannot search: input validation failed');
      return;
    }

    const searchParams = this.buildSearchParameters();

    this.hasResults = true;
  this.voyageSchedulerService.clearOptimalVoyageOption();
    console.log('VARIABLE_SPEED_SEARCH_TRIGGER');
    this.voyageSchedulerService.startOptimalVoyageCalculationFromSearch(searchParams);
    
    this.voyageSchedulerService.submitVoyageRequest(searchParams)
      .then(response => {
        this.hasResults = true;
        this.notifyResultsReady();
      })
      .catch(error => {
        console.error('Search error:', error);
        // Just log the error, don't show it to user
      });
  }

  private buildSearchParameters(): any {
    const etdDate = this.parseDateTimeString(this.etdDateFormatted, this.etdTimeFormatted, 'Select departure date');
    const etaDate = this.parseDateTimeString(this.etaDateFormatted, this.etaTimeFormatted, 'Select arrival date');
    
    return {
      vessel: this.selectedVessel,
      vesselId: this.selectedVesselId,
      route: this.selectedRouteName,
      timeWindowMode: this.selectedTimeWindowMode,
      etd: {
        date: this.etdDateFormatted,
        time: this.etdTimeFormatted,
        daysTolerance: this.etdDaysTolerance,
        hoursTolerance: this.etdHoursTolerance,
        timestamp: etdDate ? etdDate.getTime() : null
      },
      eta: {
        date: this.etaDateFormatted,
        time: this.etaTimeFormatted,
        daysTolerance: this.etaDaysTolerance,
        hoursTolerance: this.etaHoursTolerance,
        timestamp: etaDate ? etaDate.getTime() : null
      },
      speed: {
        min: this.speedMin,  
        max: this.speedMax
      }
    };
  }

 private parseDateTimeString(dateStr: string, timeStr: string, defaultDateText: string): Date | null {
  if (!dateStr || dateStr === defaultDateText) return null;
  

  const dateParts = dateStr.match(/(\w+)\s+(\d+),\s+(\d+)/);
  if (!dateParts) return null;
  
  const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 
                      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  const month = monthNames.indexOf(dateParts[1]);
  const day = parseInt(dateParts[2], 10);
  const year = parseInt(dateParts[3], 10);
  
  let hour = 0;
  let minute = 0;
  
  if (timeStr && timeStr !== 'Any time') {
    const [timePart, period] = timeStr.split(' ');
    const [hourStr, minuteStr] = timePart.split(':');
    
    hour = parseInt(hourStr, 10);
    minute = parseInt(minuteStr, 10);
    
    if (period === 'PM' && hour < 12) {
      hour += 12;
    } else if (period === 'AM' && hour === 12) {
      hour = 0;
    }
  }
  
  const date = new Date(Date.UTC(year, month, day, hour, minute, 0, 0));
  
  return date;
}

  private markUserInteraction(): void {
    this.hasUserInteracted = true;
  }

  private notifyResultsReady(): void {
    this.voyageSchedulerService.notifyResultsAvailable(true);
  }

  private validateInputs(): void {

    this.validationErrors = [];
    let hasValidationIssues = false;

    if (this.selectedTimeWindowMode === 'etd') {
      // Check departure date first
      if (this.etdDateFormatted === 'Select departure date') {
        this.validationErrors.push('Please select a departure date');
        hasValidationIssues = true;
      } 
      // Only check time if date is selected
      if (this.etdDateFormatted !== 'Select departure date' && this.etdTimeFormatted === 'Any time') {
        this.validationErrors.push('Please select a departure time');
        hasValidationIssues = true;
      }
    } else {
      // Check arrival date first  
      if (this.etaDateFormatted === 'Select arrival date') {
        this.validationErrors.push('Please select an arrival date');
        hasValidationIssues = true;
      }
      // Only check time if date is selected
      if (this.etaDateFormatted !== 'Select arrival date' && this.etaTimeFormatted === 'Any time') {
        this.validationErrors.push('Please select an arrival time');
        hasValidationIssues = true;
      }
    }
    
    this.isInputValid = !hasValidationIssues;
    
  }

  isUpdating(): boolean {
    return this.voyageSchedulerService.isLoading;
  }

  get shouldShowValidationErrors(): boolean {
    // Always show validation errors for immediate feedback
    return this.validationErrors.length > 0;
  }
}