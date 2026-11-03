using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

/// <summary>
///     Builds a provider connection string from <typeparamref name="TOptions" />, selecting the reader or writer host
///     based on the requested <see cref="DataStorageInstanceType" />. Providers supply the key/value pairs.
/// </summary>
public abstract class DbConnectionStringBase<TOptions>(TOptions options)
    where TOptions : DbConnectionOptions
{
    protected readonly TOptions Options = options;

    protected virtual string ConfigError => "Invalid database connection settings";

    protected virtual void CheckConnectionSettings()
    {
        Options.HostReader.ToString().CheckNotEmpty(nameof(Options.HostReader), ConfigError);
        Options.HostWriter.ToString().CheckNotEmpty(nameof(Options.HostWriter), ConfigError);
        Options.Username.CheckNotEmpty(nameof(Options.Username), ConfigError);
        Options.Password.CheckNotEmpty(nameof(Options.Password), ConfigError);
        Options.DbName.CheckNotEmpty(nameof(Options.DbName), ConfigError);
    }

    protected Uri Host(DataStorageInstanceType instanceType)
    {
        return instanceType switch {
            DataStorageInstanceType.Reader => Options.HostReader,
            DataStorageInstanceType.Writer => Options.HostWriter,
            _ => throw new UndefinedEnumValueException(nameof(instanceType), instanceType)
        };
    }

    /// <summary>Ordered connection string key/value pairs for the given host.</summary>
    protected abstract IEnumerable<KeyValuePair<string, object>> ConnectionSettings(Uri host);

    public string Value(DataStorageInstanceType instanceType)
    {
        CheckConnectionSettings();

        return string.Join(";", ConnectionSettings(Host(instanceType)).Select(kvp => $"{kvp.Key}={kvp.Value}"));
    }
}
