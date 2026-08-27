import { Injectable } from '@angular/core';
import { Map, View } from 'ol';
import TileLayer from 'ol/layer/Tile';
import OSM from 'ol/source/OSM';
import VectorLayer from 'ol/layer/Vector';
import VectorSource from 'ol/source/Vector';
import { LineString, Point } from 'ol/geom';
import { Style, Stroke, Circle as CircleStyle, Fill, Text } from 'ol/style';
import { Feature } from 'ol';
import { fromLonLat } from 'ol/proj';
import { FeatureLike } from 'ol/Feature';
import { Route } from '../../../models/entities/route.model';
import { RouteSegment } from '../../../models/entities/route-segment.model';
import { MapInteractionHelper } from './helpers/map-interaction.helper';
import { RouteCalculationHelper } from './helpers/route-calculation.helper';
import { OverlayRendererHelper } from './helpers/overlay-renderer.helper';
import { MAP_CONFIG, OverlayType, StyleConfig } from './voyage-map-service-type';

@Injectable({
  providedIn: 'root'
})
export class VoyageMapService {
  // ==========================================
  // STATE
  // ==========================================
  private routeSegments: RouteSegment[] = [];
  private activeOverlay: OverlayType | 'none' = 'none';
  private showLabels = false;
  private mapInstance: any = null;
  private currentZoom: number = MAP_CONFIG.DEFAULT_ZOOM;
  private isLiveMode = false;
  private vesselHeading: number | undefined;

  // ==========================================
  // CONFIGURATION
  // ==========================================
  private readonly styleConfigs: Record<OverlayType, StyleConfig> = {
    vessel: { color: '#FF6B35', size: 24, icon: '⬆', label: 'Vessel Courses' },
    wind: { color: '#2196F3', size: 28, icon: '⬆', label: 'Wind Vectors' },
    current: { color: '#9C27B0', size: 28, icon: '⬆', label: 'Ocean Currents' },
    weather: { color: '#607D8B', size: 24, icon: '●', label: 'Weather Conditions' },
    waves: { color: '#4CAF50', size: 28, icon: '⬆', label: 'Wave Vectors' }
  };

  private readonly COLOR_SCALES = {
    wind: ['#E3F2FD', '#E3F2FD', '#90CAF9', '#42A5F5', '#2196F3', '#1976D2', 
           '#FF9800', '#FF5722', '#F44336', '#E91E63', '#9C27B0', '#4A148C', '#311B92'],
    current: ['#F3E5F5', '#CE93D8', '#AB47BC', '#8E24AA', '#6A1B9A', '#4A148C'],
    waves: ['#E8F5E8', '#C8E6C9', '#81C784', '#4CAF50', '#2E7D32', '#FF9800', 
            '#FF5722', '#F44336', '#E91E63', '#9C27B0'],
    weather: {
      green: ['#9ACD32', '#32CD32', '#00FF32', '#00FF00', '#006400'],
      gray: ['#A0A0A0', '#909090', '#808080', '#707070', '#606060'],
      red: ['#FF6347', '#FF4500', '#DC143C', '#B22222', '#8B0000']
    }
  };

  private readonly THRESHOLDS = {
    wind: [0.5, 1.5, 3.3, 5.5, 7.9, 10.7, 13.8, 17.1, 20.7, 24.4, 28.4, 32.6],
    current: [0.2, 0.5, 1.0, 1.5, 2.0],
    waves: [0.2, 0.5, 1.0, 1.25, 2.0, 2.5, 4.0, 6.0, 9.0],
    weather: { green: 0.33, gray: 0.67 }
  };

  // ==========================================
  // MAP INITIALIZATION
  // ==========================================
  createMapSources() {
    return {
      route: new VectorSource(),
      interactive: new VectorSource(),
      point: new VectorSource(),
      overlay: new VectorSource(),
      vessel: new VectorSource()
    };
  }

  createMapLayers(sources: any, dragFeature: Feature | null) {
    return {
      route: new VectorLayer({
        source: sources.route,
        style: this.createRouteStyle(),
        zIndex: 10
      }),
      interactive: new VectorLayer({
        source: sources.interactive,
        style: new Style({ 
          stroke: new Stroke({ 
            color: 'transparent', 
            width: MAP_CONFIG.INTERACTIVE_STROKE_WIDTH 
          }) 
        }),
        zIndex: 50
      }),
      point: new VectorLayer({
        source: sources.point,
        style: (feature) => this.createPointStyle(feature, dragFeature),
        zIndex: 200
      }),
      overlay: new VectorLayer({
        source: sources.overlay,
        style: this.createOverlayStyle.bind(this),
        zIndex: 100,
        visible: false
      }),
      vessel: new VectorLayer({
        source: sources.vessel,
        style: this.createVesselStyle.bind(this),
        zIndex: 300
      })
    };
  }

