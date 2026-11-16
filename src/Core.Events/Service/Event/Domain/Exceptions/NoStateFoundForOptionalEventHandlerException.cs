using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;

/// <summary>
/// Thrown by optional state-mutation handlers when no aggregate state exists yet. The
/// <see cref="EventHandlerBus{T}"/> treats it as "handled, nothing to mutate".
/// </summary>
/// <remarks>
/// Raised by <see cref="Handler.OptionalStateMutationDomainEventHandlerBase{TDomainModelEntity}"/>.
/// </remarks>
public class NoStateFoundForOptionalEventHandlerException()
    : NotFoundException("No aggregate state found for the optional event handler.");
