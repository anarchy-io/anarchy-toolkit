using System.Net;

namespace Anarchy.Toolkit.Infrastructure.Http.Extension.Http;

/// <summary>
/// Classification helpers for <see cref="HttpStatusCode"/> values.
/// </summary>
public static class HttpStatusCodeExtensions
{
    /// <param name="this">The status code to classify.</param>
    extension(HttpStatusCode @this)
    {
        /// <summary>
        /// Determines whether the status code is in the 2xx range (200 to 299).
        /// </summary>
        /// <returns><see langword="true"/> for 2xx status codes; otherwise <see langword="false"/>.</returns>
        public bool Success()
        {
            return
                @this >= HttpStatusCode.OK &&
                @this < HttpStatusCode.Ambiguous;
        }

        /// <summary>
        /// Determines whether the status code is 2xx or <see cref="HttpStatusCode.Conflict"/> (409), useful for
        /// idempotent create operations where "already exists" is acceptable.
        /// </summary>
        /// <returns><see langword="true"/> for 2xx or 409; otherwise <see langword="false"/>.</returns>
        public bool SuccessOrConflict()
        {
            return @this.Success() || @this == HttpStatusCode.Conflict;
        }
    }
}
