import { VoyageOptionSet } from '../entities/voyage-option-set.model';

export interface voyageEnergyAdvisorResponse {
    voyageDistance: number;
    voyageOptionSets: VoyageOptionSet[];
    correlationId: string;
    fuelPricePerKg: number;
    emissionFactorCO2PerKg: number;
    validationMessage?: string;
}
