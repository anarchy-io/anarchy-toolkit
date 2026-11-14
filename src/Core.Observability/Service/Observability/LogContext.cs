using Serilog.Core;
using Serilog.Core.Enrichers;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

/// <summary>
/// Holds ambient properties that can be attached to log messages.
/// An abstraction over Serilog's <see cref="Serilog.Context.LogContext"/>.
/// </summary>
/// <remarks>
/// Remember to add `.Enrich.FromLogContext()` when creating logger for the context properties to work.
/// </remarks>
public class LogContext
{
    /// <summary>
    /// Pushes multiple properties onto the context. Disposing of the result will
    /// remove them again.
    /// </summary>
    /// <remarks>
    /// Remember to add `.Enrich.FromLogContext()` when creating logger for the context properties to work.
    /// </remarks>
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
    public IDisposable PushProperty(LogContextProperty property)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        return PushProperties(property);
    }
}
