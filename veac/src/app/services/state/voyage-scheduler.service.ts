import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { v4 as uuidv4 } from 'uuid';

import { VoyageApiService } from '../api/voyage-api.service';
import { UnitsOfMeasurementService } from '../utilities/units-of-measurement.service';

import { voyageEnergyAdvisorResponse } from '../../models/api/voyage-energy-advisor-response.model';
import { VoyageOption } from '../../models/entities/voyage-option.model';
import { RouteSegment } from '../../models/entities/route-segment.model';
import { VoyageOriginalRequest } from '../../models/api/voyage-original-request.model';
import { Vessel } from '../../models/entities/vessel.model';
import { Route } from '../../models/entities/route.model';

import { VoyageRequestValidator } from '../domain/voyage-request-validator';
import { VoyageTimeWindowCalculator } from '../domain/voyage-time-window-calculator';
import { VoyageUtils } from '../domain/voyage-utils';

// Enhanced interfaces for map interaction
export interface SelectedCoordinates {
  lat: number;
  lng: number;
  time?: number; // Optional time parameter for precise positioning
}

export interface SegmentSelection {
  segmentIndex: number;
  coordinates?: SelectedCoordinates;
  timeWithinSegment?: number; // Precise time within the segment
}

export type DisplayFormat = 'energy' | 'fuel' | 'cost' |'co2';

@Injectable({
  providedIn: 'root'
})
export class VoyageService {
  private selectedVesselSubject = new BehaviorSubject<number | null>(null);
  private selectedRouteSubject = new BehaviorSubject<Route | null>(null);
  private resultsAvailableSubject = new BehaviorSubject<boolean>(false);
  private responseReceivedSubject = new BehaviorSubject<voyageEnergyAdvisorResponse | null>(null);
  private optimalVoyageOptionSubject = new BehaviorSubject<VoyageOption | null>(null);
  private optimalLoadingSubject = new BehaviorSubject<boolean>(false);
  private optimalErrorSubject = new BehaviorSubject<string | null>(null);
  private selectedSegmentIndexSubject = new BehaviorSubject<number>(0);
  private showFuelConsumptionSubject = new BehaviorSubject<boolean>(false);
  private currentView: 'planning' | 'live' = 'planning';

  private displayFormatSubject = new BehaviorSubject<DisplayFormat>('energy');
  private fuelPricePerKgSubject = new BehaviorSubject<number>(0);
  private emissionFactorCO2PerKgSubject = new BehaviorSubject<number>(0);

  public displayFormat$ = this.displayFormatSubject.asObservable();
  public fuelPricePerKg$ = this.fuelPricePerKgSubject.asObservable();
  public emissionFactorCO2PerKg$ = this.emissionFactorCO2PerKgSubject.asObservable();

  private selectedCoordinatesSubject = new BehaviorSubject<SelectedCoordinates | null>(null);
  private liveRouteSubject = new BehaviorSubject<Route | null>(null);
  private selectedSegmentSubject = new BehaviorSubject<SegmentSelection | null>(null);
   public showFuelConsumption$ = this.showFuelConsumptionSubject.asObservable();

  // UI State Management - ADD THESE PROPERTIES
  private _showVoyageOptionsModal = true;
  private _selectedVoyageOption?: VoyageOption;
  private _showFuelConsumption = false;

  public selectedVessel$ = this.selectedVesselSubject.asObservable();
  public selectedRoute$ = this.selectedRouteSubject.asObservable();
  public liveRoute$ = this.liveRouteSubject.asObservable();
  public resultsAvailable$ = this.resultsAvailableSubject.asObservable();
  public responseReceived$ = this.responseReceivedSubject.asObservable();
  public optimalVoyageOption$ = this.optimalVoyageOptionSubject.asObservable();
  public optimalLoading$ = this.optimalLoadingSubject.asObservable();
  public optimalError$ = this.optimalErrorSubject.asObservable();
  public selectedSegmentIndex$ = this.selectedSegmentIndexSubject.asObservable();
  
  public selectedCoordinates$: Observable<SelectedCoordinates | null> = this.selectedCoordinatesSubject.asObservable();
  public selectedSegment$: Observable<SegmentSelection | null> = this.selectedSegmentSubject.asObservable();

  private lastRequestCorrelationId: string = '';
  private cancelSubject = new Subject<void>();
  private optimalVoyageRequestedFromSearch = false;

  public errorMessage: string | null = null;
  public isLoading: boolean = false;

  public voyageOriginalRequest: VoyageOriginalRequest | null = null;
  public voyageOriginalRequestCompleted!: VoyageOriginalRequest;

