using FluentAssertions;

namespace KSailCalc.Tests.Golden;

/// <summary>
/// Characterization ("golden master") suite: every import-ready profile in
/// docs/qa/manual-test-scenarios is run through the production pipeline and the WHOLE response
/// is compared with an approved snapshot.
///
/// What it proves: a refactor changed no observable output.
/// What it does NOT prove: the numbers are right — that was established by the manual passes
/// against the reference workbook and is documented per scenario in
/// docs/qa/manual-test-scenarios/calculations/. Snapshots were approved against those documents.
///
/// Adding a scenario: drop the profile in the scenarios folder, run once with GOLDEN_UPDATE=1,
/// review the generated snapshot against its calculation card, commit both.
/// </summary>
public class GoldenMasterTests
{
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
    public async Task Scenario_ProducesTheApprovedResult(string scenarioFile)
    {
        var response = await GoldenScenario.RunAsync(scenarioFile);

        JsonApproval.Verify(response, Path.Combine(GoldenPaths.ExpectedDir, scenarioFile));
    }

    /// <summary>
    /// The suite is only meaningful while it covers every scenario the QA folder ships.
    /// Guards against a snapshot being deleted, or a new profile landing without approval.
    /// </summary>
    [Fact]
    public void EveryScenario_HasAnApprovedSnapshot()
    {
        if (JsonApproval.UpdateMode)
            return; // snapshots are being (re)generated in this run

        var scenarios = Directory.GetFiles(GoldenPaths.ScenariosDir, "*.json")
            .Select(Path.GetFileName).OrderBy(f => f).ToList();
        var approved = Directory.Exists(GoldenPaths.ExpectedDir)
            ? Directory.GetFiles(GoldenPaths.ExpectedDir, "*.json").Select(Path.GetFileName).OrderBy(f => f).ToList()
            : new List<string?>();

        scenarios.Should().NotBeEmpty("the QA scenario folder must not be empty");
        approved.Should().BeEquivalentTo(scenarios,
            "each scenario needs exactly one approved snapshot (run with GOLDEN_UPDATE=1 to add one)");
    }
}
