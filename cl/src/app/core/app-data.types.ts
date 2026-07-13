/**
 * Complete initial application data returned in a single optimized API call
 * Replaces multiple separate calls to vessel types, engines, and operational profiles
 */
export interface AppInitialData {
  categories: VesselCategoryData[];
  engineTypes: EngineTypesData;
  operationalProfiles: VesselOperationalProfile[];
  metadata: AppDataMetadata;
  /** Default fuel prices (USD/ton) per fuel type from backend CalculatorSettings */
  fuelDefaultPrices: Record<string, number>;
}

/**
 * Vessel category with size/speed bounds for parametric selection (Epic 1)
 * Unit (dwt/TEU) is a property of the category, never a user choice
 */
export interface VesselCategoryData {
  name: string;
  unit: string;
  sizeMin: number | null;
  sizeMax: number | null; // null = unbounded
  speedMin: number;
  speedMax: number;
}

/**
 * All engine types (main and auxiliary) in one structure
 */
export interface EngineTypesData {
  mainEngines: EngineType[];
  auxiliaryEngines: AuxiliaryEngineType[];
}

/**
 * Metadata about the loaded data
 */
export interface AppDataMetadata {
  version: string;
  loadedAt: string;
  vesselTypeCount: number;
  mainEngineCount: number;
  auxiliaryEngineCount: number;
  operationalProfileCount: number;
}

/**
 * Complete vessel data including configuration, operational profile, and engine details
 * Optimized response for single API call to get all vessel-related data
 */
export interface FullVesselData {
  vesselConfig: InterpolatedVesselConfig;
  operationalProfile: VesselOperationalProfile; // For backward compatibility
  mainEngineData?: EngineType;
  auxEngineData?: AuxiliaryEngineType;
  resolution?: ResolutionInfo | null;
}

/**
 * Audit trace of a parametric (category + size + speed) resolution:
 * which reference curves bracketed the size and which record supplied the profile
 */
export interface ResolutionInfo {
  lowerRefSize: number | null;
  upperRefSize: number | null;
  t: number | null;
  profileSource: string;
  clamped: boolean;
}

// Import existing types
import { EngineType, AuxiliaryEngineType } from './engine-configuration.types';
import { VesselOperationalProfile } from './operational-profile.types';
import { InterpolatedVesselConfig } from './vessel-configuration.types';
