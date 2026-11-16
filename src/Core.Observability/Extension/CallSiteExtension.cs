using Anarchy.Toolkit.Core.Observability.Entity.Service;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Anarchy.Toolkit.Core.Observability.Extension;

/// <summary>
/// Extension members that capture a <see cref="CallSiteEntity"/> (class name + method name) for any object, used to
/// build consistent log prefixes and metric names through
/// <see cref="Anarchy.Toolkit.Core.Observability.Service.Observability.Observer"/>.
/// </summary>
/// <example>
/// <code language="csharp">
/// public async Task ProcessAsync()
/// {
///     var callSite = this.CalledFrom(); // e.g. ("OrderService", "ProcessAsync")
///     observer.LogInformation(callSite, "Processing started");
///     observer.ExecutionIncrementCount(callSite);
/// }
/// </code>
/// </example>
public static class CallSiteExtension
{
    /// <param name="caller">The object whose runtime type name is used as the class name of the call site.</param>
    extension(object caller)
    {
        /// <summary>
        /// Creates a <see cref="CallSiteEntity"/> for the calling method of <paramref name="caller"/>.
        /// </summary>
        /// <remarks>
        /// The class name is taken from <c>caller.GetType().Name</c> (the runtime type, so a derived type name is used
        /// when called from a base class). The method name is filled in by the compiler via
        /// <see cref="System.Runtime.CompilerServices.CallerMemberNameAttribute"/>.
        /// </remarks>
        /// <param name="methodName">The calling member name. Leave unset to let the compiler provide it.</param>
        /// <returns>A call site describing the caller's type and method.</returns>
        public CallSiteEntity CalledFrom([CallerMemberName] string methodName = "")
        {
            return new CallSiteEntity(caller.GetType().Name, methodName);
        }

        /// <summary>
        /// Creates a <see cref="CallSiteEntity"/> describing the method that called the method invoking
        /// <see cref="ParentCaller"/>, by inspecting the current stack trace.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="System.Diagnostics.StackTrace"/> frame index 2, so results can be affected by inlining,
        /// async state machines and compiler-generated methods; it is also comparatively expensive. Falls back to
        /// <c>"Unknown"</c> when the frame, method or declaring type cannot be resolved.
        /// </remarks>
        /// <returns>A call site for the parent caller, or one with <c>"Unknown"</c> parts when unresolved.</returns>
        public CallSiteEntity ParentCaller()
        {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(2);
            var method = frame?.GetMethod();
            var parentClassName = method?.DeclaringType?.Name ?? "Unknown";
            return new CallSiteEntity(parentClassName, method?.Name ?? "Unknown");
        }
    }
}
