# Exposing Endpoints

This guide explains how to expose endpoints from an API built on `Anarchy.Toolkit.Web`, with clear boundaries and low coupling. The goal is simple: keep HTTP contracts stable while letting domain models evolve safely. 🧭

The toolkit itself follows these rules (see `HealthController` and the `Health*Output` DTOs in `Anarchy.Toolkit.Web`), and consumer applications should too.

## Ideal class distribution

Use the same layers as the toolkit (see [Layered Architecture](architecture.md)):

```text
src/
└── Library/
    ├── Core/
    │   └── Entity/
    │       ├── DomainModel/
    │       │   ├── FooBarEntity.cs
    │       │   └── FooBarItemEntity.cs
    │       └── Api/
    │           ├── Input/
    │           │   └── CreateFooBarUseCaseInput.cs
    │           └── Output/
    │               └── FooBarAuditDto.cs
    └── Web/
        └── Presentation/
            ├── Contract/
            │   └── FooBar/
            │       └── IFooBarController.cs
            └── Entity/
                ├── Input/
                │   └── CreateFooBarInput.cs
                └── Output/
                    ├── FooBarOutput.cs
                    └── FooBarAuditOutput.cs
```

### What each layer owns

- `Core/Entity/DomainModel`: internal business state and behavior. Keep it free to evolve without forcing HTTP contract changes.
- `Core/Entity/Api/Input`: service/use-case inputs when arguments are complex and do not map to a single domain entity.
- `Core/Entity/Api/Output`: DTOs produced by services when combining several domain sources (in the toolkit, `HealthCheckReport` and `ExceptionReport` in `Anarchy.Toolkit.Core.Health` (`Entity/Api/Output`) play this role).
- `Web/Presentation/Contract`: endpoint routes and signatures. One stable contract surface helps tests and client integrations. Controller contracts may extend the toolkit's `IToolkitController` marker.
- `Web/Presentation/Entity/Input` and `Web/Presentation/Entity/Output`: HTTP request/response shapes owned by the presentation layer. Output DTOs also own their own Core-to-DTO mapping (see below); there is no separate `Web/Service/Builder` layer.

## Endpoint rules ✅

- Every endpoint must declare explicit response types (`ProducesResponseType`) using Web entities.
- **Use `ActionResult<T>` for endpoints that return payloads.** This enforces type safety at the interface level and improves API documentation. For example, `Task<ActionResult<FooBarOutput>>` makes the contract explicit.
- Only use bare `IActionResult` for endpoints without a response body (e.g. `204 No Content`, `202 Accepted`).
- **Never expose Core domain entities (or Core `Api` DTOs) in response payloads.** Every response payload must be a dedicated Web `*Output` type under `Web/Presentation/Entity/Output`, even when its shape mirrors a Core entity/DTO one-to-one.
- **Never accept Core domain entities (or Core `Api` DTOs) as controller parameters.** Every non-primitive request payload (`[FromBody]`, or an implicit-body complex parameter) must be a dedicated Web `*Input` type under `Web/Presentation/Entity/Input`. Route/query parameters may stay primitive (`string`, `long`, `int`, `double`, `bool`, enums, etc.).
- Controllers must never call repositories directly. Call abstraction services such as Use Cases and Entity-based Aggregates.
- Keep controllers thin: validate input, call the service, map output via the DTO's own static factory method, return a status.
- Only expose fields intentionally meant for API consumers.
- Favor backward-compatible response contracts. Adapt the mapping when internals change, so clients don't break from internal refactors. ✨

## Mapping Core entities/DTOs to Output DTOs

Do not create separate `*Builder` classes for this mapping. Add a `public static` factory method directly on the Output DTO, named `FromEntity(...)` (when the source is a Core domain entity) or `FromDto(...)` (when the source is a Core `Api/Output` DTO).

This is exactly how the toolkit's health endpoint works:

```csharp
namespace Anarchy.Toolkit.Web.Presentation.Entity.Output.Health;

public record HealthCheckReportOutput(
    bool Passed,
    TimeSpan Duration,
    IReadOnlyCollection<HealthCheckEntityOutput> Tests
)
{
    public static HealthCheckReportOutput FromDto(HealthCheckReport dto)
    {
        return new HealthCheckReportOutput(
            Passed: dto.Passed,
            Duration: dto.Duration,
            Tests: dto.Tests.Select(HealthCheckEntityOutput.FromDto).ToArray()
        );
    }
}
```

Rules for this mapping:

- The factory method lives on the DTO it produces, not on a separate class. If you find yourself creating a `Web/Service/Builder/Output` folder, fold that logic into the DTO instead.
- When a DTO nests other DTOs (e.g. a collection of items), give each nested DTO its own static factory method and have the parent call it (`Tests = dto.Tests.Select(HealthCheckEntityOutput.FromDto).ToArray()`), rather than inlining the nested mapping.
- Prefer `FromEntity` when mapping directly from a `Core/Entity/DomainModel(/Persisted)` entity, and `FromDto` when mapping from a `Core/Entity/Api/Output` DTO.
- **Every `FromEntity`/`FromDto` method must have a unit test** that mirrors the DTO's folder (e.g. `tests/Unit/Web/Presentation/Entity/Output/Health/HealthCheckReportOutputTest.cs`). At minimum, cover field-by-field mapping, nested-collection mapping (count + ordering), and null/optional-field handling.

