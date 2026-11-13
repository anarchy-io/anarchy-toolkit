namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>An application to add to a solution; where it goes depends on the <see cref="SolutionLayout"/>.</summary>
public sealed record ApplicationSpec(ApplicationKind Kind, string Name)
{
    /// <summary>Whether the application listens on local HTTP ports.</summary>
    public bool IsWebHost => Kind is ApplicationKind.Api or ApplicationKind.Ui;

    /// <summary>The toolkit package the application is built on.</summary>
    public string PackageId => Kind switch {
        ApplicationKind.Api => "Anarchy.Toolkit.Web",
        ApplicationKind.Ui => "Anarchy.Toolkit.Web.Ui",
        ApplicationKind.Job => "Anarchy.Toolkit.Hosting",
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null)
    };
}
