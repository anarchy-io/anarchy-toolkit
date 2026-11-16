namespace Anarchy.Toolkit.Web.Presentation.Attribute;

/// <summary>
/// Marks a controller action whose exceptions must not be handled by
/// <see cref="Anarchy.Toolkit.Web.Service.Host.Middleware.ExceptionMiddleware"/>, letting them propagate (or be
/// handled by the action itself).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class IgnoreExceptionHandlingAttribute : System.Attribute
{
}
