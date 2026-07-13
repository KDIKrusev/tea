// Import engine config types
import { VesselEngineConfig, VesselAuxEngineConfig, EngineType, AuxiliaryEngineType } from './engine-configuration.types';
import { VesselOperationalProfile } from './operational-profile.types';

/**
 * Speed and power point in vessel's speed-power curve
 * REFACTORED V2.0: Speed/power now stored as array instead of separate records
 */
export interface SpeedPowerPoint {
  speedKnots: number;
  calmWaterPowerKW: number;
}

/**
 * Vessel type definition from backend configuration with engine references
 * REFACTORED V2.0: Speed/power curve and operational profile embedded
 */
export interface VesselType {
  id: number;
  vesselTypeName: string;
  sizeCategory: string;
  category: string;
  unit: string;
  description: string;
  isActive: boolean;
  
  // Speed/Power curve (replaces single speed + calmWaterPowerKW)
  speedPowerCurve: SpeedPowerPoint[];
  
  seaMarginPercent: number; // Sea margin percentage
  
  // Engine configurations
  mainEngine?: VesselEngineConfig;
  auxEngine?: VesselAuxEngineConfig;
  
  // Embedded operational profile (replaces separate OperationalProfile service)
  operationalProfile: VesselOperationalProfile;
}

/**
 * Interpolated vessel configuration returned by the API for a specific speed
 * This is a flattened view (not the same as VesselType entity)
 */
export interface InterpolatedVesselConfig {
  vesselTypeName: string;
  calmWaterPowerKW: number;
  seaMargin: number;
  [key: string]: unknown; // Allow additional properties from the API
}

// Response model with complete vessel type and engine data
export interface VesselTypeWithEnginesResponse {
  vesselType: VesselType | InterpolatedVesselConfig;
  mainEngineData?: EngineType;
  auxEngineData?: AuxiliaryEngineType;
}
