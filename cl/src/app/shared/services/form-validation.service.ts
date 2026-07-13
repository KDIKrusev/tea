import { Injectable } from '@angular/core';
import { AbstractControl, ValidationErrors } from '@angular/forms';

/**
 * Shared service for form validation error messages
 * Provides consistent error messages across all form components
 */
@Injectable({
  providedIn: 'root'
})
export class FormValidationService {

  /**
   * Get human-readable error message for a form control
   * @param control The form control to check for errors
   * @returns Error message string, or empty string if no errors
   */
  getErrorMessage(control: AbstractControl | null): string {
    if (!control || !control.errors || !control.touched) {
      return '';
    }

    const errors: ValidationErrors = control.errors;

    // Required field
    if (errors['required']) {
      return 'This field is required';
    }

    // Minimum value
    if (errors['min']) {
      const min = errors['min'].min;
      return `Minimum value is ${min}`;
    }

    // Maximum value
    if (errors['max']) {
      const max = errors['max'].max;
      return `Maximum value is ${max}`;
    }

    // Default fallback
    return 'Invalid value';
  }

  /**
   * Check if a control has errors and should show error message
   * @param control The form control to check
   * @returns True if control has errors and has been touched
   */
  shouldShowError(control: AbstractControl | null): boolean {
    return !!(control && control.invalid && control.touched);
  }
}
