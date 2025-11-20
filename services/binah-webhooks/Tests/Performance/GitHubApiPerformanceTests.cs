using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Binah.Webhooks.Tests.Helpers;

namespace Binah.Webhooks.Tests.Performance;

/// <summary>
/// Performance benchmarks for GitHub API operations
/// Measures time and rate limit overhead for common operations
///
/// Prerequisites:
/// - GITHUB_TEST_TOKEN: Personal access token
/// - GITHUB_TEST_REPO: Test repository
/// - RUN_PERFORMANCE_TESTS=true
///
/// WARNING: These tests make many API calls and may consume your rate limit.
/// GitHub API rate limits:
/// - Authenticated: 5,000 requests/hour
/// - Unauthenticated: 60 requests/hour
/// </summary>
[Collection("GitHub Performance Tests")]
public class GitHubApiPerformanceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly GitHubTestHelper? _testHelper;
    private readonly bool _testsEnabled;

    public GitHubApiPerformanceTests(ITestOutputHelper output)
    {
        _output = output;

        var testToken = Environment.GetEnvironmentVariable("GITHUB_TEST_TOKEN") ?? string.Empty;
        var testRepo = Environment.GetEnvironmentVariable("GITHUB_TEST_REPO") ?? string.Empty;
        var runTests = Environment.GetEnvironmentVariable("RUN_PERFORMANCE_TESTS");

        _testsEnabled = !string.IsNullOrEmpty(testToken)
            && !string.IsNullOrEmpty(testRepo)
            && runTests?.ToLower() == "true";

        if (_testsEnabled)
        {
            _testHelper = new GitHubTestHelper(testToken, testRepo);
        }
    }

    [Fact(Skip = "Performance test - set RUN_PERFORMANCE_TESTS=true to enable")]
    public async Task Benchmark_CreateBranches_MeasureTime()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            _output.WriteLine("Test skipped - credentials not provided");
            return;
        }

        // Benchmark: Time to create multiple branches
        const int branchCount = 10; // Reduced from 100 to avoid rate limits
        var stopwatch = Stopwatch.StartNew();
        var branches = new List<string>();

        try
        {
            for (int i = 0; i < branchCount; i++)
            {
                var branchName = await _testHelper.CreateTestBranchAsync($"perf-test-{i}");
                branches.Add(branchName);
            }

            stopwatch.Stop();

            var totalTime = stopwatch.ElapsedMilliseconds;
            var averageTime = totalTime / branchCount;
            var throughput = (branchCount * 1000.0) / totalTime; // branches per second

            _output.WriteLine($"=== Branch Creation Benchmark ===");
            _output.WriteLine($"Branches created: {branchCount}");
            _output.WriteLine($"Total time: {totalTime}ms");
            _output.WriteLine($"Average time per branch: {averageTime}ms");
            _output.WriteLine($"Throughput: {throughput:F2} branches/sec");

            // Assertions
            Assert.True(totalTime < 60000, $"Total time {totalTime}ms exceeded 60s limit");
            Assert.True(averageTime < 6000, $"Average time {averageTime}ms exceeded 6s per branch");
        }
        finally
        {
            // Cleanup
            foreach (var branch in branches)
            {
                await _testHelper.DeleteBranchAsync(branch);
            }
        }
    }

    [Fact(Skip = "Performance test - set RUN_PERFORMANCE_TESTS=true to enable")]
    public async Task Benchmark_CreateCommits_MeasureTime()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            _output.WriteLine("Test skipped - credentials not provided");
            return;
        }

        // Benchmark: Time to create multiple commits on a single branch
        const int commitCount = 10;
        var branchName = await _testHelper.CreateTestBranchAsync("perf-commits");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            for (int i = 0; i < commitCount; i++)
            {
                await _testHelper.CreateTestFileAsync(
                    branchName,
                    $"perf-test/file-{i}.txt",
                    $"Content for file {i}",
                    $"test: Add file {i}");
            }

            stopwatch.Stop();

            var totalTime = stopwatch.ElapsedMilliseconds;
            var averageTime = totalTime / commitCount;
            var throughput = (commitCount * 1000.0) / totalTime;

            _output.WriteLine($"=== Commit Creation Benchmark ===");
            _output.WriteLine($"Commits created: {commitCount}");
            _output.WriteLine($"Total time: {totalTime}ms");
            _output.WriteLine($"Average time per commit: {averageTime}ms");
            _output.WriteLine($"Throughput: {throughput:F2} commits/sec");

            Assert.True(totalTime < 120000, $"Total time {totalTime}ms exceeded 120s limit");
            Assert.True(averageTime < 12000, $"Average time {averageTime}ms exceeded 12s per commit");
        }
        finally
        {
            await _testHelper.DeleteBranchAsync(branchName);
        }
    }

    [Fact(Skip = "Performance test - set RUN_PERFORMANCE_TESTS=true to enable")]
    public async Task Benchmark_CreatePullRequests_MeasureTime()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            _output.WriteLine("Test skipped - credentials not provided");
            return;
        }

        // Benchmark: Time to create multiple pull requests
        const int prCount = 5; // Small number to avoid spam
        var stopwatch = Stopwatch.StartNew();
        var prs = new List<int>();
        var branches = new List<string>();

        try
        {
            for (int i = 0; i < prCount; i++)
            {
                // Create branch with a commit
                var branchName = await _testHelper.CreateTestBranchAsync($"perf-pr-{i}");
                branches.Add(branchName);

                await _testHelper.CreateTestFileAsync(
                    branchName,
                    $"perf-test/pr-{i}.txt",
                    $"Content for PR {i}",
                    $"test: Add PR {i} content");

                // Create PR
                var prNumber = await _testHelper.CreateTestPullRequestAsync(
                    branchName,
                    null,
                    $"Performance test PR {i}",
                    $"Automated performance test PR {i}");

                prs.Add(prNumber);
            }

            stopwatch.Stop();

            var totalTime = stopwatch.ElapsedMilliseconds;
            var averageTime = totalTime / prCount;
            var throughput = (prCount * 1000.0) / totalTime;

            _output.WriteLine($"=== Pull Request Creation Benchmark ===");
            _output.WriteLine($"PRs created: {prCount}");
            _output.WriteLine($"Total time: {totalTime}ms");
            _output.WriteLine($"Average time per PR: {averageTime}ms");
            _output.WriteLine($"Throughput: {throughput:F2} PRs/sec");

            Assert.True(totalTime < 180000, $"Total time {totalTime}ms exceeded 180s limit");
            Assert.True(averageTime < 36000, $"Average time {averageTime}ms exceeded 36s per PR");
        }
        finally
        {
            // Cleanup PRs and branches
            foreach (var prNumber in prs)
            {
                await _testHelper.ClosePullRequestAsync(prNumber);
            }
            foreach (var branch in branches)
            {
                await _testHelper.DeleteBranchAsync(branch);
            }
        }
    }

    [Fact(Skip = "Performance test - set RUN_PERFORMANCE_TESTS=true to enable")]
    public async Task Benchmark_RateLimitOverhead_Measure()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            _output.WriteLine("Test skipped - credentials not provided");
            return;
        }

        // Benchmark: Measure rate limit overhead
        // GitHub returns rate limit info in response headers
        // This test measures the overhead of checking rate limits

        const int iterations = 20;
        var timings = new List<long>();

        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var repo = await _testHelper.GetRepositoryAsync();
            stopwatch.Stop();

            timings.Add(stopwatch.ElapsedMilliseconds);
            Assert.NotNull(repo);

            // Small delay to avoid hitting rate limits
            await Task.Delay(100);
        }

        var avgTime = timings.Average();
        var minTime = timings.Min();
        var maxTime = timings.Max();
        var stdDev = CalculateStandardDeviation(timings);

        _output.WriteLine($"=== Rate Limit Overhead Benchmark ===");
        _output.WriteLine($"Iterations: {iterations}");
        _output.WriteLine($"Average response time: {avgTime:F2}ms");
        _output.WriteLine($"Min response time: {minTime}ms");
        _output.WriteLine($"Max response time: {maxTime}ms");
        _output.WriteLine($"Std deviation: {stdDev:F2}ms");

        // Most API calls should complete within 5 seconds
        Assert.True(avgTime < 5000, $"Average response time {avgTime}ms exceeded 5s");
    }

    [Fact(Skip = "Performance test - set RUN_PERFORMANCE_TESTS=true to enable")]
    public async Task Benchmark_EndToEndWorkflow_MeasureTime()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            _output.WriteLine("Test skipped - credentials not provided");
            return;
        }

        // Benchmark: Full autonomous PR workflow
        // Create branch → Commit files → Create PR

        var stopwatch = Stopwatch.StartNew();
        var branchName = string.Empty;
        var prNumber = 0;

        try
        {
            // Phase 1: Create branch
            var branchStart = stopwatch.ElapsedMilliseconds;
            branchName = await _testHelper.CreateTestBranchAsync("perf-e2e");
            var branchEnd = stopwatch.ElapsedMilliseconds;
            var branchTime = branchEnd - branchStart;

            // Phase 2: Create commits (simulate code generation)
            var commitStart = stopwatch.ElapsedMilliseconds;
            await _testHelper.CreateTestFileAsync(
                branchName,
                "schemas/test.yaml",
                "test: content",
                "feat: Add schema");
            await _testHelper.CreateTestFileAsync(
                branchName,
                "models/Test.cs",
                "// generated code",
                "feat: Add generated model");
            var commitEnd = stopwatch.ElapsedMilliseconds;
            var commitTime = commitEnd - commitStart;

            // Phase 3: Create PR
            var prStart = stopwatch.ElapsedMilliseconds;
            prNumber = await _testHelper.CreateTestPullRequestAsync(
                branchName,
                null,
                "E2E Performance Test PR",
                "Automated E2E performance test");
            var prEnd = stopwatch.ElapsedMilliseconds;
            var prTime = prEnd - prStart;

            stopwatch.Stop();
            var totalTime = stopwatch.ElapsedMilliseconds;

            _output.WriteLine($"=== End-to-End Workflow Benchmark ===");
            _output.WriteLine($"Phase 1 - Create branch: {branchTime}ms");
            _output.WriteLine($"Phase 2 - Create commits: {commitTime}ms");
            _output.WriteLine($"Phase 3 - Create PR: {prTime}ms");
            _output.WriteLine($"Total workflow time: {totalTime}ms");
            _output.WriteLine($"");
            _output.WriteLine($"Breakdown:");
            _output.WriteLine($"  Branch: {(branchTime * 100.0 / totalTime):F1}%");
            _output.WriteLine($"  Commits: {(commitTime * 100.0 / totalTime):F1}%");
            _output.WriteLine($"  PR: {(prTime * 100.0 / totalTime):F1}%");

            // Full workflow should complete within 30 seconds
            Assert.True(totalTime < 30000, $"E2E workflow {totalTime}ms exceeded 30s limit");
        }
        finally
        {
            if (prNumber > 0)
            {
                await _testHelper.ClosePullRequestAsync(prNumber);
            }
            if (!string.IsNullOrEmpty(branchName))
            {
                await _testHelper.DeleteBranchAsync(branchName);
            }
        }
    }

    private double CalculateStandardDeviation(List<long> values)
    {
        var avg = values.Average();
        var sumOfSquares = values.Sum(val => Math.Pow(val - avg, 2));
        return Math.Sqrt(sumOfSquares / values.Count);
    }

    public void Dispose()
    {
        _testHelper?.Dispose();
    }
}
