using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Web.Extension.Service.Host.Controller.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Anarchy.Toolkit.Web.Extension.Microsoft.AspNetCore.Mvc;

public static class ControllerBaseExtensions
{
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
