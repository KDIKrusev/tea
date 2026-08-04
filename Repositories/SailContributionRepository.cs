using KSailCalc.Api.Models;
using KSailCalc.Api.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace KSailCalc.Api.Repositories;

/// <summary>
/// Data access for sail contribution lookup table.
/// Separate from KSailCalcConfigRepository because it reads from a DIFFERENT table:
/// [VoyageEnergyDB].[dbo].[Configurations] (not KSailCalc_Configurations).
/// </summary>
public sealed class SailContributionRepository : BaseRepository, ISailContributionRepository, IDisposable
{
    private List<SailContributionItem>? _cachedItems;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public SailContributionRepository(IConfiguration configuration) : base(configuration) { }

    public async Task<List<SailContributionItem>?> GetSailContributionItemsAsync()
    {
        if (_cachedItems != null)
            return _cachedItems;

        await _loadLock.WaitAsync();
        try
        {
            if (_cachedItems != null)
                return _cachedItems;

            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT TOP 1 ConfigJson
                FROM Configurations
                WHERE ConfigName = 'SailContributionServiceConfiguration.json'", connection);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var json = reader.GetString(0);
                var wrapper = DeserializeJson<SailContributionConfigJson>(json);

                if (wrapper?.SailContributionServiceConfiguration?.SailContributions?.Count > 0)
                {
                    _cachedItems = wrapper.SailContributionServiceConfiguration.SailContributions;
                    return _cachedItems;
                }
            }

            return null;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>The load lock is owned by this repository, so it is disposed with it.</summary>
    public void Dispose() => _loadLock.Dispose();
}
