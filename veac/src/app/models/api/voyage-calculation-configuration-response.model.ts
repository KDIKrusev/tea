export interface VoyageCalculationConfigurationResponse {
  success: boolean;
  fuelPricePerKg: number;
  emissionFactorCO2PerKg: number;
  message?: string;
}