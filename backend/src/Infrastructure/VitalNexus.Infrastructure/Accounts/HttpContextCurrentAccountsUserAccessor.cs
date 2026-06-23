using VitalNexus.Application.Accounts;
using VitalNexus.Application.Identity;
using VitalNexus.Domain.Accounts;
using Microsoft.AspNetCore.Http;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class HttpContextCurrentAccountsUserAccessor(
    IHttpContextAccessor httpContextAccessor,
    IExternalIdentityAccessor externalIdentityAccessor,
    IExternalIdentityAccountsUserMapper mapper) : ICurrentAccountsUserAccessor
{
    private const string CacheItemKey = "VitalNexus.CurrentAccountsUser";

    public async Task<AccountsUser?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        if (httpContext.Items.TryGetValue(CacheItemKey, out var cached) && cached is AccountsUser cachedUser)
        {
            return cachedUser;
        }

        var identity = externalIdentityAccessor.Current;
        if (identity is null)
        {
            return null;
        }

        var user = await mapper.MapAsync(identity, cancellationToken);
        httpContext.Items[CacheItemKey] = user;
        return user;
    }
}
