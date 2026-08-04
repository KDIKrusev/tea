namespace KSailCalc.Api.Models.Domain;

/// <summary>
/// Result of Level 2 optimization: optimal generator load setpoints and FOC/savings.
/// </summary>
public class Level2Result
{
    public List<GeneratorSetpoint> OptimalSetpoints { get; set; } = new();
    public double OptimalTotalSfoc { get; set; }
    public double Level2FocTonPerHour { get; set; }
    public double Level1FocTonPerHour { get; set; }
    public double Level2SavingsTonPerHour { get; set; }
}
