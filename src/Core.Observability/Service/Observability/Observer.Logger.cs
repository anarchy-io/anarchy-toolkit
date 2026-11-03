using Anarchy.Toolkit.Core.Observability.Entity.Service;
using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
public partial class Observer
{
    #region ------------- ILogger.Debug --------------

    public void LogDebug(string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Debug($"{prefix} {messageTemplate}", args);
    }

    public void LogDebug(CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogDebug(callSite.ToLogPrefix(), messageTemplate, args);
    }

    #endregion

    #region ------------- ILogger.Information --------------

    public void LogInformation(string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Information($"{prefix} {messageTemplate}", args);
    }

    public void LogInformation(CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogInformation(callSite.ToLogPrefix(), messageTemplate, args);
    }

    #endregion

    #region ------------- ILogger.Warning --------------

    public void LogWarning(string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Warning($"{prefix} {messageTemplate}", args);
    }

    public void LogWarning(CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogWarning(callSite.ToLogPrefix(), messageTemplate, args);
    }

    public void LogWarning(Exception exception, string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Warning(exception, $"{prefix} {messageTemplate}", args);
    }

    public void LogWarning(Exception exception, CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogWarning(exception, callSite.ToLogPrefix(), messageTemplate, args);
    }

    #endregion

    #region ------------- ILogger.Error --------------

    public void LogError(string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Error($"{prefix} {messageTemplate}", args);
    }

    public void LogError(CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogError(callSite.ToLogPrefix(), messageTemplate, args);
    }

    public void LogError(Exception exception, string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Error(exception, $"{prefix} {messageTemplate}", args);
    }

    public void LogError(Exception exception, CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogError(exception, callSite.ToLogPrefix(), messageTemplate, args);
    }

    #endregion

    #region ------------- ILogger.Fatal --------------

    public void LogFatal(string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Fatal($"{prefix} {messageTemplate}", args);
    }

    public void LogFatal(CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogFatal(callSite.ToLogPrefix(), messageTemplate, args);
    }

    public void LogFatal(Exception exception, string prefix, string messageTemplate, params object?[] args)
    {
        Logger.Fatal(exception, $"{prefix} {messageTemplate}", args);
    }

    public void LogFatal(Exception exception, CallSiteEntity callSite, string messageTemplate, params object?[] args)
    {
        LogFatal(exception, callSite.ToLogPrefix(), messageTemplate, args);
    }


    #endregion
}
