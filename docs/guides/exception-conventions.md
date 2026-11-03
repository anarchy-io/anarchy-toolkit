# Exception Conventions

This guide explains how exceptions are designed, named and placed in the toolkit and in applications built on it. The goal is simple: every failure is **identifiable by type**, carries **its own message**, and can still be caught by the **native .NET type** callers already know. 🧯

## The four rules

1. **Every thrown exception is specialized.** Never `throw new Exception(...)`, `ArgumentException`, `InvalidOperationException`, `JsonException` and friends directly. Throw a custom type instead.
2. **Custom exceptions extend a `Core` base whenever possible.** The primitives wrap the native exceptions, so `catch (ArgumentException)` keeps working when you throw `EmptyArgumentException`.
3. **Prefer the least context-specific exception.** If a generic exception describes the failure (an empty argument, an undefined enum value, a missing environment variable), throw that. Only if the failure is truly contextual, create a specific exception that extends a mid-level one and **encapsulates its own message**.
4. **Exception classes live in an `Exceptions` folder and namespace, never next to the code that throws or catches them.** The more specific the exception, the closer its `Exceptions` folder is to where it is used.

## 🧱 The primitives

All of these live in `Anarchy.Toolkit.Core.Exceptions` (`src/Core/Exceptions`).

| Primitive | Wraps | Use for |
|---|---|---|
| `InvalidArgumentException` | `ArgumentException` | Base for argument validation failures. |
| ↳ `EmptyArgumentException` | | A string or collection argument is null, empty or whitespace. |
| ↳ `InvalidEnumTypeException` | | A `Type` argument must be an enum but is not. |
| `OutOfRangeArgumentException` | `ArgumentOutOfRangeException` | Base for values outside the accepted set. |
| ↳ `UndefinedEnumValueException` | | An enum value is not defined (`Enum.IsDefined` fails, `switch` default arm). |
| `InvalidStateException` | `InvalidOperationException` | Base for operations that cannot run in the current object or environment state. |
| ↳ `MissingEnvironmentVariableException` | | A required environment variable is not set. |
| `InvalidFormatException` | `FormatException` | A value cannot be parsed into or formatted as a target type. Has a generic `(value, targetType)` constructor. |
| `JsonParsingException` | `System.Text.Json.JsonException` | Base for JSON parsing failures, safe to throw from `JsonConverter`s. |
| ↳ `UnexpectedJsonTokenException` | | The reader sits on a token the converter cannot handle. |
| ↳ `UnexpectedJsonEndException` | | The input ends before the value was fully read. |
| ↳ `EmptyJsonValueException` | | A null or empty value where one is required. |
| ↳ `InvalidJsonValueException` | | A well-formed value that is not valid for the target type. |
| `NotFoundException` | `Exception` | Base for "the thing you asked for does not exist". |
| ↳ `PropertyNotFoundException` | | A property cannot be found through reflection. |
| `ExternalDependencyException` | `Exception` | Base for failures of a database, broker, secret store, identity provider or external process. Keep the original failure as the inner exception. |
| `OperationTimeoutException` | `TimeoutException` | An operation exceeded its allotted time. |
| `UnsupportedOperationException` | `NotSupportedException` | Base for intentionally unsupported operations. |
| ↳ `UnsupportedTypeException` | | A code path received a type it cannot handle. |

If none of these fits, first ask whether a new **generic** primitive is warranted (it would be reused across projects). Only then add one to `Core`, wrapping the matching native exception.

## 🧭 Choosing what to throw

```text
Is there a generic primitive that describes the failure?
├── yes → throw it, with its structured constructor
│         throw new EmptyArgumentException(nameof(messages));
│         throw new UndefinedEnumValueException(nameof(instanceType), instanceType);
│         throw new MissingEnvironmentVariableException("COST_CENTER");
└── no  → is there a mid-level exception for this area (storage, broker, events...)?
          ├── yes → create a specific exception extending it, with its own message
          └── no  → create the mid-level first (extending a primitive), then the specific one
```

A specific exception **encapsulates its message**: the throw site passes data, never prose.

```csharp
// ✅ the type owns the message
public class DbConnectionReleaseException(Exception innerException)
    : DbPolicyExecutionException("Error releasing connection", innerException);

throw new DbConnectionReleaseException(exception);

// 🚫 the throw site writes the message
throw new DbPolicyExecutionException("Error releasing connection", exception);
```

Expose the data that explains the failure as properties (`SecretName`, `FailedMessageCount`, `EnumType`...), so handlers and logs do not have to parse messages.

### Mid-level exceptions

A mid-level exception groups the failures of one area so callers can catch them together. It takes a message (and usually an inner exception) and is extended by the specific exceptions; you normally do not throw it directly. Examples:

