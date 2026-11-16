using Anarchy.Toolkit.Core.Observability.Entity.Service;
using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
public partial class Observer
{
    #region ------------- ILogger.Debug --------------

    /// <summary>
    /// Writes a <c>Debug</c> log event whose template is <paramref name="prefix"/> followed by <paramref name="messageTemplate"/>.
    /// </summary>
    /// <param name="prefix">Text prepended to the message template (e.g. <c>[Class → Method]</c>).</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogDebug(string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Debug($"{prefix} {messageTemplate}", args);
    }

    /// <summary>
    /// Writes a <c>Debug</c> log event prefixed with the call site's <see cref="CallSiteEntity.ToLogPrefix"/>.
    /// </summary>
    /// <param name="callSite">The call site used to build the log prefix.</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogDebug(CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogDebug(callSite.ToLogPrefix(), messageTemplate, args);
    }

    #endregion

    #region ------------- ILogger.Information --------------

    /// <summary>
    /// Writes an <c>Information</c> log event whose template is <paramref name="prefix"/> followed by <paramref name="messageTemplate"/>.
    /// </summary>
    /// <param name="prefix">Text prepended to the message template (e.g. <c>[Class → Method]</c>).</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogInformation(string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Information($"{prefix} {messageTemplate}", args);
    }

    /// <summary>
    /// Writes an <c>Information</c> log event prefixed with the call site's <see cref="CallSiteEntity.ToLogPrefix"/>.
    /// </summary>
    /// <param name="callSite">The call site used to build the log prefix.</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogInformation(CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogInformation(callSite.ToLogPrefix(), messageTemplate, args);
    }

    #endregion

    #region ------------- ILogger.Warning --------------

    /// <summary>
    /// Writes a <c>Warning</c> log event whose template is <paramref name="prefix"/> followed by <paramref name="messageTemplate"/>.
    /// </summary>
    /// <param name="prefix">Text prepended to the message template (e.g. <c>[Class → Method]</c>).</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogWarning(string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Warning($"{prefix} {messageTemplate}", args);
    }

    /// <summary>
    /// Writes a <c>Warning</c> log event prefixed with the call site's <see cref="CallSiteEntity.ToLogPrefix"/>.
    /// </summary>
    /// <param name="callSite">The call site used to build the log prefix.</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogWarning(CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogWarning(callSite.ToLogPrefix(), messageTemplate, args);
    }

    /// <summary>
    /// Writes a <c>Warning</c> log event whose template is <paramref name="prefix"/> followed by <paramref name="messageTemplate"/>.
    /// </summary>
    /// <param name="exception">The exception associated with the event.</param>
    /// <param name="prefix">Text prepended to the message template (e.g. <c>[Class → Method]</c>).</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogWarning(Exception exception, string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Warning(exception, $"{prefix} {messageTemplate}", args);
    }

    /// <summary>
    /// Writes a <c>Warning</c> log event prefixed with the call site's <see cref="CallSiteEntity.ToLogPrefix"/>.
    /// </summary>
    /// <param name="exception">The exception associated with the event.</param>
    /// <param name="callSite">The call site used to build the log prefix.</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogWarning(Exception exception, CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogWarning(exception, callSite.ToLogPrefix(), messageTemplate, args);
    }

    #endregion

    #region ------------- ILogger.Error --------------

    /// <summary>
    /// Writes an <c>Error</c> log event whose template is <paramref name="prefix"/> followed by <paramref name="messageTemplate"/>.
    /// </summary>
    /// <param name="prefix">Text prepended to the message template (e.g. <c>[Class → Method]</c>).</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogError(string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Error($"{prefix} {messageTemplate}", args);
    }

    /// <summary>
    /// Writes an <c>Error</c> log event prefixed with the call site's <see cref="CallSiteEntity.ToLogPrefix"/>.
    /// </summary>
    /// <param name="callSite">The call site used to build the log prefix.</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogError(CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogError(callSite.ToLogPrefix(), messageTemplate, args);
    }

    /// <summary>
    /// Writes an <c>Error</c> log event whose template is <paramref name="prefix"/> followed by <paramref name="messageTemplate"/>.
    /// </summary>
    /// <param name="exception">The exception associated with the event.</param>
    /// <param name="prefix">Text prepended to the message template (e.g. <c>[Class → Method]</c>).</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogError(Exception exception, string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Error(exception, $"{prefix} {messageTemplate}", args);
    }

    /// <summary>
    /// Writes an <c>Error</c> log event prefixed with the call site's <see cref="CallSiteEntity.ToLogPrefix"/>.
    /// </summary>
    /// <param name="exception">The exception associated with the event.</param>
    /// <param name="callSite">The call site used to build the log prefix.</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogError(Exception exception, CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogError(exception, callSite.ToLogPrefix(), messageTemplate, args);
    }

    #endregion

    #region ------------- ILogger.Fatal --------------

    /// <summary>
    /// Writes a <c>Fatal</c> log event whose template is <paramref name="prefix"/> followed by <paramref name="messageTemplate"/>.
    /// </summary>
    /// <param name="prefix">Text prepended to the message template (e.g. <c>[Class → Method]</c>).</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogFatal(string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Fatal($"{prefix} {messageTemplate}", args);
    }

    /// <summary>
    /// Writes a <c>Fatal</c> log event prefixed with the call site's <see cref="CallSiteEntity.ToLogPrefix"/>.
    /// </summary>
    /// <param name="callSite">The call site used to build the log prefix.</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogFatal(CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogFatal(callSite.ToLogPrefix(), messageTemplate, args);
    }

    /// <summary>
    /// Writes a <c>Fatal</c> log event whose template is <paramref name="prefix"/> followed by <paramref name="messageTemplate"/>.
    /// </summary>
    /// <param name="exception">The exception associated with the event.</param>
    /// <param name="prefix">Text prepended to the message template (e.g. <c>[Class → Method]</c>).</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogFatal(Exception exception, string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Fatal(exception, $"{prefix} {messageTemplate}", args);
    }

    /// <summary>
    /// Writes a <c>Fatal</c> log event prefixed with the call site's <see cref="CallSiteEntity.ToLogPrefix"/>.
    /// </summary>
    /// <param name="exception">The exception associated with the event.</param>
    /// <param name="callSite">The call site used to build the log prefix.</param>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="args">Values for the template's property placeholders.</param>
    public void LogFatal(Exception exception, CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogFatal(exception, callSite.ToLogPrefix(), messageTemplate, args);
    }


    #endregion
}
