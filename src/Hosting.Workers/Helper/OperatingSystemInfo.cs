using System.Runtime.InteropServices;

namespace Anarchy.Toolkit.Hosting.Workers.Helper;

/// <summary>
/// Provides utilities for describing the current operating system.
/// </summary>
public static class OperatingSystemInfo
{
    /// <summary>
    /// Returns a description of the operating system hosting the application. This can either be
    /// "Linux", "OSX", "Windows", or "Unknown".
    /// </summary>
    public static string Current {
        get {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return nameof(OSPlatform.Linux);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return nameof(OSPlatform.OSX);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return nameof(OSPlatform.Windows);

            return "Unknown";
        }
    }
}
