using VoyageEnergyAdvisor.Core.CommonModels.Exceptions;

namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Exceptions
{
    public class OptimalVoyageRequestException : UserFacingException
    {
        public OptimalVoyageRequestException(string message) : base(message)
        {
        }

        public override string UserMessage => Message;
    }
}
