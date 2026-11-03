using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Samples.Api.Infrastructure.Entity.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Anarchy.Toolkit.Samples.Api.Web.Presentation.Controller;

[Route("greetings")]
[Produces("application/json")]
public sealed class GreetingController(AppSettings settings, Observer observer) : ControllerBase
{
    [HttpGet("{name}")]
    [AllowAnonymous]
    public IActionResult Greet(string name)
    {
        var caller = this.CalledFrom();

        observer.LogInformation(caller, "Greeting {Name}", name);
        observer.ExecutionIncrementCount(caller);

        return Ok(new { message = $"{settings.Greeting}, {name}!" });
    }
}
