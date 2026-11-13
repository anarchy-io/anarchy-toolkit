using Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;
using Fallout.Common.IO;
using Fallout.Common.Utilities;

namespace Anarchy.Toolkit.Build.Fallout.Infrastructure.Entity.UserSecrets
{
    public class DotNetUserSecretsProject
    {
        /// <summary>
        /// Project to set user secret for.
        /// </summary>
        public AbsolutePath ProjectFile { get; set; } = null!;

        /// <summary>
        /// A collection of user secrets to set.
        /// </summary>
        public Dictionary<string, DotNetUserSecretValue> UserSecrets { get; } = new Dictionary<string, DotNetUserSecretValue>();
    }

    public interface IPromptedUserSecretValue
    {
        void Prompt();
    }

    /// <summary>
    /// <para>
    /// Represents a plain-text user secret value. Used as-is.
    /// </para>
    /// <para>
    /// Can be implicitly converted from <see cref="string"/>
    /// </para>
    /// </summary>
    public class DotNetUserSecretValue
    {
        public virtual string Value { get; } = "";

        public virtual string LogValue => Value;

        public static implicit operator DotNetUserSecretValue(string value) => new DotNetUserSecretValue(value);

        protected DotNetUserSecretValue(string value) => Value = value;
        protected DotNetUserSecretValue() { }
    }

    /// <summary>
    /// Represents a user secret value obtained by prompting a user. For sensitive data like passwords use <see cref="PromptedSensitiveDotNetUserSecretValue"/>.
    /// </summary>
    public class PromptedDotNetUserSecretValue : DotNetUserSecretValue, IPromptedUserSecretValue
    {
        private string? _promptedValue;
        private readonly string _userSecretName;

        public PromptedDotNetUserSecretValue(string userSecretName) => _userSecretName = userSecretName;

        public override string Value => _promptedValue ?? "";

        public void Prompt() => _promptedValue ??= ConsoleUtility.PromptForInput($"Please enter {_userSecretName}:", defaultValue: "");
    }

    /// <summary>
    /// Represents a user secret value obtained by prompting a user with masked input.
    /// </summary>
    public class PromptedSensitiveDotNetUserSecretValue : DotNetUserSecretValue, IPromptedUserSecretValue
    {
        private string? _promptedValue;
        private readonly string _userSecretName;

        public PromptedSensitiveDotNetUserSecretValue(string userSecretName) => _userSecretName = userSecretName;

        public override string Value => _promptedValue ?? "";

        public override string LogValue => new string('*', _promptedValue?.Length ?? 0);

        public void Prompt()
        {
            if (_promptedValue != null) return;

            Console.WriteLine($"Please enter {_userSecretName}:");
            _promptedValue = ConsoleUtility.ReadSecret();
        }
    }

    /// <summary>
    /// Represents an ADO.NET Oracle connection string user secret value with password prompted to a user with masked input.
    /// </summary>
    public class OracleConnectionStringUserSecretValue : DotNetUserSecretValue, IPromptedUserSecretValue
    {
        private readonly string _host;
        private readonly string _username;
        private readonly PromptedSensitiveDotNetUserSecretValue _password;

        private string? _connectionString;

        public OracleConnectionStringUserSecretValue(string host, string username, PromptedSensitiveDotNetUserSecretValue? password = null)
        {
            _host = host;
            _username = username;

            _password = password ?? new PromptedSensitiveDotNetUserSecretValue($"Oracle password (user: \"{username}\"; host: \"{host}\")");
        }

        public override string Value => _connectionString ??= OracleConnectionStringUtility.GetOracleConnectionString(_host, _username, _password.Value);

        public override string LogValue => OracleConnectionStringUtility.GetOracleConnectionString(_host, _username, _password.LogValue);

        public void Prompt() => _password.Prompt();
    }

    /// <summary>
    /// Represents an ADO.NET MySQL connecting string user sercret value with password prompted to a user with masked input.
    /// </summary>
    public class MySqlConnectionStringUserSecretValue : DotNetUserSecretValue, IPromptedUserSecretValue
    {
        private readonly string _server;
        private readonly int _port;
        private readonly string _database;
        private readonly string _username;
        private readonly PromptedSensitiveDotNetUserSecretValue _password;

        private string? _connectionString;

        public MySqlConnectionStringUserSecretValue(
            string server,
            string database,
            string username,
            PromptedSensitiveDotNetUserSecretValue? password = null,
            int port = MySqlConnectionStringUtility.DefaultPort
        )
        {
            _server = server;
            _port = port;
            _database = database;
            _username = username;

            _password = password ?? new PromptedSensitiveDotNetUserSecretValue($"MySQL password (user: \"{username}\"; server: \"{server}\")");
        }

        public override string Value => _connectionString ??= MySqlConnectionStringUtility.GetMySqlConnectionString(_server, _database, _username, _password.Value, _port);

        public override string LogValue => MySqlConnectionStringUtility.GetMySqlConnectionString(_server, _database, _username, _password.LogValue, _port);

        public void Prompt() => _password.Prompt();
    }
}
