namespace Anarchy.Toolkit.Web.MockServer.Entity;

/// <summary>
/// A consumer middleware plugged into the mock server, in pipeline order, as returned by <c>GET /__mock/middlewares</c>.
/// </summary>
/// <param name="Order">Its position, starting at 1.</param>
/// <param name="Name">Its name: the given one, the middleware type or the delegate's method.</param>
/// <param name="Kind"><c>middleware</c> (a class), <c>delegate</c> or <c>pipeline</c> (raw <c>Configure</c>).</param>
public sealed record MockServerMiddleware(int Order, string Name, string Kind);
