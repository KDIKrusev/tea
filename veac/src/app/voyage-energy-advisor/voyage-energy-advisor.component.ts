import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';

import { VoyageSchedulerComponent } from './input-panel/voyage-scheduler.component';
import { VoyageMapComponent } from '../shared/components/map/voyage-map.component';
import { VoyageEnergyAdvisorResultsPanelComponent } from './results-panel/voyage-energy-advisor-results-panel.component';
import { VoyageEnergyAdvisorLoadingOverlayComponent } from './loading-overlay/voyage-energy-advisor-loading-overlay.component';
import { VoyageService } from '../services/state/voyage-scheduler.service';


@Component({
  selector: 'app-voyage-energy-advisor',
  standalone: true,
  imports: [
    CommonModule,
    VoyageSchedulerComponent,
    VoyageMapComponent,
    VoyageEnergyAdvisorResultsPanelComponent,
    VoyageEnergyAdvisorLoadingOverlayComponent
  ],
  templateUrl: './voyage-energy-advisor.component.html',
  styleUrls: ['./voyage-energy-advisor.component.css']
})
export class VoyageEnergyAdvisorComponent implements OnInit, OnDestroy {
  showResults = false;
  
  private resultsSubscription?: Subscription;
  
  constructor(
    public voyageService: VoyageService
  ) {}
  
  ngOnInit(): void {
    this.subscribeToResults();
  }
  
  ngOnDestroy(): void {
    this.resultsSubscription?.unsubscribe();
  }
  
  private subscribeToResults(): void {
    this.resultsSubscription = this.voyageService.resultsAvailable$.subscribe(
      available => this.showResults = available
    );
  }
}