import { Component, OnInit, OnDestroy, ElementRef, NgZone, AfterViewInit, 
  ViewChild, HostListener, Input, ChangeDetectionStrategy, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Map } from 'ol';
import { Coordinate } from 'ol/coordinate';
import { Feature } from 'ol';
import { Point } from 'ol/geom';
import { toLonLat, fromLonLat } from 'ol/proj';
import { takeUntil, Subject, distinctUntilChanged } from 'rxjs';

import { VoyageService } from '../../../services/state/voyage-scheduler.service';
import { Route } from '../../../models/entities/route.model';
import { RouteSegment } from '../../../models/entities/route-segment.model';
import { VoyageMapService } from '../../../services/ui/map/voyage-map.service';
import { MapLayers, MapSources, InteractionState, ClosestPointInfo } from '../../../services/ui/map/voyage-map-service-type';
import { VoyageOption } from '../../../models/entities/voyage-option.model';
import { MapTooltipHelper } from '../../../services/ui/map/helpers/tooltip-handlers/map-tooltip.helper';
import { OverlayType, VectorData } from '../../../services/ui/map/voyage-map-service-type';
import { TooltipData } from '../../../services/ui/map/helpers/tooltip-handlers/map-tooltip.helper';
import { CurrentPosition } from '../../../models/entities/current-vessel-position.model';

