using System.Text.Json;
using Binah.Webhooks.Models.DTOs.GitHub;
using Binah.Webhooks.Services.Implementations;
using Binah.Webhooks.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Binah.Webhooks.Tests;

/// <summary>
/// Unit tests for GitHubEventParser service
/// </summary>
public class GitHubEventParserTests
{
    private readonly IGitHubEventParser _parser;
    private readonly Mock<ILogger<GitHubEventParser>> _mockLogger;

    public GitHubEventParserTests()
    {
        _mockLogger = new Mock<ILogger<GitHubEventParser>>();
        _parser = new GitHubEventParser(_mockLogger.Object);
    }

    #region Push Event Tests

    [Fact]
    public void ParseEvent_ValidPushEvent_ReturnsPushEventDto()
    {
        // Arrange
        var payload = @"{
            ""ref"": ""refs/heads/main"",
            ""before"": ""abc123"",
            ""after"": ""def456"",
            ""created"": false,
            ""deleted"": false,
            ""forced"": false,
            ""compare"": ""https://github.com/owner/repo/compare/abc123...def456"",
            ""commits"": [
                {
                    ""id"": ""def456"",
                    ""tree_id"": ""tree123"",
                    ""distinct"": true,
                    ""message"": ""feat: Add new feature"",
                    ""timestamp"": ""2025-11-15T10:00:00Z"",
                    ""url"": ""https://github.com/owner/repo/commit/def456"",
                    ""author"": {
                        ""name"": ""John Doe"",
                        ""email"": ""john@example.com"",
                        ""username"": ""johndoe""
                    },
                    ""committer"": {
                        ""name"": ""John Doe"",
                        ""email"": ""john@example.com"",
                        ""username"": ""johndoe""
                    },
                    ""added"": [""file1.cs""],
                    ""removed"": [],
                    ""modified"": [""file2.cs""]
                }
            ],
            ""head_commit"": {
                ""id"": ""def456"",
                ""tree_id"": ""tree123"",
                ""distinct"": true,
                ""message"": ""feat: Add new feature"",
                ""timestamp"": ""2025-11-15T10:00:00Z"",
                ""url"": ""https://github.com/owner/repo/commit/def456"",
                ""author"": {
                    ""name"": ""John Doe"",
                    ""email"": ""john@example.com""
                },
                ""committer"": {
                    ""name"": ""John Doe"",
                    ""email"": ""john@example.com""
                },
                ""added"": [],
                ""removed"": [],
                ""modified"": []
            },
            ""repository"": {
                ""id"": 123456,
                ""name"": ""repo"",
                ""full_name"": ""owner/repo"",
                ""private"": false,
                ""html_url"": ""https://github.com/owner/repo"",
                ""default_branch"": ""main"",
                ""owner"": {
                    ""id"": 789,
                    ""login"": ""owner"",
                    ""type"": ""User"",
                    ""avatar_url"": ""https://github.com/avatar.png"",
                    ""html_url"": ""https://github.com/owner""
                }
            },
            ""sender"": {
                ""id"": 789,
                ""login"": ""owner"",
                ""type"": ""User"",
                ""avatar_url"": ""https://github.com/avatar.png"",
                ""html_url"": ""https://github.com/owner""
            }
        }";

