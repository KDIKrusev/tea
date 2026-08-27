import { Component, OnInit, OnDestroy, Output, EventEmitter, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SelectInputComponent } from '../select-input.component';
import { VoyageService } from '../../../../services/state/voyage-scheduler.service';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-route-selector',
  standalone: true,
  imports: [CommonModule, FormsModule, SelectInputComponent],
  templateUrl: './route-selector.component.html',
  styleUrls: ['./route-selector.component.css']
})
export class RouteSelectorComponent implements OnInit, OnDestroy {
  @Output() routeSelected = new EventEmitter<string>();
  @Input() viewContext: 'planning' | 'live' = 'planning'; // Add viewContext input

  routes: string[] = [];
  selectedRoute: string = '';
  loading: boolean = false;
  error: string | null = null;
  private vesselSubscription!: Subscription;
  private currentVesselId: number | null = null;

  constructor(private voyageService: VoyageService) {}

  async ngOnInit() {
    this.vesselSubscription = this.voyageService.selectedVessel$.subscribe(async (vesselId) => {
      if (vesselId && vesselId !== this.currentVesselId) {
        this.currentVesselId = vesselId;
        await this.loadRoutes();
      }
    });
  }

  ngOnDestroy() {
    if (this.vesselSubscription) {
      this.vesselSubscription.unsubscribe();
    }
  }

  async loadRoutes() {
    try {
      this.loading = true;
      this.error = null;

      this.routes = await this.voyageService.loadRoutes();

      if (this.routes.length > 0) {
        // Set initial route based on view context
        this.setInitialRoute();
      }
    } catch (error) {
      this.voyageService.showGenericLoadError();
    } finally {
      this.loading = false;
    }
  }

  private setInitialRoute(): void {
    // Get the appropriate current route based on view context
    const currentRoute = this.viewContext === 'live'
      ? this.voyageService.getLiveRoute()
      : this.voyageService.getPlanningRoute();

    if (currentRoute) {
      // Find the matching route in dropdown options
      const matchingRoute = this.routes.find(route => 
        route.includes(currentRoute.routeName) || 
        currentRoute.routeName.includes(route)
      );
      
      if (matchingRoute) {
        this.selectedRoute = matchingRoute;
        this.routeSelected.emit(matchingRoute);
        return;
      }
    }

    // If live mode and no live route, inherit from planning
    if (this.viewContext === 'live') {
      const planningRoute = this.voyageService.getPlanningRoute();
      if (planningRoute) {
        const matchingRoute = this.routes.find(route => 
          route.includes(planningRoute.routeName) || 
          planningRoute.routeName.includes(route)
        );
        
        if (matchingRoute) {
          this.selectedRoute = matchingRoute;
          // Set the live route to inherit from planning
          this.voyageService.setLiveRoute(planningRoute);
          this.routeSelected.emit(matchingRoute);
          return;
        }
      }
    }

    // Default to first route
    this.selectedRoute = this.routes[0];
    this.onSelectedRouteChanged(this.selectedRoute);
  }

  async onSelectedRouteChanged(route: string): Promise<void> {
    this.selectedRoute = route;

    try {
      if (route) {
        const routeDetails = await this.voyageService.loadRouteDetails(route);
        
        if (this.viewContext === 'live') {
          // Set live route
          this.voyageService.setLiveRoute(routeDetails);
        } else {
          // Set planning route
          this.voyageService.setSelectedRoute(routeDetails);
          
          // INHERITANCE RULE: When planning changes, update live route too
          this.inheritPlanningToLive(routeDetails);
        }
      } else {
        if (this.viewContext === 'live') {
          this.voyageService.setLiveRoute(null);
        } else {
          this.voyageService.setSelectedRoute(null);
          this.voyageService.setLiveRoute(null); // Clear live route too
        }
      }
    } catch (error) {
      if (this.viewContext === 'live') {
        this.voyageService.setLiveRoute(null);
      } else {
        this.voyageService.setSelectedRoute(null);
        this.voyageService.setLiveRoute(null);
      }
    }

    this.routeSelected.emit(route);
  }

  private inheritPlanningToLive(planningRoute: any): void {
    this.voyageService.setLiveRoute(planningRoute);
  }
}