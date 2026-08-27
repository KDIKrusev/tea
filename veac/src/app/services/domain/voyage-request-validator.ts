import { Route } from '../../models/entities/route.model';

export class VoyageRequestValidator {
  static validate(
    etdMin: number,
    etdMax: number,
    etaMin: number,
    etaMax: number,
    speedMin: number,
    speedMax: number,
    route: Route | null
  ): boolean {
    const hasEtd = etdMin > 0 && etdMax > 0;
    const hasEta = etaMin > 0 && etaMax > 0;

    if (!hasEtd && !hasEta) {
      console.error("Validation failed: Neither ETD nor ETA provided");
      return false;
    }

    if (hasEtd && hasEta && etdMin >= etaMax) {
      console.error(
        `Validation failed: ETD min (${new Date(etdMin).toISOString()}) must be before ETA max (${new Date(etaMax).toISOString()})`
      );
      return false;
    }

    if (speedMin < 0 || speedMax <= 0 || speedMax < speedMin) {
      console.error(`Validation failed: Invalid speed range [${speedMin}, ${speedMax}]`);
      return false;
    }

    if (!route || !route.waypoints || !route.waypoints.length) {
      console.error("Validation failed: Invalid or empty route");
      return false;
    }

    return true;
  }
}
