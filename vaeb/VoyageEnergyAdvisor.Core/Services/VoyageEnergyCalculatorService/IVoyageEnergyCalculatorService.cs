using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyCalculatorService.Models;

namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService
{
    public interface IVoyageEnergyAdvisorService
    {
         Task<VoyageEnergyAdvisorResponse> GetVoyageOptions(VoyageEnergyAdvisorRequest VoyageEnergyAdvisorRequestInfo);
         Task<VoyageEnergyAdvisorLiveResponse> GetLiveData(VoyageEnergyAdvisorLiveRequest request);
    }
}
