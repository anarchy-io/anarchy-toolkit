
using Fallout.Common;
using Fallout.Common.Tooling;
using Fallout.Common.Tools;
using Fallout.Common.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Pipenv;

/// <summary><p>For more details, visit the <a href="https://pipenv.pypa.io/en/latest/">official website</a>.</p></summary>
[ExcludeFromCodeCoverage]
[PathTool(Executable = PathExecutable)]
public partial class PipenvTasks : ToolTasks, IRequirePathTool
{
    public static string PipenvPath { get => new PipenvTasks().GetToolPathInternal(); set => new PipenvTasks().SetToolPath(value); }
    public const string PathExecutable = "pipenv";
    /// <summary><p>For more details, visit the <a href="https://pipenv.pypa.io/en/latest/">official website</a>.</p></summary>
    public static IReadOnlyCollection<Output> Pipenv(ArgumentStringHandler arguments, string workingDirectory = null, IReadOnlyDictionary<string, string> environmentVariables = null, int? timeout = null, bool? logOutput = null, bool? logInvocation = null, Action<OutputType, string> logger = null, Func<IProcess, object> exitHandler = null) => new PipenvTasks().Run(arguments, workingDirectory, environmentVariables, timeout, logOutput, logInvocation, logger, exitHandler);
    /// <summary><p>For more details, visit the <a href="https://pipenv.pypa.io/en/latest/">official website</a>.</p></summary>
    /// <remarks><p>This is a <a href="https://github.com/ChrisonSimtian/Fallout">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p></remarks>
    public static IReadOnlyCollection<Output> PipenvSync(PipenvSyncSettings options = null) => new PipenvTasks().Run<PipenvSyncSettings>(options);
    /// <inheritdoc cref="PipenvTasks.PipenvSync(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Pipenv.PipenvSyncSettings)"/>
    public static IReadOnlyCollection<Output> PipenvSync(Configure<PipenvSyncSettings> configurator) => new PipenvTasks().Run<PipenvSyncSettings>(configurator.Invoke(new PipenvSyncSettings()));
    /// <inheritdoc cref="PipenvTasks.PipenvSync(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Pipenv.PipenvSyncSettings)"/>
    public static IEnumerable<(PipenvSyncSettings Settings, IReadOnlyCollection<Output> Output)> PipenvSync(CombinatorialConfigure<PipenvSyncSettings> configurator, int degreeOfParallelism = 1, bool completeOnFailure = false) => configurator.Invoke(PipenvSync, degreeOfParallelism, completeOnFailure);
    /// <summary><p>For more details, visit the <a href="https://pipenv.pypa.io/en/latest/">official website</a>.</p></summary>
    /// <remarks><p>This is a <a href="https://github.com/ChrisonSimtian/Fallout">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p><ul><li><c>--manifest-path</c> via <see cref="PipenvDeployArtifactsSettings.ManifestPath"/></li></ul></remarks>
    public static IReadOnlyCollection<Output> PipenvDeployArtifacts(PipenvDeployArtifactsSettings options = null) => new PipenvTasks().Run<PipenvDeployArtifactsSettings>(options);
    /// <inheritdoc cref="PipenvTasks.PipenvDeployArtifacts(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Pipenv.PipenvDeployArtifactsSettings)"/>
    public static IReadOnlyCollection<Output> PipenvDeployArtifacts(Configure<PipenvDeployArtifactsSettings> configurator) => new PipenvTasks().Run<PipenvDeployArtifactsSettings>(configurator.Invoke(new PipenvDeployArtifactsSettings()));
    /// <inheritdoc cref="PipenvTasks.PipenvDeployArtifacts(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Pipenv.PipenvDeployArtifactsSettings)"/>
    public static IEnumerable<(PipenvDeployArtifactsSettings Settings, IReadOnlyCollection<Output> Output)> PipenvDeployArtifacts(CombinatorialConfigure<PipenvDeployArtifactsSettings> configurator, int degreeOfParallelism = 1, bool completeOnFailure = false) => configurator.Invoke(PipenvDeployArtifacts, degreeOfParallelism, completeOnFailure);
}
#region PipenvSyncSettings
/// <inheritdoc cref="PipenvTasks.PipenvSync(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Pipenv.PipenvSyncSettings)"/>
[ExcludeFromCodeCoverage]
[Command(Type = typeof(PipenvTasks), Command = nameof(PipenvTasks.PipenvSync), Arguments = "sync")]
public partial class PipenvSyncSettings : ToolOptions
{
}
#endregion
#region PipenvDeployArtifactsSettings
/// <inheritdoc cref="PipenvTasks.PipenvDeployArtifacts(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Pipenv.PipenvDeployArtifactsSettings)"/>
[ExcludeFromCodeCoverage]
[Command(Type = typeof(PipenvTasks), Command = nameof(PipenvTasks.PipenvDeployArtifacts), Arguments = "run deploy-artifacts")]
public partial class PipenvDeployArtifactsSettings : ToolOptions
{
    /// <summary></summary>
    [Argument(Format = "--manifest-path {value}")] public string ManifestPath => Get<string>(() => ManifestPath);
}
#endregion
#region PipenvSyncSettingsExtensions
/// <inheritdoc cref="PipenvTasks.PipenvSync(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Pipenv.PipenvSyncSettings)"/>
[ExcludeFromCodeCoverage]
public static partial class PipenvSyncSettingsExtensions
{
}
#endregion
#region PipenvDeployArtifactsSettingsExtensions
/// <inheritdoc cref="PipenvTasks.PipenvDeployArtifacts(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Pipenv.PipenvDeployArtifactsSettings)"/>
[ExcludeFromCodeCoverage]
public static partial class PipenvDeployArtifactsSettingsExtensions
{
    #region ManifestPath
    /// <inheritdoc cref="PipenvDeployArtifactsSettings.ManifestPath"/>
    [Builder(Type = typeof(PipenvDeployArtifactsSettings), Property = nameof(PipenvDeployArtifactsSettings.ManifestPath))]
    public static T SetManifestPath<T>(this T o, string v) where T : PipenvDeployArtifactsSettings => o.Modify(b => b.Set(() => o.ManifestPath, v));
    /// <inheritdoc cref="PipenvDeployArtifactsSettings.ManifestPath"/>
    [Builder(Type = typeof(PipenvDeployArtifactsSettings), Property = nameof(PipenvDeployArtifactsSettings.ManifestPath))]
    public static T ResetManifestPath<T>(this T o) where T : PipenvDeployArtifactsSettings => o.Modify(b => b.Remove(() => o.ManifestPath));
    #endregion
}
#endregion
