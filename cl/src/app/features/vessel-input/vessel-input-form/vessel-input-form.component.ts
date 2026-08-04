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
import { VesselOperationalProfile } from '../../../core/operational-profile.types';
import { debounceTime, Subject, takeUntil } from 'rxjs';
import { VALIDATION_LIMITS, DEBOUNCE_TIMES, DEFAULT_VALUES, DEFAULT_FUEL } from '../../../shared/constants';
import { VesselConfigSectionComponent, VesselDataApplied } from './vessel-config-section/vessel-config-section.component';
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

/**
 * A load in progress.
 *
 * This replaces four booleans and five `setTimeout` constants. The old code approximated "has the
 * cascade finished?" by waiting 200, 800, 1500 or 3000 ms and hoping; the flag that guarded a
 * restore was cleared by whichever vessel-config response happened to arrive first — including one
 * fetched for a vessel the user had already navigated away from. The restore's own response then
 * landed after the restore had declared itself over, and overwrote the profile with the vessel
 * type's defaults. See docs/refactoring/client-refactor-design.md §1.1.
 *
 * A sequence ends when the response it is waiting for has been applied — a fact, not a timeout.
 * While one is active, no form emission escapes, so a load produces exactly one calculation.
 */
interface LoadSequence {
  /** Why this load is happening; becomes the `source` of its single emission. */
  readonly source: 'startup' | 'restore';
  /** The saved values to apply on top of the vessel defaults, or null for a plain startup. */
  readonly profileInput: CalculatorInput | null;
}

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

  /** The load in progress, or null when the form is settled and free to emit. */
  private loadSequence: LoadSequence | null = null;

  /** The last input actually emitted — a rebuild equal to it carries no news and is dropped. */
  private lastEmittedInput: CalculatorInput | null = null;

  /** Timer handle for auto-draft */
  private draftTimer: ReturnType<typeof setInterval> | null = null;

  // Default DRC variation per vessel type (kW) — mirrors backend CalculatorSettings
  private readonly vesselVariationDefaults: Record<string, number> = {
    'Bulk Carrier': 250,
    'Container': 1500,
    'LNG': 1000
  };
  private readonly defaultVariationKw = 500;

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
    // Startup is a load like any other: it ends when the first vessel configuration lands.
    this.beginLoad('startup', null);
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
  }

  private setupAutoCalculation(): void {
    this.vesselForm.valueChanges
      .pipe(
        // Deliberate UX debounce: recalculate once the user stops typing, not per keystroke.
        debounceTime(DEBOUNCE_TIMES.FORM_INPUT),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.updateWeightedAverageHotelLoad();
        this.updateFuelPriceFromFuelType();

        if (this.vesselForm.valid) {
          this.emitFormValues('user');
        }
      });
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
   * Keep the fuel price on the main fuel's backend default — unless the user has typed their own.
   *
   * This runs on every debounced form change. It used to overwrite unconditionally, so a price the
   * user typed reverted ~500 ms later; the field looked editable but was not. The edit tracker
   * already knows the difference: `prefillPriceFromMainFuel` re-baselines the original whenever a
   * fuel or engine change applies a new default, so anything differing from that baseline is the
   * user's own value and must survive.
   */
  private updateFuelPriceFromFuelType(): void {
    const fuelDefaultPrices = this.appDataService.getFuelDefaultPrices();

    // For now, use main engine fuel type as the primary (could also check aux and use a weighted average)
    const mainFuelType = this.vesselForm.get('mainFuelType')?.value;
    if (!mainFuelType || fuelDefaultPrices[mainFuelType] === undefined) return;

    const currentPrice = this.vesselForm.get('fuelPrice')?.value;
    if (this.editTracker.isFieldEdited('fuelPrice', currentPrice)) return; // the user's price wins

    const newPrice = fuelDefaultPrices[mainFuelType];
    if (currentPrice === newPrice) return; // avoid unnecessary form updates

    this.vesselForm.patchValue({ fuelPrice: newPrice }, { emitEvent: false });
    // The applied default becomes the new baseline, so the next pass does not read it as an edit.
    this.editTracker.updateOriginalValue('fuelPrice', newPrice);
  }

  // ─── LOAD SEQUENCE ───────────────────────────────────────────────────────────

  /** Starts a load. A restore supersedes a startup still in flight. */
  private beginLoad(source: LoadSequence['source'], profileInput: CalculatorInput | null): void {
    this.loadSequence = { source, profileInput };
  }

  /**
   * The load is over: emit once, with the source that started it.
   *
   * `force` bypasses the value-equality check. A load is an explicit user action and must always
   * produce a calculation — re-loading the same scenario with a different pinned baseline would
   * otherwise be silently ignored.
   */
  private endLoad(): void {
    const sequence = this.loadSequence;
    this.loadSequence = null;
    if (!sequence || !this.vesselForm.valid) {
      return;
    }
    this.emitFormValues(sequence.source === 'restore' ? 'restore' : 'user', { force: true });
  }

  /**
   * Emit the current form as a calculator input — unless there is nothing to say.
   *
   * Two guards, for two different kinds of noise:
   *  - a load in progress suppresses everything, so the intermediate states of a cascade (vessel
   *    defaults, then the profile on top) never reach the results panels as separate calculations;
   *  - an input equal to the last one emitted is dropped, which absorbs the trailing debounced
   *    `valueChanges` that the cascade's own `emitEvent: true` patches queued behind it.
   */
  private emitFormValues(source: 'user' | 'restore', options: { force?: boolean } = {}): void {
    if (this.loadSequence) {
      return;
    }

    const input = this.buildCalculatorInput(this.vesselForm.getRawValue());
    if (!options.force && this.lastEmittedInput && this.sameInput(input, this.lastEmittedInput)) {
      return;
    }

    this.lastEmittedInput = input;
    this.formChanged.emit({ input, source });
  }

  /**
   * Wire-level equality. `buildCalculatorInput` fills a single object literal, so key order is
   * fixed and serialising is a sound comparison — and it compares exactly what would be sent,
   * `undefined` properties dropped, which is the only difference that could matter.
   */
  private sameInput(a: CalculatorInput, b: CalculatorInput): boolean {
    return JSON.stringify(a) === JSON.stringify(b);
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
      if (this.vesselForm.valid && !this.loadSequence && this.vesselConfigSection) {
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
   * Loads a saved profile. The values are applied when the vessel configuration for the
   * profile's own selection arrives — see onVesselDataApplied.
   */
  loadProfile(profile: SavedProfile): void {
    // Supersedes a startup load still in flight: whatever it was going to emit is no longer
    // what the user asked for.
    this.beginLoad('restore', profile.input);
    this.vesselTypeName = profile.vesselTypeName;

    if (this.vesselConfigSection) {
      this.vesselConfigSection.selectVessel(profile.vesselCategory, profile.vesselSize, profile.vesselSpeed);
    }
  }

  /**
   * The one place a vessel configuration is applied.
   *
   * Everything here runs synchronously off the HTTP response, in a fixed order:
   *   1. the vessel type's own fields (label, DRC variation)
   *   2. the engines — defaults, or references only when a profile owns the capacities
   *   3. the operational profile, if this response carries one
   *   4. the saved profile's values, on top of all of the above
   *   5. the load ends, and emits exactly once
   *
   * There is no step that waits to see whether another step is coming. That is the whole change:
   * the old code split 1–2 and 3 across two events and guessed the rest with 200 ms and 800 ms
   * timers, and a restore could therefore be completed by a response it never asked for.
   */
  onVesselDataApplied(event: VesselDataApplied): void {
    const profileInput = this.loadSequence?.profileInput ?? null;

    // 1 — Prefer the composed parametric label ("Bulk Carrier 75,000 dwt");
    //     fall back to the bucket record's name.
    this.vesselTypeName = this.vesselConfigSection?.selectionLabel || event.vesselType.vesselTypeName;

    const variationKw = this.getDefaultVariationForVessel(this.vesselTypeName);
    this.vesselForm.patchValue({ hotelLoadVariationKw: variationKw }, { emitEvent: true });
    this.editTracker.updateOriginalValue('hotelLoadVariationKw', variationKw);

    // 2 — A restore carries its own engine ids AND its own rated capacities, so the catalogue must
    //     not write capacities here: set the references so the dropdowns stay populated (both ids
    //     are required validators) and leave the ratings to step 4.
    const applyEngineDefaults = event.applyEngineDefaults && !profileInput;

    const vesselTypeWithRefs = event.vesselType as {
      mainEngine?: { engineTypeId?: number | string };
      auxEngine?: { engineTypeId?: number | string };
    };
    const mainEngineId = Number(event.mainEngineData?.id ?? vesselTypeWithRefs.mainEngine?.engineTypeId);
    const auxEngineId = Number(event.auxEngineData?.id ?? vesselTypeWithRefs.auxEngine?.engineTypeId);

    if (this.engineConfigSection && Number.isFinite(mainEngineId) && Number.isFinite(auxEngineId)) {
      if (applyEngineDefaults) {
        this.engineConfigSection.setEngineConfiguration(mainEngineId, auxEngineId);
      } else {
        this.engineConfigSection.setEngineTypeReferences(mainEngineId, auxEngineId);
      }
    }

    // 3
    if (event.operationalProfile) {
      this.applyOperationalProfile(event.operationalProfile);
    }

    // 4
    if (profileInput) {
      this.applyProfileInputValues(profileInput);
    }

    // 5
    this.endLoad();
  }

  /** The fetch failed. Nothing was applied — stop waiting, so the form can calculate again. */
  onVesselDataFailed(): void {
    this.endLoad();
  }

  private getDefaultVariationForVessel(vesselTypeName: string): number {
    for (const [key, value] of Object.entries(this.vesselVariationDefaults)) {
      if (vesselTypeName.toLowerCase().includes(key.toLowerCase())) {
        return value;
      }
    }
    return this.defaultVariationKw;
  }

  private applyOperationalProfile(operationalProfile: VesselOperationalProfile): void {
    if (this.operationalModesSection) {
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
    }
  }

  /**
   * Applies all form values from a saved profile, overriding the vessel type's defaults.
   * Step 4 of onVesselDataApplied — the profile is the authority on everything it carries.
   *
   * Uses emitEvent:false to prevent vesselSpeedKnots/vesselSize valueChanges
   * from triggering watchSizeAndSpeedInputs in vessel-config-section, which
   * would fire a new vessel fetch 400 ms later.
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
    // patchValue used emitEvent:false, so the debounced recalculation of the weighted hotel load
    // did not run — do it here, while the profile's mode hours are the current ones.
    this.updateWeightedAverageHotelLoad();
    // No emission here: the caller ends the load, and that is what emits.
  }

  /**
   * Handle weather input changes - triggers recalculation
   */
  onWeatherChanged(data: { trueWindSpeed: number; windAngleRelVessel: number }): void {
    if (this.vesselForm.valid) {
      this.emitFormValues('user');
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