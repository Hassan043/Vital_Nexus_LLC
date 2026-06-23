using Microsoft.AspNetCore.Http;
using VitalNexus.Application.Identity;

namespace VitalNexus.Infrastructure.Identity;

public sealed class HttpContextExternalIdentityAccessor(IHttpContextAccessor httpContextAccessor) : IExternalIdentityAccessor
{
    public TrustedExternalIdentity? Current =>
        ExternalIdentityClaimsReader.TryRead(httpContextAccessor.HttpContext?.User);
}
