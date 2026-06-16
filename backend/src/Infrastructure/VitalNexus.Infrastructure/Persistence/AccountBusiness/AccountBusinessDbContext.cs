using Microsoft.EntityFrameworkCore;
using VitalNexus.Domain.Entities;

namespace VitalNexus.Infrastructure.Persistence.AccountBusiness;

public sealed class AccountBusinessDbContext : DbContext
{
    public AccountBusinessDbContext(DbContextOptions<AccountBusinessDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
    }
}
