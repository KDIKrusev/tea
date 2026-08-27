import { Feature } from 'ol';
import { Point } from 'ol/geom';
import { Coordinate } from 'ol/coordinate';
import VectorSource from 'ol/source/Vector';
import { fromLonLat } from 'ol/proj';
import { RouteSegment } from '../../../../models/entities/route-segment.model';
import { OverlayType, VectorData, MAP_CONFIG } from '../voyage-map-service-type';

export class OverlayRendererHelper {
  
  static renderOverlay(
    overlayType: OverlayType, 
    source: VectorSource, 
    routeSegments: RouteSegment[],
    showLabels: boolean
  ): { renderedCount: number; skippedCount: number } {

    let renderedCount = 0;
    let skippedCount = 0;
    
    routeSegments.forEach((segment, index) => {
      if (!segment.startPosition) {
        console.warn(`⚠️ Segment ${index} missing startPosition`);
        skippedCount++;
        return;
      }
      
      const baseCoord = fromLonLat([segment.startPosition.longitude, segment.startPosition.latitude]);
      const data = this.extractEnhancedDataFromSegment(segment, overlayType);
      
      if (!data) {
        console.warn(`⚠️ Segment ${index} missing data for ${overlayType}`);
        skippedCount++;
        return;
      }
      
      try {
        this.createEnhancedVectorFeatures(source, baseCoord, data, overlayType, index, showLabels, segment);
        renderedCount++;
      } catch (error) {
        console.error(`❌ Error creating features for segment ${index}:`, error);
        skippedCount++;
      }
    });
    return { renderedCount, skippedCount };
  }

  private static createEnhancedVectorFeatures(
    source: VectorSource,
    baseCoord: Coordinate,
    data: VectorData,
    overlayType: OverlayType,
    index: number,
    showLabels: boolean,
    segment: RouteSegment
  ): void {
    
    // Enhanced arrow feature with additional metadata
    const arrowFeature = new Feature({
      geometry: new Point(baseCoord), 
      overlayType,
      visualType: 'arrow',
      data,
      segmentIndex: index,
      segmentData: segment,
      favorableWeatherIndex: segment.favorableWeatherIndex,
      avgNetWeatherResistancePower: segment.avgNetWeatherResistancePower,
      avgTotalResistanceFuelConsumption: segment.avgTotalResistanceFuelConsumption,
      timestamp: segment.startTime || new Date().toISOString()
    });
    
    source.addFeature(arrowFeature);

    // Enhanced label with contextual information
    if (showLabels) {
      const labelFeature = new Feature({
        geometry: new Point(baseCoord),
        overlayType,
        visualType: 'label',
        data,
        segmentIndex: index,
        segmentData: segment,
        favorableWeatherIndex: segment.favorableWeatherIndex
      });
      
      source.addFeature(labelFeature);
    }

    // Add additional context features for weather overlay
    if (overlayType === 'weather' && segment.trueWeather) {
      this.createWeatherContextFeatures(source, baseCoord, segment, index);
    }
  }

  private static createWeatherContextFeatures(
    source: VectorSource,
    baseCoord: Coordinate,
    segment: RouteSegment,
    index: number
  ): void {
    const weather = segment.trueWeather;
    if (!weather) return;

    // Create a subtle background indicator for weather conditions
    const contextFeature = new Feature({
      geometry: new Point(baseCoord),
      overlayType: 'weather',
      visualType: 'context',
      data: { weather },
      segmentIndex: index,
      segmentData: segment,
      favorabilityIndex: weather.favorableWeatherIndex,
      avgNetWeatherResistancePower: weather.avgNetWeatherResistancePower,
      avgTotalResistanceFuelConsumption: weather.avgTotalResistanceFuelConsumption
    });

    source.addFeature(contextFeature);
  }

