/**
 * Operational mode data (Port, Anchor, Maneuvering, Transit)
 * Matches backend OperationalModeData class (serialized as camelCase)
 */
export interface OperationalModeData {
  hotelLoadPowerKW: number;
  propulsionPowerKW?: number; // Optional - used for maneuvering mode
  annualHours: number;
  percentageOfYear: number;
}

/**
 * DP mode specific data with weather conditions
 * Matches backend DPModeData class (serialized as camelCase)
 */
export interface DPModeData extends OperationalModeData {
  weatherConditions?: DPWeatherCondition[];
  requiredDPPowerKW?: number;
}

export interface DPWeatherCondition {
  condition: 'Calm' | 'Moderate' | 'Rough';
  thrustDemandFactor: number;
  minAverageThrustPowerKW: number;
}

/**
 * Complete operational profile for a vessel type
 * Matches backend VesselOperationalProfile class (serialized as camelCase)
 */
export interface VesselOperationalProfile {
  vesselTypeName: string;
  sizeCategory: string;
  port: OperationalModeData;
  anchor: OperationalModeData;
  maneuvering: OperationalModeData;
  transit: OperationalModeData;
  dP?: DPModeData | null;  // DP serializes to dP (special case for all-caps acronym)
}


