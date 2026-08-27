import { Component, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { VoyagePowerChartComponent } from '../voyage-energy-advisor/results-panel/voyage-power-chart/voyage-power-chart.component';
import { SegmentDetailsComponent } from '../voyage-energy-advisor/results-panel/segment-details/segment-details.component';
import { VoyageMapComponent } from '../shared/components/map/voyage-map.component'; 
import { VoyageRouteAnalysisComponent } from '../voyage-energy-advisor/results-panel/voyage-route-analysis/voyage-route-analysis.component';
import { LiveVesselDetailsComponent, LiveTrackingInfo } from '../live-mode/components/live-vessel-details.component'; 
import { RouteSegment } from '../models/entities/route-segment.model'
import { RouteSelectorComponent } from '../voyage-energy-advisor/input-panel/select-input/route-selector/route-selector.component'; 
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { VoyageService } from '../services/state/voyage-scheduler.service';
import { VoyageApiService } from '../services/api/voyage-api.service';
import { Route } from '../models/entities/route.model';
import { Subscription, interval } from 'rxjs';
import { VoyageEnergyAdvisorLiveRequest } from '../models/api/voyage-energy-advisor-live-request.model'
import { VoyageEnergyAdvisorLiveResponse } from '../models/api/voyage-energy-advisor-live-response.model'
import {CurrentPosition} from '../models/entities/current-vessel-position.model'

@Component({
  selector: 'app-live-mode-dashboard', 
  standalone: true,
  templateUrl: './live-mode-dashboard.component.html',
  styleUrls: ['./live-mode-dashboard.component.css'],
  imports: [
    CommonModule,
    FormsModule,
    VoyagePowerChartComponent,
    SegmentDetailsComponent,
    VoyageMapComponent,
    VoyageRouteAnalysisComponent,
    LiveVesselDetailsComponent,
    RouteSelectorComponent
  ],
})
export class LiveModeDashboardComponent implements OnInit, OnDestroy {
  @Output() exitLiveMode2 = new EventEmitter<void>();
  @Output() switchToPlanningMode = new EventEmitter<void>();

  private isComponentActive = false;
  
  selectedRoute: Route | null = null;
  isVesselRunning = true;
  
  autoFollowEnabled = true;

  // Polling configuration
  private readonly POLLING_INTERVAL_MS = 5000; // 5 seconds
  private pollingSubscription: Subscription | null = null;
  private routeSubscription: Subscription | null = null;

  // Live data
  private latestLiveData: VoyageEnergyAdvisorLiveResponse | null = null;
  currentVesselPosition: CurrentPosition | null = null;

  liveVoyageData: any = {};
  liveSegmentData: any = {};

  liveTrackingInfo: LiveTrackingInfo = {
    eta: null,
    remainingTimeInSeconds: null,
    currentSpeed: null,
    progress: 0
  };

  constructor(
    private voyageService: VoyageService,
    private voyageApiService: VoyageApiService
  ) {}

  ngOnInit(): void {
    this.voyageService.setCurrentView('live');
    this.isComponentActive = true; 
    this.subscribeToRouteChanges();
    this.startAutomaticTracking();
  }

  ngOnDestroy(): void {
    this.isComponentActive = false; 
    this.stopAutomaticTracking();
    this.routeSubscription?.unsubscribe();
  }

  toggleAutoFollow(): void {
    this.autoFollowEnabled = !this.autoFollowEnabled;
    console.log(`🎯 Auto-follow ${this.autoFollowEnabled ? 'enabled' : 'disabled'}`);
  }

  get autoFollowStatusText(): string {
    return this.autoFollowEnabled ? 'Following' : 'Manual';
  }

  private subscribeToRouteChanges(): void {
    this.routeSubscription = this.voyageService.liveRoute$.subscribe(async route => {
      const previousRoute = this.selectedRoute;
      this.selectedRoute = route;
      
      if (route && this.isComponentActive) {
        // Only fetch data if component is active AND route changed
        if (!previousRoute || previousRoute.routeName !== route.routeName) {
          await this.fetchLiveData();
          this.restartTrackingWithNewRoute();
        }
      } else {
        this.selectedRoute = null;
      }
    });
  }

  switchToPlanning(): void {
    this.switchToPlanningMode.emit();
  }

  exitLiveMode(): void {
    this.exitLiveMode2.emit();
  }

  private async startAutomaticTracking(): Promise<void> {
    if (!this.isComponentActive) {
      return;
    }

    const currentRoute = this.voyageService.getLiveRoute();
    
    if (!currentRoute) {
      return;
    }
    this.startPolling();
  }

  private stopAutomaticTracking(): void {
    this.stopPolling();
    this.currentVesselPosition = null;
  }

  private restartTrackingWithNewRoute(): void {
    if (!this.isComponentActive) {
      return;
    }
    
    this.stopPolling();
    this.startPolling();
  }

  private startPolling(): void {
    if (!this.isComponentActive) {
      return;
    }

    const currentRoute = this.voyageService.getLiveRoute();
    
    if (!currentRoute) {
      console.warn('⚠️ Cannot start polling: no live route selected');
      return;
    }
    
    this.fetchLiveData();
    
    this.pollingSubscription = interval(this.POLLING_INTERVAL_MS).subscribe(() => {
      if (this.isComponentActive) {
        this.fetchLiveData();
      } else {
        this.stopPolling();
      }
    });
  }

  private stopPolling(): void {
    if (this.pollingSubscription) {
      console.log('🛑 Stopping polling');
      this.pollingSubscription.unsubscribe();
      this.pollingSubscription = null;
    }
  }

  private async fetchLiveData(): Promise<void> {
    if (!this.isComponentActive) {
      return;
    }

    const currentRoute = this.voyageService.getLiveRoute();
    
    if (!currentRoute) {
      console.warn('🚢 LIVE DASHBOARD: No live route available for fetching data');
      return;
    }

    try {
      const request: VoyageEnergyAdvisorLiveRequest = {
        route: {
          routeName: currentRoute.routeName,
          waypoints: currentRoute.waypoints.map(wp => ({
            latitude: wp.latitude,
            longitude: wp.longitude
          }))
        }
      };

      const liveData = await this.voyageApiService.getLiveVoyageData(request);
      
      if (this.isComponentActive) {
        this.processLiveData(liveData);
      }
      
    } catch (error) {
      console.error('❌ Error fetching live data:', error);
    }
  }

  public onSegmentSelected(segment: RouteSegment): void {
    this.liveSegmentData = segment;
  }

  private processLiveData(liveData: VoyageEnergyAdvisorLiveResponse): void {
    this.latestLiveData = liveData;

    if (liveData.currentPosition) {
      this.currentVesselPosition = liveData.currentPosition;
      this.isVesselRunning = this.determineVesselRunningStatus(liveData);
    }

    this.liveTrackingInfo = {
      eta: liveData.eta,
      remainingTimeInSeconds: liveData.remainingTimeInSeconds,
      currentSpeed: liveData.currentSpeed,
      progress: this.calculateProgress(liveData)
    };

    if (liveData.remainingRouteSegments && liveData.remainingRouteSegments.length > 0) {
      this.liveSegmentData = liveData.remainingRouteSegments[0];
    }

    this.updateAdaptedVoyageOption(liveData);
  }

  private determineVesselRunningStatus(liveData: VoyageEnergyAdvisorLiveResponse): boolean {
    if (liveData.currentPosition?.status) {
      const status = liveData.currentPosition.status.toLowerCase();
      return status.includes('under way') || status.includes('running');
    }
    
    return (liveData.currentSpeed || 0) > 0.5;
  }

  private updateAdaptedVoyageOption(liveData: VoyageEnergyAdvisorLiveResponse): void {
    if (!this.liveVoyageData) return;

    this.liveVoyageData = {
      ...this.liveVoyageData,
      eta: liveData.eta,
      currentSpeed: liveData.currentSpeed,
      isLiveMode: true,
      routeSegments: liveData.remainingRouteSegments || this.liveVoyageData.routeSegments,
      ...(liveData.remainingRouteSegments && liveData.remainingRouteSegments.length > 0 ? {
        averagePower: liveData.remainingRouteSegments.reduce((sum, seg) => sum + (seg.avgTotalPower || 0), 0) / liveData.remainingRouteSegments.length,
        durationInSeconds: liveData.remainingRouteSegments.reduce((sum, seg) => sum + seg.durationInSeconds, 0)
      } : {})
    };
  }

  private calculateProgress(liveData: VoyageEnergyAdvisorLiveResponse): number {
    if (!this.selectedRoute || !this.selectedRoute.waypoints) return 0;
    
    const totalSegments = this.selectedRoute.waypoints.length - 1;
    const remainingSegments = liveData.remainingRouteSegments.length;
    const completedSegments = Math.max(0, totalSegments - remainingSegments);
    
    return totalSegments > 0 ? (completedSegments / totalSegments) * 100 : 0;
  }

  getCurrentLiveData(): VoyageEnergyAdvisorLiveResponse | null {
    return this.latestLiveData;
  }

  hasRecentLiveData(): boolean {
    return this.latestLiveData !== null;
  }

  // Add the missing methods
  onMapCloseRequested(): void {
    // Handle map close request if needed
    console.log('Map close requested');
  }

  getSelectedVoyageOptionIndex(): number {
    return 0;
  }

  get durationText(): string {
    if (this.liveTrackingInfo.remainingTimeInSeconds) {
      const seconds = this.liveTrackingInfo.remainingTimeInSeconds;
      const hours = Math.floor(seconds / 3600);
      const minutes = Math.floor((seconds % 3600) / 60);
      
      if (hours > 0) {
        return `${hours}h ${minutes}m`;
      } else if (minutes > 0) {
        return `${minutes}m`;
      } else {
        return `${Math.floor(seconds)}s`;
      }
    }
    return 'Live tracking';
  }
}