using System.Net;
using TaskFlow.IntegrationTests.Common;

namespace TaskFlow.IntegrationTests;

/// <summary>
/// Proves the Testcontainers + WebApplicationFactory harness itself works:
/// container starts, migrations apply, composition root resolves, and the
/// app answers HTTP requests. Zero business-logic assertions belong here —
/// see EP01-B1-04b for CreateTask integration coverage.
/// </summary>
public sealed class HarnessSmokeTests : IntegrationTestBase
{
    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
