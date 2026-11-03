using Anarchy.Toolkit.Core.Health.Entity.Api.Output;

namespace Anarchy.Toolkit.Web.Presentation.Entity.Output;

public record ExceptionReportOutput(
    string FinalException,
    string OriginalException,
    string[] Stack
)
{
    public static ExceptionReportOutput FromDto(ExceptionReport dto)
    {
        return new ExceptionReportOutput(
            FinalException: dto.FinalException,
            OriginalException: dto.OriginalException,
            Stack: dto.Stack
        );
    }
}
