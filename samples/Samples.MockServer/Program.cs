using Anarchy.Toolkit.Samples.MockServer;
using Anarchy.Toolkit.Web.MockServer;
using Anarchy.Toolkit.Web.MockServer.Ui.Extension;

// The default expectations ship next to the assembly; --expectations adds more (e.g. per test suite).
// The UI is served at /__mock/ui unless --no-ui is given.
return await MockServerEntryPoint.RunAsync(args, server => server
    .AddExpectationsFrom(Path.Combine(AppContext.BaseDirectory, "mocks"))
    .UseMiddleware<RequireBearerTokenMiddleware>()
    .Use(StockMiddleware.InvokeAsync, "stock")
    .UseUi());
