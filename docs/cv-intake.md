# CV Intake

The first user-facing CV screen should support two complementary inputs.

## CV file

Accepted formats:
- PDF
- DOCX

The original candidate document is stored separately from extracted text and generated documents.

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

Next vertical slice:
1. Validate file.
2. Store it securely.
3. Extract PDF/DOCX text.
4. Create Resume.
5. Create or update CandidateProfile with AdditionalInformation.
6. Run ATS analysis.
7. Explain detected issues.
