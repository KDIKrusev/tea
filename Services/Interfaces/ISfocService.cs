using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Services.Interfaces;

public interface ISfocService
{
    Task<double> GetSfocForLoadAsync(decimal loadPercentage, EngineCategory engineType, int engineTypeId);

    /// <summary>
    /// Pre-fetch sorted SFOC working points for a given engine type.
    /// Use with <see cref="InterpolateSfoc"/> for synchronous lookups in tight loops.
    /// </summary>
    Task<List<SfocDataPoint>> GetSfocDataAsync(EngineCategory engineType, int engineTypeId);

}