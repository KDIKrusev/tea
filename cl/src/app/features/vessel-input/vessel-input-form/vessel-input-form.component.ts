import { Component, EventEmitter, Output, Input, OnInit, OnDestroy, AfterViewInit, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { CalculatorInput, BatteryConfigurationInput, BatteryRelevantMode, BatteryDetails } from '../../../calculations/calculator.types';
import { VesselTypeWithEnginesResponse } from '../../../core/vessel-configuration.types';
import { VesselOperationalProfile } from '../../../core/operational-profile.types';
import { debounceTime, Subject, takeUntil } from 'rxjs';
import { VALIDATION_LIMITS, DEBOUNCE_TIMES, DEFAULT_VALUES, DEFAULT_FUEL } from '../../../shared/constants';
import { VesselConfigSectionComponent } from './vessel-config-section/vessel-config-section.component';
import { EngineConfigSectionComponent } from './engine-config-section/engine-config-section.component';
import { AdditionalConfigSectionComponent } from './additional-config-section/additional-config-section.component';
import { OperationalModesSectionComponent } from './operational-modes-section/operational-modes-section.component';
import { WeatherInputSectionComponent } from './weather-input-section/weather-input-section.component';
import { BatteryConfigSectionComponent } from './battery-config-section/battery-config-section.component';
import { FormEditTrackerService } from './form-edit-tracker.service';
import { SailContributionResult } from '../../../calculations/calculator.types';
import { ProfileService } from '../../../core/profile.service';
import { SavedProfile, PROFILE_SCHEMA_VERSION } from '../../../core/profile.types';
import { AppDataService } from '../../../core/app-data.service';

/**
 * A form emission, carrying WHY it happened.
 *
 * Restoring a saved profile produces several emissions (the vessel/engine cascade patches fields
 * asynchronously, then the profile's own values are applied on top). Consumers must be able to
 * tell those apart from a real edit instead of guessing by arrival order — guessing is what let a
 * restored `baselineIndex` be wiped by the second emission of its own restore.
 */
export interface FormChangeEvent {
  input: CalculatorInput;
  /** 'restore' = part of loading a saved profile/draft · 'user' = someone changed a field. */
  source: 'user' | 'restore';
}

/** Safety net: a restore that never reaches applyProfileInputValues must not freeze the flag. */
const RESTORE_WATCHDOG_MS = 3000;

@Component({
  selector: 'app-vessel-input-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatIconModule,
    VesselConfigSectionComponent,
    EngineConfigSectionComponent,
    AdditionalConfigSectionComponent,
    OperationalModesSectionComponent,
    WeatherInputSectionComponent,
    BatteryConfigSectionComponent
  ],
  templateUrl: './vessel-input-form.component.html',
  styleUrl: './vessel-input-form.component.css'
})
export class VesselInputFormComponent implements OnInit, OnDestroy, AfterViewInit {
  @Output() formChanged = new EventEmitter<FormChangeEvent>();
  @Input() isCalculating = false;
  @Input() sailContribution: SailContributionResult | null = null;
  @Input() batteryDetails: BatteryDetails | null = null;
  @Input() hasResults = false;
  @ViewChild(EngineConfigSectionComponent) engineConfigSection!: EngineConfigSectionComponent;
  @ViewChild(OperationalModesSectionComponent) operationalModesSection!: OperationalModesSectionComponent;
  @ViewChild(VesselConfigSectionComponent) vesselConfigSection!: VesselConfigSectionComponent;
  @ViewChild(BatteryConfigSectionComponent) batteryConfigSection?: BatteryConfigSectionComponent;
  vesselForm!: FormGroup;
  vesselTypeName: string = '';
  private destroy$ = new Subject<void>();
  private editTracker = inject(FormEditTrackerService);
  private profileService = inject(ProfileService);
  private appDataService = inject(AppDataService);

  /** Input from a saved profile that should be applied after the next cascade completes */
  private pendingProfileInput: CalculatorInput | null = null;

