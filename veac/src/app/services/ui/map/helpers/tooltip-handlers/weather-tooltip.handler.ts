import { Injectable } from '@angular/core';
import { TooltipData } from './map-tooltip.helper';
import { BaseTooltipHandler } from './base-tooltip.handler';
import { VoyageService, DisplayFormat } from '../../../../../services/state/voyage-scheduler.service';

@Injectable({
  providedIn: 'root'
})
export class FavorableWeatherIndexTooltipHandler extends BaseTooltipHandler {

  private currentDisplayFormat: DisplayFormat = 'energy';

  constructor(private voyageService: VoyageService) {
    super();

    this.voyageService.displayFormat$.subscribe(format => {
      this.currentDisplayFormat = format;
    });
  }

  generateTooltip(data: TooltipData): string {
    const favorableWeatherData = data.data;

    if (!favorableWeatherData || favorableWeatherData.favorableWeatherIndex === undefined) {
      return this.generateBasicFavorableWeatherTooltip(data);
    }

    const time = this.getFormattedTimeFromData(data);

    return `
      <div class="tooltip-header">
        <div class="tooltip-icon">📊</div>
        <div class="tooltip-title">Weather contribution/resistance</div>
      </div>
      <div class="tooltip-content">
        ${this.generateSimplePowerRows(favorableWeatherData)}
        ${time ? `<div class="tooltip-time">${time}</div>` : ''}
      </div>
    `;
  }

  private generateBasicFavorableWeatherTooltip(data: TooltipData): string {
    const time = this.getFormattedTimeFromData(data);
    return `
      <div class="tooltip-header">
        <div class="tooltip-icon">📊</div>
        <div class="tooltip-title">Weather contribution/resistance</div>
      </div>
      <div class="tooltip-content">
        <div class="tooltip-row">
          <span class="tooltip-label">Status:</span>
          <span class="tooltip-value">Available</span>
        </div>
        ${time ? `<div class="tooltip-time">${time}</div>` : ''}
      </div>
    `;
  }

  private generateSimplePowerRows(favorableWeatherData: any): string {
    let rows = '';

    // --- Total Row ---
    if (this.currentDisplayFormat === 'cost') {
      rows += this.generateTotalCostRow(favorableWeatherData);
    } else if (this.currentDisplayFormat === 'fuel') {
      rows += this.generateTotalFuelRow(favorableWeatherData);
    } else {
      rows += this.generateTotalEnergyRow(favorableWeatherData);
    }

    const fwiValue = favorableWeatherData.favorableWeatherIndex;
    if (fwiValue !== undefined) {
      const percentage = Math.round(fwiValue * 100);
      // rows += this.createRow('Weather index:', `${percentage}%`);
    }

    if (this.currentDisplayFormat === 'cost') {
      rows += this.generateNetCostRow(favorableWeatherData);
    } else if (this.currentDisplayFormat === 'fuel') {
      rows += this.generateNetFuelRow(favorableWeatherData);
    } else {
      rows += this.generateNetPowerRow(favorableWeatherData);
    }

    return rows;
  }

  // ---------- TOTAL ROW HELPERS ----------

  private generateTotalCostRow(data: any): string {
    const totalFuel = data.avgTotalResistanceFuelConsumption;
    if (totalFuel === undefined) return '';

    const fuelPrice = this.voyageService.getFuelPricePerKg();
    const totalCost = (totalFuel * fuelPrice) / 1000;
    return this.createRow('Total cost:', `$${totalCost.toFixed(2)}K/h`);
  }

  private generateTotalFuelRow(data: any): string {
    const totalFuel = data.avgTotalResistanceFuelConsumption;
    if (totalFuel === undefined) return '';

    const fuelDisplay = this.formatFuelValue(totalFuel);
    return this.createRow('Total fuel:', fuelDisplay);
  }

  private generateTotalEnergyRow(data: any): string {
    const totalEnergy = data.avgTotalResistanceEnergyConsumption ?? data.avgTotalPower;
    if (totalEnergy === undefined) return '';

    const energyValue = (totalEnergy / 1000).toFixed(2);
    return this.createRow('Total energy:', `${energyValue} MWh`);
  }

  // ---------- NET IMPACT HELPERS ----------

  private generateNetCostRow(data: any): string {
    const netFuel = data.avgNetWeatherResistanceFuelConsumption;
    if (netFuel === undefined) return '';

    const fuelPrice = this.voyageService.getFuelPricePerKg();
    const costValue = (Math.abs(netFuel) * fuelPrice) / 1000;
    const label = netFuel > 0 ? 'Weather resistance:' : netFuel < 0 ? 'Weather contribution:' : 'Weather impact:';
    const value = netFuel === 0 ? 'No impact' : `$${costValue.toFixed(2)}K/h`;

    return this.createRow(label, value);
  }

  private generateNetFuelRow(data: any): string {
    const netFuel = data.avgNetWeatherResistanceFuelConsumption;
    if (netFuel === undefined) return '';

    const absValue = Math.abs(netFuel);
    const displayValue = this.formatFuelValue(absValue);
    const label = netFuel > 0 ? 'Weather resistance:' : netFuel < 0 ? 'Weather contribution:' : 'Weather impact:';
    const value = netFuel === 0 ? 'No Impact' : displayValue;

    return this.createRow(label, value);
  }

  private generateNetPowerRow(data: any): string {
    const netPower = data.avgNetWeatherResistancePower;
    if (netPower === undefined) return '';

    const absPower = Math.abs(netPower) / 1000;
    const label = netPower > 0 ? 'Weather resistance power:' : netPower < 0 ? 'Weather contribution power:' : 'Weather power impact:';
    const value = netPower === 0 ? 'No Impact' : `${absPower.toFixed(1)} MW`;

    return this.createRow(label, value);
  }

  // ---------- UTILITIES ----------

  private createRow(label: string, value: string): string {
    return `
      <div class="tooltip-row">
        <span class="tooltip-label">${label}</span>
        <span class="tooltip-value">${value}</span>
      </div>
    `;
  }

  private formatFuelValue(value: number): string {
    if (value < 1000) {
      return `${Math.round(value)} kg/h`;
    }
    return `${(value / 1000).toFixed(2)} t/h`;
  }
}
