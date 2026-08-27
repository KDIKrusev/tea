namespace VoyageEnergyAdvisor.Core.Repositories
{
    using VoyageEnergyAdvisor.Core.CommonModels;

    public interface IRouteRepository
    {
        Task<List<string>> GetRoutesListAsync();
        Task<Route?> GetRouteAsync(string routeName);
    }
}
