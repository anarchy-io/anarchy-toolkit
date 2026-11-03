namespace Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;

/// <summary>
/// Base contract of the toolkit's application settings. Each settings section exposes its own capability
/// interface deriving from this one (for example <c>IAwsAppSettings</c> or <c>IPostgresAppSettings</c>), so
/// consumers require only the sections they actually read.
/// </summary>
public interface IAppSettings;
