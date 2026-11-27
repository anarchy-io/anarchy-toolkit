using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.System;
using Anarchy.Toolkit.Runtime.Web.Service.Host.Controller.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Anarchy.Toolkit.Runtime.Web.Extensions.Microsoft.AspNetCore.Mvc;

/// <summary>
/// Extension methods for <see cref="ControllerBase"/> that standardize common controller concerns such as
/// model-state validation.
/// </summary>
public static class ControllerBaseExtensions
{
    /// <summary>
    /// Returns a <c>400 Bad Request</c> result when the controller's model state is invalid, logging the
    /// validation errors together with the received input.
    /// </summary>
    /// <remarks>
    /// When <see cref="ControllerBase.ModelState"/> is invalid, a
    /// <see cref="Anarchy.Toolkit.Runtime.Web.Service.Host.Controller.Exceptions.ModelStateValidationException"/>
    /// is built (not thrown) and logged through <paramref name="telemetry"/>, and a
    /// <see cref="BadRequestObjectResult"/> carrying the model state is returned.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// [HttpPost]
    /// public IActionResult Create([FromBody] CreateInput input)
    /// {
    ///     if (this.BadRequestIfInvalid(input, telemetry) is { } badRequest)
    ///         return badRequest;
    ///
    ///     // ... handle valid input
    ///     return Ok();
    /// }
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the received input model.</typeparam>
    /// <param name="this">The controller whose model state is checked.</param>
    /// <param name="input">The received input, included in the logged error for diagnostics.</param>
    /// <param name="telemetry">Application telemetry used to log the validation failure; when <see langword="null"/>, nothing is logged.</param>
    /// <returns>
    /// A <see cref="BadRequestObjectResult"/> when the model state is invalid; otherwise <see langword="null"/>.
    /// </returns>
    public static ActionResult? BadRequestIfInvalid<T>(this ControllerBase @this, T input, AppTelemetry? telemetry)
    {
        if (@this.ModelState.IsValid)
        {
            return null;
        }

        telemetry?.Log(
            LogLevel.Error,
            @this.CalledFrom(),
            new ModelStateValidationException(@this.ModelState, input),
            "Bad Request");

        return @this.BadRequest(@this.ModelState);

    }
}
