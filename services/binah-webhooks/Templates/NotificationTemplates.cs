using Binah.Webhooks.Models.DTOs.Notifications;
using System.Text.Json;

namespace Binah.Webhooks.Templates;

/// <summary>
/// Templates for generating notification messages
/// </summary>
public static class NotificationTemplates
{
    /// <summary>
    /// Generate Slack message for PR created event
    /// </summary>
    public static object PRCreatedSlackMessage(PullRequestNotificationData prData)
    {
        var workflowEmoji = prData.WorkflowType?.ToLower() switch
        {
            "ontology_refactoring" => "🔄",
            "code_generation" => "⚙️",
            "bug_fix" => "🐛",
            "feature_addition" => "✨",
            _ => "🤖"
        };

        return new
        {
            text = $"{workflowEmoji} New Autonomous PR Created",
            blocks = new object[]
            {
                new
                {
                    type = "header",
                    text = new
                    {
                        type = "plain_text",
                        text = $"{workflowEmoji} Auto-generated: {prData.Title}"
                    }
                },
                new
                {
                    type = "section",
                    fields = new object[]
                    {
                        new { type = "mrkdwn", text = $"*Repository:*\n{prData.Repository}" },
                        new { type = "mrkdwn", text = $"*PR Number:*\n#{prData.PrNumber}" },
                        new { type = "mrkdwn", text = $"*Workflow:*\n{FormatWorkflowType(prData.WorkflowType)}" },
                        new { type = "mrkdwn", text = $"*Status:*\n{FormatStatus(prData.Status)}" }
                    }
                },
                new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = $"*Branch:* `{prData.BranchName}`\n*Commits:* {prData.CommitCount ?? 0} | *Files Changed:* {prData.FileCount ?? 0}"
                    }
                },
                new
                {
                    type = "actions",
                    elements = new object[]
                    {
                        new
                        {
                            type = "button",
                            text = new { type = "plain_text", text = "View PR" },
                            url = prData.Url,
                            style = "primary"
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Generate Slack message for PR merged event
    /// </summary>
    public static object PRMergedSlackMessage(PullRequestNotificationData prData)
    {
        return new
        {
            text = "✅ PR Merged Successfully",
            blocks = new object[]
            {
                new
                {
                    type = "header",
                    text = new
                    {
                        type = "plain_text",
                        text = $"✅ Merged: {prData.Title}"
                    }
                },
                new
                {
                    type = "section",
                    fields = new object[]
                    {
                        new { type = "mrkdwn", text = $"*Repository:*\n{prData.Repository}" },
                        new { type = "mrkdwn", text = $"*PR Number:*\n#{prData.PrNumber}" },
                        new { type = "mrkdwn", text = $"*Workflow:*\n{FormatWorkflowType(prData.WorkflowType)}" },
                        new { type = "mrkdwn", text = "*Status:*\n✅ Merged" }
                    }
                },
                new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = "The pull request has been successfully merged. Changes are now in the main branch."
                    }
                },
                new
                {
                    type = "actions",
                    elements = new object[]
                    {
                        new
                        {
                            type = "button",
                            text = new { type = "plain_text", text = "View Merged PR" },
                            url = prData.Url
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Generate Slack message for PR failed event
    /// </summary>
    public static object PRFailedSlackMessage(PullRequestNotificationData prData)
    {
        return new
        {
            text = "❌ PR Creation/Merge Failed",
            blocks = new object[]
            {
                new
                {
                    type = "header",
                    text = new
                    {
                        type = "plain_text",
                        text = $"❌ Failed: {prData.Title}"
                    }
                },
                new
                {
                    type = "section",
                    fields = new object[]
                    {
                        new { type = "mrkdwn", text = $"*Repository:*\n{prData.Repository}" },
                        new { type = "mrkdwn", text = $"*Workflow:*\n{FormatWorkflowType(prData.WorkflowType)}" }
                    }
                },
                new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = $"*Error:*\n```{prData.Error}```"
                    }
                },
                new
                {
                    type = "context",
                    elements = new object[]
                    {
                        new
                        {
                            type = "mrkdwn",
                            text = "⚠️ Please check the error message and retry the operation."
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Generate Slack message for PR needs review event
    /// </summary>
    public static object PRNeedsReviewSlackMessage(PullRequestNotificationData prData)
    {
        return new
        {
            text = "👀 PR Needs Review",
            blocks = new object[]
            {
                new
                {
                    type = "header",
                    text = new
                    {
                        type = "plain_text",
                        text = $"👀 Review Requested: {prData.Title}"
                    }
                },
                new
                {
                    type = "section",
                    fields = new object[]
                    {
                        new { type = "mrkdwn", text = $"*Repository:*\n{prData.Repository}" },
                        new { type = "mrkdwn", text = $"*PR Number:*\n#{prData.PrNumber}" },
                        new { type = "mrkdwn", text = $"*Workflow:*\n{FormatWorkflowType(prData.WorkflowType)}" },
                        new { type = "mrkdwn", text = $"*Reviewers:*\n{string.Join(", ", prData.Reviewers)}" }
                    }
                },
                new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = "Your review has been requested on this pull request."
                    }
                },
                new
                {
                    type = "actions",
                    elements = new object[]
                    {
                        new
                        {
                            type = "button",
                            text = new { type = "plain_text", text = "Review Now" },
                            url = prData.Url,
                            style = "primary"
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Generate HTML email template for PR created
    /// </summary>
    public static string PRCreatedEmailHtml(PullRequestNotificationData prData)
    {
        var workflowType = FormatWorkflowType(prData.WorkflowType);
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0366d6; color: white; padding: 20px; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f6f8fa; padding: 20px; border: 1px solid #e1e4e8; border-radius: 0 0 5px 5px; }}
        .button {{ display: inline-block; background-color: #0366d6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin-top: 15px; }}
        .details {{ margin: 15px 0; }}
        .detail-row {{ margin: 8px 0; }}
        .label {{ font-weight: bold; }}
        code {{ background-color: #e1e4e8; padding: 2px 6px; border-radius: 3px; font-family: monospace; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>🤖 New Autonomous PR Created</h2>
        </div>
        <div class=""content"">
            <h3>{prData.Title}</h3>
            <div class=""details"">
                <div class=""detail-row""><span class=""label"">Repository:</span> {prData.Repository}</div>
                <div class=""detail-row""><span class=""label"">PR Number:</span> #{prData.PrNumber}</div>
                <div class=""detail-row""><span class=""label"">Workflow:</span> {workflowType}</div>
                <div class=""detail-row""><span class=""label"">Branch:</span> <code>{prData.BranchName}</code></div>
                <div class=""detail-row""><span class=""label"">Status:</span> {FormatStatus(prData.Status)}</div>
                <div class=""detail-row""><span class=""label"">Commits:</span> {prData.CommitCount ?? 0}</div>
                <div class=""detail-row""><span class=""label"">Files Changed:</span> {prData.FileCount ?? 0}</div>
            </div>
            <p>An autonomous pull request has been created by the Binelek platform. Please review the changes when you have a moment.</p>
            <a href=""{prData.Url}"" class=""button"">View Pull Request</a>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generate plain text email for PR created
    /// </summary>
    public static string PRCreatedEmailText(PullRequestNotificationData prData)
    {
        var workflowType = FormatWorkflowType(prData.WorkflowType);
        return $@"
New Autonomous PR Created

{prData.Title}

Repository: {prData.Repository}
PR Number: #{prData.PrNumber}
Workflow: {workflowType}
Branch: {prData.BranchName}
Status: {prData.Status}
Commits: {prData.CommitCount ?? 0}
Files Changed: {prData.FileCount ?? 0}

An autonomous pull request has been created by the Binelek platform.
Please review the changes when you have a moment.

View PR: {prData.Url}

---
This is an automated notification from Binelek Platform.
";
    }

    /// <summary>
    /// Generate HTML email template for PR merged
    /// </summary>
    public static string PRMergedEmailHtml(PullRequestNotificationData prData)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f6f8fa; padding: 20px; border: 1px solid #e1e4e8; border-radius: 0 0 5px 5px; }}
        .button {{ display: inline-block; background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin-top: 15px; }}
        .success-badge {{ display: inline-block; background-color: #dcffe4; color: #28a745; padding: 4px 8px; border-radius: 3px; font-weight: bold; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>✅ PR Merged Successfully</h2>
        </div>
        <div class=""content"">
            <h3>{prData.Title}</h3>
            <p class=""success-badge"">MERGED</p>
            <p>Repository: {prData.Repository}</p>
            <p>PR Number: #{prData.PrNumber}</p>
            <p>The pull request has been successfully merged. Changes are now in the main branch.</p>
            <a href=""{prData.Url}"" class=""button"">View Merged PR</a>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generate HTML email template for PR failed
    /// </summary>
    public static string PRFailedEmailHtml(PullRequestNotificationData prData)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #d73a49; color: white; padding: 20px; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f6f8fa; padding: 20px; border: 1px solid #e1e4e8; border-radius: 0 0 5px 5px; }}
        .error-box {{ background-color: #ffdce0; border: 1px solid #d73a49; padding: 12px; border-radius: 5px; margin: 15px 0; }}
        .error-badge {{ display: inline-block; background-color: #ffdce0; color: #d73a49; padding: 4px 8px; border-radius: 3px; font-weight: bold; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>❌ PR Creation/Merge Failed</h2>
        </div>
        <div class=""content"">
            <h3>{prData.Title}</h3>
            <p class=""error-badge"">FAILED</p>
            <p>Repository: {prData.Repository}</p>
            <p>Workflow: {FormatWorkflowType(prData.WorkflowType)}</p>
            <div class=""error-box"">
                <strong>Error:</strong><br>
                <pre>{prData.Error}</pre>
            </div>
            <p>⚠️ Please check the error message and retry the operation.</p>
        </div>
    </div>
</body>
</html>";
    }

    // Helper methods
    private static string FormatWorkflowType(string? workflowType)
    {
        if (string.IsNullOrEmpty(workflowType))
            return "General";

        return workflowType.Replace("_", " ")
            .Split(' ')
            .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower())
            .Aggregate((a, b) => $"{a} {b}");
    }

    private static string FormatStatus(string status)
    {
        return status?.ToLower() switch
        {
            "open" => "🟢 Open",
            "merged" => "✅ Merged",
            "closed" => "❌ Closed",
            _ => status ?? "Unknown"
        };
    }
}