  createMap(container: HTMLElement, layers: any, miniMapMode: boolean): Map {
    const map = new Map({
      target: container,
      layers: [
        new TileLayer({
          source: new OSM({
            url: 'https://{a-c}.tile.openstreetmap.org/{z}/{x}/{y}.png',
            attributions: ['© OpenStreetMap contributors']
          })
        }),
        layers.route,
        layers.overlay,
        layers.interactive,
        layers.point,
        layers.vessel 
      ],
      view: new View({
        center: fromLonLat(MAP_CONFIG.INITIAL_COORDS),
        zoom: miniMapMode ? MAP_CONFIG.MINI_MAP_ZOOM : MAP_CONFIG.DEFAULT_ZOOM,
        maxZoom: MAP_CONFIG.MAX_ZOOM,
        minZoom: 1,
        enableRotation: false
      }),
      controls: []
    });

    map.getView().on('change:resolution', () => {
      const currentZoom = map.getView().getZoom() || MAP_CONFIG.DEFAULT_ZOOM;
      this.updateZoom(currentZoom);
    });

    this.currentZoom = map.getView().getZoom() || MAP_CONFIG.DEFAULT_ZOOM;
    return map;
  }

  // ==========================================
  // LIVE MODE & VESSEL POSITION
  // ==========================================
  setLiveMode(isLive: boolean): void {
    this.isLiveMode = isLive;
    if (!isLive && this.mapInstance?.sources?.vessel) {
      this.mapInstance.sources.vessel.clear();
    }
  }

  updateVesselPosition(pointSource: VectorSource, lat: number, lng: number, heading?: number): void {
    this.vesselHeading = heading;
    
    if (this.isLiveMode && this.mapInstance?.sources?.vessel) {
      this.updateVesselPositionOnly(lat, lng, heading);
    } else {
      this.movePointToCoordinates(pointSource, lat, lng);
    }
  }

  updateVesselPositionOnly(lat: number, lng: number, heading?: number): void {
    if (this.isLiveMode && this.mapInstance?.sources?.vessel) {
      this.mapInstance.sources.vessel.clear();
      const vesselFeature = new Feature({
        geometry: new Point(fromLonLat([lng, lat])),
        isVessel: true,
        heading: heading || 0
      });
      this.mapInstance.sources.vessel.addFeature(vesselFeature);
    }
  }

  centerMapOnPosition(map: Map, lat: number, lng: number, zoom?: number): void {
    const view = map.getView();
    const currentZoom = zoom || view.getZoom() || MAP_CONFIG.DEFAULT_ZOOM;
    
    view.animate({
      center: fromLonLat([lng, lat]),
      zoom: Math.max(currentZoom, 8),
      duration: 1000
    });
  }

  // ==========================================
  // ROUTE RENDERING
  // ==========================================
  renderRoute(sources: any, route: Route): any[] {
    if (!route?.waypoints?.length) return [];
    
    this.clearSources(sources, ['route', 'interactive']);
    
    const segments = this.splitRouteAtDateline(route.waypoints);
    return this.renderSegments(segments, sources);
  }

  renderRouteWithWeatherColors(sources: any, route: Route): any[] {
    if (!route?.waypoints?.length) return [];

    const coordinates = route.waypoints.map(w => fromLonLat([w.longitude, w.latitude]));
    this.clearSources(sources, ['route', 'interactive']);

    if (this.routeSegments?.length && this.hasWeatherData()) {
      this.createColoredRoute(sources.route, coordinates);
    } else {
      sources.route.addFeature(new Feature({ geometry: new LineString(coordinates) }));
    }

    sources.interactive.addFeature(new Feature({ geometry: new LineString(coordinates) }));
    return coordinates;
  }

