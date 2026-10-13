using AtsCv.Domain.Resumes;

namespace AtsCv.Application.Persistence;

public interface IResumeRepository
{
    Task<Resume?> GetByIdAsync(Guid resumeId, CancellationToken cancellationToken = default);

    void Add(Resume resume);

    void Update(Resume resume);
}