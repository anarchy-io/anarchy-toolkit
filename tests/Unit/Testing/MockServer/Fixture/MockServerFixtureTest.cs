using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Testing.MockServer.Assertion;
using Anarchy.Toolkit.Testing.MockServer.Fixture;
using AwesomeAssertions;
using System.Net;

namespace Anarchy.Toolkit.Tests.Unit.Testing.MockServer.Fixture;

public sealed class SpawnedMockServer : MockServerFixture
{
    public string RecordFile { get; } = Path.Combine(Path.GetTempPath(), $"mockserver-{Guid.NewGuid():N}.jsonl");

    protected override void Configure(MockServerProcessOptions options)
    {
        options.Port = 0;
        options.RecordFile = RecordFile;
        options.Verbosity = "quiet";
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        File.Delete(RecordFile);
    }
}

public class MockServerFixtureTest(SpawnedMockServer mock) : TestCaseBase, IClassFixture<SpawnedMockServer>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SpawnedServer_AnswersRuntimeExpectationsAndRecordsRequests()
    {
        await mock.Client.ResetAsync(Ct);
        await mock.Client.AddExpectationAsync("POST", "/orders", 201, new { id = 7 }, cancellationToken: Ct);
        using var http = new HttpClient { BaseAddress = mock.BaseAddress };

        var response = await http.PostAsync("/orders", new StringContent("""{ "sku": "A1" }"""), Ct);
        await http.GetAsync("/unknown", Ct);

        mock.IsSpawned.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var orders = await mock.Client.AssertReceivedAsync("POST", "/orders", times: 1);
        orders[0].BodyAs<Dictionary<string, string>>()!["sku"].Should().Be("A1");
        await mock.Client.AssertNotReceivedAsync("DELETE", "/orders");
        RecordedRequestsFile.Read(mock.RecordFile).ShouldHaveReceived("GET", "/unknown");
    }

    [Fact]
    public async Task ShouldHaveReceived_WhenTheCountDiffers_FailsListingTheRequests()
    {
        await mock.Client.ResetAsync(Ct);
        using var http = new HttpClient { BaseAddress = mock.BaseAddress };
        await http.GetAsync("/orders/1", Ct);

        var assert = async () => await mock.Client.AssertReceivedAsync("GET", "/orders/{id}", times: 2);

        (await assert.Should().ThrowAsync<Exception>()).WithMessage("*received 1 time(s)*GET /orders/1*");
    }
}
