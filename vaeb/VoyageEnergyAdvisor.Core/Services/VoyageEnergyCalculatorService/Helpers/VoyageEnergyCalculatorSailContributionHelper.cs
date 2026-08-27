namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Helpers;

public class VoyageEnergyAdvisorSailContributionHelper
{
    public double GetSailContributionWatt(double relativeWindSpeedMeterPerSecond, double relativeWindToDirectionDeg, SailConfiguration sailConfiguration)
    {
        return GetClosestSailForce(relativeWindToDirectionDeg, sailConfiguration) * relativeWindSpeedMeterPerSecond - sailConfiguration.SailActivePowerWatt;
    }
    
    private double GetClosestSailForce(double relativeWindToDirectionDeg, SailConfiguration sailConfiguration)
    {
        // Find the SailContribution with the closest RelativeWindToDirectionDeg
        var closestSailContribution = sailConfiguration.SailContributions
            .OrderBy(sc => Math.Abs(sc.RelativeWindToDirectionDeg - relativeWindToDirectionDeg))
            .FirstOrDefault();

        // Return the SailForwardForceNewtonPerMeterPerSecond of the closest SailContribution
        return closestSailContribution?.SailForwardForceNewtonPerMeterPerSecond ?? 0;
    }
}

public record SailConfiguration
{
    public double SailActivePowerWatt { get; init; }
    public required IEnumerable<SailContribution> SailContributions { get; init; }    
}

public record SailContribution
{
    public double SailForwardForceNewtonPerMeterPerSecond { get; init; } // Ns / m
    public double RelativeWindToDirectionDeg { get; init; }
}