using Anarchy.Toolkit.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections;
using System.Text;

namespace Anarchy.Toolkit.Web.Extension.Service.Host.Controller.Exceptions;

public class ModelStateValidationException(ModelStateDictionary modelState, object? model = null)
    : InvalidArgumentException
{
    private SerializableError Errors { get; } = new(modelState);

    private Dictionary<string, object?>? Model { get; } = model?.GetType()
        .GetProperties()
        .Select(p => new { p.Name, Value = p.GetValue(model) })
        .ToDictionary(p => p.Name, p => p.Value);

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
