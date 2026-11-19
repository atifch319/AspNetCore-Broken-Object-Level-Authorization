namespace BolaDemo.Api.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public abstract class SqlIntegrationTest
{
    protected SqlIntegrationTest(SqlServerCollectionFixture fixture)
    {
        Fixture = fixture;
    }

    protected SqlServerCollectionFixture Fixture { get; }

    protected void RequireSql() => Fixture.Require();

    protected HttpClient CreateClient(string? email = null)
    {
        if (Fixture.Factory is null)
        {
            throw new InvalidOperationException("SQL Server is not available for this test.");
        }

        return DemoHttp.Create(Fixture.Factory, email);
    }
}
