import { Injectable } from '@angular/core';
import { FormGroup } from '@angular/forms';

/**
 * The fields an operational profile writes. Re-baselined together whenever one is applied, so the
 * "(edited)" badge means "the user changed this", not "the vessel type supplied it".
 */
export const OPERATIONAL_PROFILE_FIELDS: readonly string[] = [
  'dpHours', 'dpHotelPowerKW', 'requiredDPPowerKW', 'dpWeatherCondition',
  'transitHours', 'transitHotelPowerKW',
  'portHotelPowerKW', 'portHours',
  'anchorHotelPowerKW', 'anchorHours',
  'maneuveringPropulsionPowerKW', 'maneuveringHotelPowerKW', 'maneuveringHours',
  'meCount', 'aeCount'
];

@Injectable({
  providedIn: 'root'
})
export class FormEditTrackerService {
  private originalValues = new Map<string, unknown>();

  /**
   * Re-baseline a set of fields to whatever the form currently holds.
   *
   * Everything programmatic — a vessel-type cascade, a restored profile — has to do this, or the
   * values it just wrote read back as the user's own edits. This was fifteen hand-written
   * `updateOriginalValue(field, form.get(field)?.value)` pairs across two call sites, where the
   * only thing that ever varied was the field list.
   *
   * Omitting `fields` re-baselines every control, which is what a full profile restore needs.
   */
  rebaseline(form: FormGroup, fields?: readonly string[]): void {
    for (const name of fields ?? Object.keys(form.controls)) {
      const control = form.get(name);
      if (control) {
        this.updateOriginalValue(name, control.value);
      }
    }
  }

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
