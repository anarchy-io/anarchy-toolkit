using System.Net;

namespace Anarchy.Toolkit.Infrastructure.Http.Extension.Http;

public static class HttpStatusCodeExtensions
{
    extension(HttpStatusCode @this)
    {
        public bool Success()
        {
            return
                @this >= HttpStatusCode.OK &&
                @this < HttpStatusCode.Ambiguous;
        }

        public bool SuccessOrConflict()
        {
            return @this.Success() || @this == HttpStatusCode.Conflict;
        }
    }
}
