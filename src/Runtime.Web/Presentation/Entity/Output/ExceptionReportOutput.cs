using Anarchy.Toolkit.Core.Health.Reports;

namespace Anarchy.Toolkit.Runtime.Web.Presentation.Entity.Output;

/// <summary>API output describing an exception.</summary>
/// <param name="FinalException">Description of the outermost exception.</param>
/// <param name="OriginalException">Description of the root (base) exception.</param>
/// <param name="Stack">The stack trace lines.</param>
public record ExceptionReportOutput(
    string FinalException,
    string OriginalException,
    string[] Stack
)
{
    /// <summary>Maps an <see cref="ExceptionReport"/> to its API output.</summary>
    /// <param name="dto">The exception report.</param>
    /// <returns>The API output.</returns>
    public static ExceptionReportOutput FromDto(ExceptionReport dto)
    {
        return new ExceptionReportOutput(
            FinalException: dto.FinalException,
            OriginalException: dto.OriginalException,
            Stack: dto.Stack
        );
    }
}
