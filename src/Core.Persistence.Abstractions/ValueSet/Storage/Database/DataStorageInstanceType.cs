using System.ComponentModel;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;

/// <summary>
///     The type of connection that is available in the connection pool
/// </summary>
public enum DataStorageInstanceType
{
    /// <summary>A read/write (primary) instance (<c>writer</c>).</summary>
    [Description("writer")] Writer = 1,

    /// <summary>A read-only (replica) instance (<c>reader</c>).</summary>
    [Description("reader")] Reader = 2
}
