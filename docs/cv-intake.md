# CV Intake

The first user-facing CV screen should support two complementary inputs.

## CV file

Accepted formats:
- PDF
- DOCX

The original candidate document should be stored separately from extracted text and generated documents. The initial SQLite persistence currently stores the extracted text and resume metadata; original file retention and cleanup are tracked separately.

## Additional information

The screen should contain an optional multiline text area.

Candidates can use it for information that is missing from the CV, recently changed, important for a target role, or easier to explain in free text.

Examples:
- A recently completed certification.
- Additional responsibilities not listed on the CV.
- Preferred role or technology focus.
- Clarification about ownership of a project.

This is candidate-provided source data. AI may use it when analyzing or generating a CV, but must not turn unsupported assumptions into facts.

## UI direction

The first screen can contain:
- CV upload card
- accepted-format hint: PDF / DOCX
- optional "Eklemek istediğin başka bilgiler var mı?" textarea
- continue/analyze button
- upload status and validation messages

The textarea is optional. A candidate with only a CV can continue.

## Backend direction

The API receives both the original file and additional information. The application layer keeps these concepts separate so future structured fields can be added without replacing the free-form input.

### Initial intake API

`POST /api/cv/upload` accepts `multipart/form-data` with:

- `File`: required PDF or DOCX file, up to 10 MiB.
- `AdditionalInformation`: optional text, up to 2,000 characters.

The file extension, declared content type, and file signature are checked before extraction. Successful requests return HTTP 200 with `candidateProfileId`, `resumeId`, `fileName`, `extractedText`, and the normalized `additionalInformation`. The candidate profile is created as a draft; the service does not infer a candidate name from a filename or unstructured text.

Errors use the ProblemDetails contract in `api-error-contract.md`. The stable error codes include `validation_error`, `unsupported_file_type`, `file_too_large`, `extraction_failed`, `rate_limited`, and `unexpected_error`.

This endpoint creates the intake draft, extracts text, and saves the candidate profile and resume in one database unit of work. SQLite uses the configured `ConnectionStrings:AtsCv` connection string (default: `Data Source=atscv.db`); migrations are applied at API startup. Original file storage and retention remain separate follow-up work.

### Persisted-resume analysis API

`POST /api/cv/{resumeId}/analyze` loads a saved resume, verifies that text extraction has completed, runs the deterministic ATS analyzer, and persists the resulting `Analyzed` status. The typed response includes `resumeId`, `status`, `score`, `strengths`, `issues`, `recommendations`, `sectionFindings`, and `keywordFindings`. The score is an explainable heuristic, not a hiring prediction.

Next vertical slice:
1. Add repository-backed persistence for the intake draft and extracted text.
2. Store the original upload securely with a cleanup/retention policy.
3. Create or update CandidateProfile with structured, verified details.
4. Run ATS analysis and return findings.
7. Explain detected issues.
