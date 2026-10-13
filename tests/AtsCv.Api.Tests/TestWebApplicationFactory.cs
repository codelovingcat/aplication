using AtsCv.Api.Contracts;
using AtsCv.Application.AI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AtsCv.Api.Tests;

internal sealed class TestWebApplicationFactory : WebApplicationFactory<ResumeUploadForm>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"atscv-api-tests-{Guid.NewGuid():N}.db");

    private readonly bool _useFailingAnalyzer;

    public TestWebApplicationFactory(bool useFailingAnalyzer = false)
    {
        _useFailingAnalyzer = useFailingAnalyzer;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AtsCv"] = $"Data Source={_databasePath}"
            });
        });

        if (_useFailingAnalyzer)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAtsResumeAnalyzer>();
                services.AddSingleton<IAtsResumeAnalyzer, FailingAtsResumeAnalyzer>();
            });
        }
    }
}

internal sealed class FailingAtsResumeAnalyzer : IAtsResumeAnalyzer
{
    public Task<AtsCv.Application.Resumes.ResumeAnalysisResult> AnalyzeAsync(
        string resumeText,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Sensitive analyzer implementation detail.");
}
