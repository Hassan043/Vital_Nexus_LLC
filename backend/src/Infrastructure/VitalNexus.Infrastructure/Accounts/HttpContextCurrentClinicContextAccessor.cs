using VitalNexus.Application.Accounts;
using Microsoft.AspNetCore.Http;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class HttpContextCurrentClinicContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    ICurrentAccountsUserAccessor currentAccountsUserAccessor,
    IClinicContextResolver clinicContextResolver) : ICurrentClinicContextAccessor
{
    private const string CacheItemKey = "VitalNexus.CurrentClinicContext";

    private static readonly object ResolvedNull = new();

    public async Task<ClinicContext?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        if (httpContext.Items.TryGetValue(CacheItemKey, out var cached))
        {
            return cached == ResolvedNull ? null : (ClinicContext)cached!;
        }

        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            httpContext.Items[CacheItemKey] = ResolvedNull;
            return null;
        }

        Guid? requestedClinicId = null;
        if (httpContext.Request.Headers.TryGetValue(ClinicContextHeaders.ClinicId, out var headerValues)
            && Guid.TryParse(headerValues.ToString(), out var parsedClinicId))
        {
            requestedClinicId = parsedClinicId;
        }

        var context = await clinicContextResolver.ResolveAsync(
            user,
            requestedClinicId,
            cancellationToken);
        httpContext.Items[CacheItemKey] = context ?? ResolvedNull;
        return context;
    }
}
