import { RouteSegment } from './route-segment.model';

export interface VoyageOption {
    etd: number;
    eta: number;
    routeOptionNumber: number;
    isValid: boolean;
    averageSpeed: number;
    durationInSeconds: number;
    
    // Energy
    totalWindEnergyConsumption: number;
    totalWaveEnergyConsumption: number;
    totalSailEnergyConsumption: number;
    totalCurrentEnergyConsumption: number;
    totalEnergyConsumption: number;
    totalCalmWaterResistanceEnergyConsumption: number;
    relativeWindEnergyConsumption: number;
    relativeWaveEnergyConsumption: number;
    relativeCurrentEnergyConsumption: number;
    relativeSailEnergyConsumption: number;
    averagePower: number;
    energyConsumptionRelative: number;
    
    // Fuel
    totalResistanceFuelConsumption: number;
    totalCalmWaterResistanceFuelConsumption: number;
    totalWindFuelConsumption: number;
    totalWaveFuelConsumption: number;
    totalCurrentFuelConsumption: number;
    totalSailFuelConsumption: number;
    relativeWindFuelConsumption: number;
    relativeWaveFuelConsumption: number;
    relativeCurrentFuelConsumption: number;
    relativeSailFuelConsumption: number;
    averageFuelConsumptionRate: number;
    fuelConsumptionRelative: number;
    
    // Cost
    totalResistanceCost?: number;
    totalCalmWaterResistanceCost?: number;
    totalWindCost?: number;
    totalWaveCost?: number;
    totalCurrentCost?: number;
    totalSailCost?: number;
    absTotalWindCost?: number;
    absTotalWaveCost?: number;
    absTotalCurrentCost?: number;
    absTotalSailCost?: number;
    relativeWindCost?: number;
    relativeWaveCost?: number;
    relativeCurrentCost?: number;
    relativeSailCost?: number;
    averageCostRate?: number;
    costRelative?: number;
    isVariableSpeedOption?: boolean;
    
    routeSegments: RouteSegment[];
}