  private splitRouteAtDateline(waypoints: any[]): any[][] {
    const segments: any[][] = [];
    let current: any[] = [];
    
    waypoints.forEach((wp, i) => {
      current.push(fromLonLat([wp.longitude, wp.latitude]));
      
      if (i < waypoints.length - 1 && this.crossesDateline(wp, waypoints[i + 1])) {
        current.push(this.getBridgePoint(wp, waypoints[i + 1], true));
        segments.push(current);
        current = [this.getBridgePoint(wp, waypoints[i + 1], false)];
      }
    });
    
    if (current.length > 0) segments.push(current);
    return segments;
  }

  private crossesDateline(wp1: any, wp2: any): boolean {
    return Math.abs(wp1.longitude - wp2.longitude) > 180;
  }

  private getBridgePoint(wp1: any, wp2: any, isFirst: boolean): any {
    const midLat = (wp1.latitude + wp2.latitude) / 2;
    const lon = (wp1.longitude > 0) === isFirst ? 180 : -180;
    return fromLonLat([lon, midLat]);
  }

  private renderSegments(segments: any[][], sources: any): any[] {
    const allCoords: any[] = [];
    
    segments.forEach(segment => {
      if (segment.length > 1) {
        const feature = new Feature({ geometry: new LineString(segment) });
        sources.route.addFeature(feature);
        sources.interactive.addFeature(feature);
      }
      allCoords.push(...segment);
    });
    
    return allCoords;
  }

  private hasWeatherData(): boolean {
    return this.routeSegments.some(s => s.trueWeather?.favorableWeatherIndex !== undefined);
  }

  private createColoredRoute(routeSource: VectorSource, coordinates: any[]): void {
    for (let i = 0; i < coordinates.length - 1; i++) {
      const segment = this.routeSegments[i];
      const favorability = segment?.trueWeather?.favorableWeatherIndex ?? 0.5;
      
      routeSource.addFeature(new Feature({
        geometry: new LineString([coordinates[i], coordinates[i + 1]]),
        favorabilityIndex: favorability,
        segmentIndex: i,
        segmentData: segment
      }));
    }
  }

  // ==========================================
  // MAP FITTING
  // ==========================================
  fitMapToRoute(map: Map, routeSource: VectorSource): void {
    const features = routeSource.getFeatures();
    if (!features.length) return;
    
    const extent = features.length > 1 
      ? this.calculateDatelineExtent(features)
      : routeSource.getExtent();
    
    if (extent) {
      map.getView().fit(extent, { 
        padding: MAP_CONFIG.MAP_PADDING, 
        maxZoom: MAP_CONFIG.MAX_ZOOM - 2 
      });
    }
  }

  private calculateDatelineExtent(features: Feature[]): number[] | null {
    let bounds = { 
      minX: Infinity, 
      maxX: -Infinity, 
      minY: Infinity, 
      maxY: -Infinity, 
      width: -Infinity 
    };
    
    features.forEach(feature => {
      const coords = (feature.getGeometry() as LineString)?.getCoordinates();
      if (!coords) return;
      
      const segBounds = this.getSegmentBounds(coords.slice(1, -1));
      
      bounds.minY = Math.min(bounds.minY, segBounds.minY);
      bounds.maxY = Math.max(bounds.maxY, segBounds.maxY);
      
      if (segBounds.width > bounds.width) {
        bounds.minX = segBounds.minX;
        bounds.maxX = segBounds.maxX;
        bounds.width = segBounds.width;
      }
    });
    
    return this.isValidExtent(bounds) 
      ? [bounds.minX, bounds.minY, bounds.maxX, bounds.maxY] 
      : null;
  }

  private getSegmentBounds(coords: any[]) {
    const bounds = { 
      minX: Infinity, 
      maxX: -Infinity, 
      minY: Infinity, 
      maxY: -Infinity, 
      width: 0 
    };
    
    coords.forEach(([x, y]) => {
      bounds.minX = Math.min(bounds.minX, x);
      bounds.maxX = Math.max(bounds.maxX, x);
      bounds.minY = Math.min(bounds.minY, y);
      bounds.maxY = Math.max(bounds.maxY, y);
    });
    
    bounds.width = bounds.maxX - bounds.minX;
    return bounds;
  }

  private isValidExtent(bounds: any): boolean {
    return isFinite(bounds.minX) && isFinite(bounds.maxX);
  }

  resetMapView(map: Map): void {
    map.getView().setCenter(fromLonLat(MAP_CONFIG.INITIAL_COORDS));
    map.getView().setZoom(MAP_CONFIG.DEFAULT_ZOOM);
  }

