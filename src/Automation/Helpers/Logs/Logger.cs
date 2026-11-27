namespace Anarchy.Toolkit.Automation.Helpers.Logs;

/// <summary>
/// A wrapper around <see cref="Serilog.Log"/> used by the build components: <c>Normal</c> logs at
/// <c>Debug</c> level and <c>Trace</c> at <c>Verbose</c>; the others map to their Serilog namesakes.
/// </summary>
public static class Logger
{
    /// <summary>
    /// Writes a message template at <c>Debug</c> level.
    /// </summary>
    /// <param name="format">The Serilog message template.</param>
    /// <param name="args">The template's property values.</param>
    public static void Normal(string format, params object[] args)
    {
        Serilog.Log.Debug(format, args);
    }

    /// <summary>
    /// Writes a value at <c>Debug</c> level, as the <c>Object</c> property.
    /// </summary>
    /// <param name="value">The value to log.</param>
    public static void Normal(object value)
    {
        Serilog.Log.Debug("{Object}", value);
    }

    /// <summary>
    /// Writes a text at <c>Debug</c> level; an empty line by default.
    /// </summary>
    /// <param name="text">The text, used as the message template.</param>
    public static void Normal(string? text = null)
    {
        Serilog.Log.Debug(text ?? "");
    }

    /// <summary>
    /// Writes a message template at <c>Verbose</c> level.
    /// </summary>
    /// <param name="format">The Serilog message template.</param>
    /// <param name="args">The template's property values.</param>
    public static void Trace(string format, params object[] args)
    {
        Serilog.Log.Verbose(format, args);
    }

    /// <summary>
    /// Writes a value at <c>Verbose</c> level, as the <c>Object</c> property.
    /// </summary>
    /// <param name="value">The value to log.</param>
    public static void Trace(object value)
    {
        Serilog.Log.Verbose("{Object}", value);
    }

    /// <summary>
    /// Writes a text at <c>Verbose</c> level; an empty line by default.
    /// </summary>
    /// <param name="text">The text, used as the message template.</param>
    public static void Trace(string? text = null)
    {
        Serilog.Log.Verbose(text ?? "");
    }

    /// <summary>
    /// Writes a message template at <c>Information</c> level.
    /// </summary>
    /// <param name="format">The Serilog message template.</param>
    /// <param name="args">The template's property values.</param>
    public static void Info(string format, params object[] args)
    {
        Serilog.Log.Information(format, args);
    }

    /// <summary>
    /// Writes a value at <c>Information</c> level, as the <c>Object</c> property.
    /// </summary>
    /// <param name="value">The value to log.</param>
    public static void Info(object value)
    {
        Serilog.Log.Information("{Object}", value);
    }

    /// <summary>
    /// Writes a text at <c>Information</c> level; an empty line by default.
    /// </summary>
    /// <param name="text">The text, used as the message template.</param>
    public static void Info(string? text = null)
    {
        Serilog.Log.Information(text ?? "");
    }

    /// <summary>
    /// Writes a message template at <c>Warning</c> level.
    /// </summary>
    /// <param name="format">The Serilog message template.</param>
    /// <param name="args">The template's property values.</param>
    public static void Warn(string format, params object[] args)
    {
        Serilog.Log.Warning(format, args);
    }

    /// <summary>
    /// Writes a value at <c>Warning</c> level, as the <c>Object</c> property.
    /// </summary>
    /// <param name="value">The value to log.</param>
    public static void Warn(object value)
    {
        Serilog.Log.Warning("{Object}", value);
    }

    /// <summary>
    /// Writes a text at <c>Warning</c> level; an empty line by default.
    /// </summary>
    /// <param name="text">The text, used as the message template.</param>
    public static void Warn(string? text = null)
    {
        Serilog.Log.Warning(text ?? "");
    }

    /// <summary>
    /// Writes an exception at <c>Warning</c> level, with its message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    public static void Warn(Exception exception)
    {
        Serilog.Log.Warning(exception, exception.Message);
    }

    /// <summary>
    /// Writes a message template at <c>Error</c> level.
    /// </summary>
    /// <param name="format">The Serilog message template.</param>
    /// <param name="args">The template's property values.</param>
    public static void Error(string format, params object[] args)
    {
        Serilog.Log.Error(format, args);
    }

    /// <summary>
    /// Writes a value at <c>Error</c> level, as the <c>Object</c> property.
    /// </summary>
    /// <param name="value">The value to log.</param>
    public static void Error(object value)
    {
        Serilog.Log.Error("{Object}", value);
    }

    /// <summary>
    /// Writes a text at <c>Error</c> level; an empty line by default.
    /// </summary>
    /// <param name="text">The text, used as the message template.</param>
    public static void Error(string? text = null)
    {
        Serilog.Log.Error(text ?? "");
    }

    /// <summary>
    /// Writes an exception at <c>Error</c> level, with its message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    public static void Error(Exception exception)
    {
        Serilog.Log.Error(exception, exception.Message);
    }
}
