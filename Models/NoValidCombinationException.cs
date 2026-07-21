using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Models;

/// <summary>
/// Thrown when Level 1 finds no feasible engine combination for a mode. Carries an actionable,
/// user-facing reason so the API can answer 400 with guidance instead of a generic 500 (QA-C-1).
/// </summary>
public class NoValidCombinationException : InvalidOperationException
{
    public OperationalMode Mode { get; }

    /// <summary>Human-readable, actionable explanation shown directly to the user.</summary>
    public string UserMessage { get; }

    public NoValidCombinationException(OperationalMode mode, string userMessage)
        : base($"No valid engine combinations found for {mode} mode: {userMessage}")
    {
        Mode = mode;
        UserMessage = userMessage;
    }
}
