using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

/// <summary>
///     Builds a provider connection string from <typeparamref name="TOptions" />, selecting the reader or writer host
///     based on the requested <see cref="DataStorageInstanceType" />. Providers supply the key/value pairs.
/// </summary>
/// <typeparam name="TOptions">Provider options type, derived from <see cref="DbConnectionOptions" />.</typeparam>
/// <param name="options">Connection options the connection string is built from.</param>
/// <remarks>
///     The resulting string is the provider key/value pairs joined as <c>key=value</c> with <c>;</c>. Settings are
///     validated (see <see cref="CheckConnectionSettings" />) every time <see cref="Value" /> is called.
/// </remarks>
/// <example>
///     <code language="csharp">
///     public sealed class MyConnectionString(DbConnectionOptions options)
///         : DbConnectionStringBase&lt;DbConnectionOptions&gt;(options)
///     {
///         protected override IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt; ConnectionSettings(Uri host) =>
///         [
///             new("Host", host.Host),
///             new("Database", Options.DbName),
///             new("Username", Options.Username),
///             new("Password", Options.Password),
///         ];
///     }
///
///     var writer = new MyConnectionString(options).Value(DataStorageInstanceType.Writer);
///     </code>
/// </example>
public abstract class DbConnectionStringBase<TOptions>(TOptions options)
    where TOptions : DbConnectionOptions
{
    /// <summary>The connection options supplied to the constructor.</summary>
    protected readonly TOptions Options = options;

    /// <summary>Message used for the validation errors raised by <see cref="CheckConnectionSettings" />.</summary>
    protected virtual string ConfigError => "Invalid database connection settings";

    /// <summary>
    ///     Validates that the hosts, user name, password and database name are not empty. Providers may override
    ///     it to validate additional settings.
    /// </summary>
    protected virtual void CheckConnectionSettings()
    {
        Options.HostReader.ToString().CheckNotEmpty(nameof(Options.HostReader), ConfigError);
        Options.HostWriter.ToString().CheckNotEmpty(nameof(Options.HostWriter), ConfigError);
        Options.Username.CheckNotEmpty(nameof(Options.Username), ConfigError);
        Options.Password.CheckNotEmpty(nameof(Options.Password), ConfigError);
        Options.DbName.CheckNotEmpty(nameof(Options.DbName), ConfigError);
    }

    /// <summary>Selects the host to connect to for the given instance type.</summary>
    /// <param name="instanceType">Whether a reader or writer connection is requested.</param>
    /// <returns><see cref="DbConnectionOptions.HostReader" /> or <see cref="DbConnectionOptions.HostWriter" />.</returns>
    /// <exception cref="UndefinedEnumValueException">
    ///     Thrown when <paramref name="instanceType" /> is not a defined value.
    /// </exception>
    protected Uri Host(DataStorageInstanceType instanceType)
    {
        return instanceType switch {
            DataStorageInstanceType.Reader => Options.HostReader,
            DataStorageInstanceType.Writer => Options.HostWriter,
            _ => throw new UndefinedEnumValueException(nameof(instanceType), instanceType)
        };
    }

    /// <summary>Ordered connection string key/value pairs for the given host.</summary>
    /// <param name="host">The reader or writer host selected by <see cref="Host" />.</param>
    /// <returns>The key/value pairs, in the order they should appear in the connection string.</returns>
    protected abstract IEnumerable<KeyValuePair<string, object>> ConnectionSettings(Uri host);

    /// <summary>Validates the settings and builds the connection string for the given instance type.</summary>
    /// <param name="instanceType">Whether the connection string targets the reader or the writer host.</param>
    /// <returns>The provider connection string.</returns>
    /// <exception cref="UndefinedEnumValueException">
    ///     Thrown when <paramref name="instanceType" /> is not a defined value.
    /// </exception>
    public string Value(DataStorageInstanceType instanceType)
    {
        CheckConnectionSettings();

        return string.Join(";", ConnectionSettings(Host(instanceType)).Select(kvp => $"{kvp.Key}={kvp.Value}"));
    }
}
