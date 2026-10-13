# Explainable ATS analysis

The first analysis pass is deterministic and works only with extracted resume text. It does not compare a CV with a job description, infer facts that are not present, or predict hiring outcomes.

## Score (0–100)

The score is the sum of five independently visible categories. The category maximums total 100 points.

| Category | Maximum | Rules |
| --- | ---: | --- |
| Section headings | 30 | Professional summary/profile: 5; experience: 10; education: 7; skills: 8. A category earns its points when a recognized heading is present. |
| Contact details | 20 | Email address: 9; phone-number-like value: 7; professional/portfolio URL: 4. A value is counted only when it is detected in the extracted text. |
| General keyword signals | 15 | Distinct matches from a small, built-in list of common skills and action terms: 0 matches = 0; 1 = 5; 2–3 = 10; 4 or more = 15. This is not a job requirements list. |
| Text length/readability | 20 | Word count: 150–1,200 = 15; 100–149 = 8; 50–99 = 4; 1,201–1,800 = 10; over 1,800 = 6; fewer than 50 = 0. Add up to 5 points for the ratio of letters/digits to all extracted characters (at least 55% = 5; at least 35% = 3; at least 20% = 1). |
| Text-visible layout signals | 15 | Starts at 15 points and subtracts 5 for each detected signal: at least three tab separators, at least two lines containing two or more pipes, or at least three lines with wide gaps between values. This category is not scored when fewer than 50 words were extracted. |

The final value is clamped to 0–100. Because this is a heuristic, it is neither a hiring probability nor a prediction of interview success.

## Finding groups

- **Strengths** contain signals positively detected in the text.
- **Issues** describe missing, unknown, sparse, or potentially risky signals. Missing a signal does not prove the underlying information or skill is absent.
- **Recommendations** describe concrete changes a candidate can review.
- **Section findings** show which headings were recognized and their contribution to the section score.
- **Keyword findings** list matches from the built-in general vocabulary and explicitly state that job-specific matching has not yet been performed.

## Important limitations

- Results depend on what the PDF/DOCX extractor returns. A plain-text pass cannot reliably determine fonts, colors, columns, icons, tables, or the original visual hierarchy.
- The layout checks detect only visible text clues (tabs, pipe-separated lines, and wide spacing). Absence of those clues is not proof of ATS compatibility.
- No absent skills, qualifications, employers, dates, or achievements are invented.
- An empty or sparse extraction receives a low text-quality score and explicit warnings instead of a fabricated assessment.
- AtsResumeAnalysisService marks a resume as Analyzed only after the analyzer returns a non-null result. Exceptions leave the prior status unchanged.
