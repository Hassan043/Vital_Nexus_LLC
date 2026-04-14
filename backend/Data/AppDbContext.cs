using Microsoft.EntityFrameworkCore;
using NutrientInsight.Api.Models;

namespace NutrientInsight.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<PetProfile> PetProfiles { get; set; }
    public DbSet<LabReport> LabReports { get; set; }
    public DbSet<LabMarker> LabMarkers { get; set; }
    public DbSet<BodyTendencyProfile> BodyTendencyProfiles { get; set; }
    public DbSet<ExercisePlan> ExercisePlans { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<LabReport>()
            .HasIndex(lr => lr.ReportPublicId)
            .IsUnique();

        modelBuilder.Entity<PasswordResetToken>()
            .HasIndex(prt => prt.TokenHash);

        modelBuilder.Entity<LabReport>()
            .HasOne(lr => lr.User)
            .WithMany(u => u.LabReports)
            .HasForeignKey(lr => lr.UserId);

        modelBuilder.Entity<LabReport>()
            .HasOne(lr => lr.PetProfile)
            .WithMany(p => p.LabReports)
            .HasForeignKey(lr => lr.PetProfileId)
            .IsRequired(false);

        modelBuilder.Entity<LabMarker>()
            .HasOne(lm => lm.LabReport)
            .WithMany(lr => lr.LabMarkers)
            .HasForeignKey(lm => lm.LabReportId);

        modelBuilder.Entity<BodyTendencyProfile>()
            .HasOne(btp => btp.User)
            .WithOne(u => u.BodyTendencyProfile)
            .HasForeignKey<BodyTendencyProfile>(btp => btp.UserId);

        modelBuilder.Entity<PasswordResetToken>()
            .HasOne(prt => prt.User)
            .WithMany(u => u.PasswordResetTokens)
            .HasForeignKey(prt => prt.UserId);
    }
}