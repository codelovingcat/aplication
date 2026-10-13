using AtsCv.Application.AI;
using AtsCv.Application.Resumes;
using AtsCv.Domain.Resumes;
using Xunit;

namespace AtsCv.Application.Tests;

public sealed class ExplainableAtsResumeAnalyzerTests
{
    private const string CompleteResume = """
        Jane Doe
        jane.doe@example.com
        +1 (555) 234-5678
        https://linkedin.com/in/jane-doe

        Professional Summary
        Product-focused software engineer with experience improving reliable services, collaborating across teams, and delivering accessible customer-facing software. Skilled at explaining technical trade-offs, planning work, and tracking outcomes against measurable goals.

        Experience
        Senior Software Engineer
        Developed and tested API services used by multiple product teams. Led planning with design and product partners, improved release automation, and documented operational workflows. Implemented monitoring that reduced investigation time and helped the team respond to production issues. Worked with SQL data stores, Python utilities, C# services, and cloud deployment tooling. Coordinated technical reviews, mentored colleagues, and communicated project risks clearly.

        Education
        Bachelor of Science in Computer Science
        Completed coursework in algorithms, software design, databases, and distributed systems.

        Skills
        C#, .NET, SQL, Python, Git, Docker, communication, leadership, automated testing
        """;

    private readonly ExplainableAtsResumeAnalyzer _analyzer = new();

    [Fact]
    public async Task AnalyzeAsync_WithCompleteResume_ReturnsExplainableHighScore()
    {
        var result = await _analyzer.AnalyzeAsync(CompleteResume);

        Assert.InRange(result.Score, 80, 100);
        Assert.Contains(result.Strengths, value => value.Contains("email address", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Strengths, value => value.Contains("experience", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.SectionFindings, value => value.StartsWith("Found:", StringComparison.Ordinal));
        Assert.Contains(result.KeywordFindings, value => value.Contains("Job-specific keyword matching was not performed", StringComparison.Ordinal));
        Assert.Contains(result.Recommendations, value => value.Contains("not a hiring probability", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeAsync_WithEmptyText_ReturnsZeroScoreAndDoesNotInventFindings()
    {
        var result = await _analyzer.AnalyzeAsync(string.Empty);

        Assert.Equal(0, result.Score);
        Assert.Contains(result.Issues, value => value.Contains("empty or sparse", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.KeywordFindings, value => value.Contains("No terms", StringComparison.Ordinal));
        Assert.Contains(result.Recommendations, value => value.Contains("not a hiring probability", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeAsync_WithSparseResume_ReturnsLowScoreAndActionableRecommendations()
    {
        var result = await _analyzer.AnalyzeAsync("Jane Doe\njane@example.com\nSkills\nSQL");

        Assert.InRange(result.Score, 0, 40);
        Assert.Contains(result.Recommendations, value => value.Contains("experience", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Issues, value => value.Contains("sparse", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeAsync_WithSameInput_ReturnsSameScoreAndFindings()
    {
        var first = await _analyzer.AnalyzeAsync(CompleteResume);
        var second = await _analyzer.AnalyzeAsync(CompleteResume);

        Assert.Equal(first.Score, second.Score);
        Assert.Equal(first.Strengths, second.Strengths);
        Assert.Equal(first.Issues, second.Issues);
        Assert.Equal(first.Recommendations, second.Recommendations);
        Assert.Equal(first.SectionFindings, second.SectionFindings);
        Assert.Equal(first.KeywordFindings, second.KeywordFindings);
    }

    [Fact]
    public async Task AnalyzeAsync_WithColumnLikeExtractionSignals_ReportsFormattingRisks()
    {
        var text = CompleteResume
            + "\nProject A     Project B\nProject C     Project D\n"
            + "Owner\tPeriod\tOutcome\tNotes\nName | Role | Dates\nName | Role | Dates\n";

        var result = await _analyzer.AnalyzeAsync(text);

        Assert.Contains(result.Issues, value => value.Contains("tab separators", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Issues, value => value.Contains("pipe-separated", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, value => value.Contains("single-column", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeAsync_WithCanceledToken_ThrowsBeforeAnalysis()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _analyzer.AnalyzeAsync(CompleteResume, cancellationTokenSource.Token));
    }
}

public sealed class AtsResumeAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_MarksResumeAnalyzedOnlyAfterResultIsProduced()
    {
        var analyzer = new StubAnalyzer((_, _) => Task.FromResult(new ResumeAnalysisResult(
            75,
            new[] { "Found a skills section." },
            Array.Empty<string>(),
            new[] { "This is a heuristic score." },
            new[] { "Found: Skills" },
            new[] { "Job-specific matching was not performed." })));
        var service = new AtsResumeAnalysisService(analyzer);
        var resume = CreateExtractedResume();

        var result = await service.AnalyzeAsync(resume);

        Assert.Equal(75, result.Score);
        Assert.Equal(ResumeStatus.Analyzed, resume.Status);
        Assert.Equal("Extracted CV text.", analyzer.LastText);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenAnalyzerFails_LeavesResumeTextExtracted()
    {
        var analyzer = new StubAnalyzer((_, _) =>
            Task.FromException<ResumeAnalysisResult>(new InvalidOperationException("analysis failed")));
        var service = new AtsResumeAnalysisService(analyzer);
        var resume = CreateExtractedResume();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeAsync(resume));

        Assert.Equal(ResumeStatus.TextExtracted, resume.Status);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNullResume_Throws()
    {
        var service = new AtsResumeAnalysisService(new StubAnalyzer((_, _) =>
            Task.FromResult(new ResumeAnalysisResult(
                0, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
                Array.Empty<string>(), Array.Empty<string>()))));

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.AnalyzeAsync(null!));
    }

    private static Resume CreateExtractedResume()
    {
        var resume = Resume.Create(Guid.NewGuid(), "cv.pdf", "application/pdf");
        resume.SetExtractedText("Extracted CV text.");
        return resume;
    }

    private sealed class StubAnalyzer(
        Func<string, CancellationToken, Task<ResumeAnalysisResult>> analyze) : IAtsResumeAnalyzer
    {
        public string? LastText { get; private set; }

        public Task<ResumeAnalysisResult> AnalyzeAsync(
            string resumeText,
            CancellationToken cancellationToken = default)
        {
            LastText = resumeText;
            return analyze(resumeText, cancellationToken);
        }
    }
}
