using AtsCv.Domain.Candidates;

namespace AtsCv.Application.Persistence;

public interface ICandidateProfileRepository
{
    void Add(CandidateProfile candidateProfile);
}