using VoyageEnergyAdvisor.Core.CommonModels.Exceptions;

namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Exceptions
{
    /// <summary>
    /// No constant propulsion power can sail a slot within its ETD/ETA window. Raised by the power search
    /// and turned into the slot's UnavailableReason, so one impossible slot never fails the whole set.
    /// </summary>
    public class VariableSpeedSolutionException : UserFacingException
    {
        public VariableSpeedSolutionException(string message) : base(message)
        {
        }

        public override string UserMessage => Message;
    }
}
