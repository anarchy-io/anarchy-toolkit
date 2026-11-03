using Newtonsoft.Json;
using System.Net;

namespace Anarchy.Toolkit.Infrastructure.Http.Extension.Http;

public static class HttpResponseMessageExtensions
{
    extension(HttpResponseMessage @this)
    {
        /// <summary>
        /// Shortcut to read content as a type
        /// </summary>
        public async Task<TApiModel?> ReadContentAsync<TApiModel>(CancellationToken cancellationToken = default)
        {
            var content = await @this.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<TApiModel>(content);
        }

        /// <summary>
        /// Shortcut to read content as a type with the option to pass a serializer settings
        /// </summary>
        public async Task<TApiModel?> ReadContentAsync<TApiModel>(JsonSerializerSettings settings,
                                                                  CancellationToken cancellationToken = default)
        {
            var content = await @this.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<TApiModel>(content, settings);
        }

        /// <summary>
        /// Provides custom exception for non success status code failures
        /// </summary>
        public HttpResponseMessage EnsureSuccessStatusCode(Func<HttpStatusCode, Exception> errorFunc)
        {
            if (@this.IsSuccessStatusCode)
                return @this;

            throw errorFunc(@this.StatusCode);
        }
    }
}
