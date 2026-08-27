import { VoyageOption } from '../entities/voyage-option.model';

export interface voyageEnergyAdvisorResponse {
    voyageDistance: number;
    voyageOptions: VoyageOption[];
    correlationId: string;
    fuelPricePerKg: number;
    emissionFactorCO2PerKg: number;
    validationMessage?: string;
}
