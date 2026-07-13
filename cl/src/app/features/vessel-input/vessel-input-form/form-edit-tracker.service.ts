import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class FormEditTrackerService {
  private originalValues: Map<string, unknown> = new Map();

  /**
   * Store the original value for a field
   */
  setOriginalValue(fieldName: string, value: unknown): void {
    if (!this.originalValues.has(fieldName)) {
      this.originalValues.set(fieldName, value);
    }
  }

  /**
   * Update the original value (e.g., when changing engine type)
   */
  updateOriginalValue(fieldName: string, value: unknown): void {
    this.originalValues.set(fieldName, value);
  }

  /**
   * Check if a field has been edited
   */
  isFieldEdited(fieldName: string, currentValue: unknown): boolean {
    const originalValue = this.originalValues.get(fieldName);
    
    // If no original value set yet, field is not edited
    if (originalValue === undefined || originalValue === null) {
      return false;
    }

    // Compare current value with original (convert to numbers for numeric fields)
    const current = Number(currentValue);
    const original = Number(originalValue);
    
    const isEdited = !isNaN(current) && !isNaN(original) ? current !== original : currentValue !== originalValue;
    
    return isEdited;
  }

}
