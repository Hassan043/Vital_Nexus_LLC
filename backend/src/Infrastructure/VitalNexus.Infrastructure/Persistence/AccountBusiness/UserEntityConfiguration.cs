using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VitalNexus.Domain.Entities;

namespace VitalNexus.Infrastructure.Persistence.AccountBusiness;

internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(user => user.Email)
            .HasColumnName("Email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.NormalizedEmail)
            .HasColumnName("NormalizedEmail")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("UQ_Users_NormalizedEmail");

        builder.Property(user => user.DisplayName)
            .HasColumnName("DisplayName")
            .HasMaxLength(200);

        builder.Property(user => user.PasswordHash)
            .HasColumnName("PasswordHash")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();
    }
}
