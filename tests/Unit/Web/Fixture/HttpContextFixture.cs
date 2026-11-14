using Anarchy.Toolkit.Web.Presentation.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Anarchy.Toolkit.Tests.Unit.Web.Fixture;

// Note: when using the DefaultHttpContext, don't forget to dispose the context.Response.Body
public static class HttpContextFixture
{
    public static DefaultHttpContext RetrieveIgnoredContext()
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(null,
            new EndpointMetadataCollection(new ControllerActionDescriptor {
                MethodInfo = typeof(TestController).GetMethod(nameof(TestController.TestMethod)) ??
                             throw new InvalidOperationException()
            }), null));
        context.Response.Body = new MemoryStream(); // dispose this
        return context;
    }

    public static DefaultHttpContext RetrieveContext()
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(null,
            new EndpointMetadataCollection(new ControllerActionDescriptor {
                MethodInfo = typeof(TestController2).GetMethod(nameof(TestController2.TestMethod)) ??
                             throw new InvalidOperationException()
            }), null));
        context.Response.Body = new MemoryStream(); // dispose this
        return context;
    }

    private class TestController
    {
        [IgnoreExceptionHandling]
        public static void TestMethod()
        {
        }
    }

    private class TestController2
    {
        public static void TestMethod()
        {
        }
    }
}
