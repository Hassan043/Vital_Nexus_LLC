using VitalNexus.IntegrationTests.Support;

namespace VitalNexus.IntegrationTests.Authentication;

[CollectionDefinition(EntraExternalIdTestCollection.Name)]
public sealed class EntraExternalIdTestCollection : ICollectionFixture<EntraExternalIdWebApplicationFactory>
{
    public const string Name = "EntraExternalId";
}
