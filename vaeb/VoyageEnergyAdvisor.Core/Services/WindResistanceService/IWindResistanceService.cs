namespace VoyageEnergyAdvisor.Core.Services.WindResistanceService;

public interface IWindResistanceService
{
    public double GetWindResistancePower(double apparentWindSpeed, double apparentWindDirection, double sog);
}