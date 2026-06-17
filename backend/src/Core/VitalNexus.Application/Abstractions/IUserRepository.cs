using VitalNexus.Domain.Entities;

namespace VitalNexus.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
