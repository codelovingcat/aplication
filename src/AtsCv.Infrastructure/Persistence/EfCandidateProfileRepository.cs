using AtsCv.Application.Persistence;
using AtsCv.Domain.Candidates;

namespace AtsCv.Infrastructure.Persistence;

public sealed class EfCandidateProfileRepository : ICandidateProfileRepository
{
    private readonly AtsCvDbContext _dbContext;

    public EfCandidateProfileRepository(AtsCvDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public void Add(CandidateProfile candidateProfile)
    {
        ArgumentNullException.ThrowIfNull(candidateProfile);
        _dbContext.CandidateProfiles.Add(candidateProfile);
    }
}