import { DecimalPipe } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';

/*
 * Automatically converts power value from kW to more suitable unit (W, kW, MW, GW according to the magnitude) and formats it according to specified digits info
 * Usage:
 *   value | eaPowerValue:digitsInfo:targetUnit:sourceUnit
 * Example:
 *   {{ 20000 | eaPowerValue:'1.0-0' }} -> 20 MW
 *   {{ 0.2 | eaPowerValue:'1.0-0' }} -> 200 W
 *   {{ 250 | eaPowerValue:'1.0-0' }} -> 250 kW
*/
@Pipe({name: 'eaPowerValue',  standalone: true, pure: true})
export class EaPowerValuePipe implements PipeTransform {
  private readonly defaultSourceUnit: string = 'kW';
  private decimalPipe = new DecimalPipe('en-US');

  public transform(value: number, digitsInfo?: string, targetUnit?: string, sourceUnit?: string): string {
    if (sourceUnit) {
      value = this.convert(value, sourceUnit, this.defaultSourceUnit);
    }
    if (!targetUnit) {
      if (Math.abs(value) < 10) {
        targetUnit = 'W';
      } else if (Math.abs(value) >= 10 && Math.abs(value) < 10000) {
          targetUnit = 'kW';
        } else if (Math.abs(value) >= 10000 && Math.abs(value) < 10000000) {
            targetUnit = 'MW';
      }  else if (Math.abs(value) >= 10000000) {
          targetUnit = 'GW';
      }
    }
    value = this.convert(value, this.defaultSourceUnit, targetUnit ?? 'kW');
    let formattedValue: string = this.decimalPipe.transform(value, digitsInfo) ?? '';
    formattedValue = formattedValue.replace(/,/g, '');
    return `${formattedValue} ${targetUnit}`;
  }

  private convert(value: number, sourceUnit: string, targetUnit: string): number {
    if (sourceUnit === targetUnit) {
      return value;
    }

    // convert value from source unit to base unit (kW)
    switch (sourceUnit) {
      case 'W':
        value /= 1000;
        break;
      case 'MW':
        value *= 1000;
        break;
      case 'GW':
        value *= 1000000;
        break;
    }

    // convert value from base unit (kW) to target unit
    switch (targetUnit) {
      case 'W':
        value *= 1000;
        break;
      case 'MW':
        value /= 1000;
        break;
      case 'GW':
        value /= 1000000;
        break;
    }

    return value;
  }
}
