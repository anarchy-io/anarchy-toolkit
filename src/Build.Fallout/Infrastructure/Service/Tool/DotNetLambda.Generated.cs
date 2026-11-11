
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

namespace Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.DotNetLambda;

/// <summary><p>The .NET Core CLI offering a cross-platform way to create .NET-based Lambda applications.</p><p>For more details, visit the <a href="https://docs.aws.amazon.com/lambda/latest/dg/csharp-package-cli.html">official website</a>.</p></summary>
[ExcludeFromCodeCoverage]
[NuGetTool(Id = PackageId, Executable = PackageExecutable)]
public partial class DotNetLambdaTasks : ToolTasks, IRequireNuGetPackage
{
    public static string DotNetLambdaPath { get => new DotNetLambdaTasks().GetToolPathInternal(); set => new DotNetLambdaTasks().SetToolPath(value); }
    public const string PackageId = "amazon.lambda.tools";
    public const string PackageExecutable = "dotnet-lambda.dll";
    /// <summary><p>The .NET Core CLI offering a cross-platform way to create .NET-based Lambda applications.</p><p>For more details, visit the <a href="https://docs.aws.amazon.com/lambda/latest/dg/csharp-package-cli.html">official website</a>.</p></summary>
    public static IReadOnlyCollection<Output> DotNetLambda(ArgumentStringHandler arguments, string workingDirectory = null, IReadOnlyDictionary<string, string> environmentVariables = null, int? timeout = null, bool? logOutput = null, bool? logInvocation = null, Action<OutputType, string> logger = null, Func<IProcess, object> exitHandler = null) => new DotNetLambdaTasks().Run(arguments, workingDirectory, environmentVariables, timeout, logOutput, logInvocation, logger, exitHandler);
    /// <summary><p>Command to package a Lambda project into a zip file.</p><p>For more details, visit the <a href="https://docs.aws.amazon.com/lambda/latest/dg/csharp-package-cli.html">official website</a>.</p></summary>
    /// <remarks><p>This is a <a href="https://github.com/ChrisonSimtian/Fallout">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p><ul><li><c>-c</c> via <see cref="DotNetLambdaPackageSettings.Configuration"/></li><li><c>-farch</c> via <see cref="DotNetLambdaPackageSettings.FunctionArchitecture"/></li><li><c>-o</c> via <see cref="DotNetLambdaPackageSettings.OutputPackage"/></li><li><c>-pl</c> via <see cref="DotNetLambdaPackageSettings.ProjectLocation"/></li></ul></remarks>
    public static IReadOnlyCollection<Output> DotNetLambdaPackage(DotNetLambdaPackageSettings options = null) => new DotNetLambdaTasks().Run<DotNetLambdaPackageSettings>(options);
    /// <inheritdoc cref="DotNetLambdaTasks.DotNetLambdaPackage(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.DotNetLambda.DotNetLambdaPackageSettings)"/>
    public static IReadOnlyCollection<Output> DotNetLambdaPackage(Configure<DotNetLambdaPackageSettings> configurator) => new DotNetLambdaTasks().Run<DotNetLambdaPackageSettings>(configurator.Invoke(new DotNetLambdaPackageSettings()));
    /// <inheritdoc cref="DotNetLambdaTasks.DotNetLambdaPackage(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.DotNetLambda.DotNetLambdaPackageSettings)"/>
    public static IEnumerable<(DotNetLambdaPackageSettings Settings, IReadOnlyCollection<Output> Output)> DotNetLambdaPackage(CombinatorialConfigure<DotNetLambdaPackageSettings> configurator, int degreeOfParallelism = 1, bool completeOnFailure = false) => configurator.Invoke(DotNetLambdaPackage, degreeOfParallelism, completeOnFailure);
    /// <summary><p>Command to package a Lambda project into a zip file, upload it to s3 and create a deployment CF in CI.</p><p>For more details, visit the <a href="https://docs.aws.amazon.com/lambda/latest/dg/csharp-package-cli.html">official website</a>.</p></summary>
    /// <remarks><p>This is a <a href="https://github.com/ChrisonSimtian/Fallout">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p><ul><li><c>--configuration</c> via <see cref="DotNetLambdaPackageCISettings.Configuration"/></li><li><c>--disable-interactive</c> via <see cref="DotNetLambdaPackageCISettings.DisableInteractive"/></li><li><c>--output-template</c> via <see cref="DotNetLambdaPackageCISettings.OutputTemplate"/></li><li><c>--project-location</c> via <see cref="DotNetLambdaPackageCISettings.ProjectLocation"/></li><li><c>--s3-bucket</c> via <see cref="DotNetLambdaPackageCISettings.S3Bucket"/></li></ul></remarks>
    public static IReadOnlyCollection<Output> DotNetLambdaPackageCI(DotNetLambdaPackageCISettings options = null) => new DotNetLambdaTasks().Run<DotNetLambdaPackageCISettings>(options);
    /// <inheritdoc cref="DotNetLambdaTasks.DotNetLambdaPackageCI(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.DotNetLambda.DotNetLambdaPackageCISettings)"/>
    public static IReadOnlyCollection<Output> DotNetLambdaPackageCI(Configure<DotNetLambdaPackageCISettings> configurator) => new DotNetLambdaTasks().Run<DotNetLambdaPackageCISettings>(configurator.Invoke(new DotNetLambdaPackageCISettings()));
    /// <inheritdoc cref="DotNetLambdaTasks.DotNetLambdaPackageCI(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.DotNetLambda.DotNetLambdaPackageCISettings)"/>
    public static IEnumerable<(DotNetLambdaPackageCISettings Settings, IReadOnlyCollection<Output> Output)> DotNetLambdaPackageCI(CombinatorialConfigure<DotNetLambdaPackageCISettings> configurator, int degreeOfParallelism = 1, bool completeOnFailure = false) => configurator.Invoke(DotNetLambdaPackageCI, degreeOfParallelism, completeOnFailure);
}
#region DotNetLambdaPackageSettings
/// <inheritdoc cref="DotNetLambdaTasks.DotNetLambdaPackage(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.DotNetLambda.DotNetLambdaPackageSettings)"/>
[ExcludeFromCodeCoverage]
[Command(Type = typeof(DotNetLambdaTasks), Command = nameof(DotNetLambdaTasks.DotNetLambdaPackage), Arguments = "package")]
public partial class DotNetLambdaPackageSettings : ToolOptions, IToolOptionsWithFramework
{
    /// <summary>The zip file that will be created with compiled and packaged Lambda function.</summary>
    [Argument(Format = "-o {value}")] public string OutputPackage => Get<string>(() => OutputPackage);
    /// <summary>The location of the project, if not set the current directory will be assumed.</summary>
    [Argument(Format = "-pl {value}")] public string ProjectLocation => Get<string>(() => ProjectLocation);
    /// <summary>Configuration to build with, for example Release or Debug.</summary>
    [Argument(Format = "-c {value}")] public string Configuration => Get<string>(() => Configuration);
    /// <summary>The architecture of the Lambda function. Valid values: x86_64 or arm64. Default is x86_64</summary>
    [Argument(Format = "-farch {value}")] public string FunctionArchitecture => Get<string>(() => FunctionArchitecture);
}
#endregion
#region DotNetLambdaPackageCISettings
/// <inheritdoc cref="DotNetLambdaTasks.DotNetLambdaPackageCI(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.DotNetLambda.DotNetLambdaPackageCISettings)"/>
[ExcludeFromCodeCoverage]
[Command(Type = typeof(DotNetLambdaTasks), Command = nameof(DotNetLambdaTasks.DotNetLambdaPackageCI), Arguments = "package-ci")]
public partial class DotNetLambdaPackageCISettings : ToolOptions, IToolOptionsWithFramework
{
    /// <summary>The zip file that will be created with compiled and packaged Lambda function.</summary>
    [Argument(Format = "--output-template {value}")] public string OutputTemplate => Get<string>(() => OutputTemplate);
    /// <summary>The location of the project, if not set the current directory will be assumed.</summary>
    [Argument(Format = "--project-location {value}")] public string ProjectLocation => Get<string>(() => ProjectLocation);
    /// <summary>Configuration to build with, for example Release or Debug.</summary>
    [Argument(Format = "--configuration {value}")] public string Configuration => Get<string>(() => Configuration);
    /// <summary>The name of the Amazon S3 bucket where this command uploads your AWS Lambda binaries.</summary>
    [Argument(Format = "--s3-bucket {value}")] public string S3Bucket => Get<string>(() => S3Bucket);
    /// <summary>When set to true missing required parameters will not be prompted for.</summary>
    [Argument(Format = "--disable-interactive {value}")] public bool? DisableInteractive => Get<bool?>(() => DisableInteractive);
}
#endregion
#region DotNetLambdaPackageSettingsExtensions
/// <inheritdoc cref="DotNetLambdaTasks.DotNetLambdaPackage(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.DotNetLambda.DotNetLambdaPackageSettings)"/>
[ExcludeFromCodeCoverage]
public static partial class DotNetLambdaPackageSettingsExtensions
{
    #region OutputPackage
    /// <inheritdoc cref="DotNetLambdaPackageSettings.OutputPackage"/>
    [Builder(Type = typeof(DotNetLambdaPackageSettings), Property = nameof(DotNetLambdaPackageSettings.OutputPackage))]
    public static T SetOutputPackage<T>(this T o, string v) where T : DotNetLambdaPackageSettings => o.Modify(b => b.Set(() => o.OutputPackage, v));
    /// <inheritdoc cref="DotNetLambdaPackageSettings.OutputPackage"/>
    [Builder(Type = typeof(DotNetLambdaPackageSettings), Property = nameof(DotNetLambdaPackageSettings.OutputPackage))]
    public static T ResetOutputPackage<T>(this T o) where T : DotNetLambdaPackageSettings => o.Modify(b => b.Remove(() => o.OutputPackage));
    #endregion
    #region ProjectLocation
    /// <inheritdoc cref="DotNetLambdaPackageSettings.ProjectLocation"/>
    [Builder(Type = typeof(DotNetLambdaPackageSettings), Property = nameof(DotNetLambdaPackageSettings.ProjectLocation))]
    public static T SetProjectLocation<T>(this T o, string v) where T : DotNetLambdaPackageSettings => o.Modify(b => b.Set(() => o.ProjectLocation, v));
    /// <inheritdoc cref="DotNetLambdaPackageSettings.ProjectLocation"/>
    [Builder(Type = typeof(DotNetLambdaPackageSettings), Property = nameof(DotNetLambdaPackageSettings.ProjectLocation))]
    public static T ResetProjectLocation<T>(this T o) where T : DotNetLambdaPackageSettings => o.Modify(b => b.Remove(() => o.ProjectLocation));
    #endregion
    #region Configuration
    /// <inheritdoc cref="DotNetLambdaPackageSettings.Configuration"/>
    [Builder(Type = typeof(DotNetLambdaPackageSettings), Property = nameof(DotNetLambdaPackageSettings.Configuration))]
    public static T SetConfiguration<T>(this T o, string v) where T : DotNetLambdaPackageSettings => o.Modify(b => b.Set(() => o.Configuration, v));
    /// <inheritdoc cref="DotNetLambdaPackageSettings.Configuration"/>
    [Builder(Type = typeof(DotNetLambdaPackageSettings), Property = nameof(DotNetLambdaPackageSettings.Configuration))]
    public static T ResetConfiguration<T>(this T o) where T : DotNetLambdaPackageSettings => o.Modify(b => b.Remove(() => o.Configuration));
    #endregion
    #region FunctionArchitecture
    /// <inheritdoc cref="DotNetLambdaPackageSettings.FunctionArchitecture"/>
    [Builder(Type = typeof(DotNetLambdaPackageSettings), Property = nameof(DotNetLambdaPackageSettings.FunctionArchitecture))]
    public static T SetFunctionArchitecture<T>(this T o, string v) where T : DotNetLambdaPackageSettings => o.Modify(b => b.Set(() => o.FunctionArchitecture, v));
    /// <inheritdoc cref="DotNetLambdaPackageSettings.FunctionArchitecture"/>
    [Builder(Type = typeof(DotNetLambdaPackageSettings), Property = nameof(DotNetLambdaPackageSettings.FunctionArchitecture))]
    public static T ResetFunctionArchitecture<T>(this T o) where T : DotNetLambdaPackageSettings => o.Modify(b => b.Remove(() => o.FunctionArchitecture));
    #endregion
}
#endregion
#region DotNetLambdaPackageCISettingsExtensions
/// <inheritdoc cref="DotNetLambdaTasks.DotNetLambdaPackageCI(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.DotNetLambda.DotNetLambdaPackageCISettings)"/>
[ExcludeFromCodeCoverage]
public static partial class DotNetLambdaPackageCISettingsExtensions
{
    #region OutputTemplate
    /// <inheritdoc cref="DotNetLambdaPackageCISettings.OutputTemplate"/>
    [Builder(Type = typeof(DotNetLambdaPackageCISettings), Property = nameof(DotNetLambdaPackageCISettings.OutputTemplate))]
    public static T SetOutputTemplate<T>(this T o, string v) where T : DotNetLambdaPackageCISettings => o.Modify(b => b.Set(() => o.OutputTemplate, v));
    /// <inheritdoc cref="DotNetLambdaPackageCISettings.OutputTemplate"/>
    [Builder(Type = typeof(DotNetLambdaPackageCISettings), Property = nameof(DotNetLambdaPackageCISettings.OutputTemplate))]
    public static T ResetOutputTemplate<T>(this T o) where T : DotNetLambdaPackageCISettings => o.Modify(b => b.Remove(() => o.OutputTemplate));
    #endregion
    #region ProjectLocation
    /// <inheritdoc cref="DotNetLambdaPackageCISettings.ProjectLocation"/>
    [Builder(Type = typeof(DotNetLambdaPackageCISettings), Property = nameof(DotNetLambdaPackageCISettings.ProjectLocation))]
    public static T SetProjectLocation<T>(this T o, string v) where T : DotNetLambdaPackageCISettings => o.Modify(b => b.Set(() => o.ProjectLocation, v));
    /// <inheritdoc cref="DotNetLambdaPackageCISettings.ProjectLocation"/>
    [Builder(Type = typeof(DotNetLambdaPackageCISettings), Property = nameof(DotNetLambdaPackageCISettings.ProjectLocation))]
    public static T ResetProjectLocation<T>(this T o) where T : DotNetLambdaPackageCISettings => o.Modify(b => b.Remove(() => o.ProjectLocation));
    #endregion
    #region Configuration
    /// <inheritdoc cref="DotNetLambdaPackageCISettings.Configuration"/>
    [Builder(Type = typeof(DotNetLambdaPackageCISettings), Property = nameof(DotNetLambdaPackageCISettings.Configuration))]
    public static T SetConfiguration<T>(this T o, string v) where T : DotNetLambdaPackageCISettings => o.Modify(b => b.Set(() => o.Configuration, v));
    /// <inheritdoc cref="DotNetLambdaPackageCISettings.Configuration"/>
    [Builder(Type = typeof(DotNetLambdaPackageCISettings), Property = nameof(DotNetLambdaPackageCISettings.Configuration))]
    public static T ResetConfiguration<T>(this T o) where T : DotNetLambdaPackageCISettings => o.Modify(b => b.Remove(() => o.Configuration));
    #endregion
    #region S3Bucket
    /// <inheritdoc cref="DotNetLambdaPackageCISettings.S3Bucket"/>
    [Builder(Type = typeof(DotNetLambdaPackageCISettings), Property = nameof(DotNetLambdaPackageCISettings.S3Bucket))]
    public static T SetS3Bucket<T>(this T o, string v) where T : DotNetLambdaPackageCISettings => o.Modify(b => b.Set(() => o.S3Bucket, v));
    /// <inheritdoc cref="DotNetLambdaPackageCISettings.S3Bucket"/>
    [Builder(Type = typeof(DotNetLambdaPackageCISettings), Property = nameof(DotNetLambdaPackageCISettings.S3Bucket))]
    public static T ResetS3Bucket<T>(this T o) where T : DotNetLambdaPackageCISettings => o.Modify(b => b.Remove(() => o.S3Bucket));
    #endregion
    #region DisableInteractive
    /// <inheritdoc cref="DotNetLambdaPackageCISettings.DisableInteractive"/>
    [Builder(Type = typeof(DotNetLambdaPackageCISettings), Property = nameof(DotNetLambdaPackageCISettings.DisableInteractive))]
    public static T SetDisableInteractive<T>(this T o, bool? v) where T : DotNetLambdaPackageCISettings => o.Modify(b => b.Set(() => o.DisableInteractive, v));
    /// <inheritdoc cref="DotNetLambdaPackageCISettings.DisableInteractive"/>
    [Builder(Type = typeof(DotNetLambdaPackageCISettings), Property = nameof(DotNetLambdaPackageCISettings.DisableInteractive))]
    public static T ResetDisableInteractive<T>(this T o) where T : DotNetLambdaPackageCISettings => o.Modify(b => b.Remove(() => o.DisableInteractive));
    /// <inheritdoc cref="DotNetLambdaPackageCISettings.DisableInteractive"/>
    [Builder(Type = typeof(DotNetLambdaPackageCISettings), Property = nameof(DotNetLambdaPackageCISettings.DisableInteractive))]
    public static T EnableDisableInteractive<T>(this T o) where T : DotNetLambdaPackageCISettings => o.Modify(b => b.Set(() => o.DisableInteractive, true));
    /// <inheritdoc cref="DotNetLambdaPackageCISettings.DisableInteractive"/>
    [Builder(Type = typeof(DotNetLambdaPackageCISettings), Property = nameof(DotNetLambdaPackageCISettings.DisableInteractive))]
    public static T DisableDisableInteractive<T>(this T o) where T : DotNetLambdaPackageCISettings => o.Modify(b => b.Set(() => o.DisableInteractive, false));
    /// <inheritdoc cref="DotNetLambdaPackageCISettings.DisableInteractive"/>
    [Builder(Type = typeof(DotNetLambdaPackageCISettings), Property = nameof(DotNetLambdaPackageCISettings.DisableInteractive))]
    public static T ToggleDisableInteractive<T>(this T o) where T : DotNetLambdaPackageCISettings => o.Modify(b => b.Set(() => o.DisableInteractive, !o.DisableInteractive));
    #endregion
}
#endregion
