import { Waypoint } from './waypoint.model';
import { Weather } from './weather.model';

export interface RouteSegment {
    startTime: number;
    endTime: number;
    startPosition: Waypoint;
    endPosition: Waypoint;
    course: number;
    averageSpeed: number;
    durationInSeconds: number;
    trueWeather: Weather;
    
    // Power
    avgTotalPower: number;
    avgCalmWaterPower: number;
    avgWindPower: number;
    avgWavePower: number;
    avgCurrentPower: number;
    avgSailPower: number;
    avgNetWeatherResistancePower: number;
    favorableWeatherIndex: number;
    
    // Fuel Consumption
    avgTotalResistanceFuelConsumption: number;
    avgCalmWaterResistanceFuelConsumption: number;
    avgWindResistanceFuelConsumption: number;
    avgWaveResistanceFuelConsumption: number;
    avgCurrentResistanceFuelConsumption: number;
    avgSailResistanceFuelConsumption: number;
    avgNetWeatherResistanceFuelConsumption: number;
    
    // Cost
    avgTotalResistanceCost?: number;
    avgCalmWaterResistanceCost?: number;
    avgWindResistanceCost?: number;
    avgWaveResistanceCost?: number;
    avgCurrentResistanceCost?: number;
    avgSailResistanceCost?: number;
    avgNetWeatherResistanceCost?: number;
}