  constructor(
    private voyageApiService: VoyageApiService,
    public unitsOfMeasurementService: UnitsOfMeasurementService
  ) {}

  // UI STATE MANAGEMENT METHODS - ADD THESE
  get showVoyageOptionsModal(): boolean {
    return this._showVoyageOptionsModal;
  }

  set showVoyageOptionsModal(value: boolean) {
    this._showVoyageOptionsModal = value;
  }

  get selectedVoyageOption(): VoyageOption | undefined {
    return this._selectedVoyageOption;
  }

  set selectedVoyageOption(option: VoyageOption | undefined) {
    this._selectedVoyageOption = option;
  }

  get showFuelConsumption(): boolean {
    return this._showFuelConsumption;
  }

   setDisplayFormat(format: DisplayFormat): void {
    this.displayFormatSubject.next(format);
    // Keep backward compatibility
    this._showFuelConsumption = format === 'fuel';
    this.showFuelConsumptionSubject.next(format === 'fuel');
  }

   getDisplayFormat(): DisplayFormat {
    return this.displayFormatSubject.value;
  }

  setFuelPricePerKg(price: number): void {
    this.fuelPricePerKgSubject.next(price);
  }

  getFuelPricePerKg(): number {
    var price = this.fuelPricePerKgSubject.value;
    return this.fuelPricePerKgSubject.value;
  }

  // New methods for emission factor
  setEmissionFactorCO2PerKg(factor: number): void {
    this.emissionFactorCO2PerKgSubject.next(factor);
  }

  getEmissionFactorCO2PerKg(): number {
    return this.emissionFactorCO2PerKgSubject.value;
  }

  // Helper method to calculate cost for a voyage option
  calculateCost(fuelConsumptionKg: number): number {
    return fuelConsumptionKg * this.getFuelPricePerKg();
  }

   setShowFuelConsumption(showFuel: boolean): void {
    this._showFuelConsumption = showFuel;
    this.showFuelConsumptionSubject.next(showFuel);
  }

  resetUIState(): void {
    this._showVoyageOptionsModal = true;
    this._selectedVoyageOption = undefined;
  }

  hasSelectedVoyageOption(): boolean {
    return this._selectedVoyageOption !== undefined;
  }

  selectVoyageOption(option: VoyageOption, hideModal: boolean = false): void {
    this._selectedVoyageOption = option;
    this._showVoyageOptionsModal = !hideModal;
  }

  setSelectedVessel(vesselId: number): void {
    this.selectedVesselSubject.next(vesselId);
  }

  loadVessels(): Promise<Vessel[]> {
    return this.voyageApiService.getVessels();
  }

  setSelectedRoute(route: Route | null): void {
    this.selectedRouteSubject.next(route);
  }

  setLiveRoute(route: Route | null): void {
    this.liveRouteSubject.next(route);
  }

  getPlanningRoute(): Route | null {
    return this.selectedRouteSubject.getValue();
  }

  getLiveRoute(): Route | null {
    return this.liveRouteSubject.getValue();
  }

  getCurrentRoute(): Route | null {
    return this.currentView === 'live' 
      ? this.getLiveRoute() 
      : this.getPlanningRoute();
  }

  setSelectedSegmentIndex(
    index: number, 
    lat?: number, 
    lng?: number, 
    timeWithinSegment?: number
  ): void {
    this.selectedSegmentIndexSubject.next(index);
    
    const coordinates = (lat !== undefined && lng !== undefined) ? {
      lat,
      lng,
      time: timeWithinSegment
    } : undefined;

    const selection: SegmentSelection = {
      segmentIndex: index,
      coordinates,
      timeWithinSegment
    };

    this.selectedSegmentSubject.next(selection);
    
    if (coordinates) {
      this.selectedCoordinatesSubject.next(coordinates);
    }
  }

  setSelectedCoordinates(lat: number, lng: number, time?: number): void {
    const coordinates: SelectedCoordinates = { lat, lng, time };
    this.selectedCoordinatesSubject.next(coordinates);
  }

  getCurrentSegmentSelection(): SegmentSelection | null {
    return this.selectedSegmentSubject.value;
  }

  getCurrentCoordinates(): SelectedCoordinates | null {
    return this.selectedCoordinatesSubject.value;
  }

  findSegmentIndexByTime(routeSegments: RouteSegment[], time: number): number {
    if (!routeSegments?.length) return 0;

    for (let i = 0; i < routeSegments.length; i++) {
      const segment = routeSegments[i];
      if (time >= segment.startTime && time <= segment.endTime) {
        return i;
      }
    }

    if (time < routeSegments[0].startTime) return 0;
    return routeSegments.length - 1;
  }

