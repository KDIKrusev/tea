import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Output, Input, OnInit, OnDestroy, AfterViewInit, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { CalculatorInput, BatteryDetails } from '../../../calculations/calculator.types';
import { VesselOperationalProfile } from '../../../core/operational-profile.types';
import { debounceTime, Subject, takeUntil } from 'rxjs';
import { DEBOUNCE_TIMES } from '../../../shared/constants';
import { VesselConfigSectionComponent, VesselDataApplied } from './vessel-config-section/vessel-config-section.component';
import { EngineConfigSectionComponent } from './engine-config-section/engine-config-section.component';
import { AdditionalConfigSectionComponent } from './additional-config-section/additional-config-section.component';
import { OperationalModesSectionComponent } from './operational-modes-section/operational-modes-section.component';
import { WeatherInputSectionComponent } from './weather-input-section/weather-input-section.component';
import { BatteryConfigSectionComponent } from './battery-config-section/battery-config-section.component';
import { FormEditTrackerService, OPERATIONAL_PROFILE_FIELDS } from './form-edit-tracker.service';
import { SailContributionResult } from '../../../calculations/calculator.types';
import { ProfileService } from '../../../core/profile.service';
import { SavedProfile } from '../../../core/profile.types';
import { AppDataService } from '../../../core/app-data.service';
import { buildVesselForm } from './vessel-form.schema';
import { VesselFormValue, buildCalculatorInput, sameCalculatorInput } from './vessel-form.mapper';
import { profileToFormPatch } from './profile-patch';
import { totalOperationalHours, weightedAverageHotelLoad } from './operational-hours';
import { defaultVariationForVessel } from './vessel-variation';
import { DraftAutosave, DraftSnapshot } from './draft-autosave';

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
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './vessel-input-form.component.html',
  styleUrl: './vessel-input-form.component.css'
})
export class VesselInputFormComponent implements OnInit, OnDestroy, AfterViewInit {
  private fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

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
  vesselTypeName = '';
  private destroy$ = new Subject<void>();
  private editTracker = inject(FormEditTrackerService);
  private profileService = inject(ProfileService);
  private appDataService = inject(AppDataService);

  /** The load in progress, or null when the form is settled and free to emit. */
  private loadSequence: LoadSequence | null = null;

  /** The last input actually emitted — a rebuild equal to it carries no news and is dropped. */
  private lastEmittedInput: CalculatorInput | null = null;

  private readonly draftAutosave = new DraftAutosave(this.profileService);

  // Dropdown options
  sailOptions = [
    { value: 'No', label: 'No' },
    { value: 'Yes', label: 'Yes' }
  ];

  ngOnInit(): void {
    this.initializeForm();
    this.setupAutoCalculation();
    this.draftAutosave.start(() => this.draftSnapshot());
    // Startup is a load like any other: it ends when the first vessel configuration lands.
    this.beginLoad('startup', null);
  }

