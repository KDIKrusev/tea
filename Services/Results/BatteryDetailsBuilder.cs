using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Results;

/// <summary>
/// Builds the client's <c>battery-contribution-panel</c> — fourth from the top, shown only when a
/// battery actually did something.
///
/// Sits here rather than with the battery domain classes because it answers a presentation question,
/// not a physics one: <see cref="Battery.BatteryModeAdapter"/> translates an allocation INTO the
/// pipeline, this turns what came OUT of the pipeline into a panel.
///
/// The rule the panel depends on: a battery that applied to no mode at all — configured for Port on
/// a vessel with zero port hours, say — produces no panel rather than an empty one (decision G2/B10).
/// </summary>
internal static class BatteryDetailsBuilder
{
    internal static BatteryDetails? Build(CalculatorInput input, List<ModePipelineResult> modes)
    {
        var outcomes = modes.Select(m => m.Battery).OfType<BatteryModeOutcome>().ToList();
        if (input.Battery?.IsActive != true || outcomes.Count == 0)
            return null;

        var benefit = outcomes.Sum(o => o.BenefitTonPerYear);
        return new BatteryDetails
        {
            CapacityKwh = input.Battery.CapacityKwh,
            PowerKw = input.Battery.PowerKw,
            SpinningReserveKw = outcomes.Sum(o => o.Allocation.AdditionalSpinningReserveKw),
            PeakShavingKw = outcomes.Sum(o => o.Allocation.PeakShavingBandKw),
            BenefitFocTonPerYear = benefit,
            BenefitCostPerYear = benefit * input.FuelPrice,
            ModeAllocations = outcomes.Select(o => o.Allocation).ToList()
        };
    }
}
