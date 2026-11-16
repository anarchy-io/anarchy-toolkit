using Anarchy.Toolkit.Core.Extension;

namespace Anarchy.Toolkit.Core.Health.Entity.Api.Output;

/// <summary>
/// Serializable summary of an exception (and its inner exceptions) for inclusion in health-check responses.
/// </summary>
/// <remarks>Stack traces are intentionally not included; only messages and their qualified sources.</remarks>
public class ExceptionReport
{
    /// <summary>
    /// Creates a report from <paramref name="exception"/>.
    /// </summary>
    /// <param name="exception">The exception to summarise; when <see langword="null"/>, every property is empty.</param>
    public ExceptionReport(Exception? exception)
    {
        if (exception == null)
        {
            return;
        }

        FinalException = exception.Message;

        OriginalException = exception.GetBaseException().Message;

        var errorStack = new List<string>();
        var innerException = exception;

        while (innerException != null)
        {
            var source = innerException.GetQualifiedSource();
            errorStack.Add(source != null
                ? $"[{source}] {innerException.Message}"
                : innerException.Message);

            innerException = innerException.InnerException;
        }

        Stack = errorStack.ToArray();
    }

    /// <summary>
    /// Gets the message of the outermost exception.
    /// </summary>
    public string FinalException { get; } = "";

    /// <summary>
    /// Gets the message of the root cause (<see cref="Exception.GetBaseException"/>).
    /// </summary>
    public string OriginalException { get; } = "";

    /// <summary>
    /// Gets one entry per exception in the inner-exception chain, outermost first, formatted as
    /// <c>[ExceptionTypeFullName] message</c>.
    /// </summary>
    public string[] Stack { get; } = [];

    /// <summary>
    /// Serializes the report as JSON.
    /// </summary>
    /// <returns>The JSON representation of this report.</returns>
    public override string ToString()
    {
        return this.ToJson();
    }
}
