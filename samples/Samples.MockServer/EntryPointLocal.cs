using Anarchy.Toolkit.Runtime.Web.MockServer;
using Anarchy.Toolkit.Runtime.Web.MockServer.Ui.Extensions.Runtime.Web.MockServer;
using Anarchy.Toolkit.Samples.MockServer.Middlewares;

namespace Anarchy.Toolkit.Samples.MockServer;

public static class EntryPointLocal
{
    public static async Task<int> Main(string[] args) => await MockServerEntryPoint
        .RunAsync(args, server => server
            .AddExpectationsFrom(Path.Combine(AppContext.BaseDirectory, "mocks"))
            .UseMiddleware<RequireBearerTokenMiddleware>()
            .Use(StockMiddleware.InvokeAsync, "stock")
            .UseUi()
        )
        ;
}
