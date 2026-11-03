using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;

public record DbConnectionSettings
{
    [Required] public Uri HostReader { get; init; } = default!;
    [Required] public Uri HostWriter { get; init; } = default!;
    [Required] public uint? Port { get; init; }
    [Required] public string Username { get; init; } = default!;
    [Required] public string Password { get; init; } = default!;
    [Required] public string DbName { get; init; } = default!;
    [Required] public string TimeZone { get; init; } = default!;
}

public interface IPostgresAppSettings : IAppSettings
{
    [Required] public DbConnectionSettings Postgres { get; init; }
}

public interface IOracleAppSettings : IAppSettings
{
    [Required] public DbConnectionSettings Oracle { get; init; }
}
