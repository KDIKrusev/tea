namespace KSailCalc.Api.Models;

/// <summary>
/// Summary of Level 2 optimization included in the API response.
/// </summary>
public class Level2Details
{
    public List<GeneratorSetpoint> OptimalSetpoints { get; set; } = new();
    public double OptimalTotalSfoc { get; set; }
    public double SavingsTonPerHour { get; set; }

}
