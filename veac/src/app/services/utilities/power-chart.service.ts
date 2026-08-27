import { Injectable } from '@angular/core';
import { RouteSegment } from '../../models/entities/route-segment.model';
import { DisplayFormat } from '../state/voyage-scheduler.service';

export interface PowerDataItem {
  label: string;
  value: number;
  valueDisplay?: number;
  percentage?: number | null;
  color: string;
}

@Injectable({
  providedIn: 'root'
})
export class PowerChartService {

  getPowerData(segment: RouteSegment, currentDisplayFormat: DisplayFormat): PowerDataItem[] {
    if (!segment) return [];

    let calmWaterValue: number;
    let windsValue: number;
    let currentsValue: number;
    let sailsValue: number;

    switch (currentDisplayFormat) {
      case 'cost':
        calmWaterValue = segment.avgCalmWaterResistanceCost || 0;
        windsValue = segment.avgWindResistanceCost || 0;
        currentsValue = segment.avgCurrentResistanceCost || 0;
        sailsValue = segment.avgSailResistanceCost || 0;
        break;
      case 'fuel':
        calmWaterValue = segment.avgCalmWaterResistanceFuelConsumption || 0;
        windsValue = segment.avgWindResistanceFuelConsumption || 0;
        currentsValue = segment.avgCurrentResistanceFuelConsumption || 0;
        sailsValue = segment.avgSailResistanceFuelConsumption || 0;
        break;
      default:
        calmWaterValue = segment.avgCalmWaterPower || 0;
        windsValue = segment.avgWindPower || 0;
        currentsValue = segment.avgCurrentPower || 0;
        sailsValue = segment.avgSailPower || 0;
        break;
    }

    const baselineValue = calmWaterValue || 0;
    const calculatePercentage = currentDisplayFormat === 'energy'
      ? this.calculatePercentage.bind(this)
      : this.calculateFuelPercentage.bind(this);

    return [
      { label: 'Calm water', value: calmWaterValue, valueDisplay: this.getDisplayValue(calmWaterValue, currentDisplayFormat), percentage: null, color: this.getBarColor(calmWaterValue) },
      { label: 'Winds', value: windsValue, valueDisplay: this.getDisplayValue(windsValue, currentDisplayFormat), percentage: calculatePercentage(windsValue, baselineValue), color: this.getBarColor(windsValue) },
      { label: 'Currents', value: currentsValue, valueDisplay: this.getDisplayValue(currentsValue, currentDisplayFormat), percentage: calculatePercentage(currentsValue, baselineValue), color: this.getBarColor(currentsValue) },
      { label: 'Sails', value: sailsValue, valueDisplay: this.getDisplayValue(sailsValue, currentDisplayFormat), percentage: calculatePercentage(sailsValue, baselineValue), color: this.getBarColor(sailsValue) }
    ];
  }

  private calculateFuelPercentage(valueFuel: number, calmWaterFuel: number): number | null {
    if (!calmWaterFuel || calmWaterFuel === 0 || valueFuel === 0) return null;
    const percentage = (valueFuel / Math.abs(calmWaterFuel)) * 100;
    return Math.round(percentage * 10) / 10;
  }

  private calculatePercentage(valueMW: number, calmWaterMW: number): number | null {
    if (!calmWaterMW || calmWaterMW === 0 || valueMW === 0) return null;
    const percentage = (valueMW / Math.abs(calmWaterMW)) * 100;
    return Math.round(percentage * 10) / 10;
  }

  private getDisplayValue(value: number, format: DisplayFormat): number {
    return Math.round(value * (format === 'energy' ? 1000 : 1));
  }

  getBarColor(value: number): string {
    return value >= 0 ? '#E74C3C' : '#34a853';
  }

  formatPowerValue(value: number, format: DisplayFormat): string {
    const absoluteValue = Math.abs(Math.round(value));

    if (format === 'cost') {
      return absoluteValue >= 1000 ? `$${(absoluteValue / 1000).toFixed(1)}K/h` : `$${absoluteValue}/h`;
    } else if (format === 'fuel') {
      return `${absoluteValue.toLocaleString('en-US')} kg/h`;
    } else {
      return absoluteValue >= 10000 ? `${absoluteValue.toLocaleString('en-US')} kW` : `${absoluteValue} kW`;
    }
  }

  formatTotalPowerValue(totalValue: number, format: DisplayFormat): string {
    const total = totalValue / 1000;

    if (format === 'cost') {
      return `$${total.toFixed(2)}K/h`;
    } else if (format === 'fuel') {
      return total < 1 ? `${(total * 1000).toFixed(0)} kg/h` : `${total.toFixed(2)} t/h`;
    } else {
      return `${total.toFixed(2)} MW`;
    }
  }
}
