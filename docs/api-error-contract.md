# API Error Contract

API errors use RFC 7807-style ProblemDetails responses.

The response contains type, title, status, detail, instance, and the `errorCode` extension. Validation responses may also include an `errors` array with client-safe messages.

Stable error codes are `validation_error`, `unsupported_file_type`, `file_too_large`, `extraction_failed`, `rate_limited`, and `unexpected_error`.

The CV intake endpoint uses HTTP 400 for invalid requests and unsupported file contents, 413 for files above the upload limit, 422 when a supported document cannot be extracted, 429 for rate limiting, and 500 for unexpected server failures.

The persisted-resume analysis endpoint uses HTTP 404 with `resume_not_found` when the requested resume does not exist and HTTP 409 with `resume_not_ready` when text extraction has not completed. Unexpected analysis failures use HTTP 500 with `unexpected_error`.

Internal exception messages, stack traces, filesystem paths, and provider details are not returned to clients.
