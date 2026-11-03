using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;

/// <summary>
///     Callback function type that can be used in policies to handle operations in a function provided by the user
/// </summary>
public delegate Task<TResult> StoragePolicyCallback<TStorage, TResult>(
    StorageConnectionEnvelopeBase<TStorage> dataStorage,
    CancellationToken? cancellationToken = null);

/// <summary>Interface of a policy to execute an operation on a database</summary>
public interface IStoragePolicy<TStorage>
{
    /// <summary>
    ///     Execute a database operation in a custom function provided by the user. The callback function will return
    ///     the active connection where database operation can be executed on.
    /// </summary>
    /// <param name="callback">The callback function that is provided by the user that will be executed</param>
    /// <param name="operationName">Optional name of the operation that is executed, that will be used for observability</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Database result mapped to the provided type</returns>
    /// <exception cref="DsPolicyExecutionException">Thrown when executing the callback function failed</exception>
    Task<TResult> ExecuteAsync<TResult>(StoragePolicyCallback<TStorage, TResult> callback, string? operationName = null,
                                        CancellationToken? cancellationToken = null);
}