| Mid-level | Extends | Specific exceptions |
|---|---|---|
| `MessageBrokerPublisherException` | `ExternalDependencyException` | `MessagePublishException`, `MessageBatchPublishException` |
| `MessageBrokerParsingException` | `InvalidFormatException` | `EventEnvelopeParsingException` |
| `DsPolicyExecutionException` → `DbPolicyExecutionException` | `ExternalDependencyException` | `DbConnectionAcquisitionException`, `DbConnectionReleaseException` |
| `UnitOfWorkTransactionException` | `TransactionException` | `TransactionBeginException`, `TransactionCommitException`, `TransactionRollbackException`, `SessionAlreadyActiveException`, `SessionFinishException` |
| `UnitOfWorkSessionException` | `TransactionException` | `SessionCallbackException` |
| `UnableToHandleEventException` | `Exception` | `EventHandlerExecutionException`, `StateAlreadyInitializedEventHandlerException` |
| `StorageException` → `PersistanceStorageException` | `Exception` | `EmptyEntityListForBatchSaveStorageException`, `DuplicatedEntityStorageException` |
| `InvalidStartupTypeExceptionBase` (abstract) | `InvalidStateException` | `MissingStartupInterfaceException`, `MissingStartupConstructorException` |

## 📁 Where exception classes live

Exceptions always live in an `Exceptions` folder whose namespace matches the folder. Place that folder at the **lowest common ancestor** of the code that throws and catches the exception:

| Reach | Location | Example |
|---|---|---|
| Any project | `src/Core/Exceptions` | `EmptyArgumentException` |
| An abstraction shared by several implementations | the abstraction project, next to the contract | `Core.Persistence.Abstractions/Service/Storage/Exceptions/TransactionCommitException.cs` (thrown by any `IUnitOfWork`) |
| One area of one project | the area's folder | `Core.Events/Service/Event/Domain/Exceptions` |
| One class or feature | a sibling `Exceptions` folder | `Web/Helper/Presentation/Contract/Exceptions/MissingRouteParameterException.cs` |

Never declare an exception in the same file or folder as the class that throws it, and never name the folder `Exception` (singular): it shadows `System.Exception` inside the parent namespace.

## ✍️ Writing an exception class

- Name it after the failure, ending in `Exception`: `SecretNotFoundException`, not `SecretManagerException2`.
- Use a primary constructor; pass the encapsulated message to the base.
- Add a `/// <summary>Thrown when ...</summary>` doc comment.
- Accept `Exception innerException` when you wrap a caught failure, and **always** pass it on.
- Keep `sealed` / `abstract` for when it matters: `abstract` for mid-levels that must never be thrown directly.

```csharp
namespace Anarchy.Toolkit.Configuration.Service.AppSecret.Exceptions;

/// <summary>
/// Thrown when the secret store fails to return a secret.
/// </summary>
public class SecretRetrievalException(string secretName, Exception innerException)
    : ExternalDependencyException(
        $"Error getting secret with name: {secretName} returned: {innerException.Message}", innerException)
{
    public string SecretName { get; } = secretName;
}
```

## 🪤 Catching and wrapping

- Catch the narrowest type you can handle. When wrapping everything in a `try`, do not swallow your own specific exceptions: `catch (Exception e) when (e is not SecretNotFoundException)`.
- When wrapping, keep the original as the inner exception.
- Re-throw with `throw;`, never `throw ex;`.

## ⚖️ Base types change HTTP status codes

`ExceptionMiddleware` (`Web`) chooses the response status from the **native** base type: `InvalidOperationException` and Newtonsoft's `JsonException` become `400`, everything else `500`. `UnitOfWorkSessionException` is unwrapped to its base exception first.

So picking a base is also picking a status code. `InvalidStateException` derives from `InvalidOperationException` and therefore returns `400`. When you re-base an existing exception, check that it keeps the status code its callers expect.

## 🚦 Allowed exceptions to rule 1

These stay native on purpose:

- **Null guards:** `ArgumentNullException.ThrowIfNull(x)` and `x ?? throw new ArgumentNullException(nameof(x))`. They are the idiomatic BCL guard and analyzers (CA1510) prefer them.
- **Cancellation:** `cancellationToken.ThrowIfCancellationRequested()`.
- **Re-throws:** `throw;`, and re-throwing an exception that was already thrown elsewhere.
- **Caller-supplied factories**, such as `throw errorFunc(statusCode)`.
- **Third-party contracts** that require a specific type with no primitive equivalent, like Newtonsoft's `JsonSerializationException` inside a `ContractResolver`. Still create a specific subclass (`ConstructorParameterCasingException`) in an `Exceptions` folder.

## ✅ Checklist

- [ ] No `throw new <NativeException>(...)` outside the allowed cases.
- [ ] The exception extends a primitive or a mid-level that does.
- [ ] A generic primitive was used if one describes the failure.
- [ ] The message is defined by the exception class, not the throw site.
- [ ] Inner exceptions are preserved.
- [ ] The class lives in the nearest `Exceptions` folder shared by its throwers and catchers.
- [ ] The base type keeps the expected HTTP status in `ExceptionMiddleware`.
- [ ] Tests assert the specific type (AwesomeAssertions' `Throw<T>` also matches derived types).
