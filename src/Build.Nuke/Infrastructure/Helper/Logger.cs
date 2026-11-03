namespace Anarchy.Toolkit.Build.Nuke.Infrastructure.Helper
{
    /// <summary>
    /// A wrapper class around <see cref="Serilog.Log"/> that is used by NUKE
    /// <para>
    /// Previously NUKE had it's own Logger, but it's deprecated and soon will be removed in favor
    /// of <see cref="Serilog.Log"/>. So this wrapper is created (copy-pasted) for the ease of migration
    /// if they decide to change logger again :)
    /// </para>
    /// </summary>
    public static class Logger
    {
        public static void Normal(string format, params object[] args)
        {
            Serilog.Log.Debug(format, args);
        }

        public static void Normal(object value)
        {
            Serilog.Log.Debug("{Object}", value);
        }

        public static void Normal(string? text = null)
        {
            Serilog.Log.Debug(text ?? "");
        }

        public static void Trace(string format, params object[] args)
        {
            Serilog.Log.Verbose(format, args);
        }

        public static void Trace(object value)
        {
            Serilog.Log.Verbose("{Object}", value);
        }

        public static void Trace(string? text = null)
        {
            Serilog.Log.Verbose(text ?? "");
        }

        public static void Info(string format, params object[] args)
        {
            Serilog.Log.Information(format, args);
        }

        public static void Info(object value)
        {
            Serilog.Log.Information("{Object}", value);
        }

        public static void Info(string? text = null)
        {
            Serilog.Log.Information(text ?? "");
        }

        public static void Warn(string format, params object[] args)
        {
            Serilog.Log.Warning(format, args);
        }

        public static void Warn(object value)
        {
            Serilog.Log.Warning("{Object}", value);
        }

        public static void Warn(string? text = null)
        {
            Serilog.Log.Warning(text ?? "");
        }

        public static void Warn(Exception exception)
        {
            Serilog.Log.Warning(exception, exception.Message);
        }

        public static void Error(string format, params object[] args)
        {
            Serilog.Log.Error(format, args);
        }

        public static void Error(object value)
        {
            Serilog.Log.Error("{Object}", value);
        }

        public static void Error(string? text = null)
        {
            Serilog.Log.Error(text ?? "");
        }

        public static void Error(Exception exception)
        {
            Serilog.Log.Error(exception, exception.Message);
        }
    }
}
