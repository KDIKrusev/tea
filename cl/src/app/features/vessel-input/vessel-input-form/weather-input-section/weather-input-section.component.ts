import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
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
  @Output() weatherChanged = new EventEmitter<{ sailEnabled: boolean; trueWindSpeed: number; windAngleRelVessel: number }>();

  windSpeedOptions = Array.from({ length: 21 }, (_, i) => i); // 0–20 m/s
  isSailEnabled = false;

  private cdr = inject(ChangeDetectorRef);
  protected editTracker = inject(FormEditTrackerService);
  private validationService = inject(FormValidationService);

  get sailEnabled() { return this.parentForm.get('sailEnabled'); }
  get trueWindSpeed() { return this.parentForm.get('trueWindSpeed'); }
  get windAngleRelVessel() { return this.parentForm.get('windAngleRelVessel'); }

  getValidationError(controlName: string): string {
    return this.validationService.getErrorMessage(this.parentForm.get(controlName));
  }

  isFieldEdited(fieldName: string): boolean {
    const control = this.parentForm.get(fieldName);
    if (!control) return false;
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
    this.weatherChanged.emit({
      sailEnabled: this.isSailEnabled,
      trueWindSpeed: this.trueWindSpeed?.value,
      windAngleRelVessel: this.windAngleRelVessel?.value
    });
  }
}