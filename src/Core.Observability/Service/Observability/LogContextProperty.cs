using Anarchy.Toolkit.Core.Exceptions;
using Serilog.Core.Enrichers;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

/// <summary>
/// An ambient property that can be attached to log messages. An abstraction over
/// Serilog's <see cref="PropertyEnricher"/>.
/// </summary>
public class LogContextProperty
{
    /// <summary>
    /// The name of the property.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The value of the property.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// <c>true</c> indicates that the value should be destructured when rendering the
    /// log message.
    /// </summary>
    public bool DestructureObjects { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="LogContextProperty"/> class.
    /// </summary>
    /// <param name="name">The property name. Must not be <see langword="null"/> or empty.</param>
    /// <param name="value">The property value.</param>
    /// <param name="destructureObjects">
    /// <see langword="true"/> to destructure <paramref name="value"/> into a structured object when logged.
    /// </param>
    /// <exception cref="Anarchy.Toolkit.Core.Exceptions.EmptyArgumentException">Thrown if <paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is <see langword="null"/>.</exception>
    public LogContextProperty(string name, object value, bool destructureObjects = false)
    {
        if (name == "")
            throw new EmptyArgumentException(nameof(name), "Property name cannot be empty.");

        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value;
        DestructureObjects = destructureObjects;
    }
}
