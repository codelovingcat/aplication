# AI Grounding Rules

AI-generated CVs, cover letters, and application recommendations must remain grounded in candidate-provided or independently verified source data.

## Allowed source data

The generation pipeline may use:

- Information extracted from the candidate's CV.
- Information explicitly supplied by the candidate as additional information.
- Structured candidate profile data derived from those sources.
- Job information supplied by the candidate or imported from a trusted job source.

## Prohibited fabrication

The system must not invent or infer unsupported facts such as:

- Employers or job titles.
- Employment dates.
- Degrees or institutions.
- Certifications.
- Skills the candidate has not provided.
- Awards, achievements, metrics, or responsibilities.

## Unsupported information

When a requested output needs information that is not present in the source data, the system should:

1. Leave the field empty or omit it.
2. Ask the candidate for the missing information when clarification is appropriate.
3. Present a recommendation as a suggestion rather than a factual claim.

## Traceability

Generated factual claims should be traceable to candidate-provided or verified source data before they are persisted or presented as facts.

## Security boundary

Prompt instructions, generated text, and model output are not trusted sources of candidate facts. They must not override the source-data boundary above.
