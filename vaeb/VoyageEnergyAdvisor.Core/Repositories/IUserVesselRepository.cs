namespace VoyageEnergyAdvisor.Core.Repositories
{
    using VoyageEnergyAdvisor.Core.CommonModels;

    public interface IUserVesselRepository
    {
        Task<List<VesselDto>> GetUserVesselsAsync();
        Task<VesselDto?> GetCurrentVesselAsync();
        Task<VesselDto?> GetDefaultVesselForUserAsync(string userId);
    }
}
