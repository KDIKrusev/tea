using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Models;

/// <summary>
/// Validation warning for configuration issues
/// </summary>
public class ValidationWarning
{
    public string Type { get; set; } = string.Empty; // main-engine, aux-engine, shaft-generator, shaft-capacity, hotel-load
    public string? Field { get; set; } // optional field name for frontend form highlighting
    public string Message { get; set; } = string.Empty;
    public WarningSeverity Severity { get; set; } = WarningSeverity.Warning;
}
