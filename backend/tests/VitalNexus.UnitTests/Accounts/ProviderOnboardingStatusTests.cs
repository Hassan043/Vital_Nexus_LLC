using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.UnitTests.Accounts;

public sealed class ProviderOnboardingStatusTests
{
    [Fact]
    public void FromMemberships_ReturnsPendingWhenNoActiveMemberships()
    {
        var status = ProviderOnboardingStatus.FromMemberships([]);

        Assert.Equal(ProviderOnboardingStatus.Pending, status);
    }

    [Fact]
    public void FromMemberships_ReturnsPendingWhenOnlyInactiveMemberships()
    {
        var memberships = new[]
        {
            new ClinicMembership
            {
                ClinicId = Guid.NewGuid(),
                ClinicName = "Inactive Clinic",
                JoinedAt = DateTime.UtcNow,
                IsActive = false,
            },
        };

        var status = ProviderOnboardingStatus.FromMemberships(memberships);

        Assert.Equal(ProviderOnboardingStatus.Pending, status);
    }

    [Fact]
    public void FromMemberships_ReturnsCompleteWhenActiveMembershipExists()
    {
        var memberships = new[]
        {
            new ClinicMembership
            {
                ClinicId = Guid.NewGuid(),
                ClinicName = "Active Clinic",
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
            },
        };

        var status = ProviderOnboardingStatus.FromMemberships(memberships);

        Assert.Equal(ProviderOnboardingStatus.Complete, status);
    }
}
