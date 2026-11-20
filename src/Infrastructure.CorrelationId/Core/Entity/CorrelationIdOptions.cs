using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;

/// <summary>
/// Options that control how the correlation-id middleware reads, generates and echoes the correlation and attempt
/// identifiers of an HTTP request.
/// </summary>
/// <remarks>
/// <para>
/// An instance is registered as a singleton by
/// <see cref="Anarchy.Toolkit.Infrastructure.CorrelationId.DependencyInjection.Module.ModuleCorrelationId"/> or by the
/// <c>AddCorrelationId</c> service-collection extensions, and consumed by
/// <see cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware.CorrelationIdMiddleware"/>.
/// </para>
/// <para>
/// Defaults: <see cref="Behavior"/> is <see cref="CorrelationIdBehavior.GenerateValues"/>, and the header names are
/// <see cref="CorrelationHeaderNames.CorrelationId"/> (<c>X-Correlation-Id</c>) and
/// <see cref="CorrelationHeaderNames.AttemptId"/> (<c>X-Attempt-Id</c>).
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// var options = new CorrelationIdOptions
/// {
///     Behavior = CorrelationIdBehavior.ThrowIfMissing,
///     CorrelationIdHeaderName = "X-Request-Id"
/// };
/// services.AddCorrelationId(options);
/// </code>
/// </example>
public class CorrelationIdOptions
{
    /// <summary>
    /// Gets or sets what the middleware does when the correlation headers are missing from the incoming request.
    /// </summary>
    /// <value>Defaults to <see cref="CorrelationIdBehavior.GenerateValues"/>.</value>
    public CorrelationIdBehavior Behavior { get; set; }

    /// <summary>
    /// Gets or sets the name of the HTTP header carrying the correlation id, read from requests and written to
    /// responses.
    /// </summary>
    /// <value>Defaults to <see cref="CorrelationHeaderNames.CorrelationId"/>.</value>
    public string CorrelationIdHeaderName { get; set; }

    /// <summary>
    /// Gets or sets the name of the HTTP header carrying the attempt id, read from requests and written to responses.
    /// </summary>
    /// <value>Defaults to <see cref="CorrelationHeaderNames.AttemptId"/>.</value>
    public string AttemptIdHeaderName { get; set; }

    /// <summary>
    /// Gets or sets the structured log property and activity tag name for the correlation id.
    /// </summary>
    /// <value>Defaults to <c>CorrelationId</c>.</value>
    public string CorrelationIdPropertyName { get; set; }

    /// <summary>
    /// Gets or sets the structured log property and activity tag name for the attempt id.
    /// </summary>
    /// <value>Defaults to <c>AttemptId</c>.</value>
    public string AttemptIdPropertyName { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="CorrelationIdOptions"/> with the default behaviour and header names.
    /// </summary>
    public CorrelationIdOptions()
    {
        Behavior = CorrelationIdBehavior.GenerateValues;
        CorrelationIdHeaderName = CorrelationHeaderNames.CorrelationId;
        AttemptIdHeaderName = CorrelationHeaderNames.AttemptId;
        CorrelationIdPropertyName = CorrelationLogPropertyNames.CorrelationId;
        AttemptIdPropertyName = CorrelationLogPropertyNames.AttemptId;
    }
}

/// <summary>
/// Defines how the correlation-id middleware reacts when the correlation or attempt header is absent from a request.
/// </summary>
public enum CorrelationIdBehavior
{
    /// <summary>
    /// Generates a new GUID for each missing value (correlation id and/or attempt id). This is the default.
    /// </summary>
    GenerateValues = 0,

    /// <summary>
    /// Leaves missing values as <see langword="null"/>; nothing is generated and no response header is written for
    /// them.
    /// </summary>
    IgnoreIfMissing = 2,

    /// <summary>
    /// Throws a
    /// <see cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware.Exceptions.MissingCorrelationHeadersException"/>
    /// when either header is missing.
    /// </summary>
    ThrowIfMissing = 3
}
