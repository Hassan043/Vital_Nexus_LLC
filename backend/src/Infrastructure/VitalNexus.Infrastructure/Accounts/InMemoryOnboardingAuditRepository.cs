using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryOnboardingAuditRepository : IOnboardingAuditRepository
{
    private readonly ConcurrentDictionary<Guid, OnboardingAuditEvent> _events = new();

    public Task RecordAsync(OnboardingAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        _events[auditEvent.Id] = auditEvent;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OnboardingAuditEvent>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var events = _events.Values
            .Where(entry => entry.CustomerId == customerId)
            .OrderBy(entry => entry.OccurredAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<OnboardingAuditEvent>>(events);
    }
}
