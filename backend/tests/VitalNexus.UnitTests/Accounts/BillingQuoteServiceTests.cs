using VitalNexus.Infrastructure.Accounts;

namespace VitalNexus.UnitTests.Accounts;

public sealed class BillingQuoteServiceTests
{
    [Fact]
    public async Task CreateQuoteAsync_ReturnsServerPricing()
    {
        var service = new BillingQuoteService(new InMemoryPlanTierRepository());

        var quote = await service.CreateQuoteAsync(1, clientPriceCents: null);

        Assert.Equal(1, quote.PlanTierId);
        Assert.Equal("Starter", quote.PlanName);
        Assert.Equal(9900, quote.MonthlyPriceCents);
        Assert.Equal(250, quote.PatientCapMax);
    }

    [Fact]
    public async Task CreateQuoteAsync_RejectsClientSuppliedPricing()
    {
        var service = new BillingQuoteService(new InMemoryPlanTierRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateQuoteAsync(1, clientPriceCents: 100));
    }
}
