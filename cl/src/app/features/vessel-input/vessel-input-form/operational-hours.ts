import { VesselFormValue } from './vessel-form.mapper';

/** The five operational modes, in the order the form and the results panels present them. */
const MODE_HOUR_FIELDS = ['transitHours', 'dpHours', 'portHours', 'anchorHours', 'maneuveringHours'] as const;

const MODE_HOTEL_FIELDS: ReadonlyArray<readonly [hours: string, hotelPower: string]> = [
  ['transitHours', 'transitHotelPowerKW'],
  ['dpHours', 'dpHotelPowerKW'],
  ['portHours', 'portHotelPowerKW'],
  ['anchorHours', 'anchorHotelPowerKW'],
  ['maneuveringHours', 'maneuveringHotelPowerKW']
];

function num(formValue: VesselFormValue, field: string): number {
  return Number(formValue[field]) || 0;
}

/** Total annual hours across all five modes. Drives the mode-share bars and the hours warning. */
export function totalOperationalHours(formValue: VesselFormValue): number {
  return MODE_HOUR_FIELDS.reduce((sum, field) => sum + num(formValue, field), 0);
}

/**
 * Hotel load averaged over the year, weighted by the hours spent in each mode.
 *
 * Returns `null` when there is nothing to average — no hours, or no load — so the caller can leave
 * the field alone rather than writing a zero. That distinction is the original behaviour: the old
 * in-component version guarded with `if (weightedAverage > 0)` before patching.
 *
 * Extracted from `VesselInputFormComponent` (story C-G). Pure arithmetic over form values, moved
 * verbatim — including `Math.round`, which is what the disabled `hotelLoad` control has always
 * shown.
 */
export function weightedAverageHotelLoad(formValue: VesselFormValue): number | null {
  const totalHours = totalOperationalHours(formValue);
  if (totalHours <= 0) {
    return null;
  }

  const totalWeightedPower = MODE_HOTEL_FIELDS.reduce(
    (sum, [hoursField, powerField]) => sum + num(formValue, powerField) * num(formValue, hoursField),
    0
  );

  const weightedAverage = totalWeightedPower / totalHours;
  return weightedAverage > 0 ? Math.round(weightedAverage) : null;
}
