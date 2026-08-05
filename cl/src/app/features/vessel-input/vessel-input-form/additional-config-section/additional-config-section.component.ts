import { Component, Input, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { FormEditTrackerService } from '../form-edit-tracker.service';
import { FormValidationService } from '../../../../shared/services';
import { FormInputFieldComponent } from '../../../../shared/components/form-input-field/form-input-field.component';

@Component({
  selector: 'app-additional-config-section',
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
    FormInputFieldComponent
  ],
  templateUrl: './additional-config-section.component.html',
  styleUrl: './additional-config-section.component.css'
})
export class AdditionalConfigSectionComponent {
  @Input() parentForm!: FormGroup;
  @Input() sailOptions: Array<{ value: string; label: string }> = [];
  @Input() totalHours = 0;

  protected editTracker = inject(FormEditTrackerService);
  private validationService = inject(FormValidationService);

  // Getter methods for form controls - Additional Systems
  get sailInstalled(): AbstractControl | null { return this.parentForm.get('sailInstalled'); }
  get batteryCapacity(): AbstractControl | null { return this.parentForm.get('batteryCapacity'); }
  
  // Getter methods for form controls - Financial Parameters
  get fuelPrice(): AbstractControl | null { return this.parentForm.get('fuelPrice'); }
  // NOTE: annualHours removed - auto-calculated from operational mode hours

  // Get validation error message for form control
  getValidationError(controlName: string): string {
    return this.validationService.getErrorMessage(this.parentForm.get(controlName));
  }

  isFieldEdited(fieldName: string): boolean {
    const control = this.parentForm.get(fieldName);
    if (!control) {return false;}
    
    return this.editTracker.isFieldEdited(fieldName, control.value);
  }
}
