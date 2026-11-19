using Xunit.Abstractions;

namespace BolaDemo.Api.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerMetadataTests : SqlIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public SqlServerMetadataTests(SqlServerCollectionFixture fixture, ITestOutputHelper output)
        : base(fixture)
    {
        _output = output;
    }

    [SqlFact]
    public void Records_the_sql_server_version_and_seed_counts()
    {
        RequireSql();
        _output.WriteLine("ProductMajorVersion={0}", Fixture.ProductMajorVersion);
        _output.WriteLine("ServerVersion={0}", Fixture.ServerVersion);
        _output.WriteLine("Database={0}", Fixture.DatabaseName);
        _output.WriteLine("Users={0}", Fixture.UserCount);
        _output.WriteLine("Orders={0}", Fixture.OrderCount);

        Assert.False(string.IsNullOrWhiteSpace(Fixture.ServerVersion));
        Assert.Equal(3, Fixture.UserCount);
        Assert.Equal(5, Fixture.OrderCount);
    }
}
