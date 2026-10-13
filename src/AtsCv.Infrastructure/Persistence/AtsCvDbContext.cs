using System.Text.Json;
using AtsCv.Domain.Candidates;
using AtsCv.Domain.Resumes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AtsCv.Infrastructure.Persistence;

public sealed class AtsCvDbContext : DbContext
{
    private static readonly ValueConverter<IReadOnlyCollection<string>, string> StringCollectionConverter =
        new(
            values => JsonSerializer.Serialize(values, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<string[]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<string>());

    private static readonly ValueComparer<IReadOnlyCollection<string>> StringCollectionComparer =
        new(
            (left, right) => left!.SequenceEqual(right!),
            values => values.Aggregate(0, (hash, value) => HashCode.Combine(hash, value)),
            values => (IReadOnlyCollection<string>)values.ToArray());

    public AtsCvDbContext(DbContextOptions<AtsCvDbContext> options)
        : base(options)
    {
    }

    public DbSet<Resume> Resumes => Set<Resume>();

    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CandidateProfile>(entity =>
        {
            entity.ToTable("CandidateProfiles");
            entity.HasKey(profile => profile.Id);
            entity.Property(profile => profile.Id).ValueGeneratedNever();
            entity.Property(profile => profile.FullName).HasMaxLength(200).IsRequired();
            entity.Property(profile => profile.Email).HasMaxLength(320);
            entity.Property(profile => profile.Phone).HasMaxLength(50);
            entity.Property(profile => profile.Summary);
            entity.Property(profile => profile.AdditionalInformation);

            var skills = entity.Property(profile => profile.Skills).HasConversion(StringCollectionConverter);
            skills.Metadata.SetValueComparer(StringCollectionComparer);

            var languages = entity.Property(profile => profile.Languages).HasConversion(StringCollectionConverter);
            languages.Metadata.SetValueComparer(StringCollectionComparer);

            var certifications = entity.Property(profile => profile.Certifications).HasConversion(StringCollectionConverter);
            certifications.Metadata.SetValueComparer(StringCollectionComparer);
        });

        modelBuilder.Entity<Resume>(entity =>
        {
            entity.ToTable("Resumes");
            entity.HasKey(resume => resume.Id);
            entity.Property(resume => resume.Id).ValueGeneratedNever();
            entity.Property(resume => resume.CandidateProfileId).IsRequired();
            entity.Property(resume => resume.OriginalFileName).HasMaxLength(260).IsRequired();
            entity.Property(resume => resume.ContentType).HasMaxLength(200).IsRequired();
            entity.Property(resume => resume.ExtractedText);
            entity.Property(resume => resume.Status).HasConversion<int>().IsRequired();
            entity.Property(resume => resume.CreatedAtUtc).IsRequired();

            entity.HasOne<CandidateProfile>()
                .WithMany()
                .HasForeignKey(resume => resume.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasIndex(resume => resume.CandidateProfileId);
        });
    }
}