namespace VoyageEnergyAdvisor.Core.Repositories
{
    using VoyageEnergyAdvisor.Core.CommonModels;

    public interface ICurrentUserRepository
    {
        Task<CurrentUserDto?> GetCurrentUserAsync();
        Task<CurrentUserDto?> AuthenticateUserAsync(string username, string password);
        Task<OperationResult> CreateUserAsync(CreateUserDto dto);
    }
}
