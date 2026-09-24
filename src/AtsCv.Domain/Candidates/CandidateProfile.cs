namespace AtsCv.Domain.Candidates;

public sealed class CandidateProfile
{
    private CandidateProfile(
        Guid id,
        string fullName,
        string? email,
        string? phone)
    {
        Id = id;
        FullName = fullName;
        Email = email;
        Phone = phone;
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }

    // Free-form information supplied by the candidate.
    // This is treated as candidate-provided source data for later AI workflows.
    public string? AdditionalInformation { get; private set; }

    public static CandidateProfile Create(
        string fullName,
        string? email = null,
        string? phone = null,
        string? additionalInformation = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        return new CandidateProfile(Guid.NewGuid(), fullName.Trim(), email, phone)
        {
            AdditionalInformation = Normalize(additionalInformation)
        };
    }

    public void UpdateContact(string? email, string? phone)
    {
        Email = email;
        Phone = phone;
    }

    public void UpdateAdditionalInformation(string? additionalInformation)
    {
        AdditionalInformation = Normalize(additionalInformation);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
