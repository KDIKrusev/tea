import { CalculatorInput } from '../../../calculations/calculator.types';
import { DEFAULT_FUEL } from '../../../shared/constants';

/**
 * A saved profile's values → the form patch that restores them. The inverse of
 * `vessel-form.mapper.ts`, and pure for the same reason.
 *
 * Extracted from `VesselInputFormComponent` (story C-G). Every entry moved verbatim, including the
 * `?? null` defaults: patching `undefined` leaves a control at whatever the vessel-type cascade
 * put there a moment earlier, so a profile that omits a field must actively clear it. That is the
 * difference between "the profile says nothing about DP hours" and "the profile says the previous
 * vessel's DP hours", and it is why `?? null` appears on almost every optional field.
 */
export function profileToFormPatch(profile: CalculatorInput): Record<string, unknown> {
  return {
    propulsionPower: profile.propulsionPower,
    hotelLoad: profile.hotelLoad,
    seaMargin: profile.seaMargin,
    meCapacityPerEngine: profile.meCapacityPerEngine,
    meCount: profile.meCount,
    sgCapacityPerEngine: profile.sgCapacityPerEngine,
    aeCapacityPerEngine: profile.aeCapacityPerEngine,
    aeCount: profile.aeCount,
    mainEngineTypeId: profile.mainEngineTypeId,
    auxEngineTypeId: profile.auxEngineTypeId,
    sailInstalled: profile.sailInstalled ? 'Yes' : 'No',
    batteryCapacity: profile.batteryCapacity,
    batteryEnabled: !!profile.battery && profile.battery.powerKw > 0,
    batteryPowerKw: profile.battery?.powerKw ?? null,
    batteryCapacityKwh: profile.battery?.capacityKwh ?? null,
    batteryModeTransit: profile.battery?.relevantModes?.includes('Transit') ?? false,
    batteryModeDp: profile.battery?.relevantModes?.includes('DP') ?? false,
    batteryModePort: profile.battery?.relevantModes?.includes('Port') ?? false,
    batteryMaxPtiKw: profile.maxPtiPerEngineKw ?? null,
    batteryDpRedundancyKw: profile.dpRedundancyRequirementKw ?? null,
    batteryMissionMaxKw: profile.missionHeavyConsumerMaxKw ?? null,
    fuelPrice: profile.fuelPrice,
    mainFuelType: profile.mainFuelType ?? DEFAULT_FUEL,
    auxFuelType: profile.auxFuelType ?? DEFAULT_FUEL,
    hotelLoadVariationKw: profile.hotelLoadVariationKw ?? null,
    transitHours: profile.transitHours ?? null,
    transitHotelPowerKW: profile.transitHotelPowerKW ?? null,
    dpHours: profile.dpHours ?? null,
    dpHotelPowerKW: profile.dpHotelPowerKW ?? null,
    requiredDPPowerKW: profile.requiredDPPowerKW ?? null,
    dpWeatherCondition: profile.dpWeatherCondition ?? null,
    portHotelPowerKW: profile.portHotelPowerKW ?? null,
    portHours: profile.portHours ?? null,
    anchorHotelPowerKW: profile.anchorHotelPowerKW ?? null,
    anchorHours: profile.anchorHours ?? null,
    maneuveringPropulsionPowerKW: profile.maneuveringPropulsionPowerKW ?? null,
    maneuveringHotelPowerKW: profile.maneuveringHotelPowerKW ?? null,
    maneuveringHours: profile.maneuveringHours ?? null,
    sailEnabled: profile.sailEnabled ?? false,
    trueWindSpeed: profile.trueWindSpeed ?? null,
    windAngleRelVessel: profile.windAngleRelVessel ?? null,
    vesselSpeedKnots: profile.vesselSpeedKnots ?? null
  };
}
