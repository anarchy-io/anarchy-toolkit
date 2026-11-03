
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

namespace Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.AwsSam;

/// <summary><p>The AWS Serverless Application Model (AWS SAM) is an open-source framework that you can use to build serverless applications on AWS.</p><p>For more details, visit the <a href="https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/what-is-sam.html">official website</a>.</p></summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
[PathTool(Executable = PathExecutable)]
public partial class AwsSamTasks : ToolTasks, IRequirePathTool
{
    public static string AwsSamPath { get => new AwsSamTasks().GetToolPathInternal(); set => new AwsSamTasks().SetToolPath(value); }
    public const string PathExecutable = "sam";
    /// <summary><p>The AWS Serverless Application Model (AWS SAM) is an open-source framework that you can use to build serverless applications on AWS.</p><p>For more details, visit the <a href="https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/what-is-sam.html">official website</a>.</p></summary>
    public static IReadOnlyCollection<Output> AwsSam(ArgumentStringHandler arguments, string workingDirectory = null, IReadOnlyDictionary<string, string> environmentVariables = null, int? timeout = null, bool? logOutput = null, bool? logInvocation = null, Action<OutputType, string> logger = null, Func<IProcess, object> exitHandler = null) => new AwsSamTasks().Run(arguments, workingDirectory, environmentVariables, timeout, logOutput, logInvocation, logger, exitHandler);
    /// <summary><p>Invokes a local AWS Lambda function once and quits after invocation completes.</p><p>For more details, visit the <a href="https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/what-is-sam.html">official website</a>.</p></summary>
    /// <remarks><p>This is a <a href="https://www.nuke.build/docs/common/cli-tools/#fluent-api">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p><ul><li><c>&lt;functionName&gt;</c> via <see cref="AwsSamLocalInvokeSettings.FunctionName"/></li><li><c>--event</c> via <see cref="AwsSamLocalInvokeSettings.Event"/></li><li><c>--parameter-overrides</c> via <see cref="AwsSamLocalInvokeSettings.ParameterOverrides"/></li><li><c>--template</c> via <see cref="AwsSamLocalInvokeSettings.Template"/></li></ul></remarks>
    public static IReadOnlyCollection<Output> AwsSamLocalInvoke(AwsSamLocalInvokeSettings options = null) => new AwsSamTasks().Run<AwsSamLocalInvokeSettings>(options);
    /// <inheritdoc cref="AwsSamTasks.AwsSamLocalInvoke(Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.AwsSam.AwsSamLocalInvokeSettings)"/>
    public static IReadOnlyCollection<Output> AwsSamLocalInvoke(Configure<AwsSamLocalInvokeSettings> configurator) => new AwsSamTasks().Run<AwsSamLocalInvokeSettings>(configurator.Invoke(new AwsSamLocalInvokeSettings()));
    /// <inheritdoc cref="AwsSamTasks.AwsSamLocalInvoke(Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.AwsSam.AwsSamLocalInvokeSettings)"/>
    public static IEnumerable<(AwsSamLocalInvokeSettings Settings, IReadOnlyCollection<Output> Output)> AwsSamLocalInvoke(CombinatorialConfigure<AwsSamLocalInvokeSettings> configurator, int degreeOfParallelism = 1, bool completeOnFailure = false) => configurator.Invoke(AwsSamLocalInvoke, degreeOfParallelism, completeOnFailure);
}
#region AwsSamLocalInvokeSettings
/// <inheritdoc cref="AwsSamTasks.AwsSamLocalInvoke(Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.AwsSam.AwsSamLocalInvokeSettings)"/>
[PublicAPI]
[ExcludeFromCodeCoverage]
[Command(Type = typeof(AwsSamTasks), Command = nameof(AwsSamTasks.AwsSamLocalInvoke), Arguments = "local invoke")]
public partial class AwsSamLocalInvokeSettings : ToolOptions
{
    /// <summary>The Lambda function to invoke.</summary>
    [Argument(Format = "{value}", Position = 1)] public string FunctionName => Get<string>(() => FunctionName);
    /// <summary>The JSON file that contains event data that's passed to the Lambda function when it's invoked. If you don't specify this option, no event is assumed.</summary>
    [Argument(Format = "--event {value}")] public string Event => Get<string>(() => Event);
    /// <summary>The AWS SAM template file.</summary>
    [Argument(Format = "--template {value}")] public string Template => Get<string>(() => Template);
    /// <summary>A string that contains AWS CloudFormation parameter overrides encoded as key-value pairs. Uses the same format as the AWS Command Line Interface (AWS CLI). For example: 'ParameterKey=KeyPairName, ParameterValue=MyKey ParameterKey=InstanceType, ParameterValue=t1.micro'.</summary>
    [Argument(Format = "--parameter-overrides {value}", Separator = " ")] public IReadOnlyDictionary<string, object> ParameterOverrides => Get<Dictionary<string, object>>(() => ParameterOverrides);
}
#endregion
#region AwsSamLocalInvokeSettingsExtensions
/// <inheritdoc cref="AwsSamTasks.AwsSamLocalInvoke(Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.AwsSam.AwsSamLocalInvokeSettings)"/>
[PublicAPI]
[ExcludeFromCodeCoverage]
public static partial class AwsSamLocalInvokeSettingsExtensions
{
    #region FunctionName
    /// <inheritdoc cref="AwsSamLocalInvokeSettings.FunctionName"/>
    [Pure] [Builder(Type = typeof(AwsSamLocalInvokeSettings), Property = nameof(AwsSamLocalInvokeSettings.FunctionName))]
    public static T SetFunctionName<T>(this T o, string v) where T : AwsSamLocalInvokeSettings => o.Modify(b => b.Set(() => o.FunctionName, v));
    /// <inheritdoc cref="AwsSamLocalInvokeSettings.FunctionName"/>
    [Pure] [Builder(Type = typeof(AwsSamLocalInvokeSettings), Property = nameof(AwsSamLocalInvokeSettings.FunctionName))]
    public static T ResetFunctionName<T>(this T o) where T : AwsSamLocalInvokeSettings => o.Modify(b => b.Remove(() => o.FunctionName));
    #endregion
    #region Event
    /// <inheritdoc cref="AwsSamLocalInvokeSettings.Event"/>
    [Pure] [Builder(Type = typeof(AwsSamLocalInvokeSettings), Property = nameof(AwsSamLocalInvokeSettings.Event))]
    public static T SetEvent<T>(this T o, string v) where T : AwsSamLocalInvokeSettings => o.Modify(b => b.Set(() => o.Event, v));
    /// <inheritdoc cref="AwsSamLocalInvokeSettings.Event"/>
    [Pure] [Builder(Type = typeof(AwsSamLocalInvokeSettings), Property = nameof(AwsSamLocalInvokeSettings.Event))]
    public static T ResetEvent<T>(this T o) where T : AwsSamLocalInvokeSettings => o.Modify(b => b.Remove(() => o.Event));
    #endregion
    #region Template
    /// <inheritdoc cref="AwsSamLocalInvokeSettings.Template"/>
    [Pure] [Builder(Type = typeof(AwsSamLocalInvokeSettings), Property = nameof(AwsSamLocalInvokeSettings.Template))]
    public static T SetTemplate<T>(this T o, string v) where T : AwsSamLocalInvokeSettings => o.Modify(b => b.Set(() => o.Template, v));
    /// <inheritdoc cref="AwsSamLocalInvokeSettings.Template"/>
    [Pure] [Builder(Type = typeof(AwsSamLocalInvokeSettings), Property = nameof(AwsSamLocalInvokeSettings.Template))]
    public static T ResetTemplate<T>(this T o) where T : AwsSamLocalInvokeSettings => o.Modify(b => b.Remove(() => o.Template));
    #endregion
    #region ParameterOverrides
    /// <inheritdoc cref="AwsSamLocalInvokeSettings.ParameterOverrides"/>
    [Pure] [Builder(Type = typeof(AwsSamLocalInvokeSettings), Property = nameof(AwsSamLocalInvokeSettings.ParameterOverrides))]
    public static T SetParameterOverrides<T>(this T o, IDictionary<string, object> v) where T : AwsSamLocalInvokeSettings => o.Modify(b => b.Set(() => o.ParameterOverrides, v.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase)));
    /// <inheritdoc cref="AwsSamLocalInvokeSettings.ParameterOverrides"/>
    [Pure] [Builder(Type = typeof(AwsSamLocalInvokeSettings), Property = nameof(AwsSamLocalInvokeSettings.ParameterOverrides))]
    public static T SetParameterOverride<T>(this T o, string k, object v) where T : AwsSamLocalInvokeSettings => o.Modify(b => b.SetDictionary(() => o.ParameterOverrides, k, v));
    /// <inheritdoc cref="AwsSamLocalInvokeSettings.ParameterOverrides"/>
    [Pure] [Builder(Type = typeof(AwsSamLocalInvokeSettings), Property = nameof(AwsSamLocalInvokeSettings.ParameterOverrides))]
    public static T AddParameterOverride<T>(this T o, string k, object v) where T : AwsSamLocalInvokeSettings => o.Modify(b => b.AddDictionary(() => o.ParameterOverrides, k, v));
    /// <inheritdoc cref="AwsSamLocalInvokeSettings.ParameterOverrides"/>
    [Pure] [Builder(Type = typeof(AwsSamLocalInvokeSettings), Property = nameof(AwsSamLocalInvokeSettings.ParameterOverrides))]
    public static T RemoveParameterOverride<T>(this T o, string k) where T : AwsSamLocalInvokeSettings => o.Modify(b => b.RemoveDictionary(() => o.ParameterOverrides, k));
    /// <inheritdoc cref="AwsSamLocalInvokeSettings.ParameterOverrides"/>
    [Pure] [Builder(Type = typeof(AwsSamLocalInvokeSettings), Property = nameof(AwsSamLocalInvokeSettings.ParameterOverrides))]
    public static T ClearParameterOverrides<T>(this T o) where T : AwsSamLocalInvokeSettings => o.Modify(b => b.ClearDictionary(() => o.ParameterOverrides));
    #endregion
}
#endregion
