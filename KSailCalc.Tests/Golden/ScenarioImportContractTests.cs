using System.Text.Json;
using FluentAssertions;

namespace KSailCalc.Tests.Golden;

/// <summary>
/// A scenario file is not just a backend request body — it is a **saved profile in the client's
/// import format**, and the client validates it strictly before letting it load.
///
/// This test exists because scenarios 19–35 were written after the backend stopped reading
/// <c>hotelLoad</c>, <c>batteryCapacity</c> and <c>sailInstalled</c>. The backend ignored their
/// absence and every golden test passed — but the client refused all seventeen files with
/// "Invalid profile file: missing required fields", which is only discoverable by importing one
/// by hand.
///
/// The required-field list below mirrors <c>ProfileService.isValidCalculatorInput</c> in
/// <c>cl/src/app/core/profile.service.ts</c>. If the client's contract changes, this test must be
/// updated with it — that is the point: the coupling is now written down and enforced instead of
/// being discovered by a user.
/// </summary>
public class ScenarioImportContractTests
{
    /// <summary>Mirrors the client's <c>requiredNumbers</c> array.</summary>
    private static readonly string[] RequiredNumbers =
    {
        "propulsionPower", "hotelLoad", "seaMargin", "meCapacityPerEngine", "meCount",
        "sgCapacityPerEngine", "aeCapacityPerEngine", "aeCount", "mainEngineTypeId",
        "auxEngineTypeId", "batteryCapacity", "fuelPrice", "vesselSpeedKnots"
    };

    /// <summary>Mirrors the client's top-level profile checks.</summary>
    private static readonly string[] RequiredProfileNumbers =
    {
        "vesselSize", "vesselSpeed"
    };

    public static TheoryData<string> Scenarios
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var file in Directory.GetFiles(GoldenPaths.ScenariosDir, "*.json").OrderBy(f => f))
                data.Add(Path.GetFileName(file));
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void EveryScenario_CanBeImportedByTheClient(string scenarioFile)
    {
        using var doc = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(GoldenPaths.ScenariosDir, scenarioFile)));
        var root = doc.RootElement;

        // ── the profile envelope ──
        root.TryGetProperty("name", out var name).Should().BeTrue("the client lists profiles by name");
        name.ValueKind.Should().Be(JsonValueKind.String);

        foreach (var key in RequiredProfileNumbers)
        {
            root.TryGetProperty(key, out var value).Should().BeTrue($"the client requires '{key}'");
            value.ValueKind.Should().Be(JsonValueKind.Number, $"'{key}' must be a finite number");
        }

        root.TryGetProperty("input", out var input).Should().BeTrue();
        input.ValueKind.Should().Be(JsonValueKind.Object);

        // ── the input body ──
        foreach (var key in RequiredNumbers)
        {
            input.TryGetProperty(key, out var value).Should().BeTrue(
                $"the client's importer requires '{key}' — a scenario without it is rejected with " +
                "'Invalid profile file: missing required fields', however valid it is for the backend");
            value.ValueKind.Should().Be(JsonValueKind.Number, $"'{key}' must be a finite number");
        }

        input.TryGetProperty("sailInstalled", out var sail).Should().BeTrue(
            "the client requires sailInstalled even though the backend no longer reads it");
        sail.ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
    }

    /// <summary>
    /// The fields above are ignored by the backend. That is exactly why they are easy to drop —
    /// so assert the reason they still have to be there, next to the requirement itself.
    /// </summary>
    [Fact]
    public void TheClientOnlyFieldsAreDocumentedAsSuchOnAtLeastOneScenario()
    {
        var sample = Path.Combine(GoldenPaths.ScenariosDir, "19-two-aux-engines-level2.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(sample));
        var input = doc.RootElement.GetProperty("input");

        input.GetProperty("hotelLoad").GetDouble()
            .Should().Be(input.GetProperty("transitHotelPowerKW").GetDouble(),
                "hotelLoad is a client-side legacy field; scenarios keep it equal to the transit " +
                "hotel load so it cannot be mistaken for an independent input");
        input.GetProperty("batteryCapacity").GetDouble()
            .Should().Be(0, "the legacy battery stub is inert — the real battery is the Battery object");
    }
}
