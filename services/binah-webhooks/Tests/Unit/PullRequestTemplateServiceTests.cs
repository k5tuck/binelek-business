using Binah.Webhooks.Services.Implementations;
using Xunit;

namespace Binah.Webhooks.Tests.Unit;

/// <summary>
/// Unit tests for PullRequestTemplateService
/// </summary>
public class PullRequestTemplateServiceTests
{
    private readonly PullRequestTemplateService _service;

    public PullRequestTemplateServiceTests()
    {
        _service = new PullRequestTemplateService();
    }

    [Fact]
    public void GenerateOntologyRefactoringDescription_ValidInput_GeneratesMarkdown()
    {
        // Arrange
        var entityName = "Property";
        var addedProperties = 5;
        var updatedRelationships = 3;
        var refactoredValidators = 2;
        var files = new List<string>
        {
            "schemas/core-real-estate-ontology.yaml",
            "services/binah-ontology/Models/Property.cs"
        };

        // Act
        var result = _service.GenerateOntologyRefactoringDescription(
            entityName,
            addedProperties,
            updatedRelationships,
            refactoredValidators,
            files);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Property", result);
        Assert.Contains("5 new properties", result);
        Assert.Contains("3 relationships", result);
        Assert.Contains("2 validators", result);
        Assert.Contains("schemas/core-real-estate-ontology.yaml", result);
        Assert.Contains("Binah.Regen", result);
    }

    [Fact]
    public void GenerateCodeGenerationDescription_ValidInput_GeneratesMarkdown()
    {
        // Arrange
        var component = "PropertyService";
        var files = new List<string>
        {
            "Services/PropertyService.cs",
            "Services/IPropertyService.cs",
            "Tests/PropertyServiceTests.cs"
        };
        var notes = "Generated from YAML schema v2.0";

        // Act
        var result = _service.GenerateCodeGenerationDescription(component, files, notes);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("PropertyService", result);
        Assert.Contains("Services/PropertyService.cs", result);
        Assert.Contains("Generated from YAML schema v2.0", result);
        Assert.Contains("Total files: 3", result);
    }

    [Fact]
    public void GenerateCodeGenerationDescription_NoNotes_GeneratesMarkdown()
    {
        // Arrange
        var component = "PropertyService";
        var files = new List<string> { "Services/PropertyService.cs" };

        // Act
        var result = _service.GenerateCodeGenerationDescription(component, files);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("No additional notes", result);
    }

