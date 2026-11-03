using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Samples.Ui.Infrastructure.Entity.Configuration;

/// <summary>
/// Defaults run the sample as-is; override them through the environment (e.g. <c>StubBaseUrl</c>).
/// </summary>
public sealed class AppSettings : IAppSettings
{
    /// <summary>Base URL of this host; the stub auth service and orders API live under <c>/stub</c>.</summary>
    [Required] public Uri StubBaseUrl { get; init; } = new("https://localhost:5101/");

    /// <summary>Client credentials the stub auth service accepts. Test values, not a real secret.</summary>
    [Required] public string StubClientId { get; init; } = "samples-ui";

    [Required] public string StubClientSecret { get; init; } = "samples-ui-secret";
}
