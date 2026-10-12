using System.Threading.RateLimiting;
using AtsCv.Api.Contracts;
using AtsCv.Api.Errors;
using AtsCv.Application.Documents;
using AtsCv.Application.Resumes;
using AtsCv.Infrastructure.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton(new CvUploadValidator());
builder.Services.AddSingleton<CvFileSignatureValidator>();
builder.Services.AddSingleton<ICvTextExtractor, PdfCvTextExtractor>();
builder.Services.AddSingleton<ICvTextExtractor, DocxCvTextExtractor>();
builder.Services.AddScoped<CvIntakeService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("upload", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests",
            Detail = "The upload limit has been reached. Please try again later.",
            Instance = httpContext.Request.Path.Value
        };
        problem.Extensions["errorCode"] = ApiErrorCode.RateLimited;

        // The overload that accepts a content type requires the serializer options,
        // so they are passed explicitly to keep the problem+json content type.
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    };
});

var app = builder.Build();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "ats-cv-api"
}));

app.MapPost(
        "/api/cv/upload",
        async (
            [FromForm] ResumeUploadForm form,
            CvIntakeService intakeService,
            HttpContext httpContext,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            if (form.File is not { } file)
            {
                return ApiProblem.Create(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Invalid CV upload",
                    "A CV file is required.",
                    ApiErrorCode.Validation,
                    ["A CV file is required."]);
            }

            var uploadedFileName = file.FileName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(uploadedFileName))
            {
                return ApiProblem.Create(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Invalid CV upload",
                    "A file name is required.",
                    ApiErrorCode.Validation,
                    ["A file name is required."]);
            }

            var extensionStart = uploadedFileName.LastIndexOf('.');
            var extension = extensionStart >= 0 ? uploadedFileName[extensionStart..] : string.Empty;
            var documentType = extension.ToLowerInvariant() switch
            {
                ".pdf" => DocumentType.Pdf,
                ".docx" => DocumentType.Docx,
                _ => (DocumentType?)null
            };

            if (documentType is null)
            {
                return ApiProblem.Create(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Unsupported file type",
                    "Only PDF and DOCX CV files are supported.",
                    ApiErrorCode.UnsupportedFileType);
            }

            try
            {
                await using var content = file.OpenReadStream();
                var result = await intakeService.IntakeAsync(
                    new ResumeUploadRequest(
                        uploadedFileName,
                        file.ContentType,
                        documentType.Value,
                        content,
                        form.AdditionalInformation),
                    cancellationToken);

                return Results.Ok(result);
            }
            catch (CvUploadValidationException exception)
            {
                var validation = exception.ValidationResult;
                var statusCode = validation.HasFileTooLarge
                    ? StatusCodes.Status413PayloadTooLarge
                    : StatusCodes.Status400BadRequest;
                var errorCode = validation.HasFileTooLarge
                    ? ApiErrorCode.FileTooLarge
                    : validation.HasUnsupportedFileType
                        ? ApiErrorCode.UnsupportedFileType
                        : ApiErrorCode.Validation;
                var title = validation.HasFileTooLarge
                    ? "Uploaded file is too large"
                    : validation.HasUnsupportedFileType
                        ? "Unsupported file type"
                        : "Invalid CV upload";

                return ApiProblem.Create(
                    httpContext,
                    statusCode,
                    title,
                    string.Join(" ", validation.Errors),
                    errorCode,
                    validation.Errors);
            }
            catch (UnsupportedCvFileException)
            {
                return ApiProblem.Create(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Unsupported file type",
                    "The file content does not match a supported PDF or DOCX format.",
                    ApiErrorCode.UnsupportedFileType);
            }
            catch (CvTextExtractionException exception)
            {
                logger.LogWarning(exception, "CV text extraction failed.");

                return ApiProblem.Create(
                    httpContext,
                    StatusCodes.Status422UnprocessableEntity,
                    "CV could not be read",
                    "The uploaded CV could not be read. Please upload a valid PDF or DOCX file.",
                    ApiErrorCode.ExtractionFailed);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unexpected error while processing a CV upload.");

                return ApiProblem.Create(
                    httpContext,
                    StatusCodes.Status500InternalServerError,
                    "CV upload failed",
                    "The CV could not be processed. Please try again later.",
                    ApiErrorCode.Unexpected);
            }
        })
    .WithName("UploadCv")
    .Accepts<ResumeUploadForm>("multipart/form-data")
    .Produces<CvIntakeResult>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
    .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
    .ProducesProblem(StatusCodes.Status429TooManyRequests)
    .ProducesProblem(StatusCodes.Status500InternalServerError)
    .DisableAntiforgery()
    .RequireRateLimiting("upload");

app.Run();
