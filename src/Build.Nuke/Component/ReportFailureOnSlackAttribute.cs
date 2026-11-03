namespace Anarchy.Toolkit.Build.Nuke.Component
{
    [Obsolete("No longer required and will be removed in future versions. Just use" + nameof(IReportFailureOnSlack) + "." + nameof(IReportFailureOnSlack.ReportFailure))]
    public sealed class ReportFailureOnSlackAttribute : Attribute
    {
    }
}
