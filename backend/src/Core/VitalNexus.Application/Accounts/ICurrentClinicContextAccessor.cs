namespace VitalNexus.Application.Accounts;

public interface ICurrentClinicContextAccessor
{
    Task<ClinicContext?> GetCurrentAsync(CancellationToken cancellationToken = default);
}