        // Act
        var result = _parser.ParseEvent<PushEventDto>("push", payload);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("refs/heads/main", result.Ref);
        Assert.Equal("abc123", result.Before);
        Assert.Equal("def456", result.After);
        Assert.False(result.Created);
        Assert.False(result.Deleted);
        Assert.Single(result.Commits);
        Assert.Equal("def456", result.Commits[0].Id);
        Assert.Equal("feat: Add new feature", result.Commits[0].Message);
        Assert.NotNull(result.Repository);
        Assert.Equal("owner/repo", result.Repository.FullName);
        Assert.NotNull(result.Sender);
        Assert.Equal("owner", result.Sender.Login);
    }

    [Fact]
    public void ParseEvent_PushEventMissingRef_ThrowsInvalidOperationException()
    {
        // Arrange
        var payload = @"{
            ""after"": ""def456"",
            ""commits"": [],
            ""repository"": {
                ""id"": 123,
                ""name"": ""repo"",
                ""full_name"": ""owner/repo"",
                ""private"": false,
                ""html_url"": ""https://github.com/owner/repo"",
                ""default_branch"": ""main""
            },
            ""sender"": {
                ""id"": 789,
                ""login"": ""owner"",
                ""type"": ""User"",
                ""avatar_url"": ""https://github.com/avatar.png"",
                ""html_url"": ""https://github.com/owner""
            }
        }";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _parser.ParseEvent<PushEventDto>("push", payload)
        );
        Assert.Contains("ref", exception.Message.ToLower());
    }

    #endregion

    #region Pull Request Event Tests

    [Fact]
    public void ParseEvent_ValidPullRequestEvent_ReturnsPullRequestEventDto()
    {
        // Arrange
        var payload = @"{
            ""action"": ""opened"",
            ""number"": 42,
            ""pull_request"": {
                ""id"": 987654,
                ""number"": 42,
                ""state"": ""open"",
                ""locked"": false,
                ""title"": ""Add new feature"",
                ""body"": ""This PR adds a new feature"",
                ""draft"": false,
                ""merged"": false,
                ""mergeable"": true,
                ""mergeable_state"": ""clean"",
                ""comments"": 0,
                ""review_comments"": 0,
                ""commits"": 1,
                ""additions"": 10,
                ""deletions"": 5,
                ""changed_files"": 2,
                ""created_at"": ""2025-11-15T10:00:00Z"",
                ""updated_at"": ""2025-11-15T10:00:00Z"",
                ""html_url"": ""https://github.com/owner/repo/pull/42"",
                ""user"": {
                    ""id"": 789,
                    ""login"": ""developer"",
                    ""type"": ""User"",
                    ""avatar_url"": ""https://github.com/avatar.png"",
                    ""html_url"": ""https://github.com/developer""
                },
                ""head"": {
                    ""label"": ""developer:feature-branch"",
                    ""ref"": ""feature-branch"",
                    ""sha"": ""abc123""
                },
                ""base"": {
                    ""label"": ""owner:main"",
                    ""ref"": ""main"",
                    ""sha"": ""def456""
                },
                ""labels"": [],
                ""requested_reviewers"": [],
                ""assignees"": []
            },
            ""repository"": {
                ""id"": 123456,
                ""name"": ""repo"",
                ""full_name"": ""owner/repo"",
                ""private"": false,
                ""html_url"": ""https://github.com/owner/repo"",
                ""default_branch"": ""main""
            },
            ""sender"": {
                ""id"": 789,
                ""login"": ""developer"",
                ""type"": ""User"",
                ""avatar_url"": ""https://github.com/avatar.png"",
                ""html_url"": ""https://github.com/developer""
            }
        }";

        // Act
        var result = _parser.ParseEvent<PullRequestEventDto>("pull_request", payload);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("opened", result.Action);
        Assert.Equal(42, result.Number);
        Assert.NotNull(result.PullRequest);
        Assert.Equal("open", result.PullRequest.State);
        Assert.Equal("Add new feature", result.PullRequest.Title);
        Assert.False(result.PullRequest.Draft);
        Assert.NotNull(result.PullRequest.Head);
        Assert.Equal("feature-branch", result.PullRequest.Head.Ref);
        Assert.NotNull(result.PullRequest.Base);
        Assert.Equal("main", result.PullRequest.Base.Ref);
    }

    #endregion

    #region Issues Event Tests

    [Fact]
    public void ParseEvent_ValidIssuesEvent_ReturnsIssuesEventDto()
    {
        // Arrange
        var payload = @"{
            ""action"": ""opened"",
            ""issue"": {
                ""id"": 555555,
                ""number"": 10,
                ""title"": ""Bug report"",
                ""body"": ""There is a bug"",
                ""state"": ""open"",
                ""locked"": false,
                ""comments"": 0,
                ""html_url"": ""https://github.com/owner/repo/issues/10"",
                ""created_at"": ""2025-11-15T10:00:00Z"",
                ""updated_at"": ""2025-11-15T10:00:00Z"",
                ""user"": {
                    ""id"": 789,
                    ""login"": ""reporter"",
                    ""type"": ""User"",
                    ""avatar_url"": ""https://github.com/avatar.png"",
                    ""html_url"": ""https://github.com/reporter""
                },
                ""labels"": [],
                ""assignees"": []
            },
            ""repository"": {
                ""id"": 123456,
                ""name"": ""repo"",
                ""full_name"": ""owner/repo"",
                ""private"": false,
                ""html_url"": ""https://github.com/owner/repo"",
                ""default_branch"": ""main""
            },
            ""sender"": {
                ""id"": 789,
                ""login"": ""reporter"",
                ""type"": ""User"",
                ""avatar_url"": ""https://github.com/avatar.png"",
                ""html_url"": ""https://github.com/reporter""
            }
        }";

        // Act
        var result = _parser.ParseEvent<IssuesEventDto>("issues", payload);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("opened", result.Action);
        Assert.NotNull(result.Issue);
        Assert.Equal(10, result.Issue.Number);
        Assert.Equal("Bug report", result.Issue.Title);
        Assert.Equal("open", result.Issue.State);
    }

    #endregion

    #region Issue Comment Event Tests

    [Fact]
    public void ParseEvent_ValidIssueCommentEvent_ReturnsIssueCommentEventDto()
    {
        // Arrange
        var payload = @"{
            ""action"": ""created"",
            ""issue"": {
                ""id"": 555555,
                ""number"": 10,
                ""title"": ""Bug report"",
                ""state"": ""open"",
                ""locked"": false,
                ""comments"": 1,
                ""html_url"": ""https://github.com/owner/repo/issues/10"",
                ""created_at"": ""2025-11-15T10:00:00Z"",
                ""updated_at"": ""2025-11-15T10:05:00Z"",
                ""user"": {
                    ""id"": 789,
                    ""login"": ""reporter"",
                    ""type"": ""User"",
                    ""avatar_url"": ""https://github.com/avatar.png"",
                    ""html_url"": ""https://github.com/reporter""
                },
                ""labels"": [],
                ""assignees"": []
            },
            ""comment"": {
                ""id"": 999999,
                ""body"": ""This is a comment"",
                ""html_url"": ""https://github.com/owner/repo/issues/10#issuecomment-999999"",
                ""created_at"": ""2025-11-15T10:05:00Z"",
                ""updated_at"": ""2025-11-15T10:05:00Z"",
                ""author_association"": ""CONTRIBUTOR"",
                ""user"": {
                    ""id"": 888,
                    ""login"": ""commenter"",
                    ""type"": ""User"",
                    ""avatar_url"": ""https://github.com/avatar2.png"",
                    ""html_url"": ""https://github.com/commenter""
                }
            },
            ""repository"": {
                ""id"": 123456,
                ""name"": ""repo"",
                ""full_name"": ""owner/repo"",
                ""private"": false,
                ""html_url"": ""https://github.com/owner/repo"",
                ""default_branch"": ""main""
            },
            ""sender"": {
                ""id"": 888,
                ""login"": ""commenter"",
                ""type"": ""User"",
                ""avatar_url"": ""https://github.com/avatar2.png"",
                ""html_url"": ""https://github.com/commenter""
            }
        }";

        // Act
        var result = _parser.ParseEvent<IssueCommentEventDto>("issue_comment", payload);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("created", result.Action);
        Assert.NotNull(result.Issue);
        Assert.NotNull(result.Comment);
        Assert.Equal(999999, result.Comment.Id);
        Assert.Equal("This is a comment", result.Comment.Body);
        Assert.Equal("CONTRIBUTOR", result.Comment.AuthorAssociation);
    }

    #endregion

    #region Pull Request Review Event Tests

    [Fact]
    public void ParseEvent_ValidPullRequestReviewEvent_ReturnsPullRequestReviewEventDto()
    {
        // Arrange
        var payload = @"{
            ""action"": ""submitted"",
            ""review"": {
                ""id"": 777777,
                ""body"": ""Looks good to me!"",
                ""state"": ""APPROVED"",
                ""html_url"": ""https://github.com/owner/repo/pull/42#pullrequestreview-777777"",
                ""commit_id"": ""abc123"",
                ""submitted_at"": ""2025-11-15T11:00:00Z"",
                ""author_association"": ""MEMBER"",
                ""user"": {
                    ""id"": 999,
                    ""login"": ""reviewer"",
                    ""type"": ""User"",
                    ""avatar_url"": ""https://github.com/avatar3.png"",
                    ""html_url"": ""https://github.com/reviewer""
                }
            },
            ""pull_request"": {
                ""id"": 987654,
                ""number"": 42,
                ""state"": ""open"",
                ""locked"": false,
                ""title"": ""Add new feature"",
                ""draft"": false,
                ""merged"": false,
                ""comments"": 0,
                ""review_comments"": 1,
                ""commits"": 1,
                ""additions"": 10,
                ""deletions"": 5,
                ""changed_files"": 2,
                ""html_url"": ""https://github.com/owner/repo/pull/42"",
                ""created_at"": ""2025-11-15T10:00:00Z"",
                ""updated_at"": ""2025-11-15T11:00:00Z"",
                ""user"": {
                    ""id"": 789,
                    ""login"": ""developer"",
                    ""type"": ""User"",
                    ""avatar_url"": ""https://github.com/avatar.png"",
                    ""html_url"": ""https://github.com/developer""
                },
                ""head"": {
                    ""label"": ""developer:feature-branch"",
                    ""ref"": ""feature-branch"",
                    ""sha"": ""abc123""
                },
                ""base"": {
                    ""label"": ""owner:main"",
                    ""ref"": ""main"",
                    ""sha"": ""def456""
                },
                ""labels"": [],
                ""requested_reviewers"": [],
                ""assignees"": []
            },
            ""repository"": {
                ""id"": 123456,
                ""name"": ""repo"",
                ""full_name"": ""owner/repo"",
                ""private"": false,
                ""html_url"": ""https://github.com/owner/repo"",
                ""default_branch"": ""main""
            },
            ""sender"": {
                ""id"": 999,
                ""login"": ""reviewer"",
                ""type"": ""User"",
                ""avatar_url"": ""https://github.com/avatar3.png"",
                ""html_url"": ""https://github.com/reviewer""
            }
        }";

        // Act
        var result = _parser.ParseEvent<PullRequestReviewEventDto>("pull_request_review", payload);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("submitted", result.Action);
        Assert.NotNull(result.Review);
        Assert.Equal("APPROVED", result.Review.State);
        Assert.Equal("Looks good to me!", result.Review.Body);
        Assert.NotNull(result.PullRequest);
        Assert.Equal(42, result.PullRequest.Number);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void ParseEvent_NullEventType_ThrowsArgumentNullException()
    {
        // Arrange
        var payload = @"{""repository"":{},""sender"":{}}";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _parser.ParseEvent<GitHubWebhookEventDto>(null!, payload)
        );
    }

    [Fact]
    public void ParseEvent_EmptyEventType_ThrowsArgumentNullException()
    {
        // Arrange
        var payload = @"{""repository"":{},""sender"":{}}";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _parser.ParseEvent<GitHubWebhookEventDto>("", payload)
        );
    }

    [Fact]
    public void ParseEvent_NullPayload_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _parser.ParseEvent<GitHubWebhookEventDto>("push", null!)
        );
    }

    [Fact]
    public void ParseEvent_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var payload = "{ invalid json }";

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            _parser.ParseEvent<PushEventDto>("push", payload)
        );
    }

    [Fact]
    public void ParseEvent_UnsupportedEventType_ThrowsNotSupportedException()
    {
        // Arrange
        var payload = @"{""repository"":{},""sender"":{}}";

        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            _parser.ParseEvent("unknown_event", payload)
        );
    }

    [Fact]
    public void ParseEvent_MismatchedEventType_ThrowsArgumentException()
    {
        // Arrange
        var payload = @"{
            ""ref"": ""refs/heads/main"",
            ""after"": ""abc123"",
            ""commits"": [],
            ""repository"": {""id"":123,""name"":""repo"",""full_name"":""owner/repo"",""private"":false,""html_url"":""https://github.com/owner/repo"",""default_branch"":""main""},
            ""sender"": {""id"":789,""login"":""owner"",""type"":""User"",""avatar_url"":""https://github.com/avatar.png"",""html_url"":""https://github.com/owner""}
        }";

        // Act & Assert - trying to parse a push event as a pull_request
        Assert.Throws<ArgumentException>(() =>
            _parser.ParseEvent<PullRequestEventDto>("push", payload)
        );
    }

    [Fact]
    public void ParseEvent_MissingRepository_ThrowsInvalidOperationException()
    {
        // Arrange
        var payload = @"{
            ""action"": ""opened"",
            ""sender"": {""id"":789,""login"":""owner"",""type"":""User"",""avatar_url"":""https://github.com/avatar.png"",""html_url"":""https://github.com/owner""}
        }";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _parser.ParseEvent("issues", payload)
        );
        Assert.Contains("repository", exception.Message.ToLower());
    }

    #endregion

    #region Untyped ParseEvent Tests

    [Fact]
    public void ParseEvent_UntypedPushEvent_ReturnsPushEventDto()
    {
        // Arrange
        var payload = @"{
            ""ref"": ""refs/heads/main"",
            ""after"": ""abc123"",
            ""commits"": [],
            ""repository"": {""id"":123,""name"":""repo"",""full_name"":""owner/repo"",""private"":false,""html_url"":""https://github.com/owner/repo"",""default_branch"":""main""},
            ""sender"": {""id"":789,""login"":""owner"",""type"":""User"",""avatar_url"":""https://github.com/avatar.png"",""html_url"":""https://github.com/owner""}
        }";

        // Act
        var result = _parser.ParseEvent("push", payload);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<PushEventDto>(result);
        var pushEvent = (PushEventDto)result;
        Assert.Equal("refs/heads/main", pushEvent.Ref);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void ValidateEventPayload_ValidPayload_ReturnsTrue()
    {
        // Arrange
        var payload = @"{
            ""ref"": ""refs/heads/main"",
            ""after"": ""abc123"",
            ""commits"": [],
            ""repository"": {""id"":123,""name"":""repo"",""full_name"":""owner/repo"",""private"":false,""html_url"":""https://github.com/owner/repo"",""default_branch"":""main""},
            ""sender"": {""id"":789,""login"":""owner"",""type"":""User"",""avatar_url"":""https://github.com/avatar.png"",""html_url"":""https://github.com/owner""}
        }";

        // Act
        var result = _parser.ValidateEventPayload("push", payload);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateEventPayload_InvalidPayload_ReturnsFalse()
    {
        // Arrange
        var payload = "{ invalid json }";

        // Act
        var result = _parser.ValidateEventPayload("push", payload);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetSupportedEventTypes Tests

    [Fact]
    public void GetSupportedEventTypes_ReturnsExpectedTypes()
    {
        // Act
        var result = _parser.GetSupportedEventTypes();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("push", result);
        Assert.Contains("pull_request", result);
        Assert.Contains("issues", result);
        Assert.Contains("issue_comment", result);
        Assert.Contains("pull_request_review", result);
        Assert.Equal(5, result.Length);
    }

    #endregion
}
