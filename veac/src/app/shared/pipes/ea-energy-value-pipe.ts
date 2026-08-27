import { DecimalPipe } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';

/**
 * Automatically converts energy value from kWh to more suitable unit (Wh, kWh, MWh, GWh according to the magnitude)
 * and formats it according to specified digits info
 * Usage:
 *   value | eaEnergyValue:digitsInfo:targetUnit:sourceUnit
 * Example:
 *   {{ 20000 | eaEnergyValue:'1.0-0' }} -> 20 MWh
 *   {{ 0.2 | eaEnergyValue:'1.0-0' }} -> 200 Wh
 *   {{ 250 | eaEnergyValue:'1.0-0' }} -> 250 kWh
 */
@Pipe({
  name: 'eaEnergyValue',
  standalone: true,
  pure: true,
})
export class EaEnergyValuePipe implements PipeTransform {
  private readonly defaultSourceUnit: string = 'kWh';
  private decimalPipe = new DecimalPipe('en-US'); // Create an instance directly

  public transform(value: number, digitsInfo?: string, targetUnit?: string, sourceUnit?: string): string {
    if (sourceUnit) {
      value = this.convert(value, sourceUnit, this.defaultSourceUnit);
    }
    targetUnit = targetUnit || 'MWh';
    value = this.convert(value, this.defaultSourceUnit, targetUnit);

    let formattedValue: string = this.decimalPipe.transform(value, digitsInfo) ?? '0';

    formattedValue = formattedValue.replace(/,/g, '');

    if (targetUnit === 'K' && sourceUnit === '') {
      // Cost format: $1000K instead of 1000 K
      return `$${formattedValue}${targetUnit}`;
    }

    return `${formattedValue} ${targetUnit}`;
  }

  private convert(value: number, sourceUnit: string, targetUnit: string): number {
    if (sourceUnit === targetUnit) {
      return value;
    }

    // convert value from source unit to base unit (kWh)
    switch (sourceUnit) {
      case 'Wh':
        value /= 1000;
        break;
      case 'MWh':
        value *= 1000;
        break;
      case 'GWh':
        value *= 1000000;
        break;
    }

    // convert value from base unit (kWh) to target unit
    switch (targetUnit) {
      case 'Wh':
        value *= 1000;
        break;
      case 'MWh':
        value /= 1000;
        break;
      case 'GWh':
        value /= 1000000;
        break;
    }

    return value;
  }
}