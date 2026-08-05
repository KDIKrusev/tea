import { Component, Input, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormEditTrackerService } from '../../../features/vessel-input/vessel-input-form/form-edit-tracker.service';
import { FormValidationService } from '../../services';

@Component({
  selector: 'app-form-input-field',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './form-input-field.component.html',
  styleUrl: './form-input-field.component.css'
})
export class FormInputFieldComponent {
  @Input({ required: true }) formGroup!: FormGroup;
  @Input({ required: true }) controlName!: string;
  @Input({ required: true }) label!: string;
  @Input() hint?: string;
  @Input() type: 'text' | 'number' = 'text';
  @Input() step?: string;
  @Input() min?: number;
  @Input() max?: number;
  @Input() required = false;
  @Input() readonly = false; 
  @Input() disabled = false;
  @Input() prefix?: string;

  protected editTracker = inject(FormEditTrackerService);
  private validationService = inject(FormValidationService);

  get control(): AbstractControl | null {
    return this.formGroup.get(this.controlName);
  }

  isFieldEdited(): boolean {
    if (!this.control) {return false;}
    return this.editTracker.isFieldEdited(this.controlName, this.control.value);
  }

  isDisabled(): boolean {
    return this.control?.disabled || this.disabled;
  }

  getValidationError(): string {
    return this.validationService.getErrorMessage(this.control);
  }
}
