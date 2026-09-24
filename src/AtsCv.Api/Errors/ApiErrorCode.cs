namespace AtsCv.Api.Errors;

public static class ApiErrorCode
{
    public const string Validation = "validation_error";
    public const string UnsupportedFileType = "unsupported_file_type";
    public const string FileTooLarge = "file_too_large";
    public const string ExtractionFailed = "extraction_failed";
    public const string RateLimited = "rate_limited";
    public const string Unexpected = "unexpected_error";
}
