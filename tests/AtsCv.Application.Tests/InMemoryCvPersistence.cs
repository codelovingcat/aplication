using AtsCv.Application.Persistence;
using AtsCv.Domain.Candidates;
using AtsCv.Domain.Resumes;

namespace AtsCv.Application.Tests;

internal sealed class InMemoryResumeRepository : IResumeRepository
{
    private readonly Dictionary<Guid, Resume> _resumes = new();

    public Task<Resume?> GetByIdAsync(Guid resumeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _resumes.TryGetValue(resumeId, out var resume);
        return Task.FromResult(resume);
    }

    public void Add(Resume resume) => _resumes.Add(resume.Id, resume);

    public void Update(Resume resume) => _resumes[resume.Id] = resume;

    public IReadOnlyCollection<Resume> All => _resumes.Values.ToArray();
}

internal sealed class InMemoryCandidateProfileRepository : ICandidateProfileRepository
{
    private readonly Dictionary<Guid, CandidateProfile> _profiles = new();

    public void Add(CandidateProfile candidateProfile) =>
        _profiles.Add(candidateProfile.Id, candidateProfile);

    public IReadOnlyCollection<CandidateProfile> All => _profiles.Values.ToArray();
}

internal sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SaveCount++;
        return Task.FromResult(1);
    }
}
