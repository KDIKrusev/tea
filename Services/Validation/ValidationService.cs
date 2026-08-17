using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Api.Services.Interfaces;

namespace KSailCalc.Api.Services.Validation;

/// <summary>
/// Service for validating calculator input and system capacity
/// Ported from TypeScript calculator.service.ts and calculator.functions.ts
/// </summary>
public class ValidationService : IValidationService
{
    // Sketch modes: Transit / DP / Port (Anchor & Maneuvering excluded per Excel operation types, D4)
    private static readonly OperationalMode[] AllowedBatteryModes =
        { OperationalMode.Transit, OperationalMode.DP, OperationalMode.Port };

    /// <summary>
    /// Validate the whole input.
    ///
    /// The step order below is part of the contract: <see cref="ValidationResult.Errors"/> is
    /// returned as a list and rendered in order, and the golden 400-responses pin that sequence.
    /// The steps are therefore ordered slices of the original single method, not a topical regrouping.
    /// </summary>
    public ValidationResult ValidateInput(CalculatorInput input)
    {
        var errors = new List<string>();

        ValidatePlantAndFinancials(input, errors);
        ValidateBatteryConfiguration(input, errors);
        ValidatePtiAndExcelLoadInputs(input, errors);
        ValidateOperationalModes(input, errors);
        ValidateSail(input, errors);

        // Get capacity warnings — promote Error-severity to actual validation errors.
        // Operating-profile warnings are appended AFTER the capacity ones: the order of this list
        // is what the client renders and what the golden 400-responses pin.
        var warnings = ValidateSystemCapacity(input);
        ValidateOperatingProfile(input, warnings);
        var criticalWarnings = warnings.Where(w => w.Severity == WarningSeverity.Error);
        errors.AddRange(criticalWarnings.Select(w => w.Message));

        return new ValidationResult
        {
            Valid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    private static void ValidatePlantAndFinancials(CalculatorInput input, List<string> errors)
    {
        // Power & Load validations
        if (input.PropulsionPower <= 0)
            errors.Add("Propulsion power must be greater than 0");

        if (input.TransitHotelPowerKW <= 0)
            errors.Add("Transit hotel power must be greater than 0");

        if (input.SeaMargin < 0 || input.SeaMargin > 100)
            errors.Add("Sea margin must be between 0 and 100");

        // Main Engine validations. MeCount == 0 is a legal diesel-electric plant (Epic E1,
        // D-DE1): the AEs carry everything, so ME capacity/type stop being required.
        if (input.MeCount >= 1 && input.MeCapacityPerEngine <= 0)
            errors.Add("Main engine capacity per engine must be greater than 0");

        if (input.MeCount < 0)
            errors.Add("Number of main engines cannot be negative");

        // Shaft Generator validations
        if (input.SgCapacityPerEngine < 0)
            errors.Add("Shaft generator capacity per engine cannot be negative");

        // Auxiliary Engine validations
        if (input.AeCapacityPerEngine <= 0)
            errors.Add("Auxiliary engine capacity per engine must be greater than 0");

        if (input.AeCount < 1)
            errors.Add("Number of aux engines must be at least 1");

        // Financial validations
        if (input.FuelPrice <= 0)
            errors.Add("Fuel price must be greater than 0");

        if (input.AnnualHours <= 0)
            errors.Add("Annual hours must be greater than 0");

        // Diesel-electric plant: nothing may hang off the absent shaft (D-DE3 — blocking errors,
        // not silent zeroing). Appended after the existing checks so the pinned 400 order of
        // MeCount >= 1 inputs cannot move.
        if (PlantShape.IsDieselElectric(input) && input.SgCapacityPerEngine > 0)
            errors.Add("Shaft generators require a main engine. Set shaft generator capacity to 0 for a diesel-electric plant.");

        if (PlantShape.IsDieselElectric(input) && input.MaxPtiPerEngineKw > 0)
            errors.Add("PTI requires a main engine shaft. Clear the PTI capacity for a diesel-electric plant.");
    }

    private static void ValidateBatteryConfiguration(CalculatorInput input, List<string> errors)
    {
        // Battery configuration validation (Increment B — design §3.6)
        if (input.Battery is { } battery)
        {
            if (battery.PowerKw < 0)
                errors.Add("Battery power cannot be negative");
            if (battery.CapacityKwh < 0)
                errors.Add("Battery capacity cannot be negative");
            if (battery.PowerKw > 0 && battery.CapacityKwh <= 0)
                errors.Add("Battery capacity (kWh) is required when battery power is greater than 0");

            if (battery.RelevantModes.Except(AllowedBatteryModes).Any())
                errors.Add("Battery relevant modes must be Transit, DP or Port");

            if (battery.RelevantModes.Contains(OperationalMode.DP) && !input.DpEnabled)
                errors.Add("Battery cannot apply to DP mode when DP mode is not enabled");
        }
    }

    private static void ValidatePtiAndExcelLoadInputs(CalculatorInput input, List<string> errors)
    {
        // PTI validation (Increment C)
        if (input.MaxPtiPerEngineKw < 0)
            errors.Add("PTI capacity per engine cannot be negative");

        // Excel load inputs (Increment F)
        if ((input.DpRedundancyRequirementKw ?? 0) < 0)
            errors.Add("DP redundancy requirement cannot be negative");
        if ((input.MissionHeavyConsumerMaxKw ?? 0) < 0)
            errors.Add("Mission heavy-consumer maximum cannot be negative");
        if ((input.OthersConsumerMaxKw ?? 0) < 0)
            errors.Add("Others battery demand cannot be negative");
    }

    private static void ValidateOperationalModes(CalculatorInput input, List<string> errors)
    {
        // Transit validation
        if (input.TransitHours <= 0)
            errors.Add("Transit hours must be greater than 0");

        // Engine type ID validation (no main engine type without a main engine — Epic E1)
        if (input.MeCount >= 1 && input.MainEngineTypeId <= 0)
            errors.Add("Main engine type must be selected");
        if (input.AuxEngineTypeId <= 0)
            errors.Add("Auxiliary engine type must be selected");

        // DP mode validation (conditional)
        if (input.DpEnabled)
        {
            if ((input.DPHours ?? 0) <= 0)
                errors.Add("DP hours must be greater than 0 when DP mode is enabled");
            if ((input.DPHotelPowerKW ?? 0) <= 0)
                errors.Add("DP hotel power must be greater than 0 when DP mode is enabled");
            if ((input.RequiredDPPowerKW ?? 0) <= 0)
                errors.Add("Required DP power must be greater than 0 when DP mode is enabled");
        }
    }

    private static void ValidateSail(CalculatorInput input, List<string> errors)
    {
        // Sail validation (conditional)
        if (input.SailEnabled)
        {
            if (!input.TrueWindSpeed.HasValue || input.TrueWindSpeed.Value <= 0)
                errors.Add("True wind speed must be greater than 0 when sail is enabled");
            if (!input.WindAngleRelVessel.HasValue || input.WindAngleRelVessel.Value < 0 || input.WindAngleRelVessel.Value > 360)
                errors.Add("Wind angle must be between 0 and 360 degrees");
            if (input.VesselSpeedKnots <= 0)
                errors.Add("Vessel speed must be greater than 0 when sail is enabled");
        }
    }

    /// <summary>Hours in a (non-leap) year — the ceiling a vessel's operating profile must fit into.</summary>
    private const double HoursInAYear = 8760;

    /// <summary>
    /// Does the operating profile fit inside a year?
    ///
    /// Every annual figure the calculator reports — fuel, CO2, cost — is a per-hour rate multiplied
    /// by these hours, so a profile summing to more than 8760 h overstates all of them
    /// proportionally. Advisory rather than blocking: a small overrun is usually rounding across
    /// modes, and the user is better placed than we are to decide whether it matters.
    /// </summary>
    private static void ValidateOperatingProfile(CalculatorInput input, List<ValidationWarning> warnings)
    {
        if (input.AnnualHours <= HoursInAYear)
            return;

        warnings.Add(new ValidationWarning
        {
            Type = "operating-hours",
            Message = $"Total operating hours ({input.AnnualHours:0.#} h) exceed the {HoursInAYear:0} h in a year. " +
                      "Annual fuel, CO2 and cost figures are scaled by these hours and will be overstated. " +
                      "Check the hours entered for each mode.",
            Severity = WarningSeverity.Warning
        });
    }

    private static List<ValidationWarning> ValidateSystemCapacity(CalculatorInput input)
    {
        var warnings = new List<ValidationWarning>();

        if (PlantShape.IsDieselElectric(input))
            ValidateDieselElectricCapacity(input, warnings);
        else
            ValidateMechanicalPlantCapacity(input, warnings);

        // Battery advisory warnings (non-blocking)
        if (input.Battery is { PowerKw: > 0 } battery)
        {
            if (battery.RelevantModes.Count == 0)
            {
                warnings.Add(new ValidationWarning
                {
                    Type = "battery",
                    Message = "Battery power is configured but no relevant modes are selected — the battery will have no effect.",
                    Severity = WarningSeverity.Warning
                });
            }

            // 30-minute sustain plausibility (placeholder threshold pending Q1)
            if (battery.CapacityKwh > 0 && battery.CapacityKwh < battery.PowerKw * 0.5)
            {
                warnings.Add(new ValidationWarning
                {
                    Type = "battery",
                    Message = "Battery capacity cannot sustain the configured power for 30 minutes — consider increasing capacity or reducing power.",
                    Severity = WarningSeverity.Warning
                });
            }
        }

        // DP redundancy only participates in DP-mode allocations (Increment F)
        if ((input.DpRedundancyRequirementKw ?? 0) > 0 && !input.DpEnabled)
        {
            warnings.Add(new ValidationWarning
            {
                Type = "battery",
                Message = "DP redundancy requirement is set but DP mode is not enabled — it will have no effect.",
                Severity = WarningSeverity.Warning
            });
        }

        return warnings;
    }

    /// <summary>
    /// Diesel-electric plant (Epic E1): the ME-shaped checks would mislead — there is no shaft.
    /// One question replaces them: can the auxiliaries carry the whole electric load?
    /// The battery and DP advisories in the caller's tail still apply.
    /// </summary>
    private static void ValidateDieselElectricCapacity(CalculatorInput input, List<ValidationWarning> warnings)
    {
        if (input.EffectivePropulsionPower + input.TransitHotelPowerKW > input.TotalAeCapacity)
        {
            warnings.Add(new ValidationWarning
            {
                Type = "aux-engine",
                Message = "Auxiliary engine capacity cannot carry propulsion and hotel load. Consider reducing propulsion power, decreasing sea margin, reducing hotel/mission load or increasing auxiliary engine capacity.",
                Severity = WarningSeverity.Error
            });
        }
    }

    /// <summary>The pre-E1 capacity checks, verbatim: ME utilisation, hotel vs SG+AE, AE overload, shaft capacity.</summary>
    private static void ValidateMechanicalPlantCapacity(CalculatorInput input, List<ValidationWarning> warnings)
    {
        var meCapacityTotal = input.TotalMeCapacity;
        var sgCapacityTotal = input.TotalSgCapacity;
        var aeCapacityTotal = input.TotalAeCapacity;

        var sgPowerActual = Math.Min(input.TransitHotelPowerKW, sgCapacityTotal);
        var mePropulsionPower = input.EffectivePropulsionPower;
        var meTotalPower = mePropulsionPower + sgPowerActual;

        var meUtilization = meCapacityTotal > 0 ? (meTotalPower / meCapacityTotal) * 100 : 0;
        if (meUtilization > 100)
        {
            warnings.Add(new ValidationWarning
            {
                Type = "main-engine",
                Message = "Main engine utilization > 100%. Consider reducing propulsion power, decreasing sea margin, reduce hotel/mission load or increasing main engine capacity.",
                Severity = WarningSeverity.Error
            });
        }

        var aePowerNeeded = Math.Max(0, input.TransitHotelPowerKW - sgCapacityTotal);
        var aeUtilization = aeCapacityTotal > 0 ? (aePowerNeeded / aeCapacityTotal) * 100 : 0;

        // Check if hotel load exceeds combined SG + AE capacity
        if (input.TransitHotelPowerKW > sgCapacityTotal + aeCapacityTotal)
        {
            warnings.Add(new ValidationWarning
            {
                Type = "hotel-load",
                Message = "Hotel/mission load exceeds combined shaft generator and auxiliary engine capacity. Consider reducing hotel/mission load or increasing shaft generator capacity.",
                Severity = WarningSeverity.Error
            });
        }

        // Check if AE specifically is over 100% utilization (can co-exist with hotel-load warning)
        if (aeUtilization > 100)
        {
            warnings.Add(new ValidationWarning
            {
                Type = "aux-engine",
                Message = "Auxiliary engine utilization > 100%. Consider reducing hotel/mission load or increasing auxiliary engine capacity.",
                Severity = WarningSeverity.Error
            });
        }

        if (input.SgCapacityPerEngine > input.MeCapacityPerEngine)
        {
            warnings.Add(new ValidationWarning
            {
                Type = "shaft-capacity",
                Message = "Shaft generator capacity cannot exceed main engine capacity.",
                Severity = WarningSeverity.Error
            });
        }
    }
}