  // ==========================================
  // OVERLAYS
  // ==========================================
  showOverlay(overlayType: OverlayType): void {
    this.activeOverlay = overlayType;
    this.renderCurrentOverlay();
  }

  hideAllOverlays(): void {
    this.activeOverlay = 'none';
    this.clearOverlays();
  }

  setLabelsVisibility(showLabels: boolean): void {
    this.showLabels = showLabels;
    if (this.activeOverlay !== 'none') this.renderCurrentOverlay();
  }

  private renderCurrentOverlay(): void {
    const mapInstance = this.mapInstance;
    if (!mapInstance || !this.routeSegments.length) return;

    this.clearOverlays();
    if (this.activeOverlay === 'none') return;

    try {
      OverlayRendererHelper.renderOverlay(
        this.activeOverlay,
        mapInstance.sources.overlay,
        this.routeSegments,
        this.showLabels
      );
      mapInstance.layers.overlay.setVisible(true);
    } catch (error) {
      console.error('❌ Error rendering overlay:', error);
    }
  }

  private clearOverlays(): void {
    if (this.mapInstance) {
      this.mapInstance.sources.overlay.clear();
      this.mapInstance.layers.overlay.setVisible(false);
    }
  }

  // Convenience methods for overlays
  showVesselCourse(): void { this.showOverlay('vessel'); }
  showWindVectors(): void { this.showOverlay('wind'); }
  showCurrentVectors(): void { this.showOverlay('current'); }
  showWeatherData(): void { this.showOverlay('weather'); }
  showWaveVectors(): void { this.showOverlay('waves'); }

  // ==========================================
  // STYLE CREATION
  // ==========================================
  private createRouteStyle(): (feature: FeatureLike) => Style {
    return (feature: FeatureLike) => {
      const favorability = feature.get('favorabilityIndex');
      
      if (favorability !== undefined) {
        return new Style({
          stroke: new Stroke({
            color: this.getWeatherColor(favorability),
            width: this.getStrokeWidthForFavorability(favorability),
            lineCap: 'round',
            lineJoin: 'round'
          })
        });
      }
      
      return new Style({
        stroke: new Stroke({
          color: MAP_CONFIG.ROUTE_COLOR,
          width: MAP_CONFIG.ROUTE_STROKE_WIDTH,
          lineCap: 'round',
          lineJoin: 'round'
        })
      });
    };
  }

  private createPointStyle(feature: FeatureLike, dragFeature: Feature | null): Style {
    const isDragging = dragFeature === feature;
    const radius = isDragging ? MAP_CONFIG.POINT_RADIUS * 1.3 : MAP_CONFIG.POINT_RADIUS;
    const pointColor = this.isLiveMode ? '#2196F3' : MAP_CONFIG.POINT_COLOR;
    
    return new Style({
      image: new CircleStyle({
        radius,
        fill: new Fill({ color: pointColor }),
        stroke: new Stroke({ 
          color: MAP_CONFIG.POINT_BORDER_COLOR, 
          width: MAP_CONFIG.POINT_BORDER_WIDTH 
        })
      }),
      text: new Text({
        text: '●',
        font: 'bold 16px sans-serif',
        fill: new Fill({ color: '#FFFFFF' }),
        stroke: new Stroke({ color: '#000000', width: 2 })
      })
    });
  }

  private createVesselStyle(feature: FeatureLike): Style {
    const heading = feature.get('heading') || 0;
    const vesselSize = Math.max(16, this.calculateArrowSizeForZoom(this.currentZoom) + 4);
    
    return new Style({
      image: new CircleStyle({
        radius: vesselSize,
        fill: new Fill({ color: '#4CAF50' }),
        stroke: new Stroke({ color: '#FFFFFF', width: 3 })
      }),
      text: new Text({
        text: '⬆',
        font: `bold ${vesselSize}px sans-serif`,
        fill: new Fill({ color: '#FFFFFF' }),
        stroke: new Stroke({ color: '#000000', width: 2 }),
        rotation: (heading * Math.PI) / 180
      })
    });
  }

  private createOverlayStyle(feature: FeatureLike): Style {
    const overlayType = feature.get('overlayType') as OverlayType;
    const visualType = feature.get('visualType') as string;
    const data = feature.get('data') as any;
    
    if (!overlayType || !this.styleConfigs[overlayType]) return new Style();

    const config = this.styleConfigs[overlayType];
    
    if (visualType === 'arrow') {
      return this.createArrowStyle(config, data, overlayType);
    }
    
    return new Style();
  }

