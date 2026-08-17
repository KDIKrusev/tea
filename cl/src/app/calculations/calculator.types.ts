import { FuelType } from '../shared/constants/fuel.constants';

export interface CalculatorInput {
  // Power Requirements (Actual Demand)
  propulsionPower: number; // kW - average propulsion power needed
  hotelLoad: number; // kW - average hotel/mission loads needed
  seaMargin: number; // % - safety margin for propulsion
  
  // Main Engine Configuration
  meCapacityPerEngine: number; // kW - capacity of ONE main engine
  meCount: number; // number of main engines installed
  
  // Shaft Generator Configuration
  sgCapacityPerEngine: number; // kW - max capacity of shaft generator per main engine
  
  // Auxiliary Engine Configuration
  aeCapacityPerEngine: number; // kW - capacity of ONE aux engine
  aeCount: number; // number of aux engines installed
  
  // Engine Type IDs (for SFOC lookup)
  mainEngineTypeId: number; // ID of selected main engine type
  auxEngineTypeId: number; // ID of selected auxiliary engine type
  
  // Additional Systems
  sailInstalled: boolean; // SAIL system installed
  /** DEPRECATED legacy stub (never used by calculations). Use `battery` instead. */
  batteryCapacity: number; // kWh - battery capacity (0 if none)

  /** Battery configuration (Increment B contract). Null/absent ⇒ battery logic bypassed. */
  battery?: BatteryConfigurationInput | null;

  /**
   * PTI (shaft motor) capacity per main engine [kW]. 0/absent = PTI not modelled
   * (bus-level battery simplification). Physically usually equals the SG rating.
   */
  maxPtiPerEngineKw?: number;

  /** DP class redundancy requirement [kW] — pure RESERVE, battery covers 1:1 (Excel O2). */
  dpRedundancyRequirementKw?: number;

  /** Mission heavy-consumer max [kW] — the Mission row's full variation (Excel I3). */
  missionHeavyConsumerMaxKw?: number;
  /** Battery demand of the remaining (non-DP) relevant modes [kW] — Transit/Port (Epic E2). */
  othersConsumerMaxKw?: number;
  
  // Financial Parameters
  fuelPrice: number; // USD/ton

  // Fuel types (Epic 3) — per-engine; optional (null => backend uses single Co2Factor)
  mainFuelType?: FuelType;
  auxFuelType?: FuelType;

  // === MULTI-MODAL OPERATIONAL FIELDS ===

  // Transit Mode
  transitHours?: number;
  transitHotelPowerKW?: number;
  
  // DP Mode (optional)
  dpEnabled?: boolean;
  dpHours?: number;
  dpHotelPowerKW?: number;
  requiredDPPowerKW?: number;
  dpWeatherCondition?: 'Calm' | 'Moderate' | 'Rough';

  // Port Mode (no propulsion)
  portHotelPowerKW?: number;
  portHours?: number;

  // Anchor Mode (no propulsion)
  anchorHotelPowerKW?: number;
  anchorHours?: number;

  // Maneuvering Mode (propulsion + hotel)
  maneuveringPropulsionPowerKW?: number;
  maneuveringHotelPowerKW?: number;
  maneuveringHours?: number;

  // Vessel Type (for L3 DRC variation lookup)
  vesselTypeName?: string;

  // Level 3 DRC: optional user-specified variation in kW for Hotel/Mission load
  // Overrides vessel-type-based lookup when set (spec values: Bulk ±250, Container ±1500, LNG ±1000)
  hotelLoadVariationKw?: number;

  // Sail & Wind Input
  sailEnabled?: boolean;
  vesselSpeedKnots: number;
  trueWindSpeed?: number;
  windAngleRelVessel?: number;

  // Baseline selection
  baselineIndex?: number;
}

/** Modes the battery may apply to (sketch: Transit / DP / Port). Serialized as strings. */
export type BatteryRelevantMode = 'Transit' | 'DP' | 'Port';

/** Battery configuration sent to the backend. */
export interface BatteryConfigurationInput {
  capacityKwh: number; // kWh — energy content
  powerKw: number;     // kW — max charge/discharge power (allocation budget)
  relevantModes: BatteryRelevantMode[];
}

/** One row of the battery allocation cascade (Excel "Load Demands" port). */
export interface BatteryLoadAllocation {
  load: string;                 // "DpReserve" | "DpDemand" | "Mission" | "Propulsion" | "Hotel"
  function: string;             // "Reserve" | "PeakShaving"
  coverageFactor: number;
  averageLoadKw: number;
  variationKw: number;          // H
  batteryUsedKw: number;        // I
  coveredBandKw: number;        // J
  uncoveredReserveKw: number;   // L
}

/** Battery allocation for one operational mode. */
export interface BatteryModeAllocation {
  mode: string;
  loads: BatteryLoadAllocation[];
  peakShavingBandKw: number;
  additionalSpinningReserveKw: number;
  committedBatteryKw: number;
  remainingBatteryKw: number;
}

/** Battery contribution returned by the backend (null when battery inactive). */
export interface BatteryDetails {
  capacityKwh: number;
  powerKw: number;
  spinningReserveKw: number;    // ΣL — computed "Spinning Reserve" function value
  peakShavingKw: number;        // ΣJ — computed "Peak Shaving" function value
  benefitFocTonPerYear: number; // dual-scenario benefit vs no-battery reference
  benefitCostPerYear: number;
  modeAllocations: BatteryModeAllocation[];
}

/**
 * Sail contribution calculation details returned from backend
 */