## Mapping Web Input records to Core Input DTOs/entities

Symmetrically, do not map an Input record's fields inline in the controller body, and do not create a separate mapper class either. Add a `public` instance method on the Web Input record, named `ToDto(...)` (target: a Core `Api/Input` DTO) or `ToEntity(...)` (target: a Core domain entity). Pass any values the request doesn't carry itself (e.g. a route id) as method arguments:

```csharp
public class CreateFooBarInput
{
    [Required] public required string Name { get; init; }
    public required IReadOnlyCollection<string> ItemIds { get; init; }

    public CreateFooBarUseCaseInput ToUseCaseParams()
    {
        return new CreateFooBarUseCaseInput {
            Name = Name,
            ItemIds = ItemIds
        };
    }
}
```

Rules for this mapping:

- The conversion method lives on the Web Input type itself.
- Name the method for what it produces: `ToDto()`/`ToUseCaseParams()` for a Core `Api/Input` DTO, `ToEntity()` for a Core domain entity.
- Controllers call `input.ToEntity(...)`/`input.ToDto(...)` directly in the service call instead of constructing the Core type inline.
- **Every `ToDto`/`ToEntity`/`ToUseCaseParams` method must have a unit test** mirroring the Input type's folder, covering field-by-field mapping and optional/default-value fields.

The toolkit's validation attributes (`RequiredIntAttribute`, `RequiredLongAttribute`, `RequiredDecimalAttribute`, `DefinedEnumAttribute` in `Web/Presentation/Attribute`) are meant for these Input records.

## Contract-first interfaces in Web

Declare controller interfaces in `Web/Presentation/Contract` and implement them in API hosts.

This gives practical benefits:

- one source of truth for routes and endpoint signatures,
- simpler end-to-end tests against a stable contract surface (`RestApiTestCaseBase` and `HttpRouteHelper.FromTemplate` build requests from those route constants),
- easier future generation and maintenance of an official client package,
- lower risk of route drift across hosts.

## Using `ActionResult<T>` for type-safe endpoints

The native ASP.NET Core `ActionResult<T>` type gives you:

- **Type safety**: the payload type is explicit in the interface and enforced at compile time.
- **Better documentation**: Swagger (configured by `RestApiWebAppStartupBase`) picks up the response type automatically.
- **No casting**: standard helpers like `Ok()`, `BadRequest()`, `NotFound()` work without wrapper classes.
- **Backward compatibility**: existing implementations using `Ok(payload)` keep working.

### When to use `ActionResult<T>` vs `IActionResult`

| Scenario | Use |
|----------|-----|
| Endpoint returns a typed payload (200, 201, etc.) | `ActionResult<TPayload>` |
| Endpoint returns no body (202, 204, etc.) | `IActionResult` |
| Endpoint returns multiple unrelated types | `IActionResult` |

```csharp
public interface IFooBarController : IToolkitController
{
    // Returns a payload: use ActionResult<T>
    Task<ActionResult<FooBarOutput>> GetByIdAsync(string id);

    // No response body: use IActionResult
    Task<IActionResult> DeleteAsync(string id);
}
```

## Recommended endpoint flow 🚀

1. Receive and validate the transport input (`Web/Presentation/Entity/Input`).
2. Convert it to a use-case/service input (`Core/Entity/Api/Input`) or a Core entity via the Input record's own `ToDto()`/`ToEntity()` method.
3. Execute the service/application logic through abstractions (Use Cases / Aggregates), never repositories from controllers.
4. Map the domain/service output to `Web/Presentation/Entity/Output` via the Output DTO's own `FromEntity()`/`FromDto()` method.
5. Return explicit status codes and typed responses.

Unhandled exceptions are translated centrally by the toolkit's `ExceptionMiddleware`: `InvalidOperationException` and `JsonException` become `400 Bad Request`, everything else becomes `500` with an exception report body. Expected outcomes such as "not found" are part of the contract, so return them explicitly from the controller (`NotFound()`) instead of relying on the middleware. Endpoints that must handle exceptions themselves can opt out with `[IgnoreExceptionHandling]`.

## Controller example

```csharp
[Route(IFooBarController.RouteBase)]
[Produces("application/json")]
public class FooBarController(IFooBarUseCase useCase) : ControllerBase, IFooBarController
{
    [HttpPost]
    [Route(IFooBarController.RouteCreate)]
    [ProducesResponseType(typeof(FooBarOutput), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FooBarOutput>> CreateAsync([FromBody] CreateFooBarInput input)
    {
        var result = await useCase.CreateAsync(input.ToUseCaseParams());
        var output = FooBarOutput.FromDto(result);

        return CreatedAtAction(nameof(GetByIdAsync), new { fooBarId = output.Id }, output);
    }

    [HttpGet]
    [Route(IFooBarController.RouteGetById)]
    [ProducesResponseType(typeof(FooBarOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FooBarOutput>> GetByIdAsync(string fooBarId)
    {
        var result = await useCase.GetByIdAsync(fooBarId);

        return result == null
            ? NotFound()
            : Ok(FooBarOutput.FromDto(result));
    }
}
```

Small boundary, big payoff: cleaner architecture, easier refactoring, and safer public APIs. 💙
