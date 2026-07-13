using KSailCalc.Api.Models;
using KSailCalc.Api.Services.Interfaces;

namespace KSailCalc.Api.Services;

/// <summary>
/// Service for validating calculator input and system capacity
/// Ported from TypeScript calculator.service.ts and calculator.functions.ts
/// </summary>
public class ValidationService : IValidationService
{
    public ValidationResult ValidateInput(CalculatorInput input)
    {
        var errors = new List<string>();

        // Power & Load validations
        if (input.PropulsionPower <= 0)
            errors.Add("Propulsion power must be greater than 0");
        
        if (input.TransitHotelPowerKW <= 0)
            errors.Add("Transit hotel power must be greater than 0");
        
        if (input.SeaMargin < 0 || input.SeaMargin > 100)
            errors.Add("Sea margin must be between 0 and 100");
        
        // Main Engine validations
        if (input.MeCapacityPerEngine <= 0)
            errors.Add("Main engine capacity per engine must be greater than 0");
        
        if (input.MeCount < 1)
            errors.Add("Number of main engines must be at least 1");
        
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
        
        // Battery validation
        if (input.BatteryCapacity < 0)
            errors.Add("Battery capacity cannot be negative");

        // Transit validation
        if (input.TransitHours <= 0)
            errors.Add("Transit hours must be greater than 0");

        // Engine type ID validation
        if (input.MainEngineTypeId <= 0)
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

        // Get capacity warnings — promote Error-severity to actual validation errors
        var warnings = ValidateSystemCapacity(input);
        var criticalWarnings = warnings.Where(w => w.Severity == Models.Enums.WarningSeverity.Error);
        errors.AddRange(criticalWarnings.Select(w => w.Message));

        return new ValidationResult
        {
            Valid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }
    
    private List<ValidationWarning> ValidateSystemCapacity(CalculatorInput input)
    {
        var warnings = new List<ValidationWarning>();
        
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
                Severity = Models.Enums.WarningSeverity.Error
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
                Severity = Models.Enums.WarningSeverity.Error
            });
        }
        
        // Check if AE specifically is over 100% utilization (can co-exist with hotel-load warning)
        if (aeUtilization > 100)
        {
            warnings.Add(new ValidationWarning
            {
                Type = "aux-engine",
                Message = "Auxiliary engine utilization > 100%. Consider reducing hotel/mission load or increasing auxiliary engine capacity.",
                Severity = Models.Enums.WarningSeverity.Error
            });
        }
        
        if (input.SgCapacityPerEngine > input.MeCapacityPerEngine)
        {
            warnings.Add(new ValidationWarning
            {
                Type = "shaft-capacity",
                Message = "Shaft generator capacity cannot exceed main engine capacity.",
                Severity = Models.Enums.WarningSeverity.Error
            });
        }
        
        return warnings;
    }
}
