namespace KSailCalc.Api.Models;

/// <summary>
/// Result of input validation
/// </summary>
public class ValidationResult
{
    public bool Valid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
}
