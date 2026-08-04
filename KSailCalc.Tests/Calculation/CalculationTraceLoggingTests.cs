using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Tests.TestHelpers;
using Microsoft.Extensions.Logging;

namespace KSailCalc.Tests.Calculation;

/// <summary>
/// Story R-H: the calculation pipeline emits a Debug trace, so a support question can be answered
/// from the log instead of a debugger. The trace is the feature — if it silently stopped being
/// emitted nothing else would fail, hence this test.
/// </summary>
public class CalculationTraceLoggingTests
{
    /// <summary>Captures what was logged, so the assertions can be about content rather than plumbing.</summary>
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }

    [Fact]
    public async Task ACalculation_LogsWhatEachModeDecided_AndASummary()
    {
        var runnerLog = new CapturingLogger<Api.Services.Calculation.ModePipelineRunner>();
        var calculatorLog = new CapturingLogger<Api.Services.Calculation.CalculatorService>();
        var factory = TestServiceFactory.Create(runnerLogger: runnerLog, calculatorLogger: calculatorLog);

        var input = CalculatorInputBuilder.Default().Build();
        input.PortHours = 1000;
        input.PortHotelPowerKW = 400;

        await factory.CalculatorService.CalculateAllVariantsAsync(input);

        // One line per mode that ran, naming the chosen and the baseline configuration
        var modeLines = runnerLog.Entries.Select(e => e.Message).ToList();
        modeLines.Should().Contain(m => m.Contains("Transit") && m.Contains("optimal") && m.Contains("baseline"));
        modeLines.Should().Contain(m => m.Contains("Port") && m.Contains("optimal"));
        modeLines.Should().Contain(m => m.Contains("Transit L2"), "the Transit-only L2/L3 stages are traced too");

        // And one summary answering "what did this request produce"
        calculatorLog.Entries.Should().ContainSingle();
        var summary = calculatorLog.Entries[0];
        summary.Level.Should().Be(LogLevel.Debug, "per-request detail must not flood production logs");
        summary.Message.Should().Contain("baseline");
        summary.Message.Should().Contain("Advanced");
        summary.Message.Should().Contain("Premium");
    }

    [Fact]
    public async Task TheTraceIsDebugLevel_SoItCostsNothingWhenDisabled()
    {
        // A logger that reports Debug as disabled must never be asked to format a message.
        var silent = new DisabledLogger<Api.Services.Calculation.ModePipelineRunner>();
        var calculatorLog = new CapturingLogger<Api.Services.Calculation.CalculatorService>();
        var factory = TestServiceFactory.Create(runnerLogger: silent, calculatorLogger: calculatorLog);

        await factory.CalculatorService.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        silent.FormattedCount.Should().Be(0,
            "the per-mode trace is guarded with IsEnabled(Debug) — nothing is built when it is off");
    }

    private sealed class DisabledLogger<T> : ILogger<T>
    {
        public int FormattedCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => FormattedCount++;
    }
}
