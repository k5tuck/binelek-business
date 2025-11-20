using Binah.Webhooks.Models.DTOs.GitHub;
using Binah.Webhooks.Models.Domain;
using Binah.Webhooks.Models.Events;
using Binah.Webhooks.Services.Implementations;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Repositories.Interfaces;
using Binah.Infrastructure.Kafka;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Binah.Webhooks.Tests.Unit;

/// <summary>
/// Unit tests for AutonomousPRService
/// Tests the complete PR creation workflow with mocked dependencies
/// </summary>
public class AutonomousPRServiceTests
{
    private readonly Mock<IGitHubBranchService> _mockBranchService;
    private readonly Mock<IGitHubCommitService> _mockCommitService;
    private readonly Mock<IGitHubPullRequestService> _mockPullRequestService;
    private readonly Mock<IPullRequestTemplateService> _mockTemplateService;
    private readonly Mock<IAutonomousPullRequestRepository> _mockRepository;
    private readonly Mock<KafkaProducer> _mockKafkaProducer;
    private readonly Mock<ILogger<AutonomousPRService>> _mockLogger;
    private readonly AutonomousPRService _service;

    public AutonomousPRServiceTests()
    {
        _mockBranchService = new Mock<IGitHubBranchService>();
        _mockCommitService = new Mock<IGitHubCommitService>();
        _mockPullRequestService = new Mock<IGitHubPullRequestService>();
        _mockTemplateService = new Mock<IPullRequestTemplateService>();
        _mockRepository = new Mock<IAutonomousPullRequestRepository>();
        _mockKafkaProducer = new Mock<KafkaProducer>("localhost:9092");
        _mockLogger = new Mock<ILogger<AutonomousPRService>>();

        _service = new AutonomousPRService(
            _mockBranchService.Object,
            _mockCommitService.Object,
            _mockPullRequestService.Object,
            _mockTemplateService.Object,
            _mockRepository.Object,
            _mockKafkaProducer.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateAutonomousPRAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = CreateTestRequest();
        var baseBranchSha = "abc123";
        var commitSha = "def456";
        var prNumber = 42;

        SetupSuccessfulWorkflow(baseBranchSha, commitSha, prNumber);

        // Act
        var result = await _service.CreateAutonomousPRAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(prNumber, result.PrNumber);
        Assert.NotNull(result.PrUrl);
        Assert.NotNull(result.BranchName);
        Assert.Equal(commitSha, result.CommitSha);
        Assert.Equal("open", result.Status);

        // Verify workflow steps executed
        _mockBranchService.Verify(x => x.BranchExistsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);

        _mockBranchService.Verify(x => x.GetBranchHeadShaAsync(
            request.RepositoryOwner,
            request.RepositoryName,
            request.BaseBranch), Times.Once);

        _mockBranchService.Verify(x => x.CreateBranchAsync(
            request.RepositoryOwner,
            request.RepositoryName,
            It.IsAny<string>(),
            baseBranchSha), Times.Once);

        _mockCommitService.Verify(x => x.CreateCommitAsync(
            request.RepositoryOwner,
            request.RepositoryName,
            It.IsAny<string>(),
            It.IsAny<string>(),
            request.Files), Times.Once);

        _mockPullRequestService.Verify(x => x.CreatePullRequestAsync(
            request.RepositoryOwner,
            request.RepositoryName,
            It.IsAny<CreatePullRequestRequest>(),
            request.TenantId), Times.Once);

        _mockRepository.Verify(x => x.CreateAsync(
            It.IsAny<AutonomousPullRequest>()), Times.Once);

        _mockKafkaProducer.Verify(x => x.ProduceAsync(
            "autonomous.pr.created.v1",
            It.IsAny<AutonomousPRCreatedEvent>()), Times.Once);
    }

    [Fact]
    public async Task CreateAutonomousPRAsync_BranchExists_DeletesAndRecreates()
    {
        // Arrange
        var request = CreateTestRequest();
        var baseBranchSha = "abc123";

        _mockBranchService.Setup(x => x.BranchExistsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(true);

        _mockBranchService.Setup(x => x.DeleteBranchAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        SetupSuccessfulWorkflow(baseBranchSha, "commit123", 42);

        // Act
        var result = await _service.CreateAutonomousPRAsync(request);

        // Assert
        Assert.True(result.Success);

        _mockBranchService.Verify(x => x.DeleteBranchAsync(
            request.RepositoryOwner,
            request.RepositoryName,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateAutonomousPRAsync_BranchCreationFails_ReturnsFailure()
    {
        // Arrange
        var request = CreateTestRequest();
        var errorMessage = "Branch creation failed";

        _mockBranchService.Setup(x => x.BranchExistsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockBranchService.Setup(x => x.GetBranchHeadShaAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync("abc123");

        _mockBranchService.Setup(x => x.CreateBranchAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        // Act
        var result = await _service.CreateAutonomousPRAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(errorMessage, result.ErrorMessage);
        Assert.Equal("failed", result.Status);

        // Verify failure event published
        _mockKafkaProducer.Verify(x => x.ProduceAsync(
            "autonomous.pr.failed.v1",
            It.IsAny<AutonomousPRFailedEvent>()), Times.Once);
    }

    [Fact]
    public async Task CreateAutonomousPRAsync_CommitFails_RollsBackBranch()
    {
        // Arrange
        var request = CreateTestRequest();
        var baseBranchSha = "abc123";

        _mockBranchService.Setup(x => x.BranchExistsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockBranchService.Setup(x => x.GetBranchHeadShaAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(baseBranchSha);

        _mockBranchService.Setup(x => x.CreateBranchAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockCommitService.Setup(x => x.CreateCommitAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<GitHubFileChange>>()))
            .ThrowsAsync(new InvalidOperationException("Commit failed"));

        // Setup rollback
        _mockBranchService.Setup(x => x.BranchExistsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(true);

        _mockBranchService.Setup(x => x.DeleteBranchAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAutonomousPRAsync(request);

        // Assert
        Assert.False(result.Success);

        // Verify rollback attempted
        _mockBranchService.Verify(x => x.DeleteBranchAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateAutonomousPRAsync_PRCreationFails_PublishesFailureEvent()
    {
        // Arrange
        var request = CreateTestRequest();
        var baseBranchSha = "abc123";
        var commitSha = "def456";

        _mockBranchService.Setup(x => x.BranchExistsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockBranchService.Setup(x => x.GetBranchHeadShaAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(baseBranchSha);

        _mockBranchService.Setup(x => x.CreateBranchAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockCommitService.Setup(x => x.CreateCommitAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<GitHubFileChange>>()))
            .ReturnsAsync(commitSha);

        _mockTemplateService.Setup(x => x.GenerateOntologyRefactoringDescription(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<List<string>>()))
            .Returns("Test PR description");

        _mockPullRequestService.Setup(x => x.CreatePullRequestAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CreatePullRequestRequest>(),
            It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("PR creation failed"));

        // Act
        var result = await _service.CreateAutonomousPRAsync(request);

        // Assert
        Assert.False(result.Success);

        _mockKafkaProducer.Verify(x => x.ProduceAsync(
            "autonomous.pr.failed.v1",
            It.Is<AutonomousPRFailedEvent>(e => e.Payload.FailureStage == "pr_creation")),
            Times.Once);
    }

    [Fact]
    public async Task GetPRStatusAsync_ValidPRId_ReturnsStatus()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString();
        var prId = Guid.NewGuid();
        var pr = new AutonomousPullRequest
        {
            Id = prId,
            TenantId = Guid.Parse(tenantId),
            PrNumber = 42,
            RepositoryName = "k5tuck/Binelek",
            BranchName = "claude/test-branch",
            Title = "Test PR",
            Status = "open",
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(x => x.GetByIdAsync(prId))
            .ReturnsAsync(pr);

        _mockPullRequestService.Setup(x => x.GetPullRequestAsync(
            "k5tuck",
            "Binelek",
            42,
            tenantId))
            .ReturnsAsync(new PullRequestResponse
            {
                Number = 42,
                State = "open",
                HtmlUrl = "https://github.com/k5tuck/Binelek/pull/42",
                ChangedFiles = 2
            });

        // Act
        var result = await _service.GetPRStatusAsync(tenantId, prId.ToString());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(42, result.PrNumber);
        Assert.Equal("open", result.Status);
    }

    [Fact]
    public async Task ListAutonomousPRsAsync_NoFilter_ReturnsAllPRs()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var prs = new List<AutonomousPullRequest>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Status = "open" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Status = "merged" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Status = "closed" }
        };

        _mockRepository.Setup(x => x.GetByTenantAsync(tenantId))
            .ReturnsAsync(prs);

        // Act
        var result = await _service.ListAutonomousPRsAsync(tenantId.ToString());

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task ListAutonomousPRsAsync_WithStatusFilter_ReturnsFilteredPRs()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var prs = new List<AutonomousPullRequest>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Status = "open" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Status = "open" }
        };

        _mockRepository.Setup(x => x.GetOpenByTenantAsync(tenantId))
            .ReturnsAsync(prs);

        // Act
        var result = await _service.ListAutonomousPRsAsync(tenantId.ToString(), "open");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, pr => Assert.Equal("open", pr.Status));
    }

    [Fact]
    public async Task CloseAutonomousPRAsync_ValidPR_ReturnsTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString();
        var prId = Guid.NewGuid();
        var pr = new AutonomousPullRequest
        {
            Id = prId,
            TenantId = Guid.Parse(tenantId),
            PrNumber = 42,
            RepositoryName = "k5tuck/Binelek",
            Status = "open"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(prId))
            .ReturnsAsync(pr);

        _mockPullRequestService.Setup(x => x.ClosePullRequestAsync(
            "k5tuck",
            "Binelek",
            42,
            tenantId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CloseAutonomousPRAsync(tenantId, prId.ToString());

        // Assert
        Assert.True(result);

        _mockRepository.Verify(x => x.UpdateStatusAsync(prId, "closed", null), Times.Once);
    }

    [Fact]
    public async Task MergeAutonomousPRAsync_ValidPR_PublishesMergedEvent()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString();
        var prId = Guid.NewGuid();
        var pr = new AutonomousPullRequest
        {
            Id = prId,
            TenantId = Guid.Parse(tenantId),
            PrNumber = 42,
            RepositoryName = "k5tuck/Binelek",
            Status = "open",
            WorkflowType = "OntologyRefactoring",
            BranchName = "claude/test",
            Title = "Test PR"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(prId))
            .ReturnsAsync(pr);

        _mockPullRequestService.Setup(x => x.MergePullRequestAsync(
            "k5tuck",
            "Binelek",
            42,
            It.IsAny<MergePullRequestRequest>(),
            tenantId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.MergeAutonomousPRAsync(tenantId, prId.ToString());

        // Assert
        Assert.True(result);

        _mockRepository.Verify(x => x.UpdateStatusAsync(
            prId,
            "merged",
            It.IsAny<DateTime>()), Times.Once);

        _mockKafkaProducer.Verify(x => x.ProduceAsync(
            "autonomous.pr.merged.v1",
            It.IsAny<AutonomousPRMergedEvent>()), Times.Once);
    }

    #region Helper Methods

    private CreateAutonomousPRRequest CreateTestRequest()
    {
        return new CreateAutonomousPRRequest
        {
            TenantId = Guid.NewGuid().ToString(),
            RepositoryOwner = "k5tuck",
            RepositoryName = "Binelek",
            BaseBranch = "main",
            BranchPrefix = "claude/test",
            Title = "Test Autonomous PR",
            WorkflowType = WorkflowType.OntologyRefactoring,
            Files = new List<GitHubFileChange>
            {
                new() { Path = "test.yaml", Content = "test content", Mode = GitHubFileChangeMode.Add }
            },
            TemplateData = new Dictionary<string, string>
            {
                { "entityName", "TestEntity" },
                { "addedProperties", "1" },
                { "updatedRelationships", "0" },
                { "refactoredValidators", "1" }
            },
            CommitMessage = "test(ontology): Test commit",
            Reviewers = new List<string> { "k5tuck" },
            Labels = new List<string> { "auto-generated" }
        };
    }

    private void SetupSuccessfulWorkflow(string baseBranchSha, string commitSha, int prNumber)
    {
        _mockBranchService.Setup(x => x.BranchExistsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockBranchService.Setup(x => x.GetBranchHeadShaAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(baseBranchSha);

        _mockBranchService.Setup(x => x.CreateBranchAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockCommitService.Setup(x => x.CreateCommitAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<GitHubFileChange>>()))
            .ReturnsAsync(commitSha);

        _mockTemplateService.Setup(x => x.GenerateOntologyRefactoringDescription(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<List<string>>()))
            .Returns("Test PR description");

        _mockPullRequestService.Setup(x => x.CreatePullRequestAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CreatePullRequestRequest>(),
            It.IsAny<string>()))
            .ReturnsAsync(new PullRequestResponse
            {
                Number = prNumber,
                HtmlUrl = $"https://github.com/k5tuck/Binelek/pull/{prNumber}",
                State = "open",
                CreatedAt = DateTime.UtcNow
            });

        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<AutonomousPullRequest>()))
            .ReturnsAsync((AutonomousPullRequest pr) => pr);

        _mockKafkaProducer.Setup(x => x.ProduceAsync(
            It.IsAny<string>(),
            It.IsAny<object>()))
            .Returns(Task.CompletedTask);
    }

    #endregion
}
