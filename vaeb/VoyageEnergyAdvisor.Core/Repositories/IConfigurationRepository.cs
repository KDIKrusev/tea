namespace VoyageEnergyAdvisor.Core.Repositories
{
    public interface IConfigurationRepository
    {
        Task<T?> GetConfigurationAsync<T>() where T : class;
        Task UpdateConfigurationAsync<T>(T configuration) where T : class;

    }
}
