using System.Runtime.CompilerServices;

namespace KSailCalc.Tests.Golden;

/// <summary>
/// Resolves repository paths for the golden-master suite.
///
/// Uses <see cref="CallerFilePathAttribute"/> instead of walking up from the test binary:
/// the suite is regularly run with <c>-p:BaseOutputPath=&lt;temp&gt;</c> (the API keeps
/// <c>bin\</c> locked while it runs), which puts the assembly OUTSIDE the repository —
/// any AppContext.BaseDirectory-relative lookup breaks there. The compiler bakes in the
/// real source path, so this works from any output directory.
/// </summary>
internal static class GoldenPaths
{
    /// <summary>Repository root (the folder holding KSailCalc.sln).</summary>
    public static string RepoRoot { get; } = ResolveRepoRoot();

    /// <summary>Import-ready scenario profiles — the SAME files a tester imports in the UI.</summary>
    public static string ScenariosDir => Path.Combine(RepoRoot, "docs", "qa", "manual-test-scenarios");

    /// <summary>Approved API responses, one per scenario.</summary>
    public static string ExpectedDir => Path.Combine(GoldenDir, "Expected");

    /// <summary>Captured DB rows (engine SFOC curves, sail table, integration levels).</summary>
    public static string FixtureFile => Path.Combine(GoldenDir, "Fixtures", "db-fixture.json");

    /// <summary>Production configuration — bound exactly as Program.cs binds it.</summary>
    public static string AppSettingsFile => Path.Combine(RepoRoot, "appsettings.json");

    private static string GoldenDir => Path.Combine(RepoRoot, "KSailCalc.Tests", "Golden");

    private static string ResolveRepoRoot([CallerFilePath] string thisFile = "")
    {
        // <root>/KSailCalc.Tests/Golden/GoldenPaths.cs → up three levels
        var fromSource = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", ".."));
        if (File.Exists(Path.Combine(fromSource, "KSailCalc.sln")))
            return fromSource;

        // Fallback: sources were moved after the build — walk up from the assembly.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "KSailCalc.sln")))
            dir = dir.Parent;

        return dir?.FullName
            ?? throw new InvalidOperationException(
                $"Cannot locate the repository root. Compiled source path was '{thisFile}'.");
    }
}
