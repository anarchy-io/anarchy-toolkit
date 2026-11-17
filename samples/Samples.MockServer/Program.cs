using Anarchy.Toolkit.Samples.MockServer;
using Anarchy.Toolkit.Web.MockServer;

// The default expectations ship next to the assembly; --expectations adds more (e.g. per test suite).
return await MockServerEntryPoint.RunAsync(args, server => server
    .AddExpectationsFrom(Path.Combine(AppContext.BaseDirectory, "mocks"))
    .UseMiddleware<RequireBearerTokenMiddleware>()
    .Use(StockMiddleware.InvokeAsync));
