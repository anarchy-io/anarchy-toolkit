using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Storage.Database;

/// <summary>
/// Database connection settings with separate reader and writer hosts. All properties are required.
/// </summary>
public record DbConnectionSettings
{
    /// <summary>The host used for read-only connections.</summary>
    [Required] public Uri HostReader { get; init; } = default!;

    /// <summary>The host used for read/write connections.</summary>
    [Required] public Uri HostWriter { get; init; } = default!;

    /// <summary>The database port.</summary>
    [Required] public uint? Port { get; init; }

    /// <summary>The database user name.</summary>
    [Required] public string Username { get; init; } = default!;

    /// <summary>The database password.</summary>
    [Required] public string Password { get; init; } = default!;

    /// <summary>The database name.</summary>
    [Required] public string DbName { get; init; } = default!;

    /// <summary>The time zone of the database session.</summary>
    [Required] public string TimeZone { get; init; } = default!;
}

/// <summary>
/// Capability interface for application settings that contain a <c>Postgres</c> section.
/// </summary>
public interface IPostgresAppSettings : IAppSettings
{
    /// <summary>The PostgreSQL connection settings (configuration section <c>Postgres</c>). Required.</summary>
    [Required] public DbConnectionSettings Postgres { get; init; }
}

/// <summary>
/// Capability interface for application settings that contain an <c>Oracle</c> section.
/// </summary>
public interface IOracleAppSettings : IAppSettings
{
    /// <summary>The Oracle connection settings (configuration section <c>Oracle</c>). Required.</summary>
    [Required] public DbConnectionSettings Oracle { get; init; }
}
