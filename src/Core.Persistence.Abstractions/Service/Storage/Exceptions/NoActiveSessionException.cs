namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Thrown when an operation that needs the unit of work's session runs outside of it (e.g. beginning a transaction
/// outside of a session, or from another async flow).
/// </summary>
public class NoActiveSessionException()
    : UnitOfWorkTransactionException("No unit-of-work session is active in the current async flow");