  interpolateCoordinatesAtTime(route: Route, routeSegments: RouteSegment[], time: number): SelectedCoordinates | null {
    if (!route?.waypoints || !routeSegments?.length) return null;

    const segmentIndex = this.findSegmentIndexByTime(routeSegments, time);
    const segment = routeSegments[segmentIndex];
    
    if (!segment) return null;

    const startWaypoint = route.waypoints[segmentIndex];
    const endWaypoint = route.waypoints[segmentIndex + 1];
    
    if (!startWaypoint || !endWaypoint) {
      if (segment.startPosition) {
        return {
          lat: segment.startPosition.latitude,
          lng: segment.startPosition.longitude,
          time
        };
      }
      return null;
    }

    const segmentDuration = segment.endTime - segment.startTime;
    const timeIntoSegment = time - segment.startTime;
    const ratio = segmentDuration > 0 ? timeIntoSegment / segmentDuration : 0;

    const lat = startWaypoint.latitude + 
      (endWaypoint.latitude - startWaypoint.latitude) * ratio;
    const lng = startWaypoint.longitude + 
      (endWaypoint.longitude - startWaypoint.longitude) * ratio;

    return { lat, lng, time };
  }

  getSelectedRoute(): Observable<Route | null> {
    return this.selectedRouteSubject.asObservable();
  }

  loadRoutes(): Promise<string[]> {
    return this.voyageApiService.getRoutes();
  }

  loadRouteDetails(routeName: string): Promise<Route> {
    return this.voyageApiService.getRouteDetails(routeName);
  }

  notifyResultsAvailable(available: boolean): void {
    this.resultsAvailableSubject.next(available);
  }

  getResultsAvailable(): Observable<boolean> {
    return this.resultsAvailableSubject.asObservable();
  }

  // UPDATED clearResults method
  clearResults(): void {
    this.voyageOriginalRequest = null;
    this.responseReceivedSubject.next(null);
    this.clearOptimalVoyageOption();
    this.resultsAvailableSubject.next(false);
    // Reset UI state when clearing results
    this.resetUIState();
  }

  clearOptimalVoyageOption(): void {
    this.optimalVoyageRequestedFromSearch = false;
    this.optimalVoyageOptionSubject.next(null);
    this.optimalLoadingSubject.next(false);
    this.optimalErrorSubject.next(null);
  }

  wasOptimalVoyageRequestedFromSearch(): boolean {
    return this.optimalVoyageRequestedFromSearch;
  }

  public showGenericLoadError(customMessage?: string): void {
    const message = customMessage || 'Unable to load data from the server. Please try again later.';
    this.setError(message);
  }

  async sendVoyageCalculationRequest(
    etdMin: number,
    etdMax: number,
    etaMin: number,
    etaMax: number,
    vesselId: number,
    speedMin: number,
    speedMax: number,
    route: Route
  ): Promise<voyageEnergyAdvisorResponse> {

    if (!VoyageRequestValidator.validate(etdMin, etdMax, etaMin, etaMax, speedMin, speedMax, route)) {
      this.isLoading = false;
      return VoyageUtils.emptyResponse(this.getNewCorrelationId());
    }

    this.isLoading = true;
    const correlationId = this.getNewCorrelationId();

    const requestBody = VoyageUtils.buildRequestBody(
      etdMin, etdMax, etaMin, etaMax, speedMin, speedMax, route, correlationId,
      VoyageUtils.processOutgoingTimestamp,
      (speed) => VoyageUtils.processOutgoingSpeed(speed, this.unitsOfMeasurementService)
    );

    try {
      const result = await this.voyageApiService.sendCalculationRequest(requestBody, this.cancelSubject);
      if (!result) throw new Error("Received undefined response from the server");

      const transformed = VoyageUtils.transformResponse(result);
      this.receiveResponse(transformed);
      return transformed;

    } catch (error: any) {
      if (error.message?.includes('cancelled')) {
        return VoyageUtils.emptyResponse(correlationId);
      }

      if (error.error?.isUserFacing) {
        this.errorMessage = error.error.message;
      }

      throw error;

    } finally {
      this.isLoading = false;
    }
  }

