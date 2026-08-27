namespace VoyageEnergyAdvisor.Core.Services.AisService
{
    using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;

    public interface IAisService
    {
        Task<AisResponseInstance?> GetCurrentVesselDataAsync();
    }
}
