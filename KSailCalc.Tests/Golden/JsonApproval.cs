using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace KSailCalc.Tests.Golden;

/// <summary>
/// Snapshot ("approval") comparison for whole API responses.
///
/// Numbers are compared with a relative tolerance rather than by string equality: the pipeline
/// runs thousands of floating-point operations and the last bits legitimately differ between
/// runtimes. Anything a refactor could realistically break moves far more than 1e-9.
///
/// Set GOLDEN_UPDATE=1 to write missing/changed snapshots instead of failing — then READ THE DIFF
/// before committing. A snapshot is an approval: it is only worth what its reviewer checked.
/// </summary>
internal static class JsonApproval
{
    private const double RelativeTolerance = 1e-9;

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public static bool UpdateMode =>
        Environment.GetEnvironmentVariable("GOLDEN_UPDATE") is "1" or "true";

    /// <summary>Compares <paramref name="actual"/> with the approved snapshot for this scenario.</summary>
    public static void Verify(object actual, string snapshotPath)
    {
        var actualJson = JsonSerializer.Serialize(actual, WriteOptions);

        if (!File.Exists(snapshotPath))
        {
            if (!UpdateMode)
                throw new InvalidOperationException(
                    $"No approved snapshot at '{snapshotPath}'.\n" +
                    "Run the suite once with GOLDEN_UPDATE=1, review the generated file against " +
                    "docs/qa/manual-test-scenarios/calculations/, then commit it.");

            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            File.WriteAllText(snapshotPath, actualJson);
            return;
        }

        var expected = JsonNode.Parse(File.ReadAllText(snapshotPath));
        var differences = new List<string>();
        Compare(expected, JsonNode.Parse(actualJson), "$", differences);

        if (differences.Count == 0)
            return;

        if (UpdateMode)
        {
            File.WriteAllText(snapshotPath, actualJson);
            return;
        }

        var shown = differences.Take(25).ToList();
        var more = differences.Count > shown.Count ? $"\n… and {differences.Count - shown.Count} more" : "";
        throw new InvalidOperationException(
            $"{Path.GetFileName(snapshotPath)}: {differences.Count} difference(s) from the approved result:\n"
            + string.Join("\n", shown) + more
            + "\n\nIf the change is intended, re-run with GOLDEN_UPDATE=1 and review the diff.");
    }

    private static void Compare(JsonNode? expected, JsonNode? actual, string path, List<string> diffs)
    {
        if (expected is null || actual is null)
        {
            if (!(expected is null && actual is null))
                diffs.Add($"{path}: expected {Describe(expected)}, got {Describe(actual)}");
            return;
        }

        switch (expected)
        {
            case JsonObject expectedObj when actual is JsonObject actualObj:
                foreach (var (key, value) in expectedObj)
                {
                    if (!actualObj.ContainsKey(key)) diffs.Add($"{path}.{key}: missing from the result");
                    else Compare(value, actualObj[key], $"{path}.{key}", diffs);
                }
                foreach (var (key, _) in actualObj)
                    if (!expectedObj.ContainsKey(key))
                        diffs.Add($"{path}.{key}: new field not present in the approved snapshot");
                break;

            case JsonArray expectedArr when actual is JsonArray actualArr:
                if (expectedArr.Count != actualArr.Count)
                {
                    diffs.Add($"{path}: expected {expectedArr.Count} item(s), got {actualArr.Count}");
                    break;
                }
                for (var i = 0; i < expectedArr.Count; i++)
                    Compare(expectedArr[i], actualArr[i], $"{path}[{i}]", diffs);
                break;

            case JsonValue expectedValue when actual is JsonValue actualValue:
                CompareValues(expectedValue, actualValue, path, diffs);
                break;

            default:
                diffs.Add($"{path}: expected {Describe(expected)}, got {Describe(actual)}");
                break;
        }
    }

    private static void CompareValues(JsonValue expected, JsonValue actual, string path, List<string> diffs)
    {
        if (expected.TryGetValue<double>(out var expectedNumber) && actual.TryGetValue<double>(out var actualNumber))
        {
            var scale = Math.Max(Math.Abs(expectedNumber), Math.Abs(actualNumber));
            if (Math.Abs(expectedNumber - actualNumber) > RelativeTolerance * Math.Max(1.0, scale))
                diffs.Add($"{path}: expected {Format(expectedNumber)}, got {Format(actualNumber)} " +
                          $"(Δ {Format(actualNumber - expectedNumber)})");
            return;
        }

        if (expected.ToJsonString() != actual.ToJsonString())
            diffs.Add($"{path}: expected {expected.ToJsonString()}, got {actual.ToJsonString()}");
    }

    private static string Format(double value) => value.ToString("G12", CultureInfo.InvariantCulture);

    private static string Describe(JsonNode? node) => node?.ToJsonString() ?? "null";
}
