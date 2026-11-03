
using JetBrains.Annotations;
using Newtonsoft.Json;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools;
using Nuke.Common.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.DotNetUserSecrets;

/// <summary><p>The Secret Manager tool stores sensitive data during the development of a .NET project</p><p>For more details, visit the <a href="https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets">official website</a>.</p></summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
[PathTool(Executable = PathExecutable)]
public partial class DotNetUserSecretsTasks : ToolTasks, IRequirePathTool
{
    public static string DotNetUserSecretsPath { get => new DotNetUserSecretsTasks().GetToolPathInternal(); set => new DotNetUserSecretsTasks().SetToolPath(value); }
    public const string PathExecutable = "dotnet";
    /// <summary><p>The Secret Manager tool stores sensitive data during the development of a .NET project</p><p>For more details, visit the <a href="https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets">official website</a>.</p></summary>
    public static IReadOnlyCollection<Output> DotNetUserSecrets(ArgumentStringHandler arguments, string workingDirectory = null, IReadOnlyDictionary<string, string> environmentVariables = null, int? timeout = null, bool? logOutput = null, bool? logInvocation = null, Action<OutputType, string> logger = null, Func<IProcess, object> exitHandler = null) => new DotNetUserSecretsTasks().Run(arguments, workingDirectory, environmentVariables, timeout, logOutput, logInvocation, logger, exitHandler);
    /// <summary><p>Sets a user secret</p><p>For more details, visit the <a href="https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets">official website</a>.</p></summary>
    /// <remarks><p>This is a <a href="https://www.nuke.build/docs/common/cli-tools/#fluent-api">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p><ul><li><c>&lt;key&gt;</c> via <see cref="DotNetUserSecretsSetSettings.Key"/></li><li><c>&lt;value&gt;</c> via <see cref="DotNetUserSecretsSetSettings.Value"/></li><li><c>--project</c> via <see cref="DotNetUserSecretsSetSettings.Project"/></li></ul></remarks>
    public static IReadOnlyCollection<Output> DotNetUserSecretsSet(DotNetUserSecretsSetSettings options = null) => new DotNetUserSecretsTasks().Run<DotNetUserSecretsSetSettings>(options);
    /// <inheritdoc cref="DotNetUserSecretsTasks.DotNetUserSecretsSet(Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.DotNetUserSecrets.DotNetUserSecretsSetSettings)"/>
    public static IReadOnlyCollection<Output> DotNetUserSecretsSet(Configure<DotNetUserSecretsSetSettings> configurator) => new DotNetUserSecretsTasks().Run<DotNetUserSecretsSetSettings>(configurator.Invoke(new DotNetUserSecretsSetSettings()));
    /// <inheritdoc cref="DotNetUserSecretsTasks.DotNetUserSecretsSet(Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.DotNetUserSecrets.DotNetUserSecretsSetSettings)"/>
    public static IEnumerable<(DotNetUserSecretsSetSettings Settings, IReadOnlyCollection<Output> Output)> DotNetUserSecretsSet(CombinatorialConfigure<DotNetUserSecretsSetSettings> configurator, int degreeOfParallelism = 1, bool completeOnFailure = false) => configurator.Invoke(DotNetUserSecretsSet, degreeOfParallelism, completeOnFailure);
}
#region DotNetUserSecretsSetSettings
/// <inheritdoc cref="DotNetUserSecretsTasks.DotNetUserSecretsSet(Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.DotNetUserSecrets.DotNetUserSecretsSetSettings)"/>
[PublicAPI]
[ExcludeFromCodeCoverage]
[Command(Type = typeof(DotNetUserSecretsTasks), Command = nameof(DotNetUserSecretsTasks.DotNetUserSecretsSet), Arguments = "user-secrets set")]
public partial class DotNetUserSecretsSetSettings : ToolOptions
{
    /// <summary>User secret key</summary>
    [Argument(Format = "{value}", Position = 1, Secret = false)] public string Key => Get<string>(() => Key);
    /// <summary>User secret value</summary>
    [Argument(Format = "{value}", Position = 2, Secret = true)] public string Value => Get<string>(() => Value);
    /// <summary>Project to set user secret for</summary>
    [Argument(Format = "--project {value}")] public string Project => Get<string>(() => Project);
}
#endregion
#region DotNetUserSecretsSetSettingsExtensions
/// <inheritdoc cref="DotNetUserSecretsTasks.DotNetUserSecretsSet(Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.DotNetUserSecrets.DotNetUserSecretsSetSettings)"/>
[PublicAPI]
[ExcludeFromCodeCoverage]
public static partial class DotNetUserSecretsSetSettingsExtensions
{
    #region Key
    /// <inheritdoc cref="DotNetUserSecretsSetSettings.Key"/>
    [Pure] [Builder(Type = typeof(DotNetUserSecretsSetSettings), Property = nameof(DotNetUserSecretsSetSettings.Key))]
    public static T SetKey<T>(this T o, string v) where T : DotNetUserSecretsSetSettings => o.Modify(b => b.Set(() => o.Key, v));
    /// <inheritdoc cref="DotNetUserSecretsSetSettings.Key"/>
    [Pure] [Builder(Type = typeof(DotNetUserSecretsSetSettings), Property = nameof(DotNetUserSecretsSetSettings.Key))]
    public static T ResetKey<T>(this T o) where T : DotNetUserSecretsSetSettings => o.Modify(b => b.Remove(() => o.Key));
    #endregion
    #region Value
    /// <inheritdoc cref="DotNetUserSecretsSetSettings.Value"/>
    [Pure] [Builder(Type = typeof(DotNetUserSecretsSetSettings), Property = nameof(DotNetUserSecretsSetSettings.Value))]
    public static T SetValue<T>(this T o, [Secret] string v) where T : DotNetUserSecretsSetSettings => o.Modify(b => b.Set(() => o.Value, v));
    /// <inheritdoc cref="DotNetUserSecretsSetSettings.Value"/>
    [Pure] [Builder(Type = typeof(DotNetUserSecretsSetSettings), Property = nameof(DotNetUserSecretsSetSettings.Value))]
    public static T ResetValue<T>(this T o) where T : DotNetUserSecretsSetSettings => o.Modify(b => b.Remove(() => o.Value));
    #endregion
    #region Project
    /// <inheritdoc cref="DotNetUserSecretsSetSettings.Project"/>
    [Pure] [Builder(Type = typeof(DotNetUserSecretsSetSettings), Property = nameof(DotNetUserSecretsSetSettings.Project))]
    public static T SetProject<T>(this T o, string v) where T : DotNetUserSecretsSetSettings => o.Modify(b => b.Set(() => o.Project, v));
    /// <inheritdoc cref="DotNetUserSecretsSetSettings.Project"/>
    [Pure] [Builder(Type = typeof(DotNetUserSecretsSetSettings), Property = nameof(DotNetUserSecretsSetSettings.Project))]
    public static T ResetProject<T>(this T o) where T : DotNetUserSecretsSetSettings => o.Modify(b => b.Remove(() => o.Project));
    #endregion
}
#endregion
