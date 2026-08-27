import {RouteSegment} from '../entities/route-segment.model';
import {CurrentPosition} from '../entities/current-vessel-position.model';

export interface VoyageEnergyAdvisorLiveResponse {
    eta: number;
    remainingTimeInSeconds: number;
    currentSpeed: number;
    remainingRouteSegments: RouteSegment[]
    currentPosition?: CurrentPosition;
}
