using System.Runtime.CompilerServices;

namespace Anarchy.Toolkit.Tests.Unit;

internal static class TestLoggingInitializer
{
    // Hosts built with Host.CreateDefaultBuilder (e.g. the self-hosted health API) read logging configuration from
    // environment variables; this keeps their console logger from flooding the test output.
    [ModuleInitializer]
    internal static void Initialize() => Environment.SetEnvironmentVariable("Logging__LogLevel__Default", "Warning");
}
