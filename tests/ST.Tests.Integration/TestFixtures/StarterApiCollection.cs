using Xunit;
namespace ST.Tests.Integration.TestFixtures;

[CollectionDefinition("Starter API")]
public sealed class StarterApiCollection : ICollectionFixture<StarterTemplateWebApplicationFactory>;