  private createArrowStyle(config: StyleConfig, data: any, overlayType: OverlayType): Style {
    const color = this.getColorByIntensity(overlayType, this.getDataValue(data, overlayType));
    const size = this.calculateArrowSizeForZoom(this.currentZoom);
    const rotation = this.getRotation(data, overlayType);
    const symbol = overlayType === 'weather' ? '●' : '⬆';
    
    return new Style({
      image: new CircleStyle({
        radius: size,
        fill: new Fill({ color }),
        stroke: new Stroke({ 
          color: '#424242', 
          width: Math.max(1, Math.round(size * 0.15)) 
        })
      }),
      text: new Text({
        text: symbol,
        font: `bold ${Math.max(12, size - 2)}px sans-serif`,
        fill: new Fill({ color: '#FFFFFF' }),
        stroke: new Stroke({ 
          color: '#000000', 
          width: Math.max(1, Math.round(size * 0.1)) 
        }),
        rotation: overlayType === 'weather' ? 0 : rotation
      })
    });
  }

  private calculateArrowSizeForZoom(zoom: number): number {
    const MIN_SIZE = 5;
    const MAX_SIZE = 18;
    const LOW_ZOOM_THRESHOLD = 6;
    const HIGH_ZOOM_THRESHOLD = 10;
    
    if (zoom <= LOW_ZOOM_THRESHOLD) return MIN_SIZE;
    if (zoom >= HIGH_ZOOM_THRESHOLD) return MAX_SIZE;
    
    const progress = (zoom - LOW_ZOOM_THRESHOLD) / (HIGH_ZOOM_THRESHOLD - LOW_ZOOM_THRESHOLD);
    const curve = Math.pow(progress, 1.2);
    const scaledSize = MIN_SIZE + (MAX_SIZE - MIN_SIZE) * curve + 2;
    
    return Math.round(scaledSize);
  }

  // ==========================================
  // COLOR SYSTEM
  // ==========================================
  private getColorByIntensity(type: OverlayType, value: number): string {
    switch (type) {
      case 'wind':
        return this.getScaleColor(this.COLOR_SCALES.wind, value, this.THRESHOLDS.wind);
      case 'current':
        return this.getScaleColor(this.COLOR_SCALES.current, value, this.THRESHOLDS.current);
      case 'waves':
        return this.getScaleColor(this.COLOR_SCALES.waves, value, this.THRESHOLDS.waves);
      case 'weather':
        return this.getWeatherColor(value);
      default:
        return this.styleConfigs[type]?.color || '#000000';
    }
  }

  private getScaleColor(colors: string[], value: number, thresholds: number[]): string {
    const index = thresholds.findIndex(t => value < t);
    return colors[index === -1 ? colors.length - 1 : index];
  }

  private getWeatherColor(favorability: number): string {
    const clamped = Math.max(0, Math.min(1, favorability));
    const { green, gray, red } = this.COLOR_SCALES.weather;
    
    if (clamped < this.THRESHOLDS.weather.green) {
      return this.interpolateColors(green, (this.THRESHOLDS.weather.green - clamped) / this.THRESHOLDS.weather.green);
    } else if (clamped < this.THRESHOLDS.weather.gray) {
      return this.interpolateColors(gray, (clamped - this.THRESHOLDS.weather.green) / 0.34);
    } else {
      return this.interpolateColors(red, (clamped - this.THRESHOLDS.weather.gray) / 0.33);
    }
  }

  private interpolateColors(colors: string[], intensity: number): string {
    const index = Math.floor(intensity * (colors.length - 1));
    return colors[Math.min(index, colors.length - 1)];
  }

  private getStrokeWidthForFavorability(favorabilityIndex: number): number {
    const baseWidth = MAP_CONFIG.ROUTE_STROKE_WIDTH || 4;
    if (favorabilityIndex <= 0.33) return baseWidth + 2;
    if (favorabilityIndex <= 0.66) return baseWidth + 1;
    return baseWidth;
  }

  // ==========================================
  // DATA EXTRACTION
  // ==========================================
  private getDataValue(data: any, type: OverlayType): number {
    const valueMap = {
      wind: data.speed || 0,
      current: data.speed || 0,
      waves: data.height || 0,
      weather: data.favorableWeatherIndex ?? 0.5,
      vessel: 0
    };
    return valueMap[type];
  }

