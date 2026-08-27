namespace VoyageEnergyAdvisor.Core.CommonModels.Exceptions
{
    public class UserFacingException : Exception
    {
        public virtual string UserMessage { get; } = null!;
        public object? AdditionalData { get; }

        protected UserFacingException(string message, object? additionalData = null) : base(message)
        {
            AdditionalData = additionalData;
        }
    }
}
