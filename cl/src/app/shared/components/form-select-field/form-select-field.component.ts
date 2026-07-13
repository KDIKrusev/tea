import { Component, Input, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { FormValidationService } from '../../services';

@Component({
  selector: 'app-form-select-field',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatSelectModule
  ],
  templateUrl: './form-select-field.component.html',
  styleUrl: './form-select-field.component.css'
})
export class FormSelectFieldComponent {
  @Input({ required: true }) formGroup!: FormGroup;
  @Input({ required: true }) controlName!: string;
  @Input({ required: true }) label!: string;
  @Input() options: Array<{ value: string | number; label: string }> = [];
  @Input() required: boolean = false;

  private validationService = inject(FormValidationService);

  get control() {
    return this.formGroup.get(this.controlName);
  }

  getValidationError(): string {
    return this.validationService.getErrorMessage(this.control);
  }
}
