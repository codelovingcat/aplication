# ATS CV Application Architecture

## Product goal

The application will help a candidate turn an existing CV into an ATS-friendly CV and later generate job-specific CVs and cover letters.

## Core flow

1. Upload an existing PDF or DOCX CV.
2. Extract document text without changing the source-of-truth content.
3. Analyze ATS compatibility and explain important issues.
4. Build a structured candidate profile from candidate-provided information.
5. Generate an ATS-friendly CV using only verified candidate data.
6. Accept a job description and optional extra skills.
7. Match the candidate profile to job requirements.
8. Generate a tailored CV and cover letter as versioned application artifacts.

## Source-of-truth rule

The long-lived candidate profile is the source of truth.

AI-generated content must not invent:
- work experience
- employers
- dates
- education
- certifications
- technical skills
- achievements

Generated content may reorganize, summarize, or rewrite verified information, but unsupported facts must not be introduced.

## Layers

### Domain

Business entities and rules:
- CandidateProfile
- Resume
- JobPosting
- future: ResumeAnalysis
- future: TailoredApplication

### Application

Use cases and contracts:
- resume upload
- resume analysis
- job matching
- CV tailoring
- cover-letter generation
- AI provider abstractions

### Infrastructure

Technical implementations:
- PDF/DOCX text extraction
- persistence
- file storage
- AI providers
- document generation

### API

HTTP boundary for:
- resume upload
- analysis
- candidate profile management
- job posting input
- tailored application generation

## First vertical slice

First implementation milestone:

Upload CV -> store metadata -> extract text -> create candidate profile draft

Next milestone:

Analyze ATS -> explain issues -> generate optimized CV

Then:

Job posting + candidate profile -> job match -> tailored CV + cover letter

## Data model direction

The candidate profile is long-lived and reusable.

Each tailored application should be a versioned snapshot containing:
- target company
- target job title
- supplied job description
- selected candidate data
- generated CV version
- generated cover letter version
- generation metadata

This keeps historical applications reproducible even when the candidate later edits their profile.
