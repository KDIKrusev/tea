import { VoyageOption } from './voyage-option.model';

/**
 * One ETD/ETA slot with both ways of sailing it:
 *  - variablePowerOption: constant speed, propulsion power varies with the weather (the classic option).
 *  - variableSpeedOption: constant propulsion power, speed varies with the weather.
 *
 * Both come from a single /update call. etd/eta/durationInSeconds/averageSpeed describe the planned
 * window; each nested option carries its own values, which for the variable-speed one can differ.
 */
export interface VoyageOptionSet {
    etd: number;
    eta: number;
    durationInSeconds: number;
    averageSpeed: number;
    isValid: boolean;

    variablePowerOption: VoyageOption;

    /** Null when this slot has no feasible constant-power solution. */
    variableSpeedOption?: VoyageOption | null;

    /** Set only when variableSpeedOption is null and a reason is known. Safe to show to the user. */
    variableSpeedUnavailableReason?: string | null;
}