  async getOptimalVoyageOption(
    etd: number,
    eta: number,
    speedMin: number,
    speedMax: number,
    route: Route
  ): Promise<VoyageOption> {
    console.log('CLIENT_OPTIMAL_CALL', {
      etd,
      eta,
      speedMin,
      speedMax,
      routeName: route?.routeName,
      waypointCount: route?.waypoints?.length ?? 0
    });
    const response = await this.voyageApiService.getOptimalVoyage({
      etd,
      eta,
      speedMin: VoyageUtils.processOutgoingSpeed(speedMin, this.unitsOfMeasurementService),
      speedMax: VoyageUtils.processOutgoingSpeed(speedMax, this.unitsOfMeasurementService),
      route
    });
    console.log('CLIENT_OPTIMAL_RAW_RESPONSE', response?.optimalVoyageOption?.routeSegments?.map((segment, index) => ({
      index,
      averageSpeed: segment.averageSpeed,
      totalPower: segment.avgTotalPower,
      calmWaterPower: segment.avgCalmWaterPower
    })));

    const option = response.optimalVoyageOption;
    option.isVariableSpeedOption = true;
    option.averageSpeed = VoyageUtils.processIncomingSpeed(option.averageSpeed, this.unitsOfMeasurementService);
    option.routeSegments = option.routeSegments.map(segment => ({
      ...segment,
      averageSpeed: VoyageUtils.processIncomingSpeed(segment.averageSpeed, this.unitsOfMeasurementService)
    }));

    return option;
  }

  async startOptimalVoyageCalculation(
    etd: number,
    eta: number,
    speedMin: number,
    speedMax: number,
    route: Route
  ): Promise<VoyageOption | null> {
    this.optimalLoadingSubject.next(true);
    this.optimalErrorSubject.next(null);
    this.optimalVoyageOptionSubject.next(null);

    try {
      const option = await this.getOptimalVoyageOption(etd, eta, speedMin, speedMax, route);
      this.optimalVoyageOptionSubject.next(option);
      return option;
    } catch (error) {
      console.error('Optimal voyage request failed', error);
      this.optimalErrorSubject.next('Unable to calculate the variable-speed option.');
      return null;
    } finally {
      this.optimalLoadingSubject.next(false);
    }
  }

  startOptimalVoyageCalculationFromSearch(requestData: any): boolean {
    const route = this.getPlanningRoute();
    let etd = requestData?.etd?.timestamp;
    let eta = requestData?.eta?.timestamp;
    const speedMin = requestData?.speed?.min;
    const speedMax = requestData?.speed?.max;

    if (!route || speedMin == null || speedMax == null) {
      this.clearOptimalVoyageOption();
      return false;
    }

    if (etd == null || eta == null) {
      const voyageDistance = this.calculateRouteDistanceMeters(route);
      const targetSpeed = (speedMin + speedMax) / 2;
      const targetSpeedMetersPerSecond = VoyageUtils.processOutgoingSpeed(targetSpeed, this.unitsOfMeasurementService);
      const durationMilliseconds = targetSpeedMetersPerSecond > 0
        ? (voyageDistance / targetSpeedMetersPerSecond) * 1000
        : 0;

      if (etd != null && eta == null && durationMilliseconds > 0) {
        eta = Math.round(etd + durationMilliseconds);
      } else if (eta != null && etd == null && durationMilliseconds > 0) {
        etd = Math.round(eta - durationMilliseconds);
      }
    }

    if (etd == null || eta == null) {
      this.clearOptimalVoyageOption();
      console.log('VARIABLE_SPEED_SEARCH_SKIPPED', {
        hasRoute: !!route,
        etd,
        eta,
        speedMin,
        speedMax
      });
      return false;
    }

    console.log('VARIABLE_SPEED_SEARCH_START', {
      etd,
      eta,
      speedMin,
      speedMax,
      routeName: route.routeName,
      waypointCount: route.waypoints?.length ?? 0
    });
    this.optimalVoyageRequestedFromSearch = true;
    void this.startOptimalVoyageCalculation(etd, eta, speedMin, speedMax, route);
    return true;
  }

  private calculateRouteDistanceMeters(route: Route): number {
    if (!route?.waypoints || route.waypoints.length < 2) {
      return 0;
    }

    let distance = 0;
    for (let i = 1; i < route.waypoints.length; i++) {
      distance += this.calculateDistanceMeters(route.waypoints[i - 1], route.waypoints[i]);
    }

    return distance;
  }