  ngAfterViewInit(): void {
    this.restoreDraftIfAvailable();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.draftAutosave.stop();
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

  /** Keeps the disabled hotelLoad field showing the hours-weighted average of the modes. */
  private updateWeightedAverageHotelLoad(): void {
    const average = weightedAverageHotelLoad(this.vesselForm.value as VesselFormValue);
    if (average !== null) {
      this.vesselForm.patchValue({ hotelLoad: average }, { emitEvent: false });
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
    if (!mainFuelType || fuelDefaultPrices[mainFuelType] === undefined) {return;}

    const currentPrice = this.vesselForm.get('fuelPrice')?.value;
    if (this.editTracker.isFieldEdited('fuelPrice', currentPrice)) {return;} // the user's price wins

    const newPrice = fuelDefaultPrices[mainFuelType];
    if (currentPrice === newPrice) {return;} // avoid unnecessary form updates

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
    if (!options.force && this.lastEmittedInput && sameCalculatorInput(input, this.lastEmittedInput)) {
      return;
    }

    this.lastEmittedInput = input;
    this.formChanged.emit({ input, source });
  }


  getCurrentInputSnapshot(baselineIndex?: number): CalculatorInput {
    const snapshot = this.buildCalculatorInput(this.vesselForm.getRawValue());
    if (baselineIndex !== undefined) {
      return { ...snapshot, baselineIndex };
    }
    return snapshot;
  }

  /** The request body for the current form state. See vessel-form.mapper.ts. */
  private buildCalculatorInput(formValue: VesselFormValue): CalculatorInput {
    return buildCalculatorInput(formValue, this.vesselTypeName);
  }

  private initializeForm(): void {
    this.vesselForm = buildVesselForm(this.fb);
    this.editTracker.rebaseline(this.vesselForm);
  }

  // ─── AUTO-DRAFT ──────────────────────────────────────────────────────────────

  /** Returns null whenever the form is not in a state worth persisting; the service then skips. */
  private draftSnapshot(): DraftSnapshot | null {
    if (!this.vesselForm.valid || this.loadSequence || !this.vesselConfigSection) {
      return null;
    }
    const category = this.vesselConfigSection.selectedCategoryName;
    const size = this.vesselConfigSection.selectedSize;
    const speed = this.vesselConfigSection.selectedSpeed;
    if (!category || size === null || speed === null) {
      return null;
    }
    return {
      input: this.buildCalculatorInput(this.vesselForm.getRawValue()),
      vesselTypeName: this.vesselConfigSection.selectionLabel,
      vesselCategory: category,
      vesselSize: size,
      vesselSpeed: speed
    };
  }

  private restoreDraftIfAvailable(): void {
    const draft = this.draftAutosave.loadRestorable();
    if (draft) {
      this.loadProfile(draft);
    }
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

    const variationKw = defaultVariationForVessel(this.vesselTypeName);
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

    // This handler runs from an HTTP callback, and under OnPush that is not enough on its own:
    // nothing here is an @Input change or a template event, so the view would keep rendering the
    // previous vessel. Everything the template derives — vesselTypeName, getTotalHours() — depends
    // on this call. See rendered-output.spec.ts.
    this.cdr.markForCheck();
  }

  /** The fetch failed. Nothing was applied — stop waiting, so the form can calculate again. */
  onVesselDataFailed(): void {
    this.endLoad();
    this.cdr.markForCheck();
  }


  /** Step 3 of onVesselDataApplied: the vessel type's mode hours and loads. */
  private applyOperationalProfile(operationalProfile: VesselOperationalProfile): void {
    if (!this.operationalModesSection) {
      return;
    }
    this.operationalModesSection.setOperationalProfile(operationalProfile);
    // What the profile just wrote is the new baseline, not a user edit.
    this.editTracker.rebaseline(this.vesselForm, OPERATIONAL_PROFILE_FIELDS);
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

    // emitEvent:false prevents vesselSpeedKnots/vesselSize valueChanges from triggering
    // watchSizeAndSpeedInputs, which would fire a new vessel fetch 400 ms later.
    this.vesselForm.patchValue(profileToFormPatch(pending), { emitEvent: false });

    // Ensure restored fuel values are valid for the selected engine families.
    // Example: a legacy saved profile may contain HFO while the restored ME is LNG-only.
    this.engineConfigSection?.setEngineTypeReferences(
      Number(pending.mainEngineTypeId),
      Number(pending.auxEngineTypeId)
    );

    // Every restored field is original relative to the profile, not edited.
    this.editTracker.rebaseline(this.vesselForm);
    // patchValue above used emitEvent:false — the battery section's dpHours subscription
    // did not fire, so re-evaluate DP checkbox availability explicitly
    this.batteryConfigSection?.refreshDpAvailability();
    // patchValue used emitEvent:false, so the debounced recalculation of the weighted hotel load
    // did not run — do it here, while the profile's mode hours are the current ones.
    this.updateWeightedAverageHotelLoad();
    // No emission here: the caller ends the load, and that is what emits.
  }

  /** A weather field changed — the values are already on the form; just recalculate. */
  onWeatherChanged(): void {
    if (this.vesselForm.valid) {
      this.emitFormValues('user');
    }
  }

  /** Used by the template to show mode shares. */
  getTotalHours(): number {
    return totalOperationalHours(this.vesselForm.value as VesselFormValue);
  }
}