import { Component, Input, ChangeDetectionStrategy, ChangeDetectorRef, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatCheckboxModule, MatCheckboxChange } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, takeUntil } from 'rxjs';
import { FormEditTrackerService } from '../form-edit-tracker.service';
import { FormValidationService } from '../../../../shared/services';
import { BatteryDetails } from '../../../../calculations/calculator.types';

/**
 * Battery configuration section (task sketch): Capacity [kWh], Power [kW],
 * computed Functions (Spinning Reserve / Peak Shaving — read-only, from the backend
 * allocation), and Relevant Modes checkboxes (Transit / DP / Port).
 */
@Component({
  selector: 'app-battery-config-section',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatExpansionModule,
    MatCheckboxModule,
    MatTooltipModule
  ],
  templateUrl: './battery-config-section.component.html',
  styleUrl: './battery-config-section.component.css'
})
export class BatteryConfigSectionComponent implements OnInit, OnDestroy {
  @Input() parentForm!: FormGroup;
  /** Battery contribution from the last calculation (null before the first result / when inactive) */
  @Input() batteryDetails: BatteryDetails | null = null;
  /** Whether a calculation result exists at all (drives the "no effect" hint) */
  @Input() hasResults = false;

  isBatteryEnabled = false;
  isDpModeAvailable = false;

  private destroy$ = new Subject<void>();
  private cdr = inject(ChangeDetectorRef);
  protected editTracker = inject(FormEditTrackerService);
  private validationService = inject(FormValidationService);

  get batteryPowerKw(): AbstractControl | null { return this.parentForm.get('batteryPowerKw'); }
  get batteryCapacityKwh(): AbstractControl | null { return this.parentForm.get('batteryCapacityKwh'); }
  get batteryMaxPtiKw(): AbstractControl | null { return this.parentForm.get('batteryMaxPtiKw'); }

  ngOnInit(): void {
    this.isBatteryEnabled = !!this.parentForm.get('batteryEnabled')?.value;
    this.updateDpAvailability(this.parentForm.get('dpHours')?.value);

    // DP relevant-mode checkbox follows the DP operational mode (backend rejects DP battery
    // without DP mode enabled)
    this.parentForm.get('dpHours')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(value => {
        this.updateDpAvailability(value);
        this.cdr.markForCheck();
      });

    // Profile load patches batteryEnabled without emitting a checkbox event
    this.parentForm.get('batteryEnabled')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(value => {
        this.isBatteryEnabled = !!value;
        this.cdr.markForCheck();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onBatteryToggle(event: MatCheckboxChange): void {
    this.isBatteryEnabled = event.checked;
    if (!this.isBatteryEnabled) {
      this.parentForm.patchValue({
        batteryPowerKw: null,
        batteryCapacityKwh: null,
        batteryModeTransit: false,
        batteryModeDp: false,
        batteryModePort: false,
        batteryMaxPtiKw: null,
        batteryDpRedundancyKw: null,
        batteryMissionMaxKw: null,
        batteryOthersMaxKw: null
      });
    } else {
      // ADR-5: suggest the SG rating as PTI capacity (the same shaft machine in motor mode);
      // only when the user hasn't already entered a value
      const patch: Record<string, unknown> = { batteryModeTransit: true };
      if (this.parentForm.get('batteryMaxPtiKw')?.value == null) {
        const sgCapacity = Number(this.parentForm.get('sgCapacityPerEngine')?.value) || 0;
        if (sgCapacity > 0) {patch['batteryMaxPtiKw'] = sgCapacity;}
      }
      this.parentForm.patchValue(patch);
    }
    this.cdr.markForCheck();
  }

  getValidationError(controlName: string): string {
    return this.validationService.getErrorMessage(this.parentForm.get(controlName));
  }

  isFieldEdited(fieldName: string): boolean {
    const control = this.parentForm.get(fieldName);
    if (!control) {return false;}
    return this.editTracker.isFieldEdited(fieldName, control.value);
  }

  /** Battery enabled and calculated, but nothing allocated (no modes / zero hours) */
  get showNoEffectHint(): boolean {
    return this.isBatteryEnabled
      && this.hasResults
      && !this.batteryDetails
      && Number(this.batteryPowerKw?.value) > 0;
  }

  /**
   * Re-evaluate DP availability from the current form state. Called from the parent form
   * after profile restore, which patches with emitEvent:false (the dpHours subscription
   * does not fire on that path).
   */
  refreshDpAvailability(): void {
    this.updateDpAvailability(this.parentForm.get('dpHours')?.value);
    this.isBatteryEnabled = !!this.parentForm.get('batteryEnabled')?.value;
    this.cdr.markForCheck();
  }

  private updateDpAvailability(dpHours: unknown): void {
    this.isDpModeAvailable = Number(dpHours) > 0;
    const dpControl = this.parentForm.get('batteryModeDp');
    if (!dpControl) {return;}

    // Reactive forms ignore the [disabled] attribute binding — drive state via the control
    if (this.isDpModeAvailable && dpControl.disabled) {
      dpControl.enable({ emitEvent: false });
    } else if (!this.isDpModeAvailable) {
      if (dpControl.value) {
        dpControl.setValue(false); // emits → recalculation without the DP mode
      }
      if (dpControl.enabled) {
        dpControl.disable({ emitEvent: false });
      }
    }
  }
}
