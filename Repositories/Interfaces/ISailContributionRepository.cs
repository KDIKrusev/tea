using KSailCalc.Api.Models.Domain;

namespace KSailCalc.Api.Repositories.Interfaces;

/// <summary>
/// Separate repository for sail contribution data.
/// Different table: [VoyageEnergyDB].[dbo].[Configurations] (not KSailCalc_Configurations).
/// </summary>
public interface ISailContributionRepository
{
    Task<List<SailContributionItem>?> GetSailContributionItemsAsync();
}
