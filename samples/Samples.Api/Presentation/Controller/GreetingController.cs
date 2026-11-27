using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.System;
using Anarchy.Toolkit.Samples.Api.Infrastructure.Entity.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Anarchy.Toolkit.Samples.Api.Presentation.Controller;

[Route("greetings")]
[Produces("application/json")]
public sealed class GreetingController(AppSettings settings, AppTelemetry telemetry) : ControllerBase
{
    [HttpGet("{name}")]
    [AllowAnonymous]
    public IActionResult Greet(string name)
    {
        var caller = this.CalledFrom();

        telemetry.LogInformation(caller, "Greeting {Name}", name);
        telemetry.IncrementCounter(caller, MetricSuffixes.ExecutionCount);

        return Ok(new { message = $"{settings.Greeting}, {name}!" });
    }
}
