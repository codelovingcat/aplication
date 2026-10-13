using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AtsCv.Application.Resumes;

namespace AtsCv.Application.AI;

/// <summary>
/// Scores only signals that can be observed in extracted text. The rules are
/// deterministic, explainable, and intentionally do not estimate hiring outcomes.
/// </summary>
public sealed class ExplainableAtsResumeAnalyzer : IAtsResumeAnalyzer
{
    private static readonly Regex EmailRegex = new(
        @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PhoneRegex = new(
        @"(?<!\w)\+?\d[\d ()-]{6,}\d(?!\w)",
        RegexOptions.CultureInvariant);

    private static readonly Regex ProfessionalUrlRegex = new(
        @"\b(?:https?://|www\.)\S+|\b(?:linkedin\.com/in|github\.com)/[A-Za-z0-9._-]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex WordRegex = new(
        @"[\p{L}\p{N}]+",
        RegexOptions.CultureInvariant);

    private static readonly SectionRule[] SectionRules =
    {
        new("Professional summary", 5, new[]
        {
            "summary", "professional summary", "profile", "professional profile",
            "about me", "özet", "profil", "hakkımda"
        }),
        new("Experience", 10, new[]
        {
            "experience", "work experience", "professional experience", "employment history",
            "career history", "iş deneyimi", "çalışma deneyimi", "deneyim"
        }),
        new("Education", 7, new[]
        {
            "education", "educational background", "academic background",
            "eğitim", "öğrenim"
        }),
        new("Skills", 8, new[]
        {
            "skills", "technical skills", "core skills", "skills and technologies",
            "competencies", "expertise", "yetkinlikler", "beceriler", "teknik beceriler"
        })
    };

    // General-purpose examples only; this list is not a job-specific requirements list.
    private static readonly string[] GeneralKeywordVocabulary =
    {
        "developed", "implemented", "managed", "led", "designed", "analyzed",
        "improved", "created", "delivered", "built", "tested", "automated",
        "collaboration", "communication", "leadership", "planning", "budget",
        "customer", "research", "strategy", "C#", ".NET", "SQL", "Python",
        "Java", "JavaScript", "TypeScript", "React", "Azure", "AWS", "Docker",
        "Kubernetes", "Git", "Excel", "geliştirdim", "yönettim", "tasarladım",
        "uyguladım", "iletişim", "liderlik", "proje", "ekip", "müşteri"
    };

    public Task<ResumeAnalysisResult> AnalyzeAsync(
        string resumeText,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var text = resumeText ?? string.Empty;
        var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        var strengths = new List<string>();
        var issues = new List<string>();
        var recommendations = new List<string>();
        var sectionFindings = new List<string>();
        var keywordFindings = new List<string>();

        // The four section checks total 30 points.
        var sectionPoints = 0;
        foreach (var rule in SectionRules)
        {
            if (HasSectionHeading(lines, rule.Aliases))
            {
                sectionPoints += rule.Points;
                strengths.Add("A clear " + rule.Name.ToLowerInvariant() + " heading was detected.");
                sectionFindings.Add("Found: " + rule.Name + " (" + rule.Points + "/" + rule.Points + " points).");
            }
            else
            {
                issues.Add("No clear " + rule.Name.ToLowerInvariant() + " section was detected in the extracted text.");
                recommendations.Add("Add a clearly labelled " + rule.Name.ToLowerInvariant() + " section if the information is available.");
                sectionFindings.Add("Missing/unknown: " + rule.Name + " (0/" + rule.Points + " points).");
            }
        }

        // Contact details total 20 points: email 9, phone 7, professional URL 4.
        var contactPoints = 0;
        if (EmailRegex.IsMatch(text))
        {
            contactPoints += 9;
            strengths.Add("An email address was detected in the extracted text.");
        }
        else
        {
            issues.Add("No email address was detected.");
            recommendations.Add("Add a contact email address if it is appropriate to share.");
        }

        var hasPhone = PhoneRegex.Matches(text)
            .Cast<Match>()
            .Any(match => match.Value.Count(char.IsDigit) is >= 8 and <= 15);
        if (hasPhone)
        {
            contactPoints += 7;
            strengths.Add("A phone-number-like contact detail was detected.");
        }
        else
        {
            issues.Add("No phone number was detected; it may be absent or not extractable.");
            recommendations.Add("Consider including a reachable phone number if you want recruiters to contact you by phone.");
        }

        if (ProfessionalUrlRegex.IsMatch(text))
        {
            contactPoints += 4;
            strengths.Add("A professional or portfolio URL was detected.");
        }
        else
        {
            issues.Add("No professional profile or portfolio URL was detected.");
            recommendations.Add("Consider adding a relevant LinkedIn profile, portfolio, or code profile URL.");
        }

        // Keyword points are based on distinct terms from a small general vocabulary.
        // They do not measure suitability for a job without a job description.
        var recognizedKeywords = GeneralKeywordVocabulary
            .Where(term => ContainsWholeTerm(text, term))
            .ToArray();
        var keywordPoints = recognizedKeywords.Length >= 4
            ? 15
            : recognizedKeywords.Length >= 2
                ? 10
                : recognizedKeywords.Length == 1
                    ? 5
                    : 0;

        if (recognizedKeywords.Length > 0)
        {
            strengths.Add("General skill/action vocabulary was detected: " + string.Join(", ", recognizedKeywords) + ".");
            keywordFindings.Add("Recognized general terms: " + string.Join(", ", recognizedKeywords) + " (" + keywordPoints + "/15 points).");
        }
        else
        {
            issues.Add("No terms from the analyzer's general skill/action vocabulary were detected; this does not prove relevant skills are absent.");
            recommendations.Add("Name the tools, skills, methods, and actions that are genuinely supported by your experience.");
            keywordFindings.Add("No terms from the built-in general vocabulary were detected; relevant skills may still be present.");
        }

        keywordFindings.Add("Job-specific keyword matching was not performed because no job description was supplied.");

        // Text length/readability total 20 points.
        var wordCount = WordRegex.Matches(text).Count;
        var alphanumericCount = text.Count(char.IsLetterOrDigit);
        var alphanumericRatio = text.Length == 0 ? 0 : (double)alphanumericCount / text.Length;
        var lengthPoints = wordCount switch
        {
            >= 150 and <= 1200 => 15,
            >= 100 and < 150 => 8,
            >= 50 and < 100 => 4,
            > 1200 and <= 1800 => 10,
            > 1800 => 6,
            _ => 0
        };
        var readabilityPoints = alphanumericRatio switch
        {
            >= 0.55 => 5,
            >= 0.35 => 3,
            >= 0.20 => 1,
            _ => 0
        };
        var textQualityPoints = lengthPoints + (wordCount == 0 ? 0 : readabilityPoints);

        if (wordCount >= 150 && wordCount <= 1200)
        {
            strengths.Add("The extracted text contains " + wordCount + " words, within the broad 150–1,200 word screening band.");
        }
        else if (wordCount < 50)
        {
            issues.Add("The extracted text is empty or sparse (" + wordCount + " words), so the analysis is inconclusive.");
            recommendations.Add("Check that the PDF or DOCX contains selectable text and retry extraction; do not treat a sparse extraction as a complete CV.");
        }
        else if (wordCount > 1200)
        {
            issues.Add("The extracted text is longer than the analyzer's broad 1,200-word screening band (" + wordCount + " words).");
            recommendations.Add("Review whether all extracted content is relevant and whether repeated headers or footers can be removed.");
        }
        else
        {
            issues.Add("The extracted text is shorter than the analyzer's broad 150-word screening band (" + wordCount + " words).");
            recommendations.Add("Make sure the extracted text includes the available experience, education, and skills sections.");
        }

        // Formatting checks are limited to visible text-extraction clues.
        // They cannot determine the original document's visual layout.
        var formattingRisks = new List<string>();
        var tabCount = text.Count(character => character == '\t');
        if (tabCount >= 3)
        {
            formattingRisks.Add("Frequent tab separators may indicate column-style layout in the extracted text.");
        }

        var pipeSeparatedLines = lines.Count(line => line.Count(character => character == '|') >= 2);
        if (pipeSeparatedLines >= 2)
        {
            formattingRisks.Add("Several pipe-separated lines may indicate a table or columns in the extracted text.");
        }

        var wideGapLines = lines.Count(line => Regex.IsMatch(line, @"\S.{0,60}\s{3,}\S", RegexOptions.CultureInvariant));
        if (wideGapLines >= 3)
        {
            formattingRisks.Add("Several lines contain wide spacing between values, which may indicate columns that were interleaved during extraction.");
        }

        foreach (var risk in formattingRisks)
        {
            issues.Add(risk);
            recommendations.Add("Prefer a simple single-column layout with clearly labelled sections, then verify the extracted text order.");
        }

        var formattingPoints = wordCount >= 50
            ? Math.Max(0, 15 - (formattingRisks.Count * 5))
            : 0;

        if (wordCount >= 50 && formattingRisks.Count == 0)
        {
            strengths.Add("No obvious tab- or pipe-based column signals were detected; the original visual layout cannot be verified from extracted text alone.");
        }
        else if (wordCount < 50)
        {
            issues.Add("There is not enough extracted text to assess layout-related signals reliably.");
        }

        recommendations.Add("This 0–100 score is a transparent heuristic for text-based ATS-readability signals, not a hiring probability or a prediction of interview success.");

        var score = Math.Clamp(
            sectionPoints + contactPoints + keywordPoints + textQualityPoints + formattingPoints,
            0,
            100);

        return Task.FromResult(new ResumeAnalysisResult(
            score,
            strengths.ToArray(),
            issues.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            recommendations.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            sectionFindings.ToArray(),
            keywordFindings.ToArray()));
    }

    private static bool HasSectionHeading(IEnumerable<string> lines, IEnumerable<string> aliases)
    {
        var normalizedAliases = aliases.Select(NormalizeHeading).ToArray();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Trim().Length > 80)
            {
                continue;
            }

            var normalizedLine = NormalizeHeading(line);
            if (normalizedAliases.Any(alias =>
                    normalizedLine.Equals(alias, StringComparison.Ordinal) ||
                    normalizedLine.StartsWith(alias + " ", StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeHeading(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var normalized = character == 'ı' ? 'i' : char.ToLowerInvariant(character);
            if (char.IsLetterOrDigit(normalized))
            {
                builder.Append(normalized);
            }
            else if (builder.Length > 0 && builder[^1] != ' ')
            {
                builder.Append(' ');
            }
        }

        return builder.ToString().Trim();
    }

    private static bool ContainsWholeTerm(string text, string term)
    {
        var pattern = @"(?<![\p{L}\p{N}])" + Regex.Escape(term) + @"(?![\p{L}\p{N}])";
        return Regex.IsMatch(
            text,
            pattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private sealed record SectionRule(string Name, int Points, string[] Aliases);
}