  private static extractEnhancedDataFromSegment(segment: RouteSegment, overlayType: OverlayType): VectorData | null {

    switch (overlayType) {
      case 'vessel':
        if (segment.course === undefined) {
          console.warn('⚠️ Segment missing course data');
          return null;
        }
        return {
          course: segment.course,
          segmentIndex: 0
        };
      
      case 'wind':
        if (!segment.trueWeather || segment.trueWeather.windDirection === undefined) {
          console.warn('⚠️ Segment missing wind data');
          return null;
        }
        return {
          direction: segment.trueWeather.windDirection,
          speed: segment.trueWeather.windSpeed || 0,
          segmentIndex: 0
        };
      
      case 'current':
        if (!segment.trueWeather || segment.trueWeather.currentDirection === undefined) {
          console.warn('⚠️ Segment missing current data');
          return null;
        }
        return {
          direction: segment.trueWeather.currentDirection,
          speed: segment.trueWeather.currentSpeed || 0,
          segmentIndex: 0
        };

      case 'waves':
        if (!segment.trueWeather || segment.trueWeather.waveHeight === undefined) {
          console.warn('⚠️ Segment missing wave data');
          return null;
        }
        return {
          direction: segment.trueWeather.waveDirection || 0,
          height: segment.trueWeather.waveHeight,
          period: segment.trueWeather.wavePeakPeriod || 0,
          speed: segment.trueWeather.waveHeight * 2, 
          segmentIndex: 0
        };
      
      case 'weather':
        if (!segment.trueWeather) {
          console.warn('⚠️ Segment missing weather data');
          return null;
        }
        return {
          avgNetWeatherResistancePower: segment.avgNetWeatherResistancePower,
          favorableWeatherIndex: segment.favorableWeatherIndex,
          segmentIndex: 0,
          avgTotalResistanceFuelConsumption: segment.avgTotalResistanceFuelConsumption
        };
      
      default:
        console.warn('⚠️ Unknown overlay type:', overlayType);
        return null;
    }
  }

  // === ENHANCED CALCULATION UTILITIES ===

  static calculateBeaufortScale(windSpeedMs: number): number {
    if (windSpeedMs < 0.5) return 0;
    if (windSpeedMs < 1.5) return 1;
    if (windSpeedMs < 3.3) return 2;
    if (windSpeedMs < 5.5) return 3;
    if (windSpeedMs < 7.9) return 4;
    if (windSpeedMs < 10.7) return 5;
    if (windSpeedMs < 13.8) return 6;
    if (windSpeedMs < 17.1) return 7;
    if (windSpeedMs < 20.7) return 8;
    if (windSpeedMs < 24.4) return 9;
    if (windSpeedMs < 28.4) return 10;
    if (windSpeedMs < 32.6) return 11;
    return 12;
  }

  static categorizeCurrentStrength(currentSpeed: number): string {
    if (currentSpeed <= 0.1) return 'negligible';
    if (currentSpeed <= 0.5) return 'weak';
    if (currentSpeed <= 1.0) return 'moderate';
    if (currentSpeed <= 1.5) return 'strong';
    return 'very-strong';
  }

  static calculateSeaState(waveHeight: number): number {
    if (waveHeight < 0.1) return 0; // Calm
    if (waveHeight < 0.5) return 1; // Calm (rippled)
    if (waveHeight < 1.25) return 2; // Smooth
    if (waveHeight < 2.5) return 3; // Slight
    if (waveHeight < 4) return 4; // Moderate
    if (waveHeight < 6) return 5; // Rough
    if (waveHeight < 9) return 6; // Very rough
    if (waveHeight < 14) return 7; // High
    return 8; // Very high
  }

  static calculateWaveSteepness(height: number, period: number): string {
    if (period === 0) return 'unknown';
    const steepness = height / (period * period);
    if (steepness < 0.02) return 'gentle';
    if (steepness < 0.04) return 'moderate';
    return 'steep';
  }

  static identifyRiskFactors(weather: any): string[] {
    const risks: string[] = [];
    
    if (weather.windSpeed > 15) risks.push('high-wind');
    if (weather.airPressure < 1000) risks.push('low-pressure');
    if (weather.relativeHumidity > 90 && weather.cloudCoverage > 85) risks.push('fog-risk');
    if (weather.airTemperature < 0 || weather.airTemperature > 35) risks.push('extreme-temp');
    if (weather.waveHeight > 3) risks.push('rough-seas');
    
    return risks;
  }

  static summarizeConditions(weather: any): string {
    const windSpeed = weather.windSpeed || 0;
    const waveHeight = weather.waveHeight || 0;
    const favorability = weather.weatherFavorabilityIndex || 0.5;
    
    if (favorability > 0.8) return 'excellent';
    if (favorability > 0.6) return 'good';
    if (windSpeed > 20 || waveHeight > 4) return 'challenging';
    if (favorability < 0.3) return 'poor';
    return 'moderate';
  }
}