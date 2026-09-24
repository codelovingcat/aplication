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

    public static CandidateProfile Create(
        string fullName,
        string? email = null,
        string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        return new CandidateProfile(Guid.NewGuid(), fullName.Trim(), email, phone);
    }

    public void UpdateContact(string? email, string? phone)
    {
        Email = email;
        Phone = phone;
    }
}
