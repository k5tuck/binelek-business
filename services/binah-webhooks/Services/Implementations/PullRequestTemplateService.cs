using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Templates;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// Service for generating pull request descriptions from templates
/// </summary>
public class PullRequestTemplateService : IPullRequestTemplateService
{
    /// <inheritdoc/>
    public string GenerateOntologyRefactoringDescription(
        string entityName,
        int addedProperties,
        int updatedRelationships,
        int refactoredValidators,
        List<string> files)
    {
        return PullRequestTemplates.OntologyRefactoring(
            entityName,
            addedProperties,
            updatedRelationships,
            refactoredValidators,
            files);
    }

    /// <inheritdoc/>
    public string GenerateCodeGenerationDescription(
        string generatedComponent,
        List<string> files,
        string? additionalNotes = null)
    {
        return PullRequestTemplates.CodeGeneration(
            generatedComponent,
            files,
            additionalNotes);
    }

    /// <inheritdoc/>
    public string GenerateBugFixDescription(
        string bugDescription,
        string fixDescription,
        List<string> files,
        string? issueNumber = null)
    {
        return PullRequestTemplates.BugFix(
            bugDescription,
            fixDescription,
            files,
            issueNumber);
    }

    /// <inheritdoc/>
    public string GenerateFeatureAdditionDescription(
        string featureName,
        string featureDescription,
        List<string> files,
        List<string> apiEndpoints,
        bool requiresDatabaseMigration = false)
    {
        return PullRequestTemplates.FeatureAddition(
            featureName,
            featureDescription,
            files,
            apiEndpoints,
            requiresDatabaseMigration);
    }

    /// <inheritdoc/>
    public string GenerateRefactoringDescription(
        string refactoringScope,
        string refactoringReason,
        List<string> files,
        bool breakingChanges = false)
    {
        return PullRequestTemplates.Refactoring(
            refactoringScope,
            refactoringReason,
            files,
            breakingChanges);
    }

    /// <inheritdoc/>
    public string GenerateGeneralDescription(
        string title,
        string description,
        List<string> files)
    {
        return PullRequestTemplates.General(
            title,
            description,
            files);
    }
}
