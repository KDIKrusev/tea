import { Component, Input, OnInit, OnChanges, SimpleChanges, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouteSegment } from '../../../models/entities/route-segment.model';
import { Waypoint } from '../../../models/entities/waypoint.model';

@Component({
  selector: 'app-segment-details',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './segment-details.component.html',
  styleUrls: ['./segment-details.component.css'],
  schemas: [CUSTOM_ELEMENTS_SCHEMA]
})
export class SegmentDetailsComponent implements OnInit, OnChanges {
  @Input() selectedSegment: RouteSegment | null = null;
  @Input() selectedSegmentIndex: number | null = null;

  public selectedSegmentPosition: string = '';

  public convertWindSpeedToBeaufort(windSpeedMs: number): number {
    // Beaufort scale conversion from m/s
    if (windSpeedMs < 0.5) return 0;  // Calm
    if (windSpeedMs < 1.5) return 1;  // Light air
    if (windSpeedMs < 3.3) return 2;  // Light breeze
    if (windSpeedMs < 5.5) return 3;  // Gentle breeze
    if (windSpeedMs < 7.9) return 4;  // Moderate breeze
    if (windSpeedMs < 10.7) return 5; // Fresh breeze
    if (windSpeedMs < 13.8) return 6; // Strong breeze
    if (windSpeedMs < 17.1) return 7; // High wind
    if (windSpeedMs < 20.7) return 8; // Gale
    if (windSpeedMs < 24.4) return 9; // Strong gale
    if (windSpeedMs < 28.4) return 10; // Storm
    if (windSpeedMs < 32.6) return 11; // Violent storm
    return 12; // Hurricane
  }

  public convertCurrentSpeedToScale(currentSpeedMs: number): number {
    // Convert from m/s to a 0-4 scale for ocean currents
    if (currentSpeedMs < 0.3) return 0;  // Very weak/negligible current
    if (currentSpeedMs < 0.9) return 1;  // Weak current
    if (currentSpeedMs < 1.5) return 2;  // Moderate current
    if (currentSpeedMs < 2.5) return 3;  // Strong current
    return 4;  // Very strong current
  }

  ngOnInit(): void {
    if (this.selectedSegment) {
      this.updateSegmentDetails();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedSegment'] && this.selectedSegment) {
      this.updateSegmentDetails();
    }
  }

  private updateSegmentDetails(): void {
    if (!this.selectedSegment) return;

    this.ensureTrueWeather();

    const position: Waypoint = this.selectedSegment.startPosition;
    if (!position || !Number.isFinite(position.latitude) || !Number.isFinite(position.longitude)) {
      this.selectedSegmentPosition = '';
      return;
    }

    this.selectedSegmentPosition =
      Math.abs(position.latitude).toFixed(1) + '°' + (position.latitude >= 0 ? 'N' : 'S') + ' - ' +
      Math.abs(position.longitude).toFixed(1) + '°' + (position.longitude >= 0 ? 'E' : 'W');
  }

  private finiteOrZero(value: number | null | undefined): number {
    return Number.isFinite(value) ? value! : 0;
  }

  private ensureTrueWeather(): void {
    if (!this.selectedSegment) return;

    if (!this.selectedSegment.trueWeather) {
      console.warn("trueWeather is undefined, creating default values");
      this.selectedSegment.trueWeather = {
        windSpeed: 0,
        windDirection: 0,
        waveHeight: 0,
        wavePeakPeriod: 0,
        waveDirection: 0,
        currentSpeed: 0,
        currentDirection: 0,
        airTemperature: 0,
        airPressure: 0,
        relativeHumidity: 0,
        cloudCoverage: 0,
        favorableWeatherIndex: 0,
        avgNetWeatherResistancePower: 0,
        avgTotalResistanceFuelConsumption: 0
      };
    }
  }

  public getWindSpeed(): number {
    return this.finiteOrZero(this.selectedSegment?.trueWeather?.windSpeed);
  }

  public getCurrentSpeed(): number {
    return this.finiteOrZero(this.selectedSegment?.trueWeather?.currentSpeed);
  }

  public getWindDirection(): number {
    return this.finiteOrZero(this.selectedSegment?.trueWeather?.windDirection);
  }

  public getCurrentDirection(): number {
    return this.finiteOrZero(this.selectedSegment?.trueWeather?.currentDirection);
  }

  public getVesselSpeed(): number {
    return this.finiteOrZero(this.selectedSegment?.averageSpeed);
  }

  public getCourse(): number {
    return this.finiteOrZero(this.selectedSegment?.course);
  }
}