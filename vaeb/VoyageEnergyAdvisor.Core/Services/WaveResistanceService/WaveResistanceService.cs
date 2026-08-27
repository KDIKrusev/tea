namespace VoyageEnergyAdvisor.Core.Services.WaveResistanceService;

public class WaveResistanceService() : IWaveResistanceService
{
    public double GetWaveResistancePower(double wavePeriod, double waveHeight, double apparentWaveDirection, double sog)
    {
        return 0; // TODO implement
        throw new NotImplementedException();
    }
}

/*public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddWavePowerToRouteSegments(
    IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
{
    var distBowToMaxBreadthWaterline = config.Value.VesselLength * 0.05; // Todo. Assumption. Make this config.

    var context = new WaveResistanceCalculationContext(
        new StaWave1(
            new StaWave1Config((double)config.Value.VesselBreadth, (double)config.Value.VesselLength, (double)distBowToMaxBreadthWaterline)));

    return voyageOptions.Select(voyageOption =>
    {
        voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
        {
            if (routeSegment.RelativeWeather != null)
            {
                routeSegment.AvgWavePower = context.PerformCalculation(
                    routeSegment.AverageSpeed.GetValueOrDefault(),
                    routeSegment.RelativeWeather.WaveHeight.GetValueOrDefault(),
                    routeSegment.RelativeWeather.WaveDirection.GetValueOrDefault());
            }
            return routeSegment;
        }).ToList();
        return voyageOption;
    });
}*/
