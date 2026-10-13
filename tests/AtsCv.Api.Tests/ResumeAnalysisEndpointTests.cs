using System.Net;
using System.Text.Json;
using AtsCv.Application.AI;
using AtsCv.Domain.Candidates;
using AtsCv.Domain.Resumes;
using AtsCv.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AtsCv.Api.Tests;

public sealed class ResumeAnalysisEndpointTests
{
    private const string AnalysisRoute = "/api/cv/{0}/analyze";

    [Fact]
    public async Task AnalyzeAsync_WhenResumeDoesNotExist_ReturnsNotFoundProblem()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            string.Format(AnalysisRoute, Guid.NewGuid()),
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "resume_not_found",
            document.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task AnalyzeAsync_WhenResumeHasExtractedText_ReturnsFindingsAndPersistsAnalyzedStatus()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var resumeId = await SeedResumeAsync(factory, extractedText: BuildRepresentativeResumeText());

        using var response = await client.PostAsync(
            string.Format(AnalysisRoute, resumeId),
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(resumeId, root.GetProperty("resumeId").GetGuid());
        Assert.Equal("Analyzed", root.GetProperty("status").GetString());
        Assert.InRange(root.GetProperty("score").GetInt32(), 0, 100);
        Assert.True(root.GetProperty("strengths").GetArrayLength() > 0);
        Assert.True(root.GetProperty("sectionFindings").GetArrayLength() > 0);
        Assert.True(root.GetProperty("keywordFindings").GetArrayLength() > 0);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AtsCvDbContext>();
        var persistedResume = await dbContext.Resumes.SingleAsync(item => item.Id == resumeId);
        Assert.Equal(ResumeStatus.Analyzed, persistedResume.Status);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenResumeHasNoExtractedText_ReturnsConflictProblem()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var resumeId = await SeedResumeAsync(factory, extractedText: null);

        using var response = await client.PostAsync(
            string.Format(AnalysisRoute, resumeId),
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "resume_not_ready",
            document.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task AnalyzeAsync_WhenAnalyzerThrows_ReturnsSafeServerProblem()
    {
        using var factory = new TestWebApplicationFactory(useFailingAnalyzer: true);
        using var client = factory.CreateClient();
        var resumeId = await SeedResumeAsync(factory, extractedText: BuildRepresentativeResumeText());

        using var response = await client.PostAsync(
            string.Format(AnalysisRoute, resumeId),
            content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Sensitive analyzer implementation detail.", payload, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(payload);
        Assert.Equal("unexpected_error", document.RootElement.GetProperty("errorCode").GetString());
    }

    private static async Task<Guid> SeedResumeAsync(
        TestWebApplicationFactory factory,
        string? extractedText)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AtsCvDbContext>();
        var candidate = CandidateProfile.CreateDraft("Provided during API integration testing");
        var resume = Resume.Create(candidate.Id, "candidate-cv.pdf", "application/pdf");

        if (extractedText is not null)
            resume.SetExtractedText(extractedText);

        dbContext.CandidateProfiles.Add(candidate);
        dbContext.Resumes.Add(resume);
        await dbContext.SaveChangesAsync();

        return resume.Id;
    }

    private static string BuildRepresentativeResumeText() =>
        """
        Zeynep Sarı
        zeynep@example.com
        +90 555 123 4567
        https://linkedin.com/in/zeynep
        Professional Summary
        Experienced software developer focused on building reliable applications.
        Work Experience
        Developed and implemented API features, automated tests, and improved reliability.
        Education
        Computer Programming
        Skills
        C# .NET SQL Git Docker Azure JavaScript React Python
        """ + string.Concat(Enumerable.Repeat(
            " Designed tested developed implemented software services and collaborated with the team. ",
            12));
}
