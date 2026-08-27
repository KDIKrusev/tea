namespace VoyageEnergyAdvisor.Core.Services.SailContributionService;

public interface ISailContributionService
{
    public double GetSailContributionPower(double apparentWindSpeed, double apparentWindDirection, double sog);
}