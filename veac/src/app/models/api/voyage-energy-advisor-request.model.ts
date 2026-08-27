import { Route } from '../entities/route.model';

export interface voyageEnergyAdvisorRequest {
    etdMin: number;
    etdMax: number;
    etaMin: number;
    etaMax: number;
    speedMin: number;
    speedMax: number;
    route: Route;
    correlationId: string;
}
