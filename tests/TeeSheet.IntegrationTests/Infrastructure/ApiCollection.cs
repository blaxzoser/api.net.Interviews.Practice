namespace TeeSheet.IntegrationTests.Infrastructure;

/// <summary>
/// All tests in this collection share one <see cref="ApiFactory"/> (one SQL Server container)
/// and run one after another, so the SQL log and the data are not shared concurrently.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