  private getRotation(data: any, type: OverlayType): number {
  // Backend provides "from" direction (where wind/current/waves come FROM)
  // Add 180° so arrow tip points in the direction the wind/current is going TO
  // Example: wind from south (180°) → arrow points north (0°), showing wind flowing from south
  if (type === 'waves') return ((data.direction || 0) + 180) * Math.PI / 180;
  
  if (data.course !== undefined) return data.course * Math.PI / 180;

  if (data.direction !== undefined) return ((data.direction + 180) * Math.PI) / 180;
  return 0;
}

  // ==========================================
  // ROUTE SEGMENTS
  // ==========================================
  setRouteSegments(segments: RouteSegment[]): void {
    this.routeSegments = segments;
  }

  updateRouteSegmentsData(segments: RouteSegment[]): void {
    this.routeSegments = segments;
  }

  getRouteSegments(): RouteSegment[] {
    return this.routeSegments;
  }

  // ==========================================
  // MAP INSTANCE
  // ==========================================
  setMapInstance(layers: any, sources: any, map?: Map): void {
    this.mapInstance = { layers, sources, map };
    if (map) {
      this.currentZoom = map.getView().getZoom() || MAP_CONFIG.DEFAULT_ZOOM;
    }
  }

  updateZoom(newZoom: number): void {
    if (Math.abs(this.currentZoom - newZoom) > 0.1) {
      this.currentZoom = newZoom;
      this.refreshOverlayStyles();
    }
  }

  private refreshOverlayStyles(): void {
    if (this.mapInstance && this.activeOverlay !== 'none') {
      this.mapInstance.layers.overlay.getSource().changed();
    }
    
    if (this.mapInstance && this.isLiveMode) {
      this.mapInstance.layers.vessel.getSource().changed();
    }
  }

  // ==========================================
  // INTERACTIONS
  // ==========================================
  setupMapInteractions(
    map: Map, 
    pointSource: VectorSource, 
    onMapClick: (c: any) => void, 
    onDragStart: (f: Feature) => void, 
    onDragMove: (c: any) => void, 
    onDragEnd: () => void, 
    isInteractionAllowed: () => boolean
  ): void {
    MapInteractionHelper.setupMapInteractions(
      map, 
      pointSource, 
      onMapClick, 
      onDragStart, 
      onDragMove, 
      onDragEnd, 
      isInteractionAllowed
    );
  }

  findClosestPointOnRoute(targetCoordinate: any, routeCoordinates: any[]) {
    return RouteCalculationHelper.findClosestPointOnRoute(targetCoordinate, routeCoordinates);
  }

  setDragCursor(element: HTMLElement | null): void { 
    MapInteractionHelper.setDragCursor(element); 
  }

  resetCursor(element: HTMLElement | null): void { 
    MapInteractionHelper.resetCursor(element); 
  }

  disableMapPanningDuringDrag(map: Map, disable: boolean): void { 
    MapInteractionHelper.disableMapPanningDuringDrag(map, disable); 
  }

  isInteractionAllowed(selectedRoute: Route | null): boolean {
    return selectedRoute !== null;
  }

  shouldUpdateDrag(lastUpdate: number): boolean {
    return !lastUpdate || Date.now() - lastUpdate > MAP_CONFIG.DRAG_THROTTLE_MS;
  }

  // ==========================================
  // UTILITIES
  // ==========================================
  clearMapSources(sources: any): void {
    Object.values(sources).forEach((source: any) => source.clear());
  }

  clearRouteSources(sources: any): void {
    this.clearSources(sources, ['route', 'interactive', 'overlay']);
  }

  private clearSources(sources: any, sourceNames: string[]): void {
    sourceNames.forEach(name => sources[name]?.clear());
  }

  movePointToCoordinates(pointSource: VectorSource, lat: number, lng: number): void {
    pointSource.clear();
    pointSource.addFeature(new Feature({ 
      geometry: new Point(fromLonLat([lng, lat])) 
    }));
  }

  // Legacy method - kept for backward compatibility
  convertWindSpeedToBeaufort(windSpeedMs: number): number {
    const index = this.THRESHOLDS.wind.findIndex(t => windSpeedMs < t);
    return index === -1 ? 12 : index;
  }
}