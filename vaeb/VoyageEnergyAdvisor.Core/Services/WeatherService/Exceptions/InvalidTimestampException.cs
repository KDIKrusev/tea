using VoyageEnergyAdvisor.Core.CommonModels.Exceptions;

public class InvalidTimestampException : UserFacingException
{
    public DateTime MinDateTime { get; }
    public DateTime MaxDateTime { get; }
    public DateTime? InvalidStartTime { get; }
    public DateTime? InvalidEndTime { get; }

    public InvalidTimestampException(DateTime minDateTime, DateTime maxDateTime,
                                     DateTime? invalidStartTime = null, DateTime? invalidEndTime = null)
        : base("One or more weather request times fall outside the valid forecast range.")
    {
        MinDateTime = minDateTime;
        MaxDateTime = maxDateTime;
        InvalidStartTime = invalidStartTime;
        InvalidEndTime = invalidEndTime;
    }

    public override string UserMessage
    {
        get
        {
            string minLocal = MinDateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            string maxLocal = MaxDateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

            string message = $"Weather forecast data is only available between " +
                             $"{minLocal} and {maxLocal} (your local time).\n";

            if (InvalidStartTime.HasValue && InvalidEndTime.HasValue)
            {
                var invalidStartLocal = InvalidStartTime.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                var invalidEndLocal = InvalidEndTime.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

                message += $"You requested a route from {invalidStartLocal} to {invalidEndLocal}, " +
                           $"which is outside the supported range.";
            }
            else
            {
                message += "Please select a valid time range within the supported forecast window.";
            }

            return message;
        }
    }

}
