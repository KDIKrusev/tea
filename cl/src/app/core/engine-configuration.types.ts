import { FuelFamily } from '../shared/constants/fuel.constants';

// SFOC data point for engine load vs fuel consumption
export interface SfocDataPoint {
  load: number; // Load percentage (0.0 to 1.0)
  sfoc: number; // Specific fuel consumption in g/kWh
}

// Engine type definition from backend configuration with SFOC data.
// NOTE: sfocData is [JsonIgnore] server-side, so it arrives empty/absent from /api/app-data/initial
// (the curve is used server-side only). Do not rely on it from the engine list.
export interface EngineType {
  id: number;
  name: string;
  maxCapacityKW: number;
  shaftGeneratorMaxCapacityKW: number;
  description: string;
  isActive: boolean;
  sfocData: SfocDataPoint[];
  // Catalogue + fuel metadata (Epic 3). Nullable: legacy engines have null maker/series.
  fuelFamily?: FuelFamily;
  maker?: string | null;
  series?: string | null;
  ratedPowerKW?: number | null;
}

// Auxiliary engine type definition (no shaft generator capacity)
export interface AuxiliaryEngineType {
  id: number;
  name: string;
  maxCapacityKW: number;
  description: string;
  isActive: boolean;
  sfocData: SfocDataPoint[];
  // Catalogue + fuel metadata (Epic 3)
  fuelFamily?: FuelFamily;
  maker?: string | null;
  series?: string | null;
  ratedPowerKW?: number | null;
}

// Engine configuration within a vessel type
export interface VesselEngineConfig {
  engineTypeId: number;
  numberOfEngines: number;
  shaftGeneratorMaxCapacityKW: number;
}

// Auxiliary engine configuration within a vessel type
export interface VesselAuxEngineConfig {
  engineTypeId: number;
  numberOfEngines: number;
}