    [Fact]
    public void GenerateBugFixDescription_ValidInput_GeneratesMarkdown()
    {
        // Arrange
        var bugDesc = "NullReferenceException when accessing property without owner";
        var fixDesc = "Added null check before accessing owner properties";
        var files = new List<string> { "Services/PropertyService.cs" };
        var issueNumber = "123";

        // Act
        var result = _service.GenerateBugFixDescription(bugDesc, fixDesc, files, issueNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(bugDesc, result);
        Assert.Contains(fixDesc, result);
        Assert.Contains("Fixes #123", result);
        Assert.Contains("Services/PropertyService.cs", result);
    }

    [Fact]
    public void GenerateBugFixDescription_NoIssueNumber_GeneratesMarkdown()
    {
        // Arrange
        var bugDesc = "Memory leak in cache";
        var fixDesc = "Disposed cache entries properly";
        var files = new List<string> { "Services/CacheService.cs" };

        // Act
        var result = _service.GenerateBugFixDescription(bugDesc, fixDesc, files);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(bugDesc, result);
        Assert.Contains(fixDesc, result);
        Assert.DoesNotContain("Fixes #", result);
    }

    [Fact]
    public void GenerateFeatureAdditionDescription_WithApiEndpoints_GeneratesMarkdown()
    {
        // Arrange
        var featureName = "Property Search";
        var featureDesc = "Advanced search with filters";
        var files = new List<string>
        {
            "Controllers/PropertySearchController.cs",
            "Services/PropertySearchService.cs"
        };
        var apiEndpoints = new List<string>
        {
            "GET /api/properties/search",
            "POST /api/properties/advanced-search"
        };

        // Act
        var result = _service.GenerateFeatureAdditionDescription(
            featureName,
            featureDesc,
            files,
            apiEndpoints,
            requiresDatabaseMigration: false);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Property Search", result);
        Assert.Contains("Advanced search with filters", result);
        Assert.Contains("GET /api/properties/search", result);
        Assert.Contains("POST /api/properties/advanced-search", result);
        Assert.DoesNotContain("requires database migration", result);
    }

    [Fact]
    public void GenerateFeatureAdditionDescription_WithDatabaseMigration_GeneratesWarning()
    {
        // Arrange
        var featureName = "Property History";
        var featureDesc = "Track property changes over time";
        var files = new List<string> { "Models/PropertyHistory.cs" };
        var apiEndpoints = new List<string>();

        // Act
        var result = _service.GenerateFeatureAdditionDescription(
            featureName,
            featureDesc,
            files,
            apiEndpoints,
            requiresDatabaseMigration: true);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("requires database migration", result);
        Assert.Contains("Run database migrations", result);
    }

    [Fact]
    public void GenerateRefactoringDescription_NoBreakingChanges_GeneratesMarkdown()
    {
        // Arrange
        var scope = "Property validation logic";
        var reason = "Improve code maintainability and testability";
        var files = new List<string>
        {
            "Validators/PropertyValidator.cs",
            "Tests/PropertyValidatorTests.cs"
        };

        // Act
        var result = _service.GenerateRefactoringDescription(scope, reason, files, breakingChanges: false);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Property validation logic", result);
        Assert.Contains("maintainability", result);
        Assert.Contains("maintains backward compatibility", result);
        Assert.DoesNotContain("breaking changes", result);
    }

    [Fact]
    public void GenerateRefactoringDescription_WithBreakingChanges_GeneratesWarning()
    {
        // Arrange
        var scope = "API response structure";
        var reason = "Standardize response format";
        var files = new List<string> { "Controllers/PropertyController.cs" };

        // Act
        var result = _service.GenerateRefactoringDescription(scope, reason, files, breakingChanges: true);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("breaking changes", result);
        Assert.Contains("Migration guide required", result);
    }

    [Fact]
    public void GenerateGeneralDescription_ValidInput_GeneratesMarkdown()
    {
        // Arrange
        var title = "Update dependencies";
        var description = "Update NuGet packages to latest versions";
        var files = new List<string>
        {
            "Services/PropertyService.csproj",
            "Tests/Tests.csproj"
        };

        // Act
        var result = _service.GenerateGeneralDescription(title, description, files);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Update dependencies", result);
        Assert.Contains("Update NuGet packages to latest versions", result);
        Assert.Contains("Services/PropertyService.csproj", result);
        Assert.Contains("Total files: 2", result);
    }

    [Fact]
    public void GenerateOntologyRefactoringDescription_EmptyFiles_GeneratesMarkdown()
    {
        // Arrange
        var files = new List<string>();

        // Act
        var result = _service.GenerateOntologyRefactoringDescription(
            "Property",
            0,
            0,
            0,
            files);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Property", result);
        Assert.DoesNotContain("- `", result); // No file list items
    }

    [Fact]
    public void AllTemplates_ContainAutoGeneratedFooter()
    {
        // Arrange
        var files = new List<string> { "test.cs" };

        // Act
        var ontology = _service.GenerateOntologyRefactoringDescription("Entity", 1, 1, 1, files);
        var codeGen = _service.GenerateCodeGenerationDescription("Component", files);
        var bugFix = _service.GenerateBugFixDescription("Bug", "Fix", files);
        var feature = _service.GenerateFeatureAdditionDescription("Feature", "Desc", files, new List<string>());
        var refactor = _service.GenerateRefactoringDescription("Scope", "Reason", files);
        var general = _service.GenerateGeneralDescription("Title", "Desc", files);

        // Assert
        Assert.Contains("Auto-Generated", ontology);
        Assert.Contains("Auto-Generated", codeGen);
        Assert.Contains("Auto-Generated", bugFix);
        Assert.Contains("Auto-Generated", feature);
        Assert.Contains("Auto-Generated", refactor);
        Assert.Contains("Auto-Generated", general);
    }

    [Fact]
    public void AllTemplates_ContainChecklistsForTesting()
    {
        // Arrange
        var files = new List<string> { "test.cs" };

        // Act
        var ontology = _service.GenerateOntologyRefactoringDescription("Entity", 1, 1, 1, files);
        var codeGen = _service.GenerateCodeGenerationDescription("Component", files);
        var bugFix = _service.GenerateBugFixDescription("Bug", "Fix", files);
        var feature = _service.GenerateFeatureAdditionDescription("Feature", "Desc", files, new List<string>());

        // Assert - All should contain checkboxes
        Assert.Contains("- [ ]", ontology);
        Assert.Contains("- [ ]", codeGen);
        Assert.Contains("- [ ]", bugFix);
        Assert.Contains("- [ ]", feature);
    }

    [Fact]
    public void GenerateFeatureAdditionDescription_NoApiEndpoints_ShowsNoEndpoints()
    {
        // Arrange
        var files = new List<string> { "test.cs" };
        var apiEndpoints = new List<string>();

        // Act
        var result = _service.GenerateFeatureAdditionDescription(
            "Feature",
            "Description",
            files,
            apiEndpoints);

        // Assert
        Assert.Contains("No new API endpoints", result);
    }

    [Theory]
    [InlineData("Property", 1, 0, 0)]
    [InlineData("Owner", 0, 1, 0)]
    [InlineData("Transaction", 0, 0, 1)]
    [InlineData("Parcel", 5, 3, 2)]
    public void GenerateOntologyRefactoringDescription_VariousCounts_GeneratesCorrectly(
        string entityName,
        int properties,
        int relationships,
        int validators)
    {
        // Arrange
        var files = new List<string> { "test.yaml" };

        // Act
        var result = _service.GenerateOntologyRefactoringDescription(
            entityName,
            properties,
            relationships,
            validators,
            files);

        // Assert
        Assert.Contains(entityName, result);
        Assert.Contains($"{properties} new properties", result);
        Assert.Contains($"{relationships} relationships", result);
        Assert.Contains($"{validators} validators", result);
    }
}
