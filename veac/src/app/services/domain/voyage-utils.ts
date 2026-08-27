import { voyageEnergyAdvisorRequest } from '../../models/api/voyage-energy-advisor-request.model';
import { voyageEnergyAdvisorResponse } from '../../models/api/voyage-energy-advisor-response.model';
import { Route } from '../../models/entities/route.model';
import { UnitsOfMeasurementService } from '../utilities/units-of-measurement.service';

export class VoyageUtils {
  static buildRequestBody(
    etdMin: number,
    etdMax: number,
    etaMin: number,
    etaMax: number,
    speedMin: number,
    speedMax: number,
    route: Route,
    correlationId: string,
    processTimestamp: (timestamp: number) => number,
    processSpeed: (speed: number) => number
  ): voyageEnergyAdvisorRequest {
    return {
      etdMin: processTimestamp(etdMin),
      etdMax: processTimestamp(etdMax),
      etaMin: processTimestamp(etaMin),
      etaMax: processTimestamp(etaMax),
      speedMin: processSpeed(speedMin),
      speedMax: processSpeed(speedMax),
      route,
      correlationId
    };
  }

  static transformResponse(response: voyageEnergyAdvisorResponse): voyageEnergyAdvisorResponse {
    response.voyageOptions = response.voyageOptions.map(option => ({
      ...option,
      etd: option.etd * 1000,
      eta: option.eta * 1000,
      routeSegments: option.routeSegments.map(segment => ({
        ...segment,
        startTime: segment.startTime * 1000,
        endTime: segment.endTime * 1000
      }))
    }));
    return response;
  }

  static emptyResponse(correlationId: string): voyageEnergyAdvisorResponse {
    return {
      voyageDistance: 0,
      voyageOptions: [],
      correlationId,
      fuelPricePerKg: 0,
      emissionFactorCO2PerKg: 0
    };
  }

  static processOutgoingTimestamp(timestamp: number): number {
    if (!timestamp) return -1;
    return timestamp * 1000;
  }

  static processIncomingTimestamp(timestamp: number): number {
    return timestamp / 1000;
  }

  static processOutgoingSpeed(speed: number, unitsService: UnitsOfMeasurementService): number {
    return unitsService.convertKnotsToMetersPerSecond(speed);
  }

  static processIncomingSpeed(speed: number, unitsService: UnitsOfMeasurementService): number {
    return unitsService.convertMetersPerSecondToKnots(speed);
  }
}
