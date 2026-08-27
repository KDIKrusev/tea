
namespace VoyageEnergyAdvisor.WebApi.Services
{
    using VoyageEnergyAdvisor.Core.CommonModels;

    public interface IJwtTokenService
    {
        string GenerateToken(CurrentUserDto user, int? vesselId);
    }
}
