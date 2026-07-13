using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace KSailCalc.Api.Services;

public class SfocService : ISfocService
{
    private readonly IAppDataAggregationService _appDataAggregationService;
    private readonly ILogger<SfocService> _logger;

    public SfocService(IAppDataAggregationService appDataAggregationService, ILogger<SfocService> logger)
    {
        _appDataAggregationService = appDataAggregationService;
        _logger = logger;
    }

    // Loads full AppInitialData (cached 24h) to access engine SFOC curves.
    // A dedicated SFOC repository would reduce coupling but add complexity
    // without measurable performance gain since the data is already cached.
    public async Task<double> GetSfocForLoadAsync(decimal loadPercentage, EngineCategory engineType, int engineTypeId)
    {
        try
        {
            var appData = await _appDataAggregationService.GetInitialAppDataAsync();

            List<SfocDataPoint>? sfocData = null;

            if (engineType == EngineCategory.Main)
            {
                sfocData = appData.EngineTypes.MainEngines
                    .FirstOrDefault(e => e.Id == engineTypeId)?.SfocData;
            }
            else if (engineType == EngineCategory.Auxiliary)
            {
                sfocData = appData.EngineTypes.AuxiliaryEngines
                    .FirstOrDefault(e => e.Id == engineTypeId)?.SfocData;
            }

            if (sfocData?.Any() == true)
            {
                var workingPoints = sfocData.Where(p => p.Load > 0).OrderBy(x => x.Load).ToList();
                return SfocInterpolationHelper.Interpolate(loadPercentage, workingPoints);
            }

            _logger.LogWarning("No SFOC data found for {EngineType} engine ID {EngineTypeId}, using fallback {Fallback} g/kWh",
                engineType, engineTypeId, SfocInterpolationHelper.DefaultSfocFallback);
            return SfocInterpolationHelper.DefaultSfocFallback;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting SFOC for {EngineType} engine ID {EngineTypeId}, using fallback {Fallback} g/kWh",
                engineType, engineTypeId, SfocInterpolationHelper.DefaultSfocFallback);
            return SfocInterpolationHelper.DefaultSfocFallback;
        }
    }

    public async Task<List<SfocDataPoint>> GetSfocDataAsync(EngineCategory engineType, int engineTypeId)
    {
        var appData = await _appDataAggregationService.GetInitialAppDataAsync();

        List<SfocDataPoint>? sfocData = engineType switch
        {
            EngineCategory.Main => appData.EngineTypes.MainEngines
                .FirstOrDefault(e => e.Id == engineTypeId)?.SfocData,
            EngineCategory.Auxiliary => appData.EngineTypes.AuxiliaryEngines
                .FirstOrDefault(e => e.Id == engineTypeId)?.SfocData,
            _ => null
        };

        if (sfocData?.Any() != true)
            return new List<SfocDataPoint>();

        return sfocData.Where(p => p.Load > 0).OrderBy(x => x.Load).ToList();
    }

}