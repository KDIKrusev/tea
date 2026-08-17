import {
  BatteryConfigurationInput,
  BatteryRelevantMode,
  CalculatorInput
} from '../../../calculations/calculator.types';

/**
 * Form values → the request body. Pure, and the client's equivalent of the backend's
 * `TierResultBuilder`.
 *
 * Extracted from `VesselInputFormComponent` (story C-G) because it is the single place where the
 * shape the user sees becomes the shape the API is asked about — and because invariant I2 (the
 * frozen request bodies in `cl/src/testing/golden/`) is really an invariant about *this file*.
 * Testing it through a mounted component tree was the only way to reach it before.
 *
 * **Nothing here was retyped.** Every expression moved verbatim, including the falsy-vs-nullish
 * asymmetry: `x ? Number(x) : undefined` treats a legitimate `0` as absent, while
 * `x != null ? Number(x) : undefined` does not, and the two are used on different fields. That
 * distinction is load-bearing for the wire contract and is preserved exactly as it was found.
 */

/** The raw form value. Deliberately loose: `getRawValue()` returns whatever the controls hold. */
export type VesselFormValue = Record<string, unknown>;

export function buildCalculatorInput(
  formValue: VesselFormValue,
  vesselTypeName: string
): CalculatorInput {
  const v = formValue as Record<string, never>;
  return {
    propulsionPower: Number(v['propulsionPower']),
    hotelLoad: Number(v['hotelLoad']),
    seaMargin: Number(v['seaMargin']),
    meCapacityPerEngine: Number(v['meCapacityPerEngine']),
    meCount: Number(v['meCount']),
    sgCapacityPerEngine: Number(v['sgCapacityPerEngine']),
    aeCapacityPerEngine: Number(v['aeCapacityPerEngine']),
    aeCount: Number(v['aeCount']),
    mainEngineTypeId: Number(v['mainEngineTypeId']),
    auxEngineTypeId: Number(v['auxEngineTypeId']),
    sailInstalled: v['sailInstalled'] === 'Yes',
    batteryCapacity: Number(v['batteryCapacity']),
    battery: buildBatteryInput(formValue),
    maxPtiPerEngineKw: v['batteryMaxPtiKw'] ? Number(v['batteryMaxPtiKw']) : undefined,
    dpRedundancyRequirementKw: v['batteryDpRedundancyKw'] ? Number(v['batteryDpRedundancyKw']) : undefined,
    missionHeavyConsumerMaxKw: v['batteryMissionMaxKw'] ? Number(v['batteryMissionMaxKw']) : undefined,
    othersConsumerMaxKw: v['batteryOthersMaxKw'] ? Number(v['batteryOthersMaxKw']) : undefined,
    fuelPrice: Number(v['fuelPrice']),
    mainFuelType: v['mainFuelType'] || undefined,
    auxFuelType: v['auxFuelType'] || undefined,

    // Multi-Modal Operational Modes
    transitHours: v['transitHours'] ? Number(v['transitHours']) : undefined,
    transitHotelPowerKW: v['transitHotelPowerKW'] ? Number(v['transitHotelPowerKW']) : undefined,
    dpEnabled: !!(v['dpHours'] && Number(v['dpHours']) > 0),
    dpHours: v['dpHours'] ? Number(v['dpHours']) : undefined,
    dpHotelPowerKW: v['dpHotelPowerKW'] ? Number(v['dpHotelPowerKW']) : undefined,
    requiredDPPowerKW: v['requiredDPPowerKW'] ? Number(v['requiredDPPowerKW']) : undefined,
    dpWeatherCondition: v['dpWeatherCondition'] || undefined,
    portHotelPowerKW: v['portHotelPowerKW'] ? Number(v['portHotelPowerKW']) : undefined,
    portHours: v['portHours'] ? Number(v['portHours']) : undefined,
    anchorHotelPowerKW: v['anchorHotelPowerKW'] ? Number(v['anchorHotelPowerKW']) : undefined,
    anchorHours: v['anchorHours'] ? Number(v['anchorHours']) : undefined,
    maneuveringPropulsionPowerKW: v['maneuveringPropulsionPowerKW'] ? Number(v['maneuveringPropulsionPowerKW']) : undefined,
    maneuveringHotelPowerKW: v['maneuveringHotelPowerKW'] ? Number(v['maneuveringHotelPowerKW']) : undefined,
    maneuveringHours: v['maneuveringHours'] ? Number(v['maneuveringHours']) : undefined,

    // Vessel Type (for L3 DRC variation lookup) — held by the component, not by a control
    vesselTypeName: vesselTypeName || undefined,

    // Level 3 DRC: user-specified variation in kW for Hotel/Mission load
    hotelLoadVariationKw: v['hotelLoadVariationKw'] ? Number(v['hotelLoadVariationKw']) : undefined,

    // Weather Input
    sailEnabled: v['sailEnabled'] ?? false,
    trueWindSpeed: v['trueWindSpeed'] != null ? Number(v['trueWindSpeed']) : undefined,
    windAngleRelVessel: v['windAngleRelVessel'] != null ? Number(v['windAngleRelVessel']) : undefined,
    vesselSpeedKnots: v['vesselSpeedKnots'] ? Number(v['vesselSpeedKnots']) : 0
  };
}

/**
 * Battery payload: null unless enabled with positive power — keeps the request
 * identical to the pre-battery contract for existing users (zero-regression, AC1).
 */
export function buildBatteryInput(formValue: VesselFormValue): BatteryConfigurationInput | null {
  const v = formValue as Record<string, never>;
  if (!v['batteryEnabled']) {return null;}
  const powerKw = Number(v['batteryPowerKw']) || 0;
  if (powerKw <= 0) {return null;}

  const relevantModes: BatteryRelevantMode[] = [];
  if (v['batteryModeTransit']) {relevantModes.push('Transit');}
  if (v['batteryModeDp']) {relevantModes.push('DP');}
  if (v['batteryModePort']) {relevantModes.push('Port');}

  return {
    powerKw,
    capacityKwh: Number(v['batteryCapacityKwh']) || 0,
    relevantModes
  };
}

/**
 * Wire-level equality between two inputs.
 *
 * `buildCalculatorInput` fills one object literal, so key order is fixed and serialising is a
 * sound comparison — and it compares exactly what would be sent, `undefined` properties dropped,
 * which is the only difference that could matter.
 */
export function sameCalculatorInput(a: CalculatorInput, b: CalculatorInput): boolean {
  return JSON.stringify(a) === JSON.stringify(b);
}
