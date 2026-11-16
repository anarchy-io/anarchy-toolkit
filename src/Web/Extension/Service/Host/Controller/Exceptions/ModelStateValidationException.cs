using Anarchy.Toolkit.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections;
using System.Text;

namespace Anarchy.Toolkit.Web.Extension.Service.Host.Controller.Exceptions;

/// <summary>
/// Describes an invalid MVC model state, capturing the validation errors and (optionally) the received model so
/// both can be written to logs.
/// </summary>
/// <remarks>
/// Used by <see cref="Anarchy.Toolkit.Web.Extension.Microsoft.AspNetCore.Mvc.ControllerBaseExtensions.BadRequestIfInvalid{T}"/>
/// to log bad requests. The detailed information is exposed through <see cref="ToString"/>.
/// </remarks>
/// <param name="modelState">The invalid model state whose errors are reported.</param>
/// <param name="model">The received model; its public properties are listed in <see cref="ToString"/>. Optional.</param>
public class ModelStateValidationException(ModelStateDictionary modelState, object? model = null)
    : InvalidArgumentException
{
    private SerializableError Errors { get; } = new(modelState);

    private Dictionary<string, object?>? Model { get; } = model?.GetType()
        .GetProperties()
        .Select(p => new { p.Name, Value = p.GetValue(model) })
        .ToDictionary(p => p.Name, p => p.Value);

    /// <summary>
    /// Returns the base exception text followed by a "Validation Errors" section listing each invalid key and its
    /// messages and, when a model was supplied, a "Received Model" section listing its property values.
    /// </summary>
    /// <returns>A multi-line diagnostic description of the validation failure.</returns>
    public override string ToString()
    {
        var sb = new StringBuilder(base.ToString());

        if (Errors.Count <= 0)
        {
            return sb.ToString();
        }

        sb.AppendLine().AppendLine("Validation Errors:");
        foreach (var error in Errors)
        {
            sb.AppendLine($"↳ {error.Key}: {string.Join(", ", error.Value as string[] ?? [error.Value.ToString()!])}");
        }

        if (Model is null)
        {
            return sb.ToString();
        }

        sb.AppendLine().AppendLine("Received Model:");
        foreach (var value in Model)
        {
            sb.AppendLine($"↳ {value.Key}: {FormatValue(value.Value)}");
        }

        return sb.ToString();
    }

    private static string FormatValue(object? value)
    {
        switch (value)
        {
            case null:
                return "<null>";
            case string str:
                return str;
        }

        if (value is IEnumerable enumerable and not string)
        {
            return string.Join(", ", enumerable.Cast<object>().Select(o => o?.ToString() ?? "<null>"));
        }

        return value.ToString() ?? "<null>";
    }
}
