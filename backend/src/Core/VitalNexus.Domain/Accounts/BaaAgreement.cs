namespace VitalNexus.Domain.Accounts;

public sealed class BaaAgreement
{
    public Guid CustomerId { get; init; }

    public Guid SignedByUserId { get; init; }

    public DateTime SignedAt { get; init; }

    public string AgreementVersion { get; init; } = "2026.1";
}
