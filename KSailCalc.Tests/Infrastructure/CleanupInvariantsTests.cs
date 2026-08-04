using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Repositories;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace KSailCalc.Tests.Infrastructure;

/// <summary>
/// Story R-E: the cleanups that changed structure without changing results. These tests pin the two
/// items where the cleanup altered something observable — a failure message, and a lock.
/// </summary>
public class CleanupInvariantsTests
{
    // ─── E4: a deactivated pricing row must be diagnosable ──────────────────────

    [Fact]
    public async Task AMissingIntegrationLevelRow_NamesTheLevel_InsteadOfThrowingKeyNotFound()
    {
        var factory = TestServiceFactory.Create();
        // Level 3 deactivated in the database — previously a bare KeyNotFoundException → HTTP 500
        factory.ConfigRepoMock.Setup(r => r.GetIntegrationLevelConfigsAsync())
            .ReturnsAsync(TestServiceFactory.DefaultIntegrationLevels
                .Where(c => c.IntegrationLevelId != 3).ToList());

        var act = () => factory.CalculatorService.CalculateAllVariantsAsync(
            CalculatorInputBuilder.Default().Build());

        var ex = (await act.Should().ThrowAsync<InvalidOperationException>()).Which;
        ex.Should().NotBeOfType<KeyNotFoundException>();
        ex.Message.Should().Contain("IntegrationLevel");
        ex.Message.Should().Contain("3", "the message must name which level is missing");
        ex.Message.Should().Contain("IsActive", "and point at the column to check");
    }

    [Fact]
    public async Task AllPricingRowsPresent_StillSucceeds()
    {
        var factory = TestServiceFactory.Create();

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(
            CalculatorInputBuilder.Default().Build());

        result.Advanced.TotalInvestment.Should().BeGreaterThan(0);
        result.Pro.TotalInvestment.Should().BeGreaterThan(result.Advanced.TotalInvestment);
        result.Premium.TotalInvestment.Should().BeGreaterThan(result.Pro.TotalInvestment,
            "each tier reads its own pricing row — a swapped key would break this ordering");
    }

    // ─── E5: the aux load window lives in one place ─────────────────────────────

    [Fact]
    public void TheAuxLoadWindowIsTenToNinetyPercent()
    {
        PlantLimits.MinAuxLoadFraction.Should().Be(0.10);
        PlantLimits.MaxAuxLoadFraction.Should().Be(0.90,
            "the Level 2 documentation used to claim 80% while the code enforced 90%");
    }

    // ─── E7: ClearCache takes the loaders' lock ─────────────────────────────────

    /// <summary>
    /// ClearCache used to mutate the three cache fields outside <c>_loadLock</c>. It now takes the
    /// lock, so a mismatched acquire/release would surface as a hang or a SemaphoreFullException
    /// under concurrency.
    ///
    /// Note the limit of this test: exercising the real race — a clear landing in the middle of an
    /// in-flight load — requires a SQL Server connection, so it is not covered here. What is covered
    /// is that the lock is acquired and released correctly on every path.
    /// </summary>
    [Fact]
    public async Task ClearCache_IsSafeUnderConcurrency_AndDoesNotLeakTheLock()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(unused);Database=(unused);"
            })
            .Build();

        var repository = new HybridConfigRepository(configuration);

        var clears = Enumerable.Range(0, 200).Select(_ => Task.Run(() => repository.ClearCache()));
        var all = Task.WhenAll(clears);

        var finished = await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(10)));

        finished.Should().BeSameAs(all, "200 concurrent clears must not deadlock on the load lock");
        all.Exception.Should().BeNull();

        // The semaphore still has its single slot: another clear returns immediately.
        var afterwards = Task.Run(() => repository.ClearCache());
        (await Task.WhenAny(afterwards, Task.Delay(TimeSpan.FromSeconds(5))))
            .Should().BeSameAs(afterwards, "the lock was released on every path");
    }
}
