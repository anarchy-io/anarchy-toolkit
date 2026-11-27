using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service.MockServer;
using Anarchy.Toolkit.Runtime.Web.MockServer.Service;
using AwesomeAssertions;
using System.Text.Json;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Core.Service.MockServer;

public sealed class OpenApiExpectationSyncTest : IDisposable
{
    private const string Specification = """
        openapi: 3.0.3
        info: { title: Shop, version: "1" }
        paths:
          /orders:
            post:
              operationId: createOrder
              tags: [Orders]
              responses:
                "201":
                  description: Created
                  content:
                    application/json:
                      example: { id: 7, status: open }
                "400": { description: Invalid }
          /orders/{orderId}:
            get:
              operationId: getOrder
              tags: [Orders]
              responses:
                "200":
                  description: The order
                  content:
                    application/json:
                      schema: { $ref: "#/components/schemas/Order" }
                "404": { description: Not found }
          /health:
            get:
              responses:
                "204": { description: Healthy }
        components:
          schemas:
            Order:
              type: object
              properties:
                id: { type: integer }
                placedAt: { type: string, format: date-time }
                status: { type: string, enum: [open, shipped] }
                lines:
                  type: array
                  items:
                    type: object
                    properties:
                      sku: { type: string }
        """;

    private readonly string _directory = Directory.CreateTempSubdirectory("openapi-sync-").FullName;

    private string Spec => Path.Combine(_directory, "shop.yaml");

    private string Mocks => Path.Combine(_directory, "mocks");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public OpenApiExpectationSyncTest() => File.WriteAllText(Spec, Specification);

    public void Dispose() => Directory.Delete(_directory, true);

    [Fact]
    public async Task SyncAsync_WhenThereAreNoExpectations_AddsOnePerEndpointGroupedByTag()
    {
        var report = await OpenApiExpectationSync.SyncAsync(Spec, Mocks, cancellationToken: Ct);

        report.Notices.Should().BeEmpty();
        report.Added.Select(a => (a.Id, a.StatusCode, Path.GetFileName(a.File))).Should().BeEquivalentTo([
            ("createOrder", 201, "orders.json"), ("getOrder", 200, "orders.json"), ("get-health", 204, "default.json"),
        ]);

        var orders = ExpectationLoader.Parse(await File.ReadAllTextAsync(Path.Combine(Mocks, "orders.json"), Ct));
        orders.Select(e => e.HttpRequest.Path).Should().Equal("/orders", "/orders/{orderId}");
        orders[0].HttpResponse.Body!.Value.GetProperty("status").GetString().Should().Be("open");
        var order = orders[1].HttpResponse.Body!.Value;
        order.GetProperty("placedAt").GetString().Should().Be("2024-01-01T00:00:00Z");
        order.GetProperty("status").GetString().Should().Be("open");
        order.GetProperty("lines")[0].GetProperty("sku").GetString().Should().Be("string");
    }

    [Fact]
    public async Task SyncAsync_WhenRunAgain_AddsNothing()
    {
        await OpenApiExpectationSync.SyncAsync(Spec, Mocks, cancellationToken: Ct);
        var before = await File.ReadAllTextAsync(Path.Combine(Mocks, "orders.json"), Ct);

        var report = await OpenApiExpectationSync.SyncAsync(Spec, Mocks, cancellationToken: Ct);

        report.Added.Should().BeEmpty();
        report.Notices.Should().BeEmpty();
        (await File.ReadAllTextAsync(Path.Combine(Mocks, "orders.json"), Ct)).Should().Be(before);
    }

    [Fact]
    public async Task SyncAsync_NeverChangesExistingExpectations_AndReportsDifferences()
    {
        Directory.CreateDirectory(Mocks);
        const string existing = """
            // Hand-written: keep my formatting.
            [
              { "id": "getOrder", "httpRequest": { "method": "GET", "path": "/orders/{id}" }, "httpResponse": { "statusCode": 410 } },
              { "id": "createOrder", "httpRequest": { "method": "PUT", "path": "/orders" }, "httpResponse": { "statusCode": 201 } },
              { "httpRequest": { "method": "DELETE", "path": "/carts/{id}" }, "httpResponse": { "statusCode": 204 } }
            ]
            """;
        await File.WriteAllTextAsync(Path.Combine(Mocks, "orders.json"), existing, Ct);

        var report = await OpenApiExpectationSync.SyncAsync(Spec, Mocks, cancellationToken: Ct);

        report.Added.Select(a => a.Id).Should().Equal("get-health");
        report.Notices.Select(n => (n.Kind, n.Expectation)).Should().BeEquivalentTo([
            (OpenApiSyncNoticeKind.EndpointChanged, "createOrder"),
            (OpenApiSyncNoticeKind.UndocumentedStatusCode, "getOrder"),
            (OpenApiSyncNoticeKind.NotInSpecification, "DELETE /carts/{id}"),
        ]);
        (await File.ReadAllTextAsync(Path.Combine(Mocks, "orders.json"), Ct)).Should().Be(existing);
    }

    [Fact]
    public async Task SyncAsync_AppendsAfterExistingExpectations_KeepingTheirText()
    {
        Directory.CreateDirectory(Mocks);
        const string existing = """
            [
              // The happy path.
              { "id": "createOrder", "httpRequest": { "method": "POST", "path": "/orders" }, "httpResponse": { "statusCode": 201 } }
            ]
            """;
        var file = Path.Combine(Mocks, "orders.json");
        await File.WriteAllTextAsync(file, existing, Ct);

        var report = await OpenApiExpectationSync.SyncAsync(Spec, Mocks, cancellationToken: Ct);

        report.Added.Select(a => a.Id).Should().Equal("getOrder", "get-health");
        var text = await File.ReadAllTextAsync(file, Ct);
        text.Should().StartWith(existing.TrimEnd()[..^1].TrimEnd());
        ExpectationLoader.Parse(text).Select(e => e.Id).Should().Equal("createOrder", "getOrder");
    }

    [Fact]
    public async Task SyncAsync_WhenAnOperationDocumentsOnlyErrors_AcceptsSuccessCodes()
    {
        await File.WriteAllTextAsync(Spec, Specification.Replace("\"204\": { description: Healthy }", "\"503\": { description: Unhealthy }"), Ct);

        await OpenApiExpectationSync.SyncAsync(Spec, Mocks, cancellationToken: Ct);
        var report = await OpenApiExpectationSync.SyncAsync(Spec, Mocks, cancellationToken: Ct);

        report.Notices.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncAsync_InADryRun_WritesNothing()
    {
        var report = await OpenApiExpectationSync.SyncAsync(Spec, Mocks, dryRun: true, cancellationToken: Ct);

        report.Added.Should().HaveCount(3);
        Directory.Exists(Mocks).Should().BeFalse();
    }

    [Fact]
    public async Task SyncAsync_WhenTheSpecificationIsMissing_Throws()
    {
        var sync = () => OpenApiExpectationSync.SyncAsync(Path.Combine(_directory, "nope.json"), Mocks, cancellationToken: Ct);

        await sync.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public void Append_WhenTheFileHoldsASingleObject_WrapsItInAnArray()
    {
        var file = Path.Combine(_directory, "single.json");
        File.WriteAllText(file, """{ "httpRequest": { "path": "/a" } }""");

        OpenApiExpectationSync.Append(file, [new() { ["httpRequest"] = new System.Text.Json.Nodes.JsonObject { ["path"] = "/b" } }]);

        using var json = JsonDocument.Parse(File.ReadAllText(file));
        json.RootElement.GetArrayLength().Should().Be(2);
    }
}
