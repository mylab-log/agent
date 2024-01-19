using Elasticsearch.Net;
using MyLab.Search.EsTest;

namespace IntegrationTests;

public class TestEsFixtureStrategy : EsFixtureStrategy
{
    public override IConnectionPool ProvideConnection()
    {
        return new SingleNodeConnectionPool(new Uri(TestStuff.EsUrl));
    }
}