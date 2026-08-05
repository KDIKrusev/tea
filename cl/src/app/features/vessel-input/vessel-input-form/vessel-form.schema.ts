import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { VALIDATION_LIMITS, DEFAULT_VALUES, DEFAULT_FUEL } from '../../../shared/constants';

/**
 * The shape of the calculator form — declaration only, no behaviour.
 *
 * Extracted from `VesselInputFormComponent` (story C-G): the component's job is deciding *when*
 * values are written and emitted, not listing seventy controls. Keeping the schema next to the
 * cascade logic made both harder to read than either is on its own.
 *
 * Every control, default and validator is unchanged from the original `fb.group(...)` call.
 */
export function buildVesselForm(fb: FormBuilder): FormGroup {
  return fb.group({
    // Power and Load
    propulsionPower: [null, [Validators.min(VALIDATION_LIMITS.POWER.MIN)]],
    hotelLoad: [null, [Validators.min(VALIDATION_LIMITS.POWER.MIN)]],
    seaMargin: [null, [Validators.min(VALIDATION_LIMITS.SEA_MARGIN.MIN), Validators.max(VALIDATION_LIMITS.SEA_MARGIN.MAX)]],

    // Main Engine — rated power is a required manual input (ratings always differ)
    meCapacityPerEngine: [null, [Validators.required, Validators.min(VALIDATION_LIMITS.POWER.MIN_POSITIVE)]],
    meCount: [DEFAULT_VALUES.ME_COUNT, [Validators.min(VALIDATION_LIMITS.COUNT.MIN)]],

    // Shaft Generator — optional (0 = no shaft generator)
    sgCapacityPerEngine: [null, [Validators.min(VALIDATION_LIMITS.POWER.MIN)]],

    // Auxiliary Engine — rated power is a required manual input
    aeCapacityPerEngine: [null, [Validators.required, Validators.min(VALIDATION_LIMITS.POWER.MIN_POSITIVE)]],
    aeCount: [DEFAULT_VALUES.AE_COUNT, [Validators.min(VALIDATION_LIMITS.COUNT.MIN)]],

    // Engine Type IDs
    mainEngineTypeId: [null, [Validators.required]],
    auxEngineTypeId: [null, [Validators.required]],

    // Additional Systems
    sailInstalled: ['No'],
    batteryCapacity: [DEFAULT_VALUES.BATTERY_CAPACITY, [Validators.min(VALIDATION_LIMITS.POWER.MIN)]],

    // Battery configuration (Increment D — sketch: Capacity/Power + Relevant Modes)
    batteryEnabled: [false],
    batteryPowerKw: [null, [Validators.min(0)]],
    batteryCapacityKwh: [null, [Validators.min(0)]],
    batteryModeTransit: [false],
    batteryModeDp: [false],
    batteryModePort: [false],
    // PTI capacity per ME (Increment C — 0/empty = PTI not modelled)
    batteryMaxPtiKw: [null, [Validators.min(0)]],
    // Excel load inputs (Increment F): DP redundancy (RESERVE) + mission heavy-consumer max
    batteryDpRedundancyKw: [null, [Validators.min(0)]],
    batteryMissionMaxKw: [null, [Validators.min(0)]],

    // Financial
    fuelPrice: [DEFAULT_VALUES.FUEL_PRICE, [Validators.min(VALIDATION_LIMITS.FUEL_PRICE.MIN)]],

    // Fuel types (Epic 3) — per-engine; default MGO
    mainFuelType: [DEFAULT_FUEL],
    auxFuelType: [DEFAULT_FUEL],

    // Multi-Modal Operational Modes
    transitHours: [null, [Validators.min(0)]],
    transitHotelPowerKW: [null, [Validators.min(0)]],
    hotelLoadVariationKw: [null, [Validators.min(0)]],
    dpHours: [null, [Validators.min(0)]],
    dpHotelPowerKW: [null, [Validators.min(0)]],
    requiredDPPowerKW: [null, [Validators.min(0)]],
    dpWeatherCondition: [null],
    portHotelPowerKW: [null, [Validators.min(0)]],
    portHours: [null, [Validators.min(0)]],
    anchorHotelPowerKW: [null, [Validators.min(0)]],
    anchorHours: [null, [Validators.min(0)]],
    maneuveringPropulsionPowerKW: [null, [Validators.min(0)]],
    maneuveringHotelPowerKW: [null, [Validators.min(0)]],
    maneuveringHours: [null, [Validators.min(0)]],

    // Weather Input
    sailEnabled: [false],
    trueWindSpeed: [null, [Validators.min(0), Validators.max(20)]],
    windAngleRelVessel: [null, [Validators.min(0), Validators.max(360)]],
    vesselSpeedKnots: [null],

    // Parametric vessel selection (Epic 1) — validators applied per category
    // by VesselConfigSectionComponent
    vesselCategory: [null],
    vesselSize: [null, [Validators.min(1)]]
  });
}
