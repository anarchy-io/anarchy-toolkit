using Serilog.Core;
using Serilog.Core.Enrichers;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

/// <summary>
/// Holds ambient properties that can be attached to log messages.
/// An abstraction over Serilog's <see cref="Serilog.Context.LogContext"/>.
/// </summary>
/// <remarks>
/// Remember to add `.Enrich.FromLogContext()` when creating logger for the context properties to work.
/// Properties are stored in Serilog's async-local context, so they flow across <c>await</c> and are scoped to the
/// current async flow. Exposed through <see cref="Observer.LogContext"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// using (observer.LogContext.PushProperty(new LogContextProperty("OrderId", order.Id)))
/// {
///     observer.Logger.Information("Order received"); // event carries OrderId
/// }
/// </code>
/// </example>
public class LogContext
{
    /// <summary>
    /// Pushes multiple properties onto the context. Disposing of the result will
    /// remove them again.
    /// </summary>
    /// <remarks>
    /// Remember to add `.Enrich.FromLogContext()` when creating logger for the context properties to work.
    /// </remarks>
    /// <param name="properties">The properties to push.</param>
    /// <returns>A handle that removes the pushed properties when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any element of <paramref name="properties"/> is <see langword="null"/>.</exception>
    public virtual IDisposable PushProperties(params LogContextProperty[] properties)
    {
        var serilogNativeProperties = ConvertPropertiesToEnrichers(properties);

        return Serilog.Context.LogContext.Push(serilogNativeProperties);
    }

    private ILogEventEnricher[] ConvertPropertiesToEnrichers(IEnumerable<LogContextProperty> properties) =>
        properties.Select(ConvertPropertyToEnricher)
            .Cast<ILogEventEnricher>()
            .ToArray();

    private PropertyEnricher ConvertPropertyToEnricher(LogContextProperty property)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        return new PropertyEnricher(name: property.Name,
            value: property.Value,
            destructureObjects: property.DestructureObjects);
    }

    /// <summary>
    /// Pushes a single property onto the context. Disposing of the result will
    /// remove it again.
    /// </summary>
    /// <remarks>
    /// Remember to add `.Enrich.FromLogContext()` when creating logger for the context properties to work.
    /// </remarks>
    /// <param name="property">The property to push.</param>
    /// <returns>A handle that removes the pushed property when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="property"/> is <see langword="null"/>.</exception>
    public IDisposable PushProperty(LogContextProperty property)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        return PushProperties(property);
    }
}