export interface SailContributionResult {
  trueWindSpeed: number;            // m/s - user input
  windAngleRelVessel: number;       // degrees - user input
  apparentWindSpeed: number;        // m/s - calculated
  apparentWindAngle: number;        // degrees - calculated
  sailForceKn: number;              // kN - from lookup table
  sailPowerKw: number;              // kW - Force × VesselSpeed
  transitPropulsionBeforeKw: number; // kW - before sail
  transitPropulsionAfterKw: number;  // kW - after sail
  sailSavingsPercent: number;        // % of propulsion saved
}

/**
 * Power demands breakdown
 */
export interface PowerDemands {
  totalDemand: number;
  meInstalled: number;
  aeInstalled: number;
  mainEnginePowerKw: number;
  auxiliaryEnginePowerKw: number;
  
  /**
   * Detailed breakdown by operational mode
   */
  modeBreakdowns?: ModePowerBreakdown[];
}

/**
 * Power and energy breakdown for a single operational mode
 */
export interface ModePowerBreakdown {
  mode: string;  // "Transit", "DP", "Anchor", "Port", "Maneuvering"
  hours: number;
  
  // Propulsion breakdown (kW)
  propulsionMainEngineKw: number;
  propulsionSailKw: number;
  
  // Hotel breakdown (kW)
  hotelShaftGenKw: number;
  hotelAuxGenKw: number;
  
  // Annual energy (kWh/year)
  energyMainEngineKwh: number;
  energyAuxGenKwh: number;
}

/**
 * Validation warning
 */
export interface ValidationWarning {
  field: string;
  message: string;
  severity: 'warning' | 'error';
  type?: string;
}

/**
 * Baseline data for display
 */
export interface BaselineData {
  totalDemandKw: number;
  meInstalledKw: number;
  aeInstalledKw: number;
  sgInstalledKw: number;
  meLoadPercent: number;
  aeLoadPercent: number;
  totalFuelConsumptionTons: number;
  totalCO2EmissionsTons: number;
}

/**
 * Per-variant result — only what differs between integration levels
 */
export interface VariantResult {
  // Savings
  fuelSavings: number;
  fuelSavingsPercentage: number;
  optimizedFOC: number;
  optimizedCO2: number;
  co2Reduction: number;
  co2ReductionPercentage: number;

  // Financial
  annualCostSavings: number;
  totalInvestment: number;
  paybackPeriod: number;
  roi: number;

  // Efficiency
  efficiencyFactor: number;

  // Optimized fuel breakdown (baseline is shared on AllVariantsCalculationResult)
  optimizedME: number;
  optimizedAE: number;

  /** Per-engine CO2 (ton/yr) computed server-side with each engine's own fuel factor. */
  optimizedMeCO2: number;
  optimizedAeCO2: number;

  // Load % for this variant
  mainEngineLoadPercent: number;
  auxiliaryEngineLoadPercent: number;

  // Per-level annual savings breakdown (ton/yr)
  level1SavingsTonPerYear: number;
  level2SavingsTonPerYear: number;
  level3SavingsTonPerYear: number;

  // Level-specific optimization details (nullable)
  level2Details?: Level2Details;
  level3Details?: Level3Details;

  // Validation warnings
  warnings: ValidationWarning[];
}

export interface Level1Details {
  // Optimal configuration
  activeMeCount: number;
  sgEnabled: boolean;
  activeAeCount: number;
  optimalFocTonPerHour: number;

  // Baseline configuration
  baselineMeCount: number;
  baselineSgEnabled: boolean;
  baselineAeCount: number;
  baselineFocTonPerHour: number;

  // Baseline power & load (weighted across modes)
  baselineMePowerKw: number;
  baselineSgPowerKw: number;
  baselineAePowerKw: number;
  baselineMeLoadPercent: number;
  baselineAeLoadPercent: number;

  savingsTonPerHour: number;
  validCombinationsCount: number;
  selectedBaselineIndex: number;
  validCombinations: ValidCombinationDto[];
}

export interface ValidCombinationDto {
  index: number;
  activeMeCount: number;
  sgEnabled: boolean;
  activeAeCount: number;
  focTonPerHour: number;
  meLoadPercent: number;
  aeLoadPercent: number;
  /** PTI propulsion assist for this combination [kW]; null/absent when unused. */
  ptiKw?: number | null;
}

export interface Level2Details {
  optimalSetpoints: GeneratorSetpoint[];
  optimalTotalSfoc: number;
  savingsTonPerHour: number;
}

export interface Level3Details {
  drcSavingsTonPerYear: number;
  variationPerGeneratorKw: number;
  reducedVariationPerGeneratorKw: number;
  /** Hotel/mission variation already shaved by the battery — excluded from DRC (Increment E). */
  batteryShavedVariationKw?: number;
  activeGeneratorCount: number;
}

export interface GeneratorSetpoint {
  generatorType: string;
  capacityKw: number;
  loadPercent: number;
  powerKw: number;
  sfoc: number;
}

/**
 * Response from calculate-all-variants endpoint.
 * Shared data at top level; per-variant data in advanced/pro/premium.
 */
export interface AllVariantsCalculationResult {
  // Shared across all variants (displayed once)
  powerDemands: PowerDemands;
  level1Details?: Level1Details;
  sailContribution?: SailContributionResult;
  batteryDetails?: BatteryDetails | null;
  baselineFOC: number;
  baselineCO2: number;
  baselineME: number;
  baselineAE: number;
  /** Per-engine baseline CO2 (ton/yr) with each engine's own fuel factor. */
  baselineMeCO2: number;
  baselineAeCO2: number;
  warnings: ValidationWarning[];

  // Per-variant results
  advanced: VariantResult;
  pro: VariantResult;
  premium: VariantResult;
}