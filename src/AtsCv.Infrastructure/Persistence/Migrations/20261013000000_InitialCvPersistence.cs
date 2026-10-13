using AtsCv.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtsCv.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AtsCvDbContext))]
[Migration("20261013000000_InitialCvPersistence")]
public sealed class InitialCvPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CandidateProfiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Summary = table.Column<string>(type: "TEXT", nullable: true),
                Skills = table.Column<string>(type: "TEXT", nullable: false),
                Languages = table.Column<string>(type: "TEXT", nullable: false),
                Certifications = table.Column<string>(type: "TEXT", nullable: false),
                AdditionalInformation = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_CandidateProfiles", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Resumes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CandidateProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                ContentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ExtractedText = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Resumes", x => x.Id);
                table.ForeignKey(
                    name: "FK_Resumes_CandidateProfiles_CandidateProfileId",
                    column: x => x.CandidateProfileId,
                    principalTable: "CandidateProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Resumes_CandidateProfileId",
            table: "Resumes",
            column: "CandidateProfileId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Resumes");
        migrationBuilder.DropTable(name: "CandidateProfiles");
    }
}