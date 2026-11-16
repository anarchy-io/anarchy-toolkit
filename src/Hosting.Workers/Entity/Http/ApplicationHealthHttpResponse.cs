using System.Net;

namespace Anarchy.Toolkit.Hosting.Workers.Entity.Http;

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
    /// The HTTP status code to return, depending on whether the overall application is healthy
    /// (<see cref="Anarchy.Toolkit.Hosting.Workers.Service.Health.ApplicationHealthApi"/> uses OK or InternalServerError).
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Creates a new instance of <see cref="ApplicationHealthHttpResponse"/>.
    /// </summary>
    /// <param name="content">The health results formatted as JSON.</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
    public ApplicationHealthHttpResponse(string content, HttpStatusCode statusCode)
    {
        Content = content;
        StatusCode = statusCode;
    }
}
