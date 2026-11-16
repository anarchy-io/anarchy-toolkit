using Serilog.Core;
using Serilog.Events;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Destructuring;

/// <summary>
/// A Serilog destructuring policy that wraps every value as a <see cref="ScalarValue"/>, keeping the original object
/// instance so tests can compare log properties with the objects that were logged.
/// </summary>
public class ObjectTestDestructuringPolicy : IDestructuringPolicy
{
    /// <summary>Wraps <paramref name="value"/> in a <see cref="ScalarValue"/>.</summary>
    /// <param name="value">The value to destructure.</param>
    /// <param name="propertyValueFactory">Unused.</param>
    /// <param name="result">The resulting scalar value.</param>
    /// <returns>Always <see langword="true"/>.</returns>
    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        result = new ScalarValue(value);
        return true;
    }
}
