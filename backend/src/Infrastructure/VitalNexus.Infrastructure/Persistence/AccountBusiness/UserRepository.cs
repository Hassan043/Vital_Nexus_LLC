using Microsoft.EntityFrameworkCore;
using VitalNexus.Application.Abstractions;
using VitalNexus.Domain.Entities;
using VitalNexus.Infrastructure.Persistence.AccountBusiness;

namespace VitalNexus.Infrastructure.Persistence.AccountBusiness;

public sealed class UserRepository : IUserRepository
{
    private readonly AccountBusinessDbContext _dbContext;

    public UserRepository(AccountBusinessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        _dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
