using System.Text.Json;
using System.Text.Json.Serialization;
using KSailCalc.Api.Models;

namespace KSailCalc.Tests.Golden;

/// <summary>
/// Loads an import-ready profile from docs/qa/manual-test-scenarios and runs it through the
/// production pipeline. Shared by the two suites that use these files:
/// <see cref="GoldenMasterTests"/> (whole-response snapshots) and
/// <see cref="CalculationCardTests"/> (headline numbers written by hand from the cards).
/// </summary>
internal static class GoldenScenario
{
    private static readonly JsonSerializerOptions ProfileOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>The saved profile's calculator input — exactly what the client POSTs.</summary>
    public static CalculatorInput LoadInput(string scenarioFile)
    {
        var path = Path.Combine(GoldenPaths.ScenariosDir, scenarioFile);
        var profile = JsonSerializer.Deserialize<SavedProfile>(File.ReadAllText(path), ProfileOptions)
            ?? throw new InvalidOperationException($"Not a saved-profile export: {path}");

        return profile.Input
            ?? throw new InvalidOperationException($"Profile carries no 'input' object: {path}");
    }

    public static Task<GoldenResponse> RunAsync(string scenarioFile)
        => GoldenScenarioHost.Instance.RunAsync(LoadInput(scenarioFile));

    /// <summary>Runs a scenario expected to succeed and returns its result.</summary>
    public static async Task<AllVariantsCalculationResult> CalculateAsync(string scenarioFile)
    {
        var response = await RunAsync(scenarioFile);
        if (response.Status != 200 || response.Result is null)
            throw new InvalidOperationException(
                $"{scenarioFile} was expected to calculate, but answered {response.Status}: " +
                string.Join(" | ", response.Errors));

        return response.Result;
    }

    /// <summary>Shape of the client's export format (profile.service.ts, schema v3).</summary>
    private sealed class SavedProfile
    {
        public string? Name { get; set; }
        public int Version { get; set; }
        public CalculatorInput? Input { get; set; }
    }
}
