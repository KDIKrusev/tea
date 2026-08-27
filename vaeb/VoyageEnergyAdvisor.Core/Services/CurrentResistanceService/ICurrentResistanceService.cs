namespace VoyageEnergyAdvisor.Core.Services.CurrentResistanceService;

public interface ICurrentResistanceService
{
    public double GetCurrentResistancePower(double relativeCurrentSpeed, double relativeCurrentDirection, double speedOverGround);
}