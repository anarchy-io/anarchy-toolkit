using System.Net;

namespace Anarchy.Toolkit.Hosting.Workers.Entity.Http
{
    /// <summary>
    /// Results of an application health test formatted as JSON and the HTTP status code to use when returning it.
    /// </summary>
    public class ApplicationHealthHttpResponse
    {
        /// <summary>
        /// The results of the application health check formatted as JSON.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// The HTTP status code to return. This should be OK or ServiceUnavailable, depending on whether the overall
        /// application is healthy.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ApplicationHealthHttpResponse"/>.
        /// </summary>
        public ApplicationHealthHttpResponse(string content, HttpStatusCode statusCode)
        {
            Content = content;
            StatusCode = statusCode;
        }
    }
}
