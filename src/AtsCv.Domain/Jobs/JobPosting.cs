namespace AtsCv.Domain.Jobs;

public sealed class JobPosting
{
    private JobPosting(
        Guid id,
        string companyName,
        string jobTitle,
        string description)
    {
        Id = id;
        CompanyName = companyName;
        JobTitle = jobTitle;
        Description = description;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string CompanyName { get; private set; }
    public string JobTitle { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static JobPosting Create(
        string companyName,
        string jobTitle,
        string description)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name is required.", nameof(companyName));

        if (string.IsNullOrWhiteSpace(jobTitle))
            throw new ArgumentException("Job title is required.", nameof(jobTitle));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Job description is required.", nameof(description));

        return new JobPosting(
            Guid.NewGuid(),
            companyName.Trim(),
            jobTitle.Trim(),
            description.Trim());
    }
}
