# API Error Contract

API errors use RFC 7807-style ProblemDetails responses.

The response contains type, title, status, detail, instance, and errorCode.

Stable error codes are validation_error, unsupported_file_type, file_too_large, extraction_failed, rate_limited, and unexpected_error.

Internal exception messages, stack traces, filesystem paths, and provider details are not returned to clients.
