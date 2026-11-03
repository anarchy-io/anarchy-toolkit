# Testing Conventions

This guide explains how test cases and shared test resources are named and organised in the toolkit. The goal is simple: a test or fixture should be easy to find from the type it exercises. 🧭

## Test layout

Test projects are never published, so there is **one test project per test kind**, not one per package. Today that is only `tests/Unit/Unit.csproj`. Inside it, the tests mirror the source tree twice: a folder path built from the name of the `src/` project under test (one folder per dot-separated segment), then the SUT's own path inside that project.

```text
src/
└── Core.Events/
    └── Service/Event/Domain/
        └── EventHandlerBus.cs

tests/
└── Unit/
    ├── Unit.csproj
    └── Core/
        └── Events/
            └── Core/Service/Event/Domain/
                └── EventHandlerBusTest.cs
```

- **One test project per kind**: `tests/<Kind>/<Kind>.csproj` (`tests/Unit/Unit.csproj`). It references every `src/` project it tests.
- **One folder per tested project**, built from the `src/` project name with each dot-separated segment as a nested folder: `src/Core` → `Core/`, `src/Core.Events` → `Core/Events/`, `src/Infrastructure.Persistence.Postgres` → `Infrastructure/Persistence/Postgres/`, `src/Web.Ui` → `Web/Ui/`. The tests of a project therefore sit next to the folders of the projects that extend its name (`Core/` holds `src/Core`'s tests alongside `Events/`, `Health/`, …).
- **Inside that folder, mirror the SUT's path** in its project. Do not invent folders that do not exist in the source namespace. Some suites (the `Core.*` projects, `Hosting.Workers` and parts of `Infrastructure.Persistence.Postgres`) still wrap that path in a layer folder (`Core/`, `Configuration/`, `Infrastructure/`), as in the example above; follow the layout already used by the suite you are adding to.
- **The namespace follows the folder**, with `Anarchy.Toolkit.Tests.<Kind>` as root:

  ```csharp
  namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Core.Service.Event.Domain;
  ```

  Because the folders split the project name on its dots, the namespace still reads `<Project>.<SUT path>`.

- **Resources a test needs embedded** (settings files, build metadata, text resources) are declared in `<Kind>.csproj` with an explicit `LogicalName` based on `$(AssemblyName)`, because every test now shares one assembly.
- **`InternalsVisibleTo`** in a `src/` project targets the test assembly name, `Anarchy.Toolkit.Tests.Unit`.

### Test kinds

| Kind | Project | Runs | Nuke target |
|---|---|---|---|
| Unit | `tests/Unit/Unit.csproj` | in-process, no infrastructure | `TestUnit` (`make test_unit`) |
| Integration | `tests/Integration/Integration.csproj` (none yet) | against real local resources (Docker: PostgreSQL, LocalStack) | `TestIntegration` |
| Acceptance | `tests/Acceptance/Acceptance.csproj` (none yet) | end-to-end through a hosted API or job | `TestAcceptance` |

The Nuke build discovers test projects by name (a project named after its kind is of that kind), so keep the naming exact. All test projects share `tests/Directory.Build.props`: xunit.v3 on Microsoft.Testing.Platform, AwesomeAssertions, Moq, and a reference to `Anarchy.Toolkit.Testing`. All suites of a kind run in one process, so tests must not rely on process-wide state another suite could change.

## Test naming rules

- Name the test type after its concrete subject under test (SUT), with the singular `Test` suffix: `FooBar.cs` becomes `FooBarTest.cs`.
- Keep the test file in the same relative folder as the SUT, with the namespace aligned to that folder.
- Use the exact SUT name. Do not shorten or rename path segments in the test tree.

```text
Good:  FooBarServiceTest.cs
Avoid: FooBarServiceTests.cs
```

> Some older suites (`Core.Health`, `Core.Observability`, `Infrastructure.CorrelationId`, the `Hosting.Workers` health API, `Infrastructure.Observability.*`, and the `Testing` in-memory sinks) still use `*Tests` class names. They do follow the folder and namespace rules above. New tests should use `Test`.

### Partial test classes

When one test type is split across files, keep a single primary file and name the others after the same test type:

```text
FooBarControllerTest.cs
FooBarControllerTest.Create.cs
FooBarControllerTest.Read.cs
```

### Test methods

Name test methods after the observed behaviour:

```csharp
Create_WhenFooHasNoBars_ReturnsEmptyResult()
```

This makes the scenario, condition and outcome visible in the test results without reading the implementation.

## Choosing the test level

Choose the lowest test level that gives confidence in the behaviour. Unit tests are fast and focused; integration and acceptance tests verify boundaries that mocks cannot prove.

### Unit tests

Services, models and entities that hold business rules are usually covered by unit tests only. This applies especially to `Core` code, where the goal is to verify decisions, transformations and validation without infrastructure.

When the SUT depends on another abstraction, mock it. Avoid configuring `Mock<T>` directly in individual tests unless there is a compelling local reason; prefer a reusable Mocker class (see [Shared test resources](#shared-test-resources)):

```csharp
public class EventHandlerBusTest : TestCaseBase
{
    private readonly Mock<IDomainEventHandler<DummyStateEntity>> _handler =
        DomainEventHandlerMocker.Mock<DummyStateEntity>(canHandle: false);

    [Fact]
    public async Task ProcessAsync_WhenNoHandlerMatches_ThrowsNoHandlersForEventException()
    {
        // Configure only the behaviour this scenario needs, then assert the business result.
    }
}
```

When a business-rule class is an *input* to the SUT, create a real instance with a Fixture rather than a mock. Fixtures provide valid defaults and let the test override only the state that matters:

```csharp
var state = new DummyStateEntityFixture().Build(new {
    Status = "active"
});
```

### Integration tests

Classes that wrap external resources need integration tests: adapters around APIs, queues, databases, storage and other infrastructure, usually in the `Infrastructure` layer or above.

An integration test is structured like a unit test, but it reaches the real resource instead of a mock. Run those resources locally through Docker and use the shared helpers from the test kits (`Anarchy.Toolkit.Testing.Postgres`, `Anarchy.Toolkit.Testing.Oracle`, `Anarchy.Toolkit.Testing.Aws`) to start, prepare, clean up and query them consistently.

Mocks can still be used for dependencies outside the boundary under test, but do not mock the API, queue, database or other resource the integration test is meant to validate.

### Acceptance tests

APIs and jobs built on the toolkit are validated end-to-end in `*.Acceptance.Tests` projects. These tests exercise the hosted application and can verify several integrations together. They should validate externally observable behaviour (an API response, a processed message, a completed job) rather than implementation details.

## Test kits 🧰

The packable test kits under `src/Testing` are the toolkit's shared test helpers. Reuse them before adding local setup code.

### `TestCaseBase` ⭐ (`Anarchy.Toolkit.Testing`)

Most unit and integration tests should inherit from `TestCaseBase`. It provides:

- `TestEnvironment()`: the test `IHostEnvironment`,
- `FixtureObserver`: an `InMemoryObserver` for SUTs that need an `Observer`.

Tests that read settings inherit from `TestCaseBase<TAppSettings>` instead, where `TAppSettings` is your own settings type implementing `IAppSettings` (the toolkit never ships a concrete one). It adds `TestSettings()`: a cached `TAppSettings` bound from the `appsettings.json` and `appsettings.Development.json` embedded in the assembly that declares `TAppSettings` (`<EmbeddedResource Include="appsettings*.json" />`). `TestHelper.TestSettings<TAppSettings>()` returns the same instance outside a `TestCaseBase`.

`FixtureObserver` is fully in memory, so its logs (`SinkLogs`) and metrics (`InMemoryMeasurer`) can be asserted when observability is the behaviour under test. Avoid asserting incidental logs or metrics in ordinary business-rule tests; assert the business result first. 🎯

```csharp
public class FooBarServiceTest : TestCaseBase
{
    [Fact]
    public void Execute_WhenFooIsValid_ReturnsBar()
    {
        var sut = new FooBarService(FixtureObserver);

        var result = sut.Execute(new FooBarEntityFixture().Build());

        result.Should().NotBeNull();
    }
}
```

### `TestHelper` 🎲 (`Anarchy.Toolkit.Testing`)

`TestHelper` provides random values and common setup utilities. Prefer it when a value must be unique or intentionally arbitrary, instead of hard-coding data unrelated to the scenario:

- `RandomId<T>()`, `RandomId<T>(min, max)` and `RandomId(prefix, suffix)` generate numeric or string identifiers.
- `RandomBytes()` and `RandomMemoryStream()` create random binary input.
- `RandomEnum<TEnum>()` and `RandomBool()` provide random values for controlled variations.
- `RandomObject()` creates an anonymous object with varied values.
- `LowPrecisionNow()` provides a timestamp suitable for serialization-sensitive assertions.
- `WaitAsync(...)` and `OverrideProperty(...)` support asynchronous checks and exceptional setup cases.

Keep scenario-defining values explicit. For example, use `const long fooId = 1001L` when the assertion depends on that id, and `TestHelper.RandomId<long>()` for a unique value whose exact content does not matter.

### `PostgresHelper` 🐘 (`Anarchy.Toolkit.Testing.Postgres`)

`PostgresHelper` creates PostgreSQL dependencies backed by the local database configured in the test settings of your settings type:

- `DbPolicyFixture<TAppSettings>()` creates a real `PostgresResilienceDbPolicy`,
- `DbUnitOfWorkFixture<TAppSettings>()` creates a real `PostgresDbUnitOfWork`.

Each also has an overload taking the settings instance directly.

Use them in database integration tests, so the concrete repository reaches PostgreSQL with the same connection configuration as the test environment. `SingletonPostgresHelper` shares a single policy across a test run, and `RegisterEnumTypeHandler<TEnum>()` wires up `[Description]`-based enum columns.

`OracleHelper` / `SingletonOracleHelper` (`Anarchy.Toolkit.Testing.Oracle`) offer the same helpers for Oracle. Both delegate the provider-agnostic parts (enum type handlers, `TruncateTableAsync`, `ClearTableAsync`, options mapping) to `DbTestHelper` and `DbOptionsMapper` in `Anarchy.Toolkit.Testing.Db`.

### AWS helpers ☁️ (`Anarchy.Toolkit.Testing.Aws`)

`SqsHelper<TAppSettings>`, `AwsMessageBrokerHelper` (constructed with an `IAwsAppSettings`) and `SqsMessageFixture` create, purge and read LocalStack SNS/SQS resources and build `SQSEvent` payloads for message-driven tests.

### `RestApiTestCaseBase` 🚀 (`Anarchy.Toolkit.Testing.Web`)

REST API acceptance tests should inherit from `RestApiTestCaseBase<TStartup, TAppSettings>`; the mock bearer token's issuer defaults to `AuthService.Host` from the test settings of `TAppSettings`. It supplies a hosted `HttpClient`, route helpers (`Get`, `Post`, `Put`, `Patch`), authorization-token setup, response parsing and status assertions.

Its key benefit is that it exercises both hosting models:

- `TestEndpointNativeAsync(...)` tests the native ASP.NET Core pipeline,
- `TestEndpointLambdaAsync(...)` tests the API Gateway-to-Lambda entry point,
- `TestEndpointAsync(...)` runs both paths and verifies equivalent deterministic responses.

Use the separate native and Lambda methods for responses with variable fields (timestamps, durations, generated ids). Use `TestEndpointAsync(...)` when both paths should return the same result.

## Shared test resources

Resources shared across *many* packages belong in the test kits under `src/Testing`. Resources used by the tests of a single package live at the root of that package's folder, next to the layer folders, grouped like this:

```text
tests/Unit/Core/Events/
├── Core/...            ← tests, mirroring src
├── Dummy/              ← test-local stand-ins for consumer types (enums, entities, payloads)
├── Fixture/            ← builders of real instances (…Fixture)
├── Mock/               ← mocking-framework setups (…Mocker / …Mock)
└── TestHelpers/        ← other utilities (in-memory doubles, port helpers, test data)
```

### Dummies

The toolkit is generic, so tests often need a concrete type to instantiate generic code with, such as an enum with `[Description]` attributes or an `IEntity` implementation. Put those in `Dummy/` and prefix them with `Dummy` (`DummyTypeEvent`, `DummyStateEntity`).

### Fixtures

Use a fixture when it creates a real, usable instance of a concrete type. Name it after that type with the `Fixture` suffix and derive from `EntityFixtureBase<T>` when the type has several properties.

### Mocks

Use a mock resource when it configures or returns a mocking-framework substitute. Name it after the mocked abstraction with `Mocker` (or `Mock`, following the established name). A resource that builds the real SUT is a fixture, not a mock.

## Building entities with `EntityFixtureBase`

Use `EntityFixtureBase<T>` for repeatable defaults with focused overrides. A fixture provides a valid default object through `BuildDefaultFixture()`, so a test only supplies the values that matter to its scenario:

```csharp
public class FooBarPayloadFixture : EntityFixtureBase<FooBarPayload>
{
    protected override FooBarPayload BuildDefaultFixture()
    {
        return new FooBarPayload {
            FooId = 1,
            BarId = 2,
            IsEnabled = true
        };
    }
}
```

Call `Build()` without arguments when the default instance is enough, or pass an anonymous object to override selected properties:

```csharp
var payload = new FooBarPayloadFixture().Build(new { BarId = 1001L });
```

By default, a `null` override keeps the fixture's default value. Pass `overrideWithNulls: true` only when the test explicitly needs a `null`:

```csharp
var withoutBar = new FooBarPayloadFixture().Build(new { BarId = (long?)null }, overrideWithNulls: true);
```

Use `BuildList(...)`, `ToJson(...)` and `ToJsonList(...)` when the scenario needs a collection or serialized data. ✨

## Quick checklist

Before adding a test or shared test resource, check:

1. Is the test in the right kind's project (`tests/<Kind>/<Kind>.csproj`), under the folder path built from the tested project's name?
2. Do the test's folder and namespace mirror the SUT's path?
3. Is a concrete SUT test named `<SutName>Test`?
4. Is the test level appropriate for the SUT and its boundary?
5. Does a real-object builder use the `Fixture` suffix, a mocking setup `Mocker`/`Mock`, and a stand-in type the `Dummy` prefix?
6. Is a resource needed by several packages placed in a `src/Testing` kit instead of being copied?

Small, predictable conventions make the test suite fast to navigate and structural drift obvious in review. 💙
