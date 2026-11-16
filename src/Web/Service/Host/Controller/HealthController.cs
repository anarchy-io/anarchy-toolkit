using Anarchy.Toolkit.Core.Health.Entity.Api.Output;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Web.Presentation.Entity.Output.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Anarchy.Toolkit.Web.Service.Host.Controller;

/// <summary>
/// Controller exposing the dependency health check endpoint (<c>GET /health/services</c>).
/// </summary>
/// <remarks>
/// Runs every registered <see cref="Anarchy.Toolkit.Core.Health.Service.Health.IServiceHealthReport"/> and returns a
/// <see cref="HealthCheckReportOutput"/>. Anonymous access is allowed.
/// </remarks>
/// <param name="reports">The health reports to run.</param>
/// <param name="observer">The observer used for logging and metrics.</param>
[Route("health")]
[Produces("application/json")]
public class HealthController(
    IReadOnlyCollection<IServiceHealthReport> reports,
    Observer observer) : ControllerBase
{
    /// <summary>Runs all registered health checks.</summary>
    /// <param name="cancellationToken">Token to cancel the checks.</param>
    /// <returns>
    /// <c>200 OK</c> with the report when all checks pass; <c>500</c> with the report when any check fails; or a
    /// <c>500</c> problem details response when running the checks throws.
    /// </returns>
    [Route("services")]
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HealthCheckReportOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthCheckReportOutput), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckServicesAsync(CancellationToken cancellationToken = default)
    {
        var caller = this.CalledFrom();
        var timer = Stopwatch.StartNew();

        try
        {
            var report = await HealthCheckReport.RunAsync(reports, cancellationToken);
            var response = HealthCheckReportOutput.FromDto(report);

            observer.ExecutionRecordDuration(caller, timer.Elapsed, response.Passed ? Tags.Success : Tags.Failure);

            return response.Passed
                ? Ok(response)
                : new ObjectResult(response) { StatusCode = StatusCodes.Status500InternalServerError };
        }
        catch (Exception ex)
        {
            observer.ExecutionRecordDuration(caller, timer.Elapsed, Tags.Failure);
            observer.LogError(ex, caller, "Failed to check health for registered services");
            return Problem(ex.Message);
        }
    }
}
