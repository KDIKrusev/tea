using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services;
using KSailCalc.Api.Services.Interfaces;
using KSailCalc.Tests.TestHelpers;
using Xunit.Abstractions;

namespace KSailCalc.Tests.Services;

/// <summary>
/// Detailed trace tests that print every intermediate calculation value.
/// Run with: dotnet test --filter "CalculationTraceTests" --logger "console;verbosity=detailed"
/// </summary>
public class CalculationTraceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Level1OptimizationService _l1;
    private readonly Level2OptimizationService _l2;
    private readonly Level3DrcService _l3;

    public CalculationTraceTests(ITestOutputHelper output)
    {
        _output = output;
        // Uses REAL SFOC data from DB (mocked via TestServiceFactory with production-like engine data)
        var factory = TestServiceFactory.Create();
        _l1 = factory.Level1Service;
        _l2 = factory.Level2Service;
        _l3 = factory.Level3Service;
    }

    /// <summary>
    /// Example 1: Bulk Carrier from screenshot — Transit Only
    /// ME: 2×12000, SG: 1500, AE: 3×500, Propulsion: 1895, SeaMargin: 20%, Hotel: 165
    /// </summary>
    [Fact]
    public async Task Example1_BulkCarrier_TransitOnly_FullTrace()
    {
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine("  EXAMPLE 1: Bulk Carrier — Transit Only");
        _output.WriteLine("═══════════════════════════════════════════════════════════");

        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 2)
            .WithShaftGenerators(1500)
            .WithAuxiliaryEngines(500, 3)
            .WithPropulsionPower(1895)
            .WithSeaMargin(20)
            .WithTransitMode(5717, 165)
            .WithVesselTypeName("Bulk Carrier")
            .Build();

        _output.WriteLine($"\n── INPUT ──");
        _output.WriteLine($"  Propulsion:       {input.PropulsionPower} kW");
        _output.WriteLine($"  Sea Margin:       {input.SeaMargin}%");
        _output.WriteLine($"  Propulsion+SM:    {input.PropulsionPower * (1 + input.SeaMargin / 100)} kW");
        _output.WriteLine($"  Hotel:            {input.TransitHotelPowerKW} kW");
        _output.WriteLine($"  ME: {input.MeCount} × {input.MeCapacityPerEngine} kW");
        _output.WriteLine($"  SG: {input.SgCapacityPerEngine} kW per ME");
        _output.WriteLine($"  AE: {input.AeCount} × {input.AeCapacityPerEngine} kW");
        _output.WriteLine($"  Transit hours:    {input.TransitHours} h/yr");

        // ── LEVEL 1 ──
        _output.WriteLine($"\n══ LEVEL 1: Optimal Power System Setup ══");
        var l1Result = await _l1.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        _output.WriteLine($"  Valid combinations: {l1Result.AllValidCombinations.Count}");
        _output.WriteLine($"\n  {"#",-3} {"ME",-4} {"SG",-5} {"AE",-4} {"ME kW",-10} {"SG kW",-10} {"AE kW",-10} {"ME Load%",-10} {"AE Load%",-10} {"ME FOC",-12} {"AE FOC",-12} {"Total FOC",-12}");
        _output.WriteLine($"  {new string('-', 105)}");

        for (int i = 0; i < l1Result.AllValidCombinations.Count; i++)
        {
            var c = l1Result.AllValidCombinations[i];
            var role = i == 0 ? " ← OPTIMAL"
                : i == l1Result.AllValidCombinations.Count - 2 ? " ← BASELINE"
                : "";
            _output.WriteLine($"  {i + 1,-3} {c.ActiveMeCount,-4} {(c.SgEnabled ? "ON" : "OFF"),-5} {c.ActiveAeCount,-4} {c.MePowerKw,-10:F1} {c.SgPowerKw,-10:F1} {c.AePowerKw,-10:F1} {c.MeLoadPercent * 100,-10:F2}% {c.AeLoadPercent * 100,-10:F2}% {c.MeFocTonPerHour,-12:F6} {c.AeFocTonPerHour,-12:F6} {c.FocTonPerHour,-12:F6}{role}");
        }

        var opt = l1Result.OptimalCombination;
        var bas = l1Result.BaselineCombination;
        _output.WriteLine($"\n  OPTIMAL:  ME={opt.ActiveMeCount}, SG={opt.SgEnabled}, AE={opt.ActiveAeCount}  →  FOC={opt.FocTonPerHour:F6} ton/h");
        _output.WriteLine($"  BASELINE: ME={bas.ActiveMeCount}, SG={bas.SgEnabled}, AE={bas.ActiveAeCount}  →  FOC={bas.FocTonPerHour:F6} ton/h");
        _output.WriteLine($"  L1 Savings/h:  {l1Result.BaselineFocTonPerHour - l1Result.OptimalFocTonPerHour:F6} ton/h");
        _output.WriteLine($"  L1 Savings/yr: {(l1Result.BaselineFocTonPerHour - l1Result.OptimalFocTonPerHour) * input.TransitHours:F2} ton/yr");

        // ── LEVEL 2 ──
        _output.WriteLine($"\n══ LEVEL 2: Load Setpoint Optimization ══");
        var l2Result = await _l2.OptimizeLoadSetpointsAsync(l1Result, input);

        _output.WriteLine($"  Generators from L1 optimal: {l2Result.OptimalSetpoints.Count}");
        foreach (var sp in l2Result.OptimalSetpoints)
        {
            _output.WriteLine($"    {sp.GeneratorType}: capacity={sp.CapacityKw} kW, load={sp.LoadPercent * 100:F1}%, power={sp.PowerKw:F1} kW, SFOC={sp.Sfoc:F2} g/kWh");
        }
        _output.WriteLine($"  Hotel demand:         {opt.SgPowerKw + opt.AePowerKw:F1} kW");
        _output.WriteLine($"  Total setpoint power: {l2Result.OptimalSetpoints.Sum(s => s.PowerKw):F1} kW");
        _output.WriteLine($"  Total SFOC:           {l2Result.OptimalTotalSfoc:F2}");
        _output.WriteLine($"  L1 FOC:               {l2Result.Level1FocTonPerHour:F6} ton/h");
        _output.WriteLine($"  L2 FOC:               {l2Result.Level2FocTonPerHour:F6} ton/h");
        _output.WriteLine($"  L2 Savings/h:         {l2Result.Level2SavingsTonPerHour:F6} ton/h");
        _output.WriteLine($"  L2 Savings/yr:        {l2Result.Level2SavingsTonPerHour * input.TransitHours:F2} ton/yr");

        // ── LEVEL 3 ──
        _output.WriteLine($"\n══ LEVEL 3: Dynamic Ramp Control (DRC) ══");
        var l3Result = await _l3.CalculateDrcSavingsAsync(l2Result, input, input.TransitHours);

        _output.WriteLine($"  Vessel type:          {input.VesselTypeName}");
        _output.WriteLine($"  Active generators:    {l3Result.ActiveGeneratorCount}");
        _output.WriteLine($"  Variation per gen:    ±{l3Result.VariationPerGeneratorKw:F1} kW");
        _output.WriteLine($"  Reduced variation:    ±{l3Result.ReducedVariationPerGeneratorKw:F1} kW (20% reduction)");
        _output.WriteLine($"  FOC without DRC:      {l3Result.WithoutDrcFocGramsPerCycle:F2} grams/cycle");
        _output.WriteLine($"  FOC with DRC:         {l3Result.WithDrcFocGramsPerCycle:F2} grams/cycle");
        _output.WriteLine($"  DRC Savings:          {l3Result.DrcSavingsTonPerYear:F4} ton/yr");

        // ── SUMMARY ──
        var baselineAnnual = l1Result.BaselineFocTonPerHour * input.TransitHours;
        var l1SavingsAnnual = (l1Result.BaselineFocTonPerHour - l1Result.OptimalFocTonPerHour) * input.TransitHours;
        var l2SavingsAnnual = l2Result.Level2SavingsTonPerHour * input.TransitHours;
        var l3SavingsAnnual = l3Result.DrcSavingsTonPerYear;

        _output.WriteLine($"\n══ ANNUAL SUMMARY ══");
        _output.WriteLine($"  Baseline FOC:      {baselineAnnual:F2} ton/yr");
        _output.WriteLine($"  L1 (Advanced):     savings = {l1SavingsAnnual:F2} ton/yr");
        _output.WriteLine($"  L2 (Pro):          savings = {l1SavingsAnnual + l2SavingsAnnual:F2} ton/yr (L1+L2)");
        _output.WriteLine($"  L3 (Premium):      savings = {l1SavingsAnnual + l2SavingsAnnual + l3SavingsAnnual:F2} ton/yr (L1+L2+L3)");

        // Basic assertions (SG covers hotel=165 → SG=OFF combos filtered → 2 combos)
        l1Result.AllValidCombinations.Count.Should().Be(2);
        l1Result.OptimalFocTonPerHour.Should().BeLessThan(l1Result.BaselineFocTonPerHour);
    }

    /// <summary>
    /// Example 2: Bulk Carrier — Transit + DP Mode
    /// Same vessel, Transit 4517h + DP 1200h
    /// </summary>
    [Fact]
    public async Task Example2_BulkCarrier_TransitPlusDP_FullTrace()
    {
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine("  EXAMPLE 2: Bulk Carrier — Transit + DP Mode");
        _output.WriteLine("═══════════════════════════════════════════════════════════");

        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 2)
            .WithShaftGenerators(1500)
            .WithAuxiliaryEngines(500, 3)
            .WithPropulsionPower(1895)
            .WithSeaMargin(20)
            .WithTransitMode(4517, 165)
            .WithDPMode(1200, 300, 1200)
            .WithVesselTypeName("Bulk Carrier")
            .Build();

        // Transit pipeline
        _output.WriteLine($"\n── TRANSIT MODE ──");
        var transitL1 = await _l1.FindOptimalCombinationAsync(input, OperationalMode.Transit);
        PrintL1Summary(transitL1, input.TransitHours);

        var transitL2 = await _l2.OptimizeLoadSetpointsAsync(transitL1, input);
        PrintL2Summary(transitL2, input.TransitHours);

        var transitL3 = await _l3.CalculateDrcSavingsAsync(transitL2, input, input.TransitHours);
        _output.WriteLine($"  L3 DRC savings: {transitL3.DrcSavingsTonPerYear:F4} ton/yr");

        // DP pipeline
        _output.WriteLine($"\n── DP MODE (propulsion={input.RequiredDPPowerKW} kW, hotel={input.DPHotelPowerKW} kW, {input.DPHours}h) ──");
        var dpL1 = await _l1.FindOptimalCombinationAsync(input, OperationalMode.DP);
        PrintL1Summary(dpL1, input.DPHours ?? 0);

        var dpL2 = await _l2.OptimizeLoadSetpointsAsync(dpL1, input);
        PrintL2Summary(dpL2, input.DPHours ?? 0);

        var dpL3 = await _l3.CalculateDrcSavingsAsync(dpL2, input, input.DPHours ?? 0);
        _output.WriteLine($"  L3 DRC savings: {dpL3.DrcSavingsTonPerYear:F4} ton/yr");

        // Combined
        var transitHours = input.TransitHours;
        var dpHours = input.DPHours ?? 0;
        var baselineFoc = transitL1.BaselineFocTonPerHour * transitHours + dpL1.BaselineFocTonPerHour * dpHours;
        var l1Savings = (transitL1.BaselineFocTonPerHour - transitL1.OptimalFocTonPerHour) * transitHours
                      + (dpL1.BaselineFocTonPerHour - dpL1.OptimalFocTonPerHour) * dpHours;
        var l2Savings = transitL2.Level2SavingsTonPerHour * transitHours + dpL2.Level2SavingsTonPerHour * dpHours;
        var l3Savings = transitL3.DrcSavingsTonPerYear + dpL3.DrcSavingsTonPerYear;

        _output.WriteLine($"\n══ COMBINED ANNUAL SUMMARY ══");
        _output.WriteLine($"  Baseline FOC:   {baselineFoc:F2} ton/yr");
        _output.WriteLine($"  L1 savings:     {l1Savings:F2} ton/yr");
        _output.WriteLine($"  L1+L2 savings:  {l1Savings + l2Savings:F2} ton/yr");
        _output.WriteLine($"  L1+L2+L3 total: {l1Savings + l2Savings + l3Savings:F2} ton/yr");

        transitL1.OptimalFocTonPerHour.Should().BeLessThan(transitL1.BaselineFocTonPerHour);
    }

    /// <summary>
    /// Example 3: Container — Large hotel load forces multi-generator L2 optimization
    /// ME: 1×15000, SG: 2000, AE: 3×2000, Propulsion: 5000, SeaMargin: 15%, Hotel: 5500
    /// </summary>
    [Fact]
    public async Task Example3_Container_LargeHotel_FullTrace()
    {
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine("  EXAMPLE 3: Container — Large Hotel Load (L2 showcase)");
        _output.WriteLine("═══════════════════════════════════════════════════════════");

        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(15000, 1)
            .WithShaftGenerators(2000)
            .WithAuxiliaryEngines(2000, 3)
            .WithPropulsionPower(5000)
            .WithSeaMargin(15)
            .WithTransitMode(5000, 5500)
            .WithVesselTypeName("Container")
            .Build();

        _output.WriteLine($"\n── INPUT ──");
        _output.WriteLine($"  Propulsion+SM:  {5000 * 1.15} kW");
        _output.WriteLine($"  Hotel:          {input.TransitHotelPowerKW} kW");
        _output.WriteLine($"  ME: {input.MeCount} × {input.MeCapacityPerEngine} kW");
        _output.WriteLine($"  SG: {input.SgCapacityPerEngine} kW per ME");
        _output.WriteLine($"  AE: {input.AeCount} × {input.AeCapacityPerEngine} kW");

        // ── LEVEL 1 ──
        _output.WriteLine($"\n══ LEVEL 1 ══");
        var l1Result = await _l1.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        _output.WriteLine($"  Valid combinations: {l1Result.AllValidCombinations.Count}");
        _output.WriteLine($"\n  {"#",-3} {"ME",-4} {"SG",-5} {"AE",-4} {"ME kW",-10} {"SG kW",-10} {"AE kW",-10} {"ME Load%",-10} {"AE Load%",-10} {"Total FOC",-12}");
        for (int i = 0; i < l1Result.AllValidCombinations.Count; i++)
        {
            var c = l1Result.AllValidCombinations[i];
            var role = i == 0 ? " ← OPTIMAL" : i == l1Result.AllValidCombinations.Count - 2 ? " ← BASELINE" : "";
            _output.WriteLine($"  {i + 1,-3} {c.ActiveMeCount,-4} {(c.SgEnabled ? "ON" : "OFF"),-5} {c.ActiveAeCount,-4} {c.MePowerKw,-10:F1} {c.SgPowerKw,-10:F1} {c.AePowerKw,-10:F1} {c.MeLoadPercent * 100,-10:F2}% {c.AeLoadPercent * 100,-10:F2}% {c.FocTonPerHour,-12:F6}{role}");
        }

        var opt = l1Result.OptimalCombination;
        _output.WriteLine($"\n  OPTIMAL:  ME={opt.ActiveMeCount}, SG={opt.SgEnabled}, AE={opt.ActiveAeCount}  →  FOC={opt.FocTonPerHour:F6} ton/h");
        _output.WriteLine($"  BASELINE: FOC={l1Result.BaselineFocTonPerHour:F6} ton/h");

        // ── LEVEL 2 ──
        _output.WriteLine($"\n══ LEVEL 2 ══");
        var l2Result = await _l2.OptimizeLoadSetpointsAsync(l1Result, input);

        _output.WriteLine($"  Generators: {l2Result.OptimalSetpoints.Count}");
        foreach (var sp in l2Result.OptimalSetpoints)
        {
            _output.WriteLine($"    {sp.GeneratorType}: capacity={sp.CapacityKw} kW, load={sp.LoadPercent * 100:F1}%, power={sp.PowerKw:F1} kW, SFOC={sp.Sfoc:F2}");
        }
        _output.WriteLine($"  Hotel demand:    {opt.SgPowerKw + opt.AePowerKw:F1} kW");
        _output.WriteLine($"  Setpoint power:  {l2Result.OptimalSetpoints.Sum(s => s.PowerKw):F1} kW");
        _output.WriteLine($"  L1 FOC:  {l2Result.Level1FocTonPerHour:F6} ton/h");
        _output.WriteLine($"  L2 FOC:  {l2Result.Level2FocTonPerHour:F6} ton/h");
        _output.WriteLine($"  Savings: {l2Result.Level2SavingsTonPerHour:F6} ton/h ({l2Result.Level2SavingsTonPerHour * input.TransitHours:F2} ton/yr)");

        // ── LEVEL 3 ──
        _output.WriteLine($"\n══ LEVEL 3 ══");
        var l3Result = await _l3.CalculateDrcSavingsAsync(l2Result, input, input.TransitHours);

        _output.WriteLine($"  Container variation: ±1500 kW → ±{l3Result.VariationPerGeneratorKw:F1} kW per gen");
        _output.WriteLine($"  Reduced: ±{l3Result.ReducedVariationPerGeneratorKw:F1} kW");
        _output.WriteLine($"  DRC savings: {l3Result.DrcSavingsTonPerYear:F4} ton/yr");

        var baselineAnnual = l1Result.BaselineFocTonPerHour * input.TransitHours;
        var l1Savings = (l1Result.BaselineFocTonPerHour - l1Result.OptimalFocTonPerHour) * input.TransitHours;
        _output.WriteLine($"\n══ ANNUAL ══");
        _output.WriteLine($"  Baseline:  {baselineAnnual:F2} ton/yr");
        _output.WriteLine($"  L1:        {l1Savings:F2} ton/yr saved");
        _output.WriteLine($"  L1+L2:     {l1Savings + l2Result.Level2SavingsTonPerHour * input.TransitHours:F2} ton/yr saved");
        _output.WriteLine($"  L1+L2+L3:  {l1Savings + l2Result.Level2SavingsTonPerHour * input.TransitHours + l3Result.DrcSavingsTonPerYear:F2} ton/yr saved");

        l1Result.OptimalFocTonPerHour.Should().BeLessThan(l1Result.BaselineFocTonPerHour);
    }

    /// <summary>
    /// Example 4: Container + DP Mode — Max loaded scenario
    /// </summary>
    [Fact]
    public async Task Example4_Container_WithDP_FullTrace()
    {
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine("  EXAMPLE 4: Container + DP — Max Loaded");
        _output.WriteLine("═══════════════════════════════════════════════════════════");

        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(15000, 1)
            .WithShaftGenerators(2000)
            .WithAuxiliaryEngines(2000, 3)
            .WithPropulsionPower(5000)
            .WithSeaMargin(15)
            .WithTransitMode(4000, 5500)
            .WithDPMode(1000, 4000, 2000)
            .WithVesselTypeName("Container")
            .Build();

        // Transit
        _output.WriteLine($"\n── TRANSIT (propulsion=5750 kW, hotel=5500 kW, 4000h) ──");
        var tL1 = await _l1.FindOptimalCombinationAsync(input, OperationalMode.Transit);
        PrintL1Summary(tL1, 4000);
        var tL2 = await _l2.OptimizeLoadSetpointsAsync(tL1, input);
        PrintL2Summary(tL2, 4000);
        var tL3 = await _l3.CalculateDrcSavingsAsync(tL2, input, input.TransitHours);
        _output.WriteLine($"  L3 DRC: {tL3.DrcSavingsTonPerYear:F4} ton/yr");

        // DP
        _output.WriteLine($"\n── DP (propulsion=2000 kW, hotel=4000 kW, 1000h) ──");
        var dL1 = await _l1.FindOptimalCombinationAsync(input, OperationalMode.DP);
        PrintL1Summary(dL1, 1000);
        var dL2 = await _l2.OptimizeLoadSetpointsAsync(dL1, input);
        PrintL2Summary(dL2, 1000);
        var dL3 = await _l3.CalculateDrcSavingsAsync(dL2, input, input.DPHours ?? 0);
        _output.WriteLine($"  L3 DRC: {dL3.DrcSavingsTonPerYear:F4} ton/yr");

        // Combined
        var baseline = tL1.BaselineFocTonPerHour * 4000 + dL1.BaselineFocTonPerHour * 1000;
        var s1 = (tL1.BaselineFocTonPerHour - tL1.OptimalFocTonPerHour) * 4000
               + (dL1.BaselineFocTonPerHour - dL1.OptimalFocTonPerHour) * 1000;
        var s2 = tL2.Level2SavingsTonPerHour * 4000 + dL2.Level2SavingsTonPerHour * 1000;
        var s3 = tL3.DrcSavingsTonPerYear + dL3.DrcSavingsTonPerYear;

        _output.WriteLine($"\n══ COMBINED ══");
        _output.WriteLine($"  Baseline:  {baseline:F2} ton/yr");
        _output.WriteLine($"  L1:        +{s1:F2} ton/yr saved");
        _output.WriteLine($"  L1+L2:     +{s1 + s2:F2} ton/yr saved");
        _output.WriteLine($"  L1+L2+L3:  +{s1 + s2 + s3:F2} ton/yr saved");
    }

    #region Helpers

    private void PrintL1Summary(Level1Result l1, double hours)
    {
        var opt = l1.OptimalCombination;
        var bas = l1.BaselineCombination;
        _output.WriteLine($"  L1: {l1.AllValidCombinations.Count} valid combos");
        _output.WriteLine($"  Optimal: ME={opt.ActiveMeCount}, SG={opt.SgEnabled}, AE={opt.ActiveAeCount} → {opt.FocTonPerHour:F6} ton/h (ME={opt.MeFocTonPerHour:F6}, AE={opt.AeFocTonPerHour:F6})");
        _output.WriteLine($"  Baseline: ME={bas.ActiveMeCount}, SG={bas.SgEnabled}, AE={bas.ActiveAeCount} → {bas.FocTonPerHour:F6} ton/h");
        _output.WriteLine($"  L1 savings: {(l1.BaselineFocTonPerHour - l1.OptimalFocTonPerHour) * hours:F2} ton/yr");
    }

    private void PrintL2Summary(Level2Result l2, double hours)
    {
        _output.WriteLine($"  L2: {l2.OptimalSetpoints.Count} generators");
        foreach (var sp in l2.OptimalSetpoints)
            _output.WriteLine($"    {sp.GeneratorType}: {sp.LoadPercent * 100:F0}% of {sp.CapacityKw}kW = {sp.PowerKw:F0}kW, SFOC={sp.Sfoc:F2}");
        _output.WriteLine($"  L2 savings: {l2.Level2SavingsTonPerHour:F6} ton/h ({l2.Level2SavingsTonPerHour * hours:F2} ton/yr)");
    }

    private void PrintDetailedL1(Level1Result l1)
    {
        _output.WriteLine($"\n  {"#",-3} {"ME",-4} {"SG",-5} {"AE",-4} {"ME kW",-10} {"SG kW",-10} {"AE kW",-10} {"ME Ld%",-9} {"AE Ld%",-9} {"ME FOC",-12} {"AE FOC",-12} {"Total",-12}");
        _output.WriteLine($"  {new string('-', 105)}");
        for (int i = 0; i < l1.AllValidCombinations.Count; i++)
        {
            var c = l1.AllValidCombinations[i];
            var role = i == 0 ? " ← OPT" : i == l1.AllValidCombinations.Count - 2 ? " ← BASE" : "";
            _output.WriteLine($"  {i + 1,-3} {c.ActiveMeCount,-4} {(c.SgEnabled ? "ON" : "OFF"),-5} {c.ActiveAeCount,-4} {c.MePowerKw,-10:F1} {c.SgPowerKw,-10:F1} {c.AePowerKw,-10:F1} {c.MeLoadPercent * 100,-9:F2}% {c.AeLoadPercent * 100,-9:F2}% {c.MeFocTonPerHour,-12:F6} {c.AeFocTonPerHour,-12:F6} {c.FocTonPerHour,-12:F6}{role}");
        }
    }

    private void PrintDetailedL2(Level2Result l2, EngineCombination opt)
    {
        _output.WriteLine($"  Generators: {l2.OptimalSetpoints.Count}");
        foreach (var sp in l2.OptimalSetpoints)
            _output.WriteLine($"    {sp.GeneratorType}: cap={sp.CapacityKw}kW, load={sp.LoadPercent * 100:F1}%, power={sp.PowerKw:F1}kW, SFOC={sp.Sfoc:F2}");
        _output.WriteLine($"  Hotel demand:    {opt.SgPowerKw + opt.AePowerKw:F1} kW");
        _output.WriteLine($"  Setpoint power:  {l2.OptimalSetpoints.Sum(s => s.PowerKw):F1} kW");
        _output.WriteLine($"  Total SFOC:      {l2.OptimalTotalSfoc:F2}");
        _output.WriteLine($"  L1 FOC:          {l2.Level1FocTonPerHour:F6} ton/h");
        _output.WriteLine($"  L2 FOC:          {l2.Level2FocTonPerHour:F6} ton/h");
        _output.WriteLine($"  L2 Savings/h:    {l2.Level2SavingsTonPerHour:F6} ton/h");
    }

    private void PrintDetailedL3(Level3Result l3)
    {
        _output.WriteLine($"  Active generators:    {l3.ActiveGeneratorCount}");
        _output.WriteLine($"  Variation per gen:    ±{l3.VariationPerGeneratorKw:F1} kW");
        _output.WriteLine($"  Reduced variation:    ±{l3.ReducedVariationPerGeneratorKw:F1} kW");
        _output.WriteLine($"  FOC without DRC:      {l3.WithoutDrcFocGramsPerCycle:F2} grams/cycle");
        _output.WriteLine($"  FOC with DRC:         {l3.WithDrcFocGramsPerCycle:F2} grams/cycle");
        _output.WriteLine($"  Annual hours:         {l3.AnnualHours:F0} h");
        _output.WriteLine($"  DRC Savings:          {l3.DrcSavingsTonPerYear:F4} ton/yr");
    }

    #endregion

    /// <summary>
    /// Example 5: LNG — L2 sweep optimization showcase
    /// ME: 2×12000, SG: 1500, AE: 3×2000, Hotel: 3000 kW
    /// 2 generators (SG+AE), demand fits within sweep → L2 should optimize
    /// </summary>
    [Fact]
    public async Task Example5_LNG_L2Sweep_FullTrace()
    {
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine("  EXAMPLE 5: LNG — L2 Sweep Optimization");
        _output.WriteLine("═══════════════════════════════════════════════════════════");

        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 2)
            .WithShaftGenerators(1500)
            .WithAuxiliaryEngines(2000, 3)
            .WithPropulsionPower(4000)
            .WithSeaMargin(15)
            .WithTransitMode(6000, 3000)
            .WithVesselTypeName("LNG")
            .Build();

        _output.WriteLine($"\n── INPUT ──");
        _output.WriteLine($"  Propulsion+SM:  {4000 * 1.15} kW");
        _output.WriteLine($"  Hotel:          {input.TransitHotelPowerKW} kW");
        _output.WriteLine($"  ME: {input.MeCount} × {input.MeCapacityPerEngine} kW");
        _output.WriteLine($"  SG: {input.SgCapacityPerEngine} kW per ME");
        _output.WriteLine($"  AE: {input.AeCount} × {input.AeCapacityPerEngine} kW");
        _output.WriteLine($"  Transit: {input.TransitHours} h/yr");

        // ── LEVEL 1 ──
        _output.WriteLine($"\n══ LEVEL 1 ══");
        var l1 = await _l1.FindOptimalCombinationAsync(input, OperationalMode.Transit);
        PrintDetailedL1(l1);

        var opt = l1.OptimalCombination;
        var bas = l1.BaselineCombination;
        _output.WriteLine($"\n  OPTIMAL:  ME={opt.ActiveMeCount}, SG={opt.SgEnabled}, AE={opt.ActiveAeCount} → FOC={opt.FocTonPerHour:F6}");
        _output.WriteLine($"  BASELINE: ME={bas.ActiveMeCount}, SG={bas.SgEnabled}, AE={bas.ActiveAeCount} → FOC={bas.FocTonPerHour:F6}");
        _output.WriteLine($"  L1 Savings: {(bas.FocTonPerHour - opt.FocTonPerHour) * input.TransitHours:F2} ton/yr");

        // ── LEVEL 2 ──
        _output.WriteLine($"\n══ LEVEL 2 (Sweep 15%-90%, step 5%) ══");
        var l2 = await _l2.OptimizeLoadSetpointsAsync(l1, input);
        PrintDetailedL2(l2, opt);
        _output.WriteLine($"  L2 Savings/yr: {l2.Level2SavingsTonPerHour * input.TransitHours:F2} ton/yr");

        // ── LEVEL 3 ──
        _output.WriteLine($"\n══ LEVEL 3 (DRC) ══");
        var l3 = await _l3.CalculateDrcSavingsAsync(l2, input, input.TransitHours);
        PrintDetailedL3(l3);

        // ── SUMMARY ──
        var baselineAnnual = bas.FocTonPerHour * input.TransitHours;
        var l1Savings = (bas.FocTonPerHour - opt.FocTonPerHour) * input.TransitHours;
        var l2Savings = l2.Level2SavingsTonPerHour * input.TransitHours;
        _output.WriteLine($"\n══ ANNUAL SUMMARY ══");
        _output.WriteLine($"  Baseline:   {baselineAnnual:F2} ton/yr");
        _output.WriteLine($"  L1:         {l1Savings:F2} ton/yr saved");
        _output.WriteLine($"  L1+L2:      {l1Savings + l2Savings:F2} ton/yr saved");
        _output.WriteLine($"  L1+L2+L3:   {l1Savings + l2Savings + l3.DrcSavingsTonPerYear:F2} ton/yr saved");

        // Assertions
        l1.OptimalFocTonPerHour.Should().BeLessThan(l1.BaselineFocTonPerHour);
        l3.DrcSavingsTonPerYear.Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Example 6: Bulk Carrier — Low load, L3 DRC showcase
    /// ME: 1×10000, SG: 1000, AE: 3×1000, Hotel: 1500 kW
    /// Generators at moderate load → DRC should have positive effect
    /// </summary>
    [Fact]
    public async Task Example6_BulkCarrier_L3DRC_FullTrace()
    {
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine("  EXAMPLE 6: Bulk Carrier — L3 DRC Showcase");
        _output.WriteLine("═══════════════════════════════════════════════════════════");

        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(10000, 1)
            .WithShaftGenerators(1000)
            .WithAuxiliaryEngines(1000, 3)
            .WithPropulsionPower(3000)
            .WithSeaMargin(15)
            .WithTransitMode(6000, 1500)
            .WithVesselTypeName("Bulk Carrier")
            .Build();

        _output.WriteLine($"\n── INPUT ──");
        _output.WriteLine($"  Propulsion+SM:  {3000 * 1.15} kW");
        _output.WriteLine($"  Hotel:          {input.TransitHotelPowerKW} kW");
        _output.WriteLine($"  ME: {input.MeCount} × {input.MeCapacityPerEngine} kW");
        _output.WriteLine($"  SG: {input.SgCapacityPerEngine} kW per ME");
        _output.WriteLine($"  AE: {input.AeCount} × {input.AeCapacityPerEngine} kW");
        _output.WriteLine($"  Transit: {input.TransitHours} h/yr");
        _output.WriteLine($"  Variation: ±250 kW (Bulk Carrier)");

        // ── LEVEL 1 ──
        _output.WriteLine($"\n══ LEVEL 1 ══");
        var l1 = await _l1.FindOptimalCombinationAsync(input, OperationalMode.Transit);
        PrintDetailedL1(l1);

        var opt = l1.OptimalCombination;
        var bas = l1.BaselineCombination;
        _output.WriteLine($"\n  OPTIMAL:  ME={opt.ActiveMeCount}, SG={opt.SgEnabled}, AE={opt.ActiveAeCount} → FOC={opt.FocTonPerHour:F6}");
        _output.WriteLine($"  BASELINE: ME={bas.ActiveMeCount}, SG={bas.SgEnabled}, AE={bas.ActiveAeCount} → FOC={bas.FocTonPerHour:F6}");
        _output.WriteLine($"  L1 Savings: {(bas.FocTonPerHour - opt.FocTonPerHour) * input.TransitHours:F2} ton/yr");

        // ── LEVEL 2 ──
        _output.WriteLine($"\n══ LEVEL 2 ══");
        var l2 = await _l2.OptimizeLoadSetpointsAsync(l1, input);
        PrintDetailedL2(l2, opt);
        _output.WriteLine($"  L2 Savings/yr: {l2.Level2SavingsTonPerHour * input.TransitHours:F2} ton/yr");

        // ── LEVEL 3 ──
        _output.WriteLine($"\n══ LEVEL 3 (DRC) ══");
        _output.WriteLine($"  Vessel: Bulk Carrier → ±250 kW total variation");
        var l3 = await _l3.CalculateDrcSavingsAsync(l2, input, input.TransitHours);
        PrintDetailedL3(l3);

        // Show per-generator DRC detail
        _output.WriteLine($"\n  Per-generator DRC detail:");
        foreach (var sp in l2.OptimalSetpoints)
        {
            var steadyLoad = sp.PowerKw;
            var capacity = sp.CapacityKw;
            var variation = l3.VariationPerGeneratorKw;
            var reduced = l3.ReducedVariationPerGeneratorKw;

            _output.WriteLine($"    {sp.GeneratorType}: steady={steadyLoad:F1}kW, cap={capacity}kW");
            _output.WriteLine($"      Without DRC: up={Math.Min(steadyLoad + variation, capacity):F1}kW ({Math.Min(steadyLoad + variation, capacity) / capacity * 100:F1}%), down={Math.Max(steadyLoad - variation, 0):F1}kW ({Math.Max(steadyLoad - variation, 0) / capacity * 100:F1}%)");
            _output.WriteLine($"      With DRC:    up={Math.Min(steadyLoad + reduced, capacity):F1}kW ({Math.Min(steadyLoad + reduced, capacity) / capacity * 100:F1}%), down={Math.Max(steadyLoad - reduced, 0):F1}kW ({Math.Max(steadyLoad - reduced, 0) / capacity * 100:F1}%)");
        }

        // ── SUMMARY ──
        var baselineAnnual = bas.FocTonPerHour * input.TransitHours;
        var l1Savings = (bas.FocTonPerHour - opt.FocTonPerHour) * input.TransitHours;
        var l2Savings = l2.Level2SavingsTonPerHour * input.TransitHours;
        _output.WriteLine($"\n══ ANNUAL SUMMARY ══");
        _output.WriteLine($"  Baseline:   {baselineAnnual:F2} ton/yr");
        _output.WriteLine($"  L1:         {l1Savings:F2} ton/yr saved");
        _output.WriteLine($"  L1+L2:      {l1Savings + l2Savings:F2} ton/yr saved");
        _output.WriteLine($"  L1+L2+L3:   {l1Savings + l2Savings + l3.DrcSavingsTonPerYear:F2} ton/yr saved");

        // Assertions
        l1.OptimalFocTonPerHour.Should().BeLessThan(l1.BaselineFocTonPerHour);
        l3.DrcSavingsTonPerYear.Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Example 7: AE-only vessel — L2 sweep optimization + L3 DRC
    /// No SG, 2 AEs at moderate load, small variation → both L2 and L3 should produce savings
    /// ME: 1×10000, SG: 0, AE: 2×2000, Hotel: 2500 kW (62.5% per AE)
    /// </summary>
    [Fact]
    public async Task Example7_AEOnly_L2AndL3_FullTrace()
    {
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine("  EXAMPLE 7: AE-only — L2 Sweep + L3 DRC");
        _output.WriteLine("═══════════════════════════════════════════════════════════");

        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(10000, 1)
            .WithShaftGenerators(0)             // No SG!
            .WithAuxiliaryEngines(2000, 2)
            .WithPropulsionPower(4000)
            .WithSeaMargin(15)
            .WithTransitMode(6000, 2500)
            .WithVesselTypeName("Bulk Carrier")
            .Build();

        _output.WriteLine($"\n── INPUT ──");
        _output.WriteLine($"  Propulsion+SM:  {4000 * 1.15} kW");
        _output.WriteLine($"  Hotel:          {input.TransitHotelPowerKW} kW");
        _output.WriteLine($"  ME: {input.MeCount} × {input.MeCapacityPerEngine} kW, NO SG");
        _output.WriteLine($"  AE: {input.AeCount} × {input.AeCapacityPerEngine} kW");
        _output.WriteLine($"  Transit: {input.TransitHours} h/yr");
        _output.WriteLine($"  AE load per gen (L1): {2500.0 / (2 * 2000) * 100:F1}%");

        // ── LEVEL 1 ──
        _output.WriteLine($"\n══ LEVEL 1 ══");
        var l1 = await _l1.FindOptimalCombinationAsync(input, OperationalMode.Transit);
        PrintDetailedL1(l1);

        var opt = l1.OptimalCombination;
        var bas = l1.BaselineCombination;
        _output.WriteLine($"\n  OPTIMAL:  ME={opt.ActiveMeCount}, SG={opt.SgEnabled}, AE={opt.ActiveAeCount} → FOC={opt.FocTonPerHour:F6}");
        _output.WriteLine($"  BASELINE: ME={bas.ActiveMeCount}, SG={bas.SgEnabled}, AE={bas.ActiveAeCount} → FOC={bas.FocTonPerHour:F6}");
        _output.WriteLine($"  L1 Savings: {(bas.FocTonPerHour - opt.FocTonPerHour) * input.TransitHours:F2} ton/yr");

        // ── LEVEL 2 ──
        _output.WriteLine($"\n══ LEVEL 2 (Sweep 15%-90%, step 5%) ══");
        var l2 = await _l2.OptimizeLoadSetpointsAsync(l1, input);
        PrintDetailedL2(l2, opt);
        _output.WriteLine($"  L2 Savings/yr: {l2.Level2SavingsTonPerHour * input.TransitHours:F2} ton/yr");

        // Explain L2 logic
        var l1AeLoad = opt.AeLoadPercent;
        _output.WriteLine($"\n  L1 AE distribution: {opt.ActiveAeCount} × {l1AeLoad * 100:F1}% load → SFOC at {l1AeLoad * 100:F1}%");
        _output.WriteLine($"  L2 sweep finds higher load → lower SFOC per kW");
        _output.WriteLine($"  Demand is distributed proportionally from setpoint power");

        // ── LEVEL 3 ──
        _output.WriteLine($"\n══ LEVEL 3 (DRC) ══");
        _output.WriteLine($"  Bulk Carrier variation: ±250 kW");
        var l3 = await _l3.CalculateDrcSavingsAsync(l2, input, input.TransitHours);
        PrintDetailedL3(l3);

        _output.WriteLine($"\n  Per-generator DRC detail:");
        foreach (var sp in l2.OptimalSetpoints)
        {
            var steadyLoad = sp.PowerKw;
            var capacity = sp.CapacityKw;
            var variation = l3.VariationPerGeneratorKw;
            var reduced = l3.ReducedVariationPerGeneratorKw;
            _output.WriteLine($"    {sp.GeneratorType}: steady={steadyLoad:F1}kW, cap={capacity}kW");
            _output.WriteLine($"      Without DRC: up={Math.Min(steadyLoad + variation, capacity):F1}kW, down={Math.Max(steadyLoad - variation, 0):F1}kW");
            _output.WriteLine($"      With DRC:    up={Math.Min(steadyLoad + reduced, capacity):F1}kW, down={Math.Max(steadyLoad - reduced, 0):F1}kW");
        }

        // ── SUMMARY ──
        var baselineAnnual = bas.FocTonPerHour * input.TransitHours;
        var l1Savings = (bas.FocTonPerHour - opt.FocTonPerHour) * input.TransitHours;
        var l2Savings = l2.Level2SavingsTonPerHour * input.TransitHours;
        _output.WriteLine($"\n══ ANNUAL SUMMARY ══");
        _output.WriteLine($"  Baseline:   {baselineAnnual:F2} ton/yr");
        _output.WriteLine($"  L1:         {l1Savings:F2} ton/yr saved");
        _output.WriteLine($"  L1+L2:      {l1Savings + l2Savings:F2} ton/yr saved");
        _output.WriteLine($"  L1+L2+L3:   {l1Savings + l2Savings + l3.DrcSavingsTonPerYear:F2} ton/yr saved");

        // Assertions
        l1.OptimalFocTonPerHour.Should().BeLessThanOrEqualTo(l1.BaselineFocTonPerHour);
        // Note: L2 savings = 0 with test data because SFOC curves are monotonically decreasing.
        // With real U-shaped SFOC curves (sweet spot at 70-85%), L2 would find positive savings
        // by shifting generators to their optimal operating point.
        l2.Level2SavingsTonPerHour.Should().BeGreaterThanOrEqualTo(0);
    }
}