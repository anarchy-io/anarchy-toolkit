namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for "item not found" failures (lookups, reflection, client resolution, storage reads...).
/// </summary>
/// <remarks>
/// Depending on the constructor used, the exception carries the missing <see cref="Object"/> or the
/// <see cref="Filters"/> used in the failed lookup.
/// </remarks>
public class NotFoundException : Exception
{
    /// <summary>
    /// The filters used in the lookup that returned no result, when provided through
    /// <see cref="NotFoundException(string, object, Exception?)"/>; otherwise <see langword="null"/>.
    /// </summary>
    public readonly object? Filters;

    /// <summary>
    /// The item that was not found, when provided through <see cref="NotFoundException(object)"/>; otherwise
    /// <see langword="null"/>.
    /// </summary>
    public readonly object? Object;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with a generic message.
    /// </summary>
    public NotFoundException() : base("The item was not found")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class for a specific item.
    /// </summary>
    /// <param name="obj">The item (or its identifier) that was not found; stored in <see cref="Object"/>.</param>
    public NotFoundException(object obj) : base($"The item {obj} not found")
    {
        Object = obj;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class for a filtered lookup.
    /// </summary>
    /// <param name="type">A name for the type of item searched for.</param>
    /// <param name="filters">The filters used in the lookup; stored in <see cref="Filters"/>.</param>
    /// <param name="innerException">The exception that caused this one, if any.</param>
    public NotFoundException(string type, object filters, Exception? innerException = null)
        : base($"No item of type {type} found based on filters", innerException)
    {
        Filters = filters;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="message">A message describing what was not found.</param>
    public NotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with a custom message and inner
    /// exception, for use by derived exceptions.
    /// </summary>
    /// <param name="message">A message describing what was not found.</param>
    /// <param name="innerException">The exception that caused this one, if any.</param>
    protected NotFoundException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}
