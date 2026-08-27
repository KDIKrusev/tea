import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UnitsOfMeasurementService {

  public convertMetersPerSecondToKnots(value: number): number {
    return value * 1.943844;
  }

  public convertKnotsToMetersPerSecond(value: number): number {
    return value / 1.943844;
  }

  public convertKwToMw(value: number): number {
    return value / 1000;
  }

  public convertMwToKw(value: number): number {
    return value / 1000;
  }
}