  /** True from the moment a profile starts loading until its values have been applied. */
  private restoreInFlight = false;
  private restoreWatchdog: ReturnType<typeof setTimeout> | null = null;

  /** Timer handle for auto-draft */
  private draftTimer: ReturnType<typeof setInterval> | null = null;

  // Default DRC variation per vessel type (kW) — mirrors backend CalculatorSettings
  private readonly vesselVariationDefaults: Record<string, number> = {
    'Bulk Carrier': 250,
    'Container': 1500,
    'LNG': 1000
  };
  private readonly defaultVariationKw = 500;
  
  // Flags to prevent double initialization calls
  private componentsLoaded = false;
  private initialEmissionScheduled = false;

  // Dropdown options
  sailOptions = [
    { value: 'No', label: 'No' },
    { value: 'Yes', label: 'Yes' }
  ];

  constructor(
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.setupAutoCalculation();
    this.startAutoDraft();
  }

  ngAfterViewInit(): void {
    this.restoreDraftIfAvailable();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.draftTimer !== null) {
      clearInterval(this.draftTimer);
    }
    if (this.restoreWatchdog !== null) {
      clearTimeout(this.restoreWatchdog);
    }
  }

  private setupAutoCalculation(): void {
    this.vesselForm.valueChanges
      .pipe(
        debounceTime(DEBOUNCE_TIMES.FORM_INPUT),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.updateWeightedAverageHotelLoad();
        this.updateFuelPriceFromFuelType();
        
        if (this.vesselForm.valid && this.componentsLoaded) {
          this.emitFormValues();
        }
      });

    this.scheduleInitialEmission();
  }

  private updateWeightedAverageHotelLoad(): void {
    const formValue = this.vesselForm.value;

    const transitHotelPowerKW = Number(formValue.transitHotelPowerKW) || 0;
    const transitHours = Number(formValue.transitHours) || 0;
    const dpHotelPowerKW = Number(formValue.dpHotelPowerKW) || 0;
    const dpHours = Number(formValue.dpHours) || 0;
    const portHotelPowerKW = Number(formValue.portHotelPowerKW) || 0;
    const portHours = Number(formValue.portHours) || 0;
    const anchorHotelPowerKW = Number(formValue.anchorHotelPowerKW) || 0;
    const anchorHours = Number(formValue.anchorHours) || 0;
    const maneuveringHotelPowerKW = Number(formValue.maneuveringHotelPowerKW) || 0;
    const maneuveringHours = Number(formValue.maneuveringHours) || 0;

    const totalHours = transitHours + dpHours + portHours + anchorHours + maneuveringHours;

    const totalWeightedPower =
      (transitHotelPowerKW * transitHours) +
      (dpHotelPowerKW * dpHours) +
      (portHotelPowerKW * portHours) +
      (anchorHotelPowerKW * anchorHours) +
      (maneuveringHotelPowerKW * maneuveringHours);

    const weightedAverage = totalHours > 0 ? totalWeightedPower / totalHours : 0;

    if (weightedAverage > 0) {
      this.vesselForm.patchValue({ hotelLoad: Math.round(weightedAverage) }, { emitEvent: false });
    }
  }

  /** 
   * Dynamically update fuel price based on selected fuel type
   * Reads from backend CalculatorSettings via AppDataService
   */
  private updateFuelPriceFromFuelType(): void {
    const fuelDefaultPrices = this.appDataService.getFuelDefaultPrices();
    
    // For now, use main engine fuel type as the primary (could also check aux and use a weighted average)
    const mainFuelType = this.vesselForm.get('mainFuelType')?.value;
    
    if (mainFuelType && fuelDefaultPrices[mainFuelType] !== undefined) {
      const newPrice = fuelDefaultPrices[mainFuelType];
      const currentPrice = this.vesselForm.get('fuelPrice')?.value;
      
      // Only update if price changed to avoid unnecessary form updates
      if (currentPrice !== newPrice) {
        this.vesselForm.patchValue({ fuelPrice: newPrice }, { emitEvent: false });
      }
    }
  }

  private scheduleInitialEmission(): void {
    if (this.initialEmissionScheduled) return;
    
    this.initialEmissionScheduled = true;
    setTimeout(() => {
      if (!this.componentsLoaded) {
        this.componentsLoaded = true;
        if (this.vesselForm.valid) {
          // Known fallback: setTimeout ensures child components have finished initializing
          this.emitFormValues();
        }
      }
    }, 1500);
  }

  private emitFormValues(): void {
    const formValue = this.vesselForm.getRawValue();
    this.formChanged.emit({
      input: this.buildCalculatorInput(formValue),
      source: this.restoreInFlight ? 'restore' : 'user'
    });
  }

  /** Marks every emission until the profile's values land as part of the restore. */
  private beginRestore(): void {
    this.restoreInFlight = true;
    if (this.restoreWatchdog !== null) {
      clearTimeout(this.restoreWatchdog);
    }
    this.restoreWatchdog = setTimeout(() => this.endRestore(), RESTORE_WATCHDOG_MS);
  }

  private endRestore(): void {
    this.restoreInFlight = false;
    if (this.restoreWatchdog !== null) {
      clearTimeout(this.restoreWatchdog);
      this.restoreWatchdog = null;
    }
  }

  getCurrentInputSnapshot(baselineIndex?: number): CalculatorInput {
    const snapshot = this.buildCalculatorInput(this.vesselForm.getRawValue());
    if (baselineIndex !== undefined) {
      return { ...snapshot, baselineIndex };
    }
    return snapshot;
  }

  private buildCalculatorInput(formValue: ReturnType<typeof this.vesselForm.getRawValue>): CalculatorInput {
    return {
      propulsionPower: Number(formValue.propulsionPower),
      hotelLoad: Number(formValue.hotelLoad),
      seaMargin: Number(formValue.seaMargin),
      meCapacityPerEngine: Number(formValue.meCapacityPerEngine),
      meCount: Number(formValue.meCount),
      sgCapacityPerEngine: Number(formValue.sgCapacityPerEngine),
      aeCapacityPerEngine: Number(formValue.aeCapacityPerEngine),
      aeCount: Number(formValue.aeCount),
      mainEngineTypeId: Number(formValue.mainEngineTypeId),
      auxEngineTypeId: Number(formValue.auxEngineTypeId),
      sailInstalled: formValue.sailInstalled === 'Yes',
      batteryCapacity: Number(formValue.batteryCapacity),
      battery: this.buildBatteryInput(formValue),
      maxPtiPerEngineKw: formValue.batteryMaxPtiKw ? Number(formValue.batteryMaxPtiKw) : undefined,
      dpRedundancyRequirementKw: formValue.batteryDpRedundancyKw ? Number(formValue.batteryDpRedundancyKw) : undefined,
      missionHeavyConsumerMaxKw: formValue.batteryMissionMaxKw ? Number(formValue.batteryMissionMaxKw) : undefined,
      fuelPrice: Number(formValue.fuelPrice),
      mainFuelType: formValue.mainFuelType || undefined,
      auxFuelType: formValue.auxFuelType || undefined,
      
      // Multi-Modal Operational Modes
      transitHours: formValue.transitHours ? Number(formValue.transitHours) : undefined,
      transitHotelPowerKW: formValue.transitHotelPowerKW ? Number(formValue.transitHotelPowerKW) : undefined,
      dpEnabled: !!(formValue.dpHours && Number(formValue.dpHours) > 0),
      dpHours: formValue.dpHours ? Number(formValue.dpHours) : undefined,
      dpHotelPowerKW: formValue.dpHotelPowerKW ? Number(formValue.dpHotelPowerKW) : undefined,
      requiredDPPowerKW: formValue.requiredDPPowerKW ? Number(formValue.requiredDPPowerKW) : undefined,
      dpWeatherCondition: formValue.dpWeatherCondition || undefined,
      portHotelPowerKW: formValue.portHotelPowerKW ? Number(formValue.portHotelPowerKW) : undefined,
      portHours: formValue.portHours ? Number(formValue.portHours) : undefined,
      anchorHotelPowerKW: formValue.anchorHotelPowerKW ? Number(formValue.anchorHotelPowerKW) : undefined,
      anchorHours: formValue.anchorHours ? Number(formValue.anchorHours) : undefined,
      maneuveringPropulsionPowerKW: formValue.maneuveringPropulsionPowerKW ? Number(formValue.maneuveringPropulsionPowerKW) : undefined,
      maneuveringHotelPowerKW: formValue.maneuveringHotelPowerKW ? Number(formValue.maneuveringHotelPowerKW) : undefined,
      maneuveringHours: formValue.maneuveringHours ? Number(formValue.maneuveringHours) : undefined,

      // Vessel Type (for L3 DRC variation lookup)
      vesselTypeName: this.vesselTypeName || undefined,

      // Level 3 DRC: user-specified variation in kW for Hotel/Mission load
      hotelLoadVariationKw: formValue.hotelLoadVariationKw ? Number(formValue.hotelLoadVariationKw) : undefined,

      // Weather Input
      sailEnabled: formValue.sailEnabled ?? false,
      trueWindSpeed: formValue.trueWindSpeed != null ? Number(formValue.trueWindSpeed) : undefined,
      windAngleRelVessel: formValue.windAngleRelVessel != null ? Number(formValue.windAngleRelVessel) : undefined,
      vesselSpeedKnots: formValue.vesselSpeedKnots ? Number(formValue.vesselSpeedKnots) : 0
    };
  }

  /**
   * Battery payload: null unless enabled with positive power — keeps the request
   * identical to the pre-battery contract for existing users (zero-regression, AC1).
   */
  private buildBatteryInput(formValue: ReturnType<typeof this.vesselForm.getRawValue>): BatteryConfigurationInput | null {
    if (!formValue.batteryEnabled) return null;
    const powerKw = Number(formValue.batteryPowerKw) || 0;
    if (powerKw <= 0) return null;

    const relevantModes: BatteryRelevantMode[] = [];
    if (formValue.batteryModeTransit) relevantModes.push('Transit');
    if (formValue.batteryModeDp) relevantModes.push('DP');
    if (formValue.batteryModePort) relevantModes.push('Port');

    return {
      powerKw,
      capacityKwh: Number(formValue.batteryCapacityKwh) || 0,
      relevantModes
    };
  }

  private initializeForm(): void {
    this.vesselForm = this.fb.group({
      // Power and Load
      propulsionPower: [null, [Validators.min(VALIDATION_LIMITS.POWER.MIN)]],
      hotelLoad: [null, [Validators.min(VALIDATION_LIMITS.POWER.MIN)]],
      seaMargin: [null, [Validators.min(VALIDATION_LIMITS.SEA_MARGIN.MIN), Validators.max(VALIDATION_LIMITS.SEA_MARGIN.MAX)]],
      
      // Main Engine — rated power is a required manual input (ratings always differ)
      meCapacityPerEngine: [null, [Validators.required, Validators.min(VALIDATION_LIMITS.POWER.MIN_POSITIVE)]],
      meCount: [DEFAULT_VALUES.ME_COUNT, [Validators.min(VALIDATION_LIMITS.COUNT.MIN)]],

      // Shaft Generator — optional (0 = no shaft generator)
      sgCapacityPerEngine: [null, [Validators.min(VALIDATION_LIMITS.POWER.MIN)]],

      // Auxiliary Engine — rated power is a required manual input
      aeCapacityPerEngine: [null, [Validators.required, Validators.min(VALIDATION_LIMITS.POWER.MIN_POSITIVE)]],
      aeCount: [DEFAULT_VALUES.AE_COUNT, [Validators.min(VALIDATION_LIMITS.COUNT.MIN)]],
      
      // Engine Type IDs
      mainEngineTypeId: [null, [Validators.required]],
      auxEngineTypeId: [null, [Validators.required]],
      
      // Additional Systems
      sailInstalled: ['No'],
      batteryCapacity: [DEFAULT_VALUES.BATTERY_CAPACITY, [Validators.min(VALIDATION_LIMITS.POWER.MIN)]],

      // Battery configuration (Increment D — sketch: Capacity/Power + Relevant Modes)
      batteryEnabled: [false],
      batteryPowerKw: [null, [Validators.min(0)]],
      batteryCapacityKwh: [null, [Validators.min(0)]],
      batteryModeTransit: [false],
      batteryModeDp: [false],
      batteryModePort: [false],
      // PTI capacity per ME (Increment C — 0/empty = PTI not modelled)
      batteryMaxPtiKw: [null, [Validators.min(0)]],
      // Excel load inputs (Increment F): DP redundancy (RESERVE) + mission heavy-consumer max
      batteryDpRedundancyKw: [null, [Validators.min(0)]],
      batteryMissionMaxKw: [null, [Validators.min(0)]],
      
      // Financial
      fuelPrice: [DEFAULT_VALUES.FUEL_PRICE, [Validators.min(VALIDATION_LIMITS.FUEL_PRICE.MIN)]],

      // Fuel types (Epic 3) — per-engine; default MGO
      mainFuelType: [DEFAULT_FUEL],
      auxFuelType: [DEFAULT_FUEL],
      
      // Multi-Modal Operational Modes
      transitHours: [null, [Validators.min(0)]],
      transitHotelPowerKW: [null, [Validators.min(0)]],
      hotelLoadVariationKw: [null, [Validators.min(0)]],
      dpHours: [null, [Validators.min(0)]],
      dpHotelPowerKW: [null, [Validators.min(0)]],
      requiredDPPowerKW: [null, [Validators.min(0)]],
      dpWeatherCondition: [null],
      portHotelPowerKW: [null, [Validators.min(0)]],
      portHours: [null, [Validators.min(0)]],
      anchorHotelPowerKW: [null, [Validators.min(0)]],
      anchorHours: [null, [Validators.min(0)]],
      maneuveringPropulsionPowerKW: [null, [Validators.min(0)]],
      maneuveringHotelPowerKW: [null, [Validators.min(0)]],
      maneuveringHours: [null, [Validators.min(0)]],

      // Weather Input
      sailEnabled: [false],
      trueWindSpeed: [null, [Validators.min(0), Validators.max(20)]],
      windAngleRelVessel: [null, [Validators.min(0), Validators.max(360)]],
      vesselSpeedKnots: [null],

      // Parametric vessel selection (Epic 1) — validators applied per category
      // by VesselConfigSectionComponent
      vesselCategory: [null],
      vesselSize: [null, [Validators.min(1)]]
    });
    
    this.storeOriginalValues();
  }

  private storeOriginalValues(): void {
    Object.keys(this.vesselForm.controls).forEach(key => {
      const control = this.vesselForm.get(key);
      if (control) {
        this.editTracker.setOriginalValue(key, control.value);
      }
    });
  }

  // ─── AUTO-DRAFT ──────────────────────────────────────────────────────────────

  private startAutoDraft(): void {
    this.draftTimer = setInterval(() => {
      if (this.vesselForm.valid && this.componentsLoaded && this.vesselConfigSection) {
        const formValue = this.vesselForm.getRawValue();
        const category = this.vesselConfigSection.selectedCategoryName;
        const size = this.vesselConfigSection.selectedSize;
        const speed = this.vesselConfigSection.selectedSpeed;
        if (!category || size == null || speed == null) {
          return;
        }
        this.profileService.saveDraft(
          this.buildCalculatorInput(formValue),
          this.vesselConfigSection.selectionLabel,
          category,
          size,
          speed
        );
      }
    }, 30_000);
  }

  private restoreDraftIfAvailable(): void {
    const draft = this.profileService.loadDraft();
    if (!draft || !draft.input || !draft.vesselCategory
      || !Number.isFinite(draft.vesselSize) || !Number.isFinite(draft.vesselSpeed)) {
      return;
    }

    const draftProfile: SavedProfile = {
      id: 'draft',
      name: 'Auto Draft',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      vesselTypeName: draft.vesselTypeName,
      vesselCategory: draft.vesselCategory,
      vesselSize: draft.vesselSize,
      vesselSpeed: draft.vesselSpeed,
      input: draft.input,
      version: PROFILE_SCHEMA_VERSION // v2 drafts still load fine (battery absent ⇒ disabled)
    };

    this.loadProfile(draftProfile);
  }

  // ─── PROFILE LOAD ─────────────────────────────────────────────────────────────

  /**
   * Loads a saved profile: triggers the vessel/engine cascade and applies all
   * custom values once the cascade finishes (in onOperationalProfileLoaded).
   */
  loadProfile(profile: SavedProfile): void {
    this.beginRestore();
    this.pendingProfileInput = profile.input;
    this.vesselTypeName = profile.vesselTypeName;

    if (this.vesselConfigSection) {
      this.vesselConfigSection.selectVessel(profile.vesselCategory, profile.vesselSize, profile.vesselSpeed);
    }
  }

  onVesselEngineConfigSelected(vesselWithEngines: VesselTypeWithEnginesResponse & { applyEngineDefaults?: boolean }): void {
    // Prefer the composed parametric label ("Bulk Carrier 75,000 dwt");
    // fall back to the bucket record's name
    this.vesselTypeName = this.vesselConfigSection?.selectionLabel
      || vesselWithEngines.vesselType.vesselTypeName;
    const applyEngineDefaults = vesselWithEngines.applyEngineDefaults !== false;

    const vesselTypeWithRefs = vesselWithEngines.vesselType as {
      mainEngine?: { engineTypeId?: number | string };
      auxEngine?: { engineTypeId?: number | string };
    };
    const mainEngineId = Number(
      vesselWithEngines.mainEngineData?.id ?? vesselTypeWithRefs.mainEngine?.engineTypeId
    );
    const auxEngineId = Number(
      vesselWithEngines.auxEngineData?.id ?? vesselTypeWithRefs.auxEngine?.engineTypeId
    );

    // Auto-populate DRC variation based on vessel type
    const variationKw = this.getDefaultVariationForVessel(this.vesselTypeName);
    this.vesselForm.patchValue({ hotelLoadVariationKw: variationKw }, { emitEvent: true });
    this.editTracker.updateOriginalValue('hotelLoadVariationKw', variationKw);
    
    if (
      this.engineConfigSection &&
      Number.isFinite(mainEngineId) &&
      Number.isFinite(auxEngineId)
    ) {
      if (applyEngineDefaults) {
        this.engineConfigSection.setEngineConfiguration(
          mainEngineId,
          auxEngineId
        );
      } else {
        this.engineConfigSection.setEngineTypeReferences(
          mainEngineId,
          auxEngineId
        );
      }
    }

    // Fallback: if a profile is pending and operationalProfileLoaded is NOT emitted
    // (e.g. when fullData.operationalProfile is null for some vessel configs), apply
    // the profile here after a longer delay so it doesn't race with
    // onOperationalProfileLoaded (which uses 200 ms and clears pendingProfileInput).
    if (this.pendingProfileInput && applyEngineDefaults) {
      const capturedPending = this.pendingProfileInput;
      setTimeout(() => {
        if (this.pendingProfileInput === capturedPending) {
          this.pendingProfileInput = null;
          this.componentsLoaded = true;
          this.applyProfileInputValues(capturedPending);
        }
      }, 800);
    }
  }

  private getDefaultVariationForVessel(vesselTypeName: string): number {
    for (const [key, value] of Object.entries(this.vesselVariationDefaults)) {
      if (vesselTypeName.toLowerCase().includes(key.toLowerCase())) {
        return value;
      }
    }
    return this.defaultVariationKw;
  }

  onOperationalProfileLoaded(operationalProfile: VesselOperationalProfile): void {
    
    if (this.operationalModesSection && operationalProfile) {
      this.operationalModesSection.setOperationalProfile(operationalProfile);
      
      // Update original values for all DP mode fields
      const dpHours = this.vesselForm.get('dpHours')?.value;
      this.editTracker.updateOriginalValue('dpHours', dpHours);
      const dpHotelPowerKW = this.vesselForm.get('dpHotelPowerKW')?.value;
      this.editTracker.updateOriginalValue('dpHotelPowerKW', dpHotelPowerKW);
      const requiredDPPowerKW = this.vesselForm.get('requiredDPPowerKW')?.value;
      this.editTracker.updateOriginalValue('requiredDPPowerKW', requiredDPPowerKW);
      const dpWeatherCondition = this.vesselForm.get('dpWeatherCondition')?.value;
      this.editTracker.updateOriginalValue('dpWeatherCondition', dpWeatherCondition);

      // Update original values for all Transit Mode fields
      const transitHours = this.vesselForm.get('transitHours')?.value;
      this.editTracker.updateOriginalValue('transitHours', transitHours);
      const transitHotelPowerKW = this.vesselForm.get('transitHotelPowerKW')?.value;
      this.editTracker.updateOriginalValue('transitHotelPowerKW', transitHotelPowerKW);

      // Update original values for Port, Anchor, Maneuvering modes
      for (const field of ['portHotelPowerKW', 'portHours', 'anchorHotelPowerKW', 'anchorHours',
        'maneuveringPropulsionPowerKW', 'maneuveringHotelPowerKW', 'maneuveringHours']) {
        const val = this.vesselForm.get(field)?.value;
        this.editTracker.updateOriginalValue(field, val);
      }

      // Update original values for number of main and auxiliary engines
      const meCount = this.vesselForm.get('meCount')?.value;
      this.editTracker.updateOriginalValue('meCount', meCount);
      const aeCount = this.vesselForm.get('aeCount')?.value;
      this.editTracker.updateOriginalValue('aeCount', aeCount);

      // Mark components as loaded and trigger calculation
      this.componentsLoaded = true;

      // If a profile was pending, apply its values now — overriding cascade defaults.
      // Small delay ensures all patchValue calls from cascade have settled.
      if (this.pendingProfileInput) {
        const pending = this.pendingProfileInput;
        this.pendingProfileInput = null;
        setTimeout(() => { this.applyProfileInputValues(pending); }, 200);
      }

      if (this.vesselForm.valid) {
        this.emitFormValues();
      }
    }
  }

  /**
   * Applies all form values from a saved profile, overriding cascade defaults.
   * Called both from onOperationalProfileLoaded (primary path) and from
   * onVesselEngineConfigSelected (fallback when operationalProfile is absent).
   *
   * Uses emitEvent:false to prevent vesselSpeedKnots/vesselSize valueChanges
   * from triggering watchSizeAndSpeedInputs in vessel-config-section, which
   * would fire a new vessel fetch 400 ms later and overwrite the engine type
   * back to the vessel default (the "flicker" race condition).
   */
  private applyProfileInputValues(pending: CalculatorInput): void {
    this.engineConfigSection?.setEngineTypeReferences(
      pending.mainEngineTypeId,
      pending.auxEngineTypeId
    );

    this.vesselForm.patchValue({
      propulsionPower: pending.propulsionPower,
      hotelLoad: pending.hotelLoad,
      seaMargin: pending.seaMargin,
      meCapacityPerEngine: pending.meCapacityPerEngine,
      meCount: pending.meCount,
      sgCapacityPerEngine: pending.sgCapacityPerEngine,
      aeCapacityPerEngine: pending.aeCapacityPerEngine,
      aeCount: pending.aeCount,
      mainEngineTypeId: pending.mainEngineTypeId,
      auxEngineTypeId: pending.auxEngineTypeId,
      sailInstalled: pending.sailInstalled ? 'Yes' : 'No',
      batteryCapacity: pending.batteryCapacity,
      batteryEnabled: !!pending.battery && pending.battery.powerKw > 0,
      batteryPowerKw: pending.battery?.powerKw ?? null,
      batteryCapacityKwh: pending.battery?.capacityKwh ?? null,
      batteryModeTransit: pending.battery?.relevantModes?.includes('Transit') ?? false,
      batteryModeDp: pending.battery?.relevantModes?.includes('DP') ?? false,
      batteryModePort: pending.battery?.relevantModes?.includes('Port') ?? false,
      batteryMaxPtiKw: pending.maxPtiPerEngineKw ?? null,
      batteryDpRedundancyKw: pending.dpRedundancyRequirementKw ?? null,
      batteryMissionMaxKw: pending.missionHeavyConsumerMaxKw ?? null,
      fuelPrice: pending.fuelPrice,
      mainFuelType: pending.mainFuelType ?? 'MGO',
      auxFuelType: pending.auxFuelType ?? 'MGO',
      hotelLoadVariationKw: pending.hotelLoadVariationKw ?? null,
      transitHours: pending.transitHours ?? null,
      transitHotelPowerKW: pending.transitHotelPowerKW ?? null,
      dpHours: pending.dpHours ?? null,
      dpHotelPowerKW: pending.dpHotelPowerKW ?? null,
      requiredDPPowerKW: pending.requiredDPPowerKW ?? null,
      dpWeatherCondition: pending.dpWeatherCondition ?? null,
      portHotelPowerKW: pending.portHotelPowerKW ?? null,
      portHours: pending.portHours ?? null,
      anchorHotelPowerKW: pending.anchorHotelPowerKW ?? null,
      anchorHours: pending.anchorHours ?? null,
      maneuveringPropulsionPowerKW: pending.maneuveringPropulsionPowerKW ?? null,
      maneuveringHotelPowerKW: pending.maneuveringHotelPowerKW ?? null,
      maneuveringHours: pending.maneuveringHours ?? null,
      sailEnabled: pending.sailEnabled ?? false,
      trueWindSpeed: pending.trueWindSpeed ?? null,
      windAngleRelVessel: pending.windAngleRelVessel ?? null,
      vesselSpeedKnots: pending.vesselSpeedKnots ?? null
    }, { emitEvent: false }); // emitEvent:false prevents watchSizeAndSpeedInputs race

    // Ensure restored fuel values are valid for the selected engine families.
    // Example: a legacy saved profile may contain HFO while the restored ME is LNG-only.
    this.engineConfigSection?.setEngineTypeReferences(
      Number(pending.mainEngineTypeId),
      Number(pending.auxEngineTypeId)
    );

    // Mark all restored fields as original (not edited) relative to the profile
    Object.keys(this.vesselForm.controls).forEach(key => {
      const control = this.vesselForm.get(key);
      if (control) this.editTracker.updateOriginalValue(key, control.value);
    });
    // patchValue above used emitEvent:false — the battery section's dpHours subscription
    // did not fire, so re-evaluate DP checkbox availability explicitly
    this.batteryConfigSection?.refreshDpAvailability();
    // emitEvent:false suppresses setupAutoCalculation — trigger calculation explicitly
    this.updateWeightedAverageHotelLoad();

    // The profile's own values are now in the form — this emission, and everything after it,
    // is no longer part of the restore.
    this.endRestore();
    if (this.vesselForm.valid && this.componentsLoaded) {
      this.emitFormValues();
    }
  }

  /**
   * Handle weather input changes - triggers recalculation
   */
  onWeatherChanged(data: { trueWindSpeed: number; windAngleRelVessel: number }): void {
    if (this.vesselForm.valid && this.componentsLoaded) {
      this.emitFormValues();
    }
  }

  getTotalHours(): number {
    const formValue = this.vesselForm.value;
    const transitHours = Number(formValue.transitHours) || 0;
    const dpHours = Number(formValue.dpHours) || 0;
    const portHours = Number(formValue.portHours) || 0;
    const anchorHours = Number(formValue.anchorHours) || 0;
    const maneuveringHours = Number(formValue.maneuveringHours) || 0;
    return transitHours + dpHours + portHours + anchorHours + maneuveringHours;
  }
}