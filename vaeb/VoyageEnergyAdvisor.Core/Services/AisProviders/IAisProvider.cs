namespace VoyageEnergyAdvisor.Core.Services.AisProviders
{
    using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;

    public interface IAisProvider
    {
        AisProviderType AisProviderType { get; }
        AisResponseInstance? GetAisData(AisRequestInstance request);
        void ValidateRequest(AisRequestInstance request);
    }
}
