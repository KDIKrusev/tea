import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, of, firstValueFrom } from 'rxjs';
import { map, shareReplay, tap } from 'rxjs/operators';
import { ConfigService } from './config.service';
import { AppInitialData, FullVesselData, VesselCategoryData } from './app-data.types';
import { EngineType, AuxiliaryEngineType } from './engine-configuration.types';

/**
 * Optimized service for managing ALL static application data
 * Loads all data in a SINGLE API call and provides client-side filtering
 * REPLACES: Multiple calls to vessel-configuration.service and engine-configuration.service
 */
@Injectable({
  providedIn: 'root'
})
export class AppDataService {
  private http = inject(HttpClient);
  private configService = inject(ConfigService);

  // Single source of truth for all app data
  private appData$ = new BehaviorSubject<AppInitialData | null>(null);
  private loadingPromise: Promise<AppInitialData> | null = null;

  /**
   * Load ALL application data in a single optimized API call
   * Returns cached data if already loaded
   * REPLACES: /names + /all-configurations + /speeds + /operational-profile calls
   */
  loadInitialData(): Observable<AppInitialData> {
    // Return cached data if available
    const currentData = this.appData$.value;
    if (currentData) {
      return of(currentData);
    }

    // If loading is in progress, wait for it
    if (this.loadingPromise) {
      return new Observable(observer => {
        this.loadingPromise!.then(data => {
          observer.next(data);
          observer.complete();
        }).catch(error => {
          observer.error(error);
        });
      });
    }

    // Start fresh load
    const apiUrl = this.configService.apiUrl;
    
    const loadObservable = this.http.get<AppInitialData>(`${apiUrl}/api/app-data/initial`).pipe(
      tap(data => {
        this.appData$.next(data);
        this.loadingPromise = null;
      }),
      shareReplay(1)
    );

    // Convert to promise for tracking
    this.loadingPromise = firstValueFrom(loadObservable);

    return loadObservable;
  }

  /**
   * Get current app data (synchronous)
   * Returns null if not loaded yet
   */
  getCurrentData(): AppInitialData | null {
    return this.appData$.value;
  }

  /**
   * Get app data as observable (emits when data changes)
   */
  getAppData(): Observable<AppInitialData | null> {
    return this.appData$.asObservable();
  }

  // ==================== VESSEL CATEGORY METHODS ====================

  /**
   * Get vessel categories with their unit and size/speed bounds (Epic 1 parametric selection)
   * CLIENT-SIDE filtering from cached data
   */
  getCategories(): Observable<VesselCategoryData[]> {
    return this.ensureDataLoaded().pipe(
      map(data => data.categories)
    );
  }

  // ==================== ENGINE TYPE METHODS ====================

  /**
   * Get all main engine types
   * CLIENT-SIDE filtering from cached data
   */
  getMainEngineTypes(): Observable<EngineType[]> {
    return this.ensureDataLoaded().pipe(
      map(data => data.engineTypes.mainEngines)
    );
  }

  /**
   * Get all auxiliary engine types
   * CLIENT-SIDE filtering from cached data
   */
  getAuxiliaryEngineTypes(): Observable<AuxiliaryEngineType[]> {
    return this.ensureDataLoaded().pipe(
      map(data => data.engineTypes.auxiliaryEngines)
    );
  }

  // ==================== FULL VESSEL DATA METHOD ====================

  /**
   * Get complete vessel data for a category + size + speed (parametric, Epic 1)
   * Calm water power is interpolated server-side over speed AND size between
   * reference curves. Includes vessel config, operational profile, engine data,
   * and the resolution audit trace.
   * This is the ONLY method that requires a separate API call (user selection dependent)
   */
  getFullVesselDataByCategory(category: string, size: number, speed: number): Observable<FullVesselData> {
    const apiUrl = this.configService.apiUrl;
    return this.http.get<FullVesselData>(`${apiUrl}/api/app-data/vessel-config`, {
      params: {
        category: category,
        size: size.toString(),
        speed: speed.toString()
      }
    });
  }

  // ==================== FUEL PRICING ====================

  /**
   * Get fuel default prices from backend CalculatorSettings
   * Returns a Record<fuel_type, price_usd_per_ton>
   * Used by frontend to dynamically update fuel price when user changes fuel type
   */
  getFuelDefaultPrices(): Record<string, number> {
    const appData = this.appData$.value;
    return appData?.fuelDefaultPrices || {};
  }

  // ==================== HELPER METHODS ====================

  /**
   * Ensure data is loaded before returning
   */
  private ensureDataLoaded(): Observable<AppInitialData> {
    const currentData = this.appData$.value;
    if (currentData) {
      return of(currentData);
    }
    return this.loadInitialData();
  }

}
