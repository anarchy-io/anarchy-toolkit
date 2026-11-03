using Anarchy.Toolkit.Core.Extension;

namespace Anarchy.Toolkit.Core.Health.Entity.Api.Output;

public class ExceptionReport
{
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

    public string FinalException { get; } = "";
    public string OriginalException { get; } = "";
    public string[] Stack { get; } = [];

    public override string ToString()
    {
        return this.ToJson();
    }
}
