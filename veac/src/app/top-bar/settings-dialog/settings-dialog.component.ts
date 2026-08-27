import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { VoyageService } from '../../services/state/voyage-scheduler.service';
import { VoyageApiService } from '../../services/api/voyage-api.service';

export interface SettingsConfig {
  displayFormat: 'energy' | 'fuel' | 'co2' | 'cost';
  fuelPricePerKg: number;
  fuelPricePerTon: number; 
}

@Component({
  selector: 'app-settings-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings-dialog.component.html',
  styleUrls: ['./settings-dialog.component.css']
})
export class SettingsDialogComponent {
  isOpen = false;
  isLoading = false;
  isSaving = false;
  saveError: string | null = null;
  hasUnsavedChanges = false;
  
  settings: SettingsConfig = {
    displayFormat: 'energy',
    fuelPricePerKg: 0.61,
    fuelPricePerTon: 610 
  };

  private originalFuelPrice = 0;
  private originalDisplayFormat: 'energy' | 'fuel' | 'co2' | 'cost' = 'energy';

  constructor(
    private voyageService: VoyageService,
    private voyageApiService: VoyageApiService
  ) {}

  async open(): Promise<void> {
    this.isOpen = true;
    this.saveError = null;
    this.hasUnsavedChanges = false;
    
    const currentFormat = this.voyageService.getDisplayFormat();
    if (currentFormat) {
      this.settings.displayFormat = currentFormat;
      this.originalDisplayFormat = currentFormat;
    }
    
    await this.loadConfiguration();
  }

  private async loadConfiguration(): Promise<void> {
    this.isLoading = true;
    this.saveError = null;

    try {
      const config = await this.voyageApiService.getVoyageCalculationConfiguration();
      
      if (config.fuelPricePerKg !== undefined) {
        this.settings.fuelPricePerKg = config.fuelPricePerKg;
        this.settings.fuelPricePerTon = config.fuelPricePerKg * 1000;
        this.originalFuelPrice = config.fuelPricePerKg;
        this.voyageService.setFuelPricePerKg(config.fuelPricePerKg);
      }
      
      if (config.emissionFactorCO2PerKg !== undefined) {
        this.voyageService.setEmissionFactorCO2PerKg(config.emissionFactorCO2PerKg);
      }

    } catch (error: any) {
      console.error('Error loading configuration:', error);
      this.saveError = 'Failed to load configuration. Using current values.';
      this.settings.fuelPricePerKg = this.voyageService.getFuelPricePerKg();
      this.settings.fuelPricePerTon = this.settings.fuelPricePerKg * 1000;
      this.originalFuelPrice = this.settings.fuelPricePerKg;
    } finally {
      this.isLoading = false;
    }
  }

  close(): void {
    if (this.hasUnsavedChanges) {
      this.settings.displayFormat = this.originalDisplayFormat;
      this.settings.fuelPricePerKg = this.originalFuelPrice;
      this.settings.fuelPricePerTon = this.originalFuelPrice * 1000;
    }
    this.isOpen = false;
  }

  onDisplayFormatChange(newValue: SettingsConfig['displayFormat']): void {
    this.hasUnsavedChanges = true;
    this.saveError = null;
  }

  onFuelPriceChange(): void {
    this.hasUnsavedChanges = true;
    this.saveError = null;
  }

  async saveAndClose(): Promise<void> {
    if (this.settings.fuelPricePerTon <= 0) {
      this.saveError = 'Fuel price must be greater than 0';
      return;
    }

    const fuelPricePerKg = this.settings.fuelPricePerTon / 1000;

    this.isSaving = true;
    this.saveError = null;

    try {
      const response = await this.voyageApiService.updateVoyageCalculationConfiguration({
        fuelPricePerKg: fuelPricePerKg
      });

      if (response.success) {
        this.voyageService.setFuelPricePerKg(response.fuelPricePerKg);
        this.voyageService.setEmissionFactorCO2PerKg(response.emissionFactorCO2PerKg);

        if (this.settings.displayFormat !== this.originalDisplayFormat) {
          this.voyageService.setDisplayFormat(this.settings.displayFormat);
          this.originalDisplayFormat = this.settings.displayFormat;
        }

        this.settings.fuelPricePerKg = response.fuelPricePerKg;
        this.settings.fuelPricePerTon = response.fuelPricePerKg * 1000;
        this.originalFuelPrice = response.fuelPricePerKg;
        this.hasUnsavedChanges = false;

        // Close the dialog immediately after successful save
        this.isOpen = false;
      } else {
        throw new Error(response.message || 'Failed to save settings');
      }
    } catch (error: any) {
      this.saveError = error.message || 'An error occurred while saving settings';
    } finally {
      this.isSaving = false;
    }
  }

  resetToDefaults(): void {
    if (!confirm('Reset to original values loaded from vessel configuration?')) {
      return;
    }

    this.settings.fuelPricePerKg = this.originalFuelPrice;
    this.settings.fuelPricePerTon = this.originalFuelPrice * 1000;
    this.settings.displayFormat = this.originalDisplayFormat;
    this.hasUnsavedChanges = false;
    this.saveError = null;
  }
}