namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// Service for generating pull request descriptions from templates
/// </summary>
public interface IPullRequestTemplateService
{
    /// <summary>
    /// Generate PR description for ontology refactoring
    /// </summary>
    /// <param name="entityName">Name of the refactored entity</param>
    /// <param name="addedProperties">Number of added properties</param>
    /// <param name="updatedRelationships">Number of updated relationships</param>
    /// <param name="refactoredValidators">Number of refactored validators</param>
    /// <param name="files">List of modified files</param>
    /// <returns>Formatted PR description in Markdown</returns>
    string GenerateOntologyRefactoringDescription(
        string entityName,
        int addedProperties,
        int updatedRelationships,
        int refactoredValidators,
        List<string> files);

    /// <summary>
    /// Generate PR description for code generation
    /// </summary>
    /// <param name="generatedComponent">Name of the generated component</param>
    /// <param name="files">List of generated files</param>
    /// <param name="additionalNotes">Optional additional notes</param>
    /// <returns>Formatted PR description in Markdown</returns>
    string GenerateCodeGenerationDescription(
        string generatedComponent,
        List<string> files,
        string? additionalNotes = null);

    /// <summary>
    /// Generate PR description for bug fix
    /// </summary>
    /// <param name="bugDescription">Description of the bug</param>
    /// <param name="fixDescription">Description of the fix</param>
    /// <param name="files">List of modified files</param>
    /// <param name="issueNumber">GitHub issue number (optional)</param>
    /// <returns>Formatted PR description in Markdown</returns>
    string GenerateBugFixDescription(
        string bugDescription,
        string fixDescription,
        List<string> files,
        string? issueNumber = null);

    /// <summary>
    /// Generate PR description for feature addition
    /// </summary>
    /// <param name="featureName">Name of the feature</param>
    /// <param name="featureDescription">Description of the feature</param>
    /// <param name="files">List of modified files</param>
    /// <param name="apiEndpoints">List of new API endpoints</param>
    /// <param name="requiresDatabaseMigration">Whether feature requires DB migration</param>
    /// <returns>Formatted PR description in Markdown</returns>
    string GenerateFeatureAdditionDescription(
        string featureName,
        string featureDescription,
        List<string> files,
        List<string> apiEndpoints,
        bool requiresDatabaseMigration = false);

    /// <summary>
    /// Generate PR description for refactoring
    /// </summary>
    /// <param name="refactoringScope">Scope of the refactoring</param>
    /// <param name="refactoringReason">Reason for refactoring</param>
    /// <param name="files">List of modified files</param>
    /// <param name="breakingChanges">Whether refactoring contains breaking changes</param>
    /// <returns>Formatted PR description in Markdown</returns>
    string GenerateRefactoringDescription(
        string refactoringScope,
        string refactoringReason,
        List<string> files,
        bool breakingChanges = false);

    /// <summary>
    /// Generate general PR description
    /// </summary>
    /// <param name="title">PR title</param>
    /// <param name="description">PR description</param>
    /// <param name="files">List of modified files</param>
    /// <returns>Formatted PR description in Markdown</returns>
    string GenerateGeneralDescription(
        string title,
        string description,
        List<string> files);
}
