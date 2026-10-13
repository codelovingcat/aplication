using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AtsCv.Api.Contracts;
using AtsCv.Application.Documents;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AtsCv.Api.Tests;

/// <summary>
/// Maps CV intake validation failures to the documented ProblemDetails contract.
/// The API is a minimal hosting application whose generated <c>Program</c> type is
/// internal, so the in-memory test host is anchored to a public API type instead.
/// </summary>
public sealed class CvUploadEndpointTests
{
    private const string UploadPath = "/api/cv/upload";

    // "%PDF-1"
    private static readonly byte[] PdfSignature = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31 };

    [Fact]
    public async Task UploadWithoutFile_ReturnsValidationProblem()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("I recently completed a certification."), "AdditionalInformation");

        using var response = await client.PostAsync(UploadPath, form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await ReadProblemAsync(response);

        Assert.Equal("Invalid CV upload", problem.GetProperty("title").GetString());
        Assert.Equal("validation_error", problem.GetProperty("errorCode").GetString());
        Assert.Equal(UploadPath, problem.GetProperty("instance").GetString());
        Assert.True(
            ContainsErrorMessage(problem, "A CV file is required."),
            "Expected the missing file to be reported as a validation error.");
    }

    [Fact]
    public async Task UploadWithUnsupportedExtension_ReturnsUnsupportedFileTypeProblem()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var form = CreateForm(PdfSignature, "notes.txt", "text/plain");

        using var response = await client.PostAsync(UploadPath, form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemAsync(response);

        Assert.Equal("unsupported_file_type", problem.GetProperty("errorCode").GetString());
        Assert.Equal("Unsupported file type", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UploadWithContentTypeNotMatchingTheFileName_ReturnsUnsupportedFileTypeProblem()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var form = CreateForm(PdfSignature, "cv.pdf", "text/plain");

        using var response = await client.PostAsync(UploadPath, form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemAsync(response);

        Assert.Equal("unsupported_file_type", problem.GetProperty("errorCode").GetString());
        Assert.True(
            ContainsErrorMessage(problem, "content type"),
            "Expected the unsupported content type to be reported.");
    }

    [Fact]
    public async Task UploadWhoseContentDoesNotMatchTheFileName_ReturnsUnsupportedFileTypeProblem()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var form = CreateForm(
            Encoding.ASCII.GetBytes("This is not an actual PDF document."),
            "cv.pdf",
            "application/pdf");

        using var response = await client.PostAsync(UploadPath, form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemAsync(response);

        Assert.Equal("unsupported_file_type", problem.GetProperty("errorCode").GetString());
        Assert.Equal("Unsupported file type", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UploadOfAnEmptyFile_ReturnsValidationProblem()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var form = CreateForm(Array.Empty<byte>(), "cv.pdf", "application/pdf");

        using var response = await client.PostAsync(UploadPath, form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemAsync(response);

        Assert.Equal("validation_error", problem.GetProperty("errorCode").GetString());
        Assert.True(
            ContainsErrorMessage(problem, "must not be empty"),
            "Expected the empty file to be reported as a validation error.");
    }

    [Fact]
    public async Task UploadWithAdditionalInformationOverTheLimit_ReturnsValidationProblem()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var form = CreateForm(
            PdfSignature,
            "cv.pdf",
            "application/pdf",
            new string('a', CvUploadValidator.DefaultMaxAdditionalInformationLength + 1));

        using var response = await client.PostAsync(UploadPath, form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemAsync(response);

        Assert.Equal("validation_error", problem.GetProperty("errorCode").GetString());
        Assert.True(
            ContainsErrorMessage(problem, "Additional information"),
            "Expected the additional information limit to be reported.");
    }

    [Fact]
    public async Task UploadOverTheSizeLimit_ReturnsPayloadTooLargeProblem()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var form = CreateForm(
            new byte[CvUploadValidator.DefaultMaxFileSizeBytes + 1],
            "cv.pdf",
            "application/pdf");

        using var response = await client.PostAsync(UploadPath, form);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);

        var problem = await ReadProblemAsync(response);

        Assert.Equal("file_too_large", problem.GetProperty("errorCode").GetString());
        Assert.Equal("Uploaded file is too large", problem.GetProperty("title").GetString());
        Assert.True(
            ContainsErrorMessage(problem, "must not exceed"),
            "Expected the file size limit to be reported.");
    }

    [Fact]
    public async Task UploadOverTheRateLimit_ReturnsRateLimitedProblem()
    {
        // The upload endpoint allows ten requests per window, so the eleventh must be
        // rejected with the documented problem response instead of a successful status.
        const int PermittedRequests = 10;

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        HttpResponseMessage? response = null;

        try
        {
            for (var attempt = 0; attempt <= PermittedRequests; attempt++)
            {
                using var form = CreateForm(PdfSignature, "cv.pdf", "application/pdf");

                response?.Dispose();
                response = await client.PostAsync(UploadPath, form);
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, response!.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            var problem = await ReadProblemAsync(response);

            Assert.Equal("rate_limited", problem.GetProperty("errorCode").GetString());
            Assert.Equal("Too many requests", problem.GetProperty("title").GetString());
        }
        finally
        {
            response?.Dispose();
        }
    }

    private static TestWebApplicationFactory CreateFactory() => new();

    private static MultipartFormDataContent CreateForm(
        byte[] content,
        string fileName,
        string? contentType,
        string? additionalInformation = null)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);

        if (contentType is not null)
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        form.Add(fileContent, "File", fileName);

        if (additionalInformation is not null)
            form.Add(new StringContent(additionalInformation), "AdditionalInformation");

        return form;
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);

        return document.RootElement.Clone();
    }

    private static bool ContainsErrorMessage(JsonElement problem, string fragment) =>
        problem.GetProperty("errors")
            .EnumerateArray()
            .Any(error => error.GetString()?.Contains(fragment, StringComparison.OrdinalIgnoreCase) == true);
}