@Component({
  selector: 'app-voyage-map',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './voyage-map.component.html',
  styleUrls: ['./voyage-map.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VoyageMapComponent implements OnInit, AfterViewInit, OnDestroy, OnChanges {
  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef;
  @Input() miniMapMode: boolean = false;
  @Input() selectedVoyageOption: VoyageOption | null = null;
  
  @Input() syncWithGlobalState: boolean = true;
  
  // Live mode inputs
  @Input() isLiveMode: boolean = false;
  @Input() currentVesselPosition: CurrentPosition | null = null;
  @Input() autoFollow: boolean = true;
  
  @Output() expandRequested = new EventEmitter<void>();
  
  @Output() currentPositionSelected = new EventEmitter<{lat: number, lng: number, segmentIndex?: number, timeInSegment?: number}>();
  @Output() selectedPositionChanged = new EventEmitter<{lat: number, lng: number, segmentIndex?: number, timeInSegment?: number}>();

  // Core components
  private map!: Map;
  public mapInitialized = false;
  private layers!: MapLayers;
  private sources!: MapSources;

  private selectedRoute: Route | null = null;
  private routeCoordinates: Coordinate[] = [];
  public currentRouteSegments: RouteSegment[] = [];
  private interactionState: InteractionState = {
    isDragging: false,
    dragFeature: null,
    lastDragUpdate: 0
  };

  private readonly destroy$ = new Subject<void>();
  private resizeObserver?: ResizeObserver;
  private lastSelectedPosition: {lat: number, lng: number} | null = null;
  private isUpdatingVesselPosition = false; 

  constructor(
    private voyageSchedulerService: VoyageService,
    private voyageMapService: VoyageMapService,
    private mapTooltipHelper: MapTooltipHelper,
    private zone: NgZone,
    private el: ElementRef
  ) {}

  ngOnInit(): void {
    if (this.syncWithGlobalState) {
      this.setupSubscriptions();
    }
  }

  ngAfterViewInit(): void {
    if (this.mapContainer?.nativeElement) {
      this.zone.runOutsideAngular(() => setTimeout(() => this.initializeMap(), 100));
      this.setupResizeHandling();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedVoyageOption'] && this.selectedVoyageOption) {

      
       this.setupSubscriptions();
      if (this.selectedVoyageOption.routeSegments) {
        this.currentRouteSegments = this.selectedVoyageOption.routeSegments;
        this.voyageMapService.setRouteSegments(this.selectedVoyageOption.routeSegments);
        
        // Create a route from routeSegments for this instance
        this.selectedRoute = this.createRouteFromSegments(this.selectedVoyageOption.routeSegments);
        
        if (this.mapInitialized) {
          if (this.isLiveMode) {
            setTimeout(() => {
              this.redrawSelectedPosition();
            }, 50);
          } else {
            this.updateMapDisplay();
          }
        }
      }
    }

    if (changes['isLiveMode'] && this.mapInitialized) {
      this.voyageMapService.setLiveMode(this.isLiveMode);
      this.updateMapDisplay();
    }

    if (changes['currentVesselPosition'] && this.currentVesselPosition && this.mapInitialized) {
      this.updateVesselPosition(this.currentVesselPosition);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.cleanupResizeHandling();
    this.mapTooltipHelper.destroy();
  }

  // ===== PUBLIC API =====
  public movePointToCoordinates(lat: number, lng: number): void {
    if (this.mapInitialized) {
      this.voyageMapService.movePointToCoordinates(this.sources.point, lat, lng);
      
      const positionInfo = this.getPositionInfoFromCoordinates(lat, lng);
      if (this.isLiveMode) {
        this.currentPositionSelected.emit(positionInfo);
      } else {
        this.selectedPositionChanged.emit(positionInfo);
      }
    }
  }

  public redrawSelectedPosition(): void {
    if (this.lastSelectedPosition && this.mapInitialized) {
      this.voyageMapService.movePointToCoordinates(
        this.sources.point, 
        this.lastSelectedPosition.lat, 
        this.lastSelectedPosition.lng
      );
    } 
  }

  private updateSelectedPosition(lat: number, lng: number): void {
    this.lastSelectedPosition = { lat, lng };
    this.movePointToCoordinates(lat, lng);
  }

  public expandMap(): void {
    this.expandRequested.emit();
  }

  public updateVesselPosition(position: CurrentPosition): void {
    if (!this.mapInitialized) return;

    this.zone.runOutsideAngular(() => {
      if (this.isLiveMode) {
        this.isUpdatingVesselPosition = true;
        
        this.voyageMapService.updateVesselPositionOnly(
          position.latitude, 
          position.longitude,
          position.heading || position.course
        );

        if (this.autoFollow) {
          this.voyageMapService.centerMapOnPosition(this.map, position.latitude, position.longitude);
        }
        const positionInfo = this.getPositionInfoFromCoordinates(position.latitude, position.longitude);
        this.currentPositionSelected.emit(positionInfo);
        
        setTimeout(() => {
          this.isUpdatingVesselPosition = false;
        }, 100);
      }
    });
  }

  public updateMapDisplay(): void {
    if (!this.map || !this.mapInitialized) return;
   
    this.zone.runOutsideAngular(() => {
      if (this.isLiveMode) {
        this.voyageMapService.clearRouteSources(this.sources);
      } else {
        this.voyageMapService.clearMapSources(this.sources);
      }
      
      this.routeCoordinates = [];

      if (this.selectedRoute?.waypoints && this.selectedRoute.waypoints.length > 0) {
        this.routeCoordinates = this.voyageMapService.renderRoute(this.sources, this.selectedRoute);
        
        if (!this.isLiveMode || !this.currentVesselPosition) {
          this.voyageMapService.fitMapToRoute(this.map, this.sources.route);
        }
      } else {
        this.voyageMapService.resetMapView(this.map);
      }
      if (this.isLiveMode && this.currentVesselPosition) {
        this.updateVesselPosition(this.currentVesselPosition);
      }
    });
  }

  // ===== PRIVATE SETUP =====
  private setupSubscriptions(): void {

    if (this.syncWithGlobalState) {
      this.voyageSchedulerService.selectedRoute$
      .pipe(takeUntil(this.destroy$), distinctUntilChanged((a, b) => a?.routeName === b?.routeName))
      .subscribe(route => {
        if (!this.isLiveMode) {
          this.selectedRoute = route;
          if (this.mapInitialized) {
            this.updateMapDisplay();
            if (!this.isInteractionAllowed()) this.sources.point.clear();
          }
        }
      });
    }

    // Route changes for live mode
    this.voyageSchedulerService.liveRoute$
      .pipe(takeUntil(this.destroy$), distinctUntilChanged((a, b) => a?.routeName === b?.routeName))
      .subscribe(route => {
        if (this.isLiveMode) {
          this.selectedRoute = route;
          if (this.mapInitialized) {
            this.updateMapDisplay();
          }
        }
      });

    // Coordinate changes from chart
    this.voyageSchedulerService.selectedCoordinates$
      .pipe(takeUntil(this.destroy$), distinctUntilChanged((a, b) => a?.lat === b?.lat && a?.lng === b?.lng))
      .subscribe(coords => {
        if (coords && this.mapInitialized && this.isInteractionAllowed() && !this.isUpdatingVesselPosition) {
          this.updateSelectedPosition(coords.lat, coords.lng);
        }
      });
  }

  private async initializeMap(): Promise<void> {
    try {
      this.sources = this.voyageMapService.createMapSources();
      this.layers = this.voyageMapService.createMapLayers(this.sources, this.interactionState.dragFeature);
      this.map = this.voyageMapService.createMap(this.mapContainer.nativeElement, this.layers, this.miniMapMode);
      
      this.voyageMapService.setMapInstance(this.layers, this.sources, this.map);
      this.voyageMapService.setLiveMode(this.isLiveMode);
      
      this.setupInteractions();
      this.mapInitialized = true;

      if (this.selectedVoyageOption?.routeSegments) {
        this.currentRouteSegments = this.selectedVoyageOption.routeSegments;
        this.voyageMapService.setRouteSegments(this.selectedVoyageOption.routeSegments);
      }

      this.zone.run(() => this.updateMapDisplay());

    } catch (error) {
      console.error('❌ Error initializing map:', error);
    }
  }

  private setupInteractions(): void {
    this.voyageMapService.setupMapInteractions(
      this.map, this.sources.point,
      (coordinate: Coordinate) => this.handleMapClick(coordinate),
      (feature: Feature) => this.handleDragStart(feature),
      (coordinate: Coordinate) => this.handleDragMove(coordinate),
      () => this.handleDragEnd(),
      () => this.isInteractionAllowed()
    );

    // Tooltip interactions
    this.map.on('pointermove', (event) => {
      const features = this.map.getFeaturesAtPixel(this.map.getEventPixel(event.originalEvent));
      const overlayFeature = features.find(f => 
        ['vessel', 'wind', 'current', 'weather', 'waves'].includes(f.get('overlayType'))
      );
      
      if (overlayFeature) {
        const tooltipData: TooltipData = {
          overlayType: overlayFeature.get('overlayType') as OverlayType,
          data: overlayFeature.get('data') as VectorData,
          coordinate: event.coordinate,
          pixel: this.map.getEventPixel(event.originalEvent),
          segmentIndex: overlayFeature.get('segmentIndex') as number,
          routeSegments: this.currentRouteSegments,
          ...this.getTimeFromSegment(overlayFeature.get('segmentIndex'))
        };
        this.mapTooltipHelper.showTooltip(tooltipData, this.mapContainer.nativeElement);
        this.mapContainer.nativeElement.style.cursor = 'pointer';
      } else {
        this.mapTooltipHelper.hideTooltip();
        this.mapContainer.nativeElement.style.cursor = 'default';
      }
    });

    this.mapContainer.nativeElement.addEventListener('mouseleave', () => {
      this.mapTooltipHelper.hideTooltip();
    });
  }

  // ===== INTERACTION HANDLERS =====
  private handleMapClick(coordinate: Coordinate): void {
    if (!this.routeCoordinates.length) return;

    const closestInfo = this.voyageMapService.findClosestPointOnRoute(coordinate, this.routeCoordinates);
    if (!closestInfo || this.getDistanceKm(coordinate, closestInfo.coordinate) > 50) return;

    if (this.currentRouteSegments?.length) {
      const latLng = toLonLat(coordinate);
      const segmentIndex = this.findClosestSegment(latLng, this.currentRouteSegments);
      const segment = this.currentRouteSegments[segmentIndex];
      
      if (segment?.startPosition) {
        this.updateSelectedPosition(segment.startPosition.latitude, segment.startPosition.longitude);
        this.updateChart({
          coordinate: fromLonLat([segment.startPosition.longitude, segment.startPosition.latitude]),
          segmentIndex,
          timeRatio: 0.5
        });
      }
    } else {
      const [lng, lat] = toLonLat(closestInfo.coordinate);
      this.updateSelectedPosition(lat, lng);
      this.updateChart(closestInfo);
    }
  }

  private handleDragStart(feature: Feature): void {
    this.interactionState.isDragging = true;
    this.interactionState.dragFeature = feature;
    this.voyageMapService.setDragCursor(this.map.getTargetElement());
    this.voyageMapService.disableMapPanningDuringDrag(this.map, true);
  }

  private handleDragMove(coordinate: Coordinate): void {
    if (!this.routeCoordinates.length || !this.interactionState.dragFeature) return;

    const closestInfo = this.voyageMapService.findClosestPointOnRoute(coordinate, this.routeCoordinates);
    if (!closestInfo) return;

    // Update visual position
    (this.interactionState.dragFeature.getGeometry() as Point).setCoordinates(closestInfo.coordinate);
    this.layers.point?.getSource()?.changed();

    if (this.voyageMapService.shouldUpdateDrag(this.interactionState.lastDragUpdate)) {
      this.interactionState.lastDragUpdate = Date.now();
      
      const [lng, lat] = toLonLat(closestInfo.coordinate);
      
      const positionInfo = this.getPositionInfoFromCoordinates(lat, lng);
      if (this.isLiveMode) {
        this.currentPositionSelected.emit(positionInfo);
      } else {
        this.selectedPositionChanged.emit(positionInfo);
      }
      
      if (this.currentRouteSegments?.length) {
        const segmentIndex = this.findClosestSegment([lng, lat], this.currentRouteSegments);
        this.updateChart({ ...closestInfo, segmentIndex, timeRatio: 0.5 });
      } else {
        this.updateChart(closestInfo);
      }
    }
  }

   private getPositionInfoFromCoordinates(lat: number, lng: number): {lat: number, lng: number, segmentIndex?: number, timeInSegment?: number} {
    if (this.currentRouteSegments?.length) {
      const segmentIndex = this.findClosestSegment([lng, lat], this.currentRouteSegments);
      const segment = this.currentRouteSegments[segmentIndex];
      
      if (segment?.startTime && segment?.endTime) {
        const timeInSegment = segment.startTime + ((segment.endTime - segment.startTime) * 0.5);
        return { lat, lng, segmentIndex, timeInSegment };
      }
      
      return { lat, lng, segmentIndex };
    }
    
    return { lat, lng };
  }

  private handleDragEnd(): void {
    this.voyageMapService.disableMapPanningDuringDrag(this.map, false);
    this.interactionState.isDragging = false;
    this.interactionState.dragFeature = null;
    this.interactionState.lastDragUpdate = 0;
    this.voyageMapService.resetCursor(this.map.getTargetElement());
    this.layers.point?.getSource()?.changed();
  }

  // ===== UTILITIES =====
  private createRouteFromSegments(segments: RouteSegment[]): Route | null {
    if (!segments || segments.length === 0) return null;
    
    const waypoints = segments.map(segment => segment.startPosition);
    // Add the last segment's end position
    if (segments.length > 0) {
      waypoints.push(segments[segments.length - 1].endPosition);
    }
    
    return {
      routeName: `Generated Route`,
      waypoints: waypoints,
    } as Route;
  }

  private findClosestSegment(latLng: number[], segments: RouteSegment[]): number {
    let closest = 0, minDist = Infinity;
    segments.forEach((seg, i) => {
      if (seg.startPosition) {
        const dist = Math.sqrt(
          Math.pow(latLng[1] - seg.startPosition.latitude, 2) + 
          Math.pow(latLng[0] - seg.startPosition.longitude, 2)
        );
        if (dist < minDist) { minDist = dist; closest = i; }
      }
    });
    return closest;
  }

  private getDistanceKm(coord1: Coordinate, coord2: Coordinate): number {
    const [lng1, lat1] = toLonLat(coord1);
    const [lng2, lat2] = toLonLat(coord2);
    const R = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLng = (lng2 - lng1) * Math.PI / 180;
    const a = Math.sin(dLat/2) * Math.sin(dLat/2) + Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * Math.sin(dLng/2) * Math.sin(dLng/2);
    return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
  }

  private getTimeFromSegment(segmentIndex: number): { startTime?: Date, endTime?: Date } {
    const segment = this.currentRouteSegments?.[segmentIndex];
    if (!segment) return {};
    
    return {
      startTime: segment.startTime ? new Date(segment.startTime) : undefined,
      endTime: segment.endTime ? new Date(segment.endTime) : 
               (this.currentRouteSegments[segmentIndex + 1]?.startTime ? 
                new Date(this.currentRouteSegments[segmentIndex + 1].startTime) : undefined)
    };
  }

  private updateChart(closestInfo: ClosestPointInfo): void {
    const segments = this.voyageMapService.getRouteSegments();
    if (!segments?.length) {
      const [lng, lat] = toLonLat(closestInfo.coordinate);
      this.voyageSchedulerService.setSelectedSegmentIndex(closestInfo.segmentIndex, lat, lng);
      return;
    }

    const segment = segments[closestInfo.segmentIndex];
    if (segment) {
      const [lng, lat] = toLonLat(closestInfo.coordinate);
      const timeInSegment = segment.startTime + ((segment.endTime - segment.startTime) * closestInfo.timeRatio);
      this.voyageSchedulerService.setSelectedSegmentIndex(closestInfo.segmentIndex, lat, lng, timeInSegment);
    }
  }

  private isInteractionAllowed(): boolean {
    return this.voyageMapService.isInteractionAllowed(this.selectedRoute) || this.isLiveMode;
  }

  private setupResizeHandling(): void {
    window.addEventListener('resize', this.handleResize);
    if (typeof ResizeObserver !== 'undefined') {
      this.resizeObserver = new ResizeObserver(() => {
        if (this.map && this.mapInitialized) this.map.updateSize();
      });
      this.resizeObserver.observe(this.el.nativeElement);
      if (this.mapContainer?.nativeElement) {
        this.resizeObserver.observe(this.mapContainer.nativeElement);
      }
    }
  }

  @HostListener('window:resize')
  private readonly handleResize = (): void => {
    if (this.map && this.mapInitialized) this.map.updateSize();
  };

  private cleanupResizeHandling(): void {
    window.removeEventListener('resize', this.handleResize);
    if (this.resizeObserver) this.resizeObserver.disconnect();
  }
}