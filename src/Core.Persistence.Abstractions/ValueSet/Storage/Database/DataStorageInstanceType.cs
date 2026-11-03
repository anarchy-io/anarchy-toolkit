using System.ComponentModel;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;

/// <summary>
///     The type of connection that is available in the connection pool
/// </summary>
public enum DataStorageInstanceType
{
    [Description("writer")] Writer = 1,
    [Description("reader")] Reader = 2
}