  private calculateDistanceMeters(
    start: { latitude: number; longitude: number },
    end: { latitude: number; longitude: number }
  ): number {
    const earthRadiusMeters = 6371000;
    const lat1 = this.toRadians(start.latitude);
    const lat2 = this.toRadians(end.latitude);
    const deltaLat = this.toRadians(end.latitude - start.latitude);
    const deltaLon = this.toRadians(end.longitude - start.longitude);

    const a = Math.sin(deltaLat / 2) * Math.sin(deltaLat / 2) +
      Math.cos(lat1) * Math.cos(lat2) *
      Math.sin(deltaLon / 2) * Math.sin(deltaLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

    return earthRadiusMeters * c;
  }

  private toRadians(value: number): number {
    return value * Math.PI / 180;
  }

  private receiveResponse(message: voyageEnergyAdvisorResponse): void {

    if (message.fuelPricePerKg !== undefined) {
      this.setFuelPricePerKg(message.fuelPricePerKg);
    }
    if (message.emissionFactorCO2PerKg !== undefined) {
      this.setEmissionFactorCO2PerKg(message.emissionFactorCO2PerKg);
    }

    message.voyageOptions.forEach((option: VoyageOption) => {
      option.eta = VoyageUtils.processIncomingTimestamp(option.eta);
      option.etd = VoyageUtils.processIncomingTimestamp(option.etd);
      option.averageSpeed = VoyageUtils.processIncomingSpeed(option.averageSpeed, this.unitsOfMeasurementService);

      option.routeSegments.forEach((segment: RouteSegment) => {
        segment.startTime = VoyageUtils.processIncomingTimestamp(segment.startTime);
        segment.endTime = VoyageUtils.processIncomingTimestamp(segment.endTime);
        segment.averageSpeed = VoyageUtils.processIncomingSpeed(segment.averageSpeed, this.unitsOfMeasurementService);

        if (!segment.trueWeather) {
          segment.trueWeather = {
            windSpeed: 0,
            windDirection: 0,
            waveHeight: 0,
            wavePeakPeriod: 0,
            waveDirection: 0,
            currentSpeed: 0,
            currentDirection: 0,
            airTemperature: 0,
            airPressure: 0,
            relativeHumidity: 0,
            cloudCoverage: 0,
            favorableWeatherIndex: 0,
            avgNetWeatherResistancePower: 0,
            avgTotalResistanceFuelConsumption:0 
          };
        } else {
          segment.trueWeather.currentSpeed = segment.trueWeather.currentSpeed;
          segment.trueWeather.windSpeed = segment.trueWeather.windSpeed;
        }
      });
    });

    this.voyageOriginalRequestCompleted = Object.assign({}, this.voyageOriginalRequest);
    this.responseReceivedSubject.next(message);
    this.resultsAvailableSubject.next(true);
  }

  cancelRequest(): void {
    this.cancelSubject.next();
    this.lastRequestCorrelationId = this.getNewCorrelationId();
    this.isLoading = false;
    this.errorMessage = '';
    this.resultsAvailableSubject.next(false);
    this.responseReceivedSubject.next(null);
    this.voyageOriginalRequest = null;
  }

  getOriginalRequestData(): VoyageOriginalRequest {
    return this.voyageOriginalRequestCompleted;
  }

  async submitVoyageRequest(requestData: any): Promise<any> {
    this.setLoading(true);
    this.errorMessage = null;
    this.voyageOriginalRequest = requestData;

    const route = this.getPlanningRoute();
    if (!route) {
      this.setLoading(false);
      return Promise.reject(new Error('No route selected'));
    }

    const vesselId = this.selectedVesselSubject.getValue() || 0;

    try {
      const {
        min: etdMin, max: etdMax
      } = requestData.timeWindowMode === 'etd'
        ? VoyageTimeWindowCalculator.getTimestampRangeFromForm(requestData.etd)
        : { min: -1, max: -1 };

      const {
        min: etaMin, max: etaMax
      } = requestData.timeWindowMode === 'eta'
        ? VoyageTimeWindowCalculator.getTimestampRangeFromForm(requestData.eta)
        : { min: -1, max: -1 };

      const speedMin = requestData.speed.min;
      const speedMax = requestData.speed.max;

      return this.sendVoyageCalculationRequest(
        etdMin, etdMax, etaMin, etaMax, vesselId, speedMin, speedMax, route
      );

    } catch (error) {
      this.setLoading(false);
      return Promise.reject(error);
    }
  }

  setCurrentView(view: 'planning' | 'live'): void {
    this.currentView = view;
  }

  private getNewCorrelationId(): string {
    this.lastRequestCorrelationId = uuidv4();
    return this.lastRequestCorrelationId;
  }

  public setLoading(loading: boolean): void {
    this.isLoading = loading;
  }

  public setError(message: string | null): void {
    this.errorMessage = message;
  }
}