namespace VoyageEnergyAdvisor.Core.Services.WaveResistanceService;

public interface IWaveResistanceService
{
    public double GetWaveResistancePower(double wavePeriod, double waveHeight, double apparentWaveDirection,
        double sog);
}