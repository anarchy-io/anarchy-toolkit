using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Web.Extension.Service.Host.Controller.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Anarchy.Toolkit.Web.Extension.Microsoft.AspNetCore.Mvc;

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
    /// <see cref="Anarchy.Toolkit.Web.Extension.Service.Host.Controller.Exceptions.ModelStateValidationException"/>
    /// is built (not thrown) and logged through <paramref name="observer"/>, and a
    /// <see cref="BadRequestObjectResult"/> carrying the model state is returned.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// [HttpPost]
    /// public IActionResult Create([FromBody] CreateInput input)
    /// {
    ///     if (this.BadRequestIfInvalid(input, observer) is { } badRequest)
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
    /// <param name="observer">The observer used to log the validation failure; when <see langword="null"/>, nothing is logged.</param>
    /// <returns>
    /// A <see cref="BadRequestObjectResult"/> when the model state is invalid; otherwise <see langword="null"/>.
    /// </returns>
    public static ActionResult? BadRequestIfInvalid<T>(this ControllerBase @this, T input, Observer? observer)
    {
        if (@this.ModelState.IsValid)
        {
            return null;
        }

        observer?.LogError(
            new ModelStateValidationException(@this.ModelState, input),
            @this.CalledFrom(),
            "Bad Request");

        return @this.BadRequest(@this.ModelState);

    }
}
