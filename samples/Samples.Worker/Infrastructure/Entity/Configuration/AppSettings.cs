using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Samples.Worker.Infrastructure.Entity.Configuration;

public sealed class AppSettings : IAppSettings
{
    /// <summary>Supplied by the environment (e.g. the <c>Greeting</c> variable or the Parameter Store).</summary>
    [Required] public string Greeting { get; init; } = "Hello";
}
