
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

namespace Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Aws;

/// <summary><p>The AWS Command Line Interface (CLI) is a unified tool to manage your AWS services. With just one tool to download and configure, you can control multiple AWS services from the command line and automate them through scripts.</p><p>For more details, visit the <a href="https://aws.amazon.com/cli/">official website</a>.</p></summary>
[ExcludeFromCodeCoverage]
[PathTool(Executable = PathExecutable)]
public partial class AwsTasks : ToolTasks, IRequirePathTool
{
    public static string AwsPath { get => new AwsTasks().GetToolPathInternal(); set => new AwsTasks().SetToolPath(value); }
    public const string PathExecutable = "aws";
    /// <summary><p>The AWS Command Line Interface (CLI) is a unified tool to manage your AWS services. With just one tool to download and configure, you can control multiple AWS services from the command line and automate them through scripts.</p><p>For more details, visit the <a href="https://aws.amazon.com/cli/">official website</a>.</p></summary>
    public static IReadOnlyCollection<Output> Aws(ArgumentStringHandler arguments, string workingDirectory = null, IReadOnlyDictionary<string, string> environmentVariables = null, int? timeout = null, bool? logOutput = null, bool? logInvocation = null, Action<OutputType, string> logger = null, Func<IProcess, object> exitHandler = null) => new AwsTasks().Run(arguments, workingDirectory, environmentVariables, timeout, logOutput, logInvocation, logger, exitHandler);
    /// <summary><p>Packages the local artifacts (local paths) that your AWS CloudFormation template references.</p><p>For more details, visit the <a href="https://aws.amazon.com/cli/">official website</a>.</p></summary>
    /// <remarks><p>This is a <a href="https://github.com/ChrisonSimtian/Fallout">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p><ul><li><c>--output-template-file</c> via <see cref="AwsLambdaPackageSettings.OutputTemplateFile"/></li><li><c>--s3-bucket</c> via <see cref="AwsLambdaPackageSettings.S3Bucket"/></li><li><c>--template-file</c> via <see cref="AwsLambdaPackageSettings.TemplateFile"/></li></ul></remarks>
    public static IReadOnlyCollection<Output> AwsLambdaPackage(AwsLambdaPackageSettings options = null) => new AwsTasks().Run<AwsLambdaPackageSettings>(options);
    /// <inheritdoc cref="AwsTasks.AwsLambdaPackage(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Aws.AwsLambdaPackageSettings)"/>
    public static IReadOnlyCollection<Output> AwsLambdaPackage(Configure<AwsLambdaPackageSettings> configurator) => new AwsTasks().Run<AwsLambdaPackageSettings>(configurator.Invoke(new AwsLambdaPackageSettings()));
    /// <inheritdoc cref="AwsTasks.AwsLambdaPackage(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Aws.AwsLambdaPackageSettings)"/>
    public static IEnumerable<(AwsLambdaPackageSettings Settings, IReadOnlyCollection<Output> Output)> AwsLambdaPackage(CombinatorialConfigure<AwsLambdaPackageSettings> configurator, int degreeOfParallelism = 1, bool completeOnFailure = false) => configurator.Invoke(AwsLambdaPackage, degreeOfParallelism, completeOnFailure);
}
#region AwsLambdaPackageSettings
/// <inheritdoc cref="AwsTasks.AwsLambdaPackage(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Aws.AwsLambdaPackageSettings)"/>
[ExcludeFromCodeCoverage]
[Command(Type = typeof(AwsTasks), Command = nameof(AwsTasks.AwsLambdaPackage), Arguments = "cloudformation package")]
public partial class AwsLambdaPackageSettings : ToolOptions
{
    /// <summary>The path and file name where your AWS SAM template is located</summary>
    [Argument(Format = "--template-file {value}")] public string TemplateFile => Get<string>(() => TemplateFile);
    /// <summary>The name of the Amazon S3 bucket where this command uploads your AWS CloudFormation template.</summary>
    [Argument(Format = "--s3-bucket {value}")] public string S3Bucket => Get<string>(() => S3Bucket);
    /// <summary>The path to the file where the command writes the packaged template. If you don't specify a path, the command writes the template to the standard output.</summary>
    [Argument(Format = "--output-template-file {value}")] public string OutputTemplateFile => Get<string>(() => OutputTemplateFile);
}
#endregion
#region AwsLambdaPackageSettingsExtensions
/// <inheritdoc cref="AwsTasks.AwsLambdaPackage(Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Aws.AwsLambdaPackageSettings)"/>
[ExcludeFromCodeCoverage]
public static partial class AwsLambdaPackageSettingsExtensions
{
    #region TemplateFile
    /// <inheritdoc cref="AwsLambdaPackageSettings.TemplateFile"/>
    [Builder(Type = typeof(AwsLambdaPackageSettings), Property = nameof(AwsLambdaPackageSettings.TemplateFile))]
    public static T SetTemplateFile<T>(this T o, string v) where T : AwsLambdaPackageSettings => o.Modify(b => b.Set(() => o.TemplateFile, v));
    /// <inheritdoc cref="AwsLambdaPackageSettings.TemplateFile"/>
    [Builder(Type = typeof(AwsLambdaPackageSettings), Property = nameof(AwsLambdaPackageSettings.TemplateFile))]
    public static T ResetTemplateFile<T>(this T o) where T : AwsLambdaPackageSettings => o.Modify(b => b.Remove(() => o.TemplateFile));
    #endregion
    #region S3Bucket
    /// <inheritdoc cref="AwsLambdaPackageSettings.S3Bucket"/>
    [Builder(Type = typeof(AwsLambdaPackageSettings), Property = nameof(AwsLambdaPackageSettings.S3Bucket))]
    public static T SetS3Bucket<T>(this T o, string v) where T : AwsLambdaPackageSettings => o.Modify(b => b.Set(() => o.S3Bucket, v));
    /// <inheritdoc cref="AwsLambdaPackageSettings.S3Bucket"/>
    [Builder(Type = typeof(AwsLambdaPackageSettings), Property = nameof(AwsLambdaPackageSettings.S3Bucket))]
    public static T ResetS3Bucket<T>(this T o) where T : AwsLambdaPackageSettings => o.Modify(b => b.Remove(() => o.S3Bucket));
    #endregion
    #region OutputTemplateFile
    /// <inheritdoc cref="AwsLambdaPackageSettings.OutputTemplateFile"/>
    [Builder(Type = typeof(AwsLambdaPackageSettings), Property = nameof(AwsLambdaPackageSettings.OutputTemplateFile))]
    public static T SetOutputTemplateFile<T>(this T o, string v) where T : AwsLambdaPackageSettings => o.Modify(b => b.Set(() => o.OutputTemplateFile, v));
    /// <inheritdoc cref="AwsLambdaPackageSettings.OutputTemplateFile"/>
    [Builder(Type = typeof(AwsLambdaPackageSettings), Property = nameof(AwsLambdaPackageSettings.OutputTemplateFile))]
    public static T ResetOutputTemplateFile<T>(this T o) where T : AwsLambdaPackageSettings => o.Modify(b => b.Remove(() => o.OutputTemplateFile));
    #endregion
}
#endregion
