using Anarchy.Toolkit.Core.Observability.Entity.Service;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Core.Observability.Extension.Observability;

/// <summary>
/// Helpers that build common <see cref="LogContextProperty"/> sets (correlation id, caller site) and push them onto a
/// <see cref="LogContext"/>.
/// </summary>
/// <remarks>
/// Properties pushed through <see cref="LogContext"/> only appear in log events if the Serilog logger was configured
/// with <c>.Enrich.FromLogContext()</c>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// using (observer.LogContext.WithCorrelationId(message.CorrelationId))
/// using (observer.LogContext.WithCallerSite(this.CalledFrom()))
/// {
///     observer.Logger.Information("Handling message");
/// }
/// </code>
/// </example>
public static class LogContextExtensions
{
    /// <summary>
    /// Builds the properties for a <c>CorrelationId</c> log context entry.
    /// </summary>
    /// <param name="correlationId">
    /// The correlation id to use. When <see langword="null"/>, a new <see cref="System.Guid"/> is generated.
    /// </param>
    /// <returns>An array containing a single <c>CorrelationId</c> property.</returns>
    public static LogContextProperty[] ForCorrelationId(string? correlationId = null)
    {
        return [
            new LogContextProperty("CorrelationId", correlationId ?? Guid.NewGuid().ToString())
        ];
    }

    /// <summary>
    /// Builds the <c>ClassName</c> and <c>MethodName</c> log context properties from a call site.
    /// </summary>
    /// <param name="callerSite">The call site to describe.</param>
    /// <returns>An array with the <c>ClassName</c> and <c>MethodName</c> properties.</returns>
    public static LogContextProperty[] ForCallerSite(CallSiteEntity callerSite)
    {
        return [
            new LogContextProperty("ClassName", callerSite.ClassName),
            new LogContextProperty("MethodName", callerSite.MethodName)
        ];
    }

    /// <param name="logContext">The log context the properties are pushed onto.</param>
    extension(LogContext logContext)
    {
        /// <summary>
        /// Pushes a <c>CorrelationId</c> property onto the log context until the result is disposed.
        /// </summary>
        /// <param name="correlationId">
        /// The correlation id to use. When <see langword="null"/>, a new <see cref="System.Guid"/> is generated.
        /// </param>
        /// <returns>A handle that removes the property when disposed.</returns>
        public IDisposable WithCorrelationId(string? correlationId = null)
        {
            return logContext.PushProperties(ForCorrelationId(correlationId));
        }

        /// <summary>
        /// Pushes <c>ClassName</c> and <c>MethodName</c> properties onto the log context until the result is disposed.
        /// </summary>
        /// <param name="callerSite">The call site to describe.</param>
        /// <returns>A handle that removes the properties when disposed.</returns>
        public IDisposable WithCallerSite(CallSiteEntity callerSite)
        {
            return logContext.PushProperties(ForCallerSite(callerSite));
        }
    }
}
