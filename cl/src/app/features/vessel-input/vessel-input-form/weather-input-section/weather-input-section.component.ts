import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatCheckboxModule, MatCheckboxChange } from '@angular/material/checkbox';
import { FormEditTrackerService } from '../form-edit-tracker.service';
import { FormValidationService } from '../../../../shared/services';
import { SailContributionResult } from '../../../../calculations/calculator.types';

@Component({
  selector: 'app-weather-input-section',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatExpansionModule,
    MatCheckboxModule
  ],
  templateUrl: './weather-input-section.component.html',
  styleUrl: './weather-input-section.component.css'
})
export class WeatherInputSectionComponent {
  @Input() parentForm!: FormGroup;
  @Input() sailContribution: SailContributionResult | null = null;
  /**
   * "A weather field changed — recalculate."
   *
   * Deliberately carries no payload: the values live on the shared form (`sailEnabled`,
   * `trueWindSpeed`, `windAngleRelVessel`), and those controls are what reach the request. The
   * event used to duplicate all three, and the parent read none of them.
   */
  @Output() weatherChanged = new EventEmitter<void>();

  windSpeedOptions = Array.from({ length: 21 }, (_, i) => i); // 0–20 m/s
  isSailEnabled = false;

  private cdr = inject(ChangeDetectorRef);
  protected editTracker = inject(FormEditTrackerService);
  private validationService = inject(FormValidationService);

  get sailEnabled(): AbstractControl | null { return this.parentForm.get('sailEnabled'); }
  get trueWindSpeed(): AbstractControl | null { return this.parentForm.get('trueWindSpeed'); }
  get windAngleRelVessel(): AbstractControl | null { return this.parentForm.get('windAngleRelVessel'); }

  getValidationError(controlName: string): string {
    return this.validationService.getErrorMessage(this.parentForm.get(controlName));
  }

  isFieldEdited(fieldName: string): boolean {
    const control = this.parentForm.get(fieldName);
    if (!control) {return false;}
    return this.editTracker.isFieldEdited(fieldName, control.value);
  }

onSailToggle(event: MatCheckboxChange): void {
  this.isSailEnabled = event.checked;
  if (!this.isSailEnabled) {
    this.trueWindSpeed?.reset();
    this.windAngleRelVessel?.reset();
    this.sailContribution = null;
  } else {
    this.trueWindSpeed?.setValue(0);
    this.windAngleRelVessel?.setValue(0);
  }
  this.cdr.markForCheck();
  this.emitChange();
}

  onFieldChange(): void {
    this.emitChange();
  }

  private emitChange(): void {
    this.weatherChanged.emit();
  }
}