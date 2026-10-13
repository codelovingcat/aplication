using AtsCv.Application.Persistence;
using AtsCv.Domain.Resumes;
using Microsoft.EntityFrameworkCore;

namespace AtsCv.Infrastructure.Persistence;

public sealed class EfResumeRepository : IResumeRepository
{
    private readonly AtsCvDbContext _dbContext;

    public EfResumeRepository(AtsCvDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<Resume?> GetByIdAsync(
        Guid resumeId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Resumes.SingleOrDefaultAsync(resume => resume.Id == resumeId, cancellationToken);

    public void Add(Resume resume)
    {
        ArgumentNullException.ThrowIfNull(resume);
        _dbContext.Resumes.Add(resume);
    }

    public void Update(Resume resume)
    {
        ArgumentNullException.ThrowIfNull(resume);
        _dbContext.Resumes.Update(resume);
    }
}