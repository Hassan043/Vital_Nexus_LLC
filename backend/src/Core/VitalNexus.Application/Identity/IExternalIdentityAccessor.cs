namespace VitalNexus.Application.Identity;

public interface IExternalIdentityAccessor
{
    TrustedExternalIdentity? Current { get; }
}
