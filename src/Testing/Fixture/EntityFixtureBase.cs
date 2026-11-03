using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Testing.Fixture.Exceptions;
using Anarchy.Toolkit.Testing.Helper;
using System.Reflection;

namespace Anarchy.Toolkit.Testing.Fixture;

public interface IEntityFixture<T>
{
    public T Build(T? inputEntity = default);
    public string ToJson(object? inputObject = null, bool overrideWithNulls = false);
    public T Build(object? inputObject, bool overrideWithNulls = false);

    public IEnumerable<T> BuildList(int? elementCount = null, object? inputObject = null,
                                    bool overrideWithNulls = false);

    public string ToJsonList(int? elementCount = null, object? inputObject = null, bool overrideWithNulls = false);
}

public abstract class EntityFixtureBase<T> : IEntityFixture<T>
{
    protected abstract T BuildDefaultFixture();

    public T Build(T? inputEntity = default) => Build(inputEntity, false);

    public string ToJson(object? inputObject = null, bool overrideWithNulls = false) =>
       Build(inputObject, overrideWithNulls)!.ToJson();

    public string ToJsonList(int? elementCount = null, object? inputObject = null, bool overrideWithNulls = false) =>
       BuildList(elementCount, inputObject, overrideWithNulls).ToJson();

    public IEnumerable<T> BuildList(int? elementCount = null, object? inputObject = null,
                                    bool overrideWithNulls = false)
    {
        var result = new List<T>();

        for (var count = 0; count < (elementCount ?? TestHelper.RandomId(1, 10)); count++)
        {
            result.Add(Build(inputObject, overrideWithNulls));
        }

        return result;
    }

    public T Build(object? inputObject, bool overrideWithNulls = false)
    {
        var defaultObject = BuildDefaultFixture();

        if (inputObject == null) return defaultObject;

        try
        {
            return BuildWithActivator(defaultObject, inputObject, overrideWithNulls)
                   ?? BuildViaConstructor(defaultObject, inputObject, overrideWithNulls)
                   ?? OverrideDefaultProperties(defaultObject, inputObject, overrideWithNulls)
                   ?? defaultObject;
        }
        catch
        {
            return defaultObject;
        }
    }

    private T? BuildWithActivator(T defaultObject, object inputObject, bool overrideWithNulls)
    {
        try
        {
            var result = Activator.CreateInstance<T>();

            var inputProperties = inputObject.GetType().GetProperties();

            foreach (var propertyTarget in typeof(T).GetProperties())
            {
                var defaultValue = propertyTarget.GetValue(defaultObject);
                try
                {
                    if (inputProperties.All(p => p.Name != propertyTarget.Name))
                    {
                        propertyTarget.SetValue(result, defaultValue);
                        continue;
                    }

                    var propertyInputValue = inputProperties
                        .First(p => p.Name == propertyTarget.Name)
                        .GetValue(inputObject);

                    propertyTarget.SetValue(result,
                        propertyInputValue != null || overrideWithNulls ? propertyInputValue : defaultValue);
                }
                catch (InvalidOperationException)
                {
                    propertyTarget.SetValue(result, defaultValue);
                }
                catch (ArgumentException exception)
                {
                    throw new FixtureOverrideException(_fixtureType, propertyTarget.Name, exception);
                }
            }

            return result;
        }
        catch (MissingMethodException)
        {
            return default;
        }
    }

    private T OverrideDefaultProperties(T defaultObject, object inputObject, bool overrideWithNulls)
    {
        var inputProperties = inputObject.GetType().GetProperties();

        foreach (var propertyTarget in typeof(T).GetProperties())
        {
            var inputProperty = inputProperties.FirstOrDefault(p => p.Name == propertyTarget.Name);

            if (inputProperty == null) continue;

            var propertyInputValue = inputProperty.GetValue(inputObject);
            var defaultValue = propertyTarget.GetValue(defaultObject);

            try
            {
                propertyTarget.SetValue(defaultObject,
                    propertyInputValue != null || overrideWithNulls ? propertyInputValue : defaultValue);
            }
            catch (ArgumentException)
            {
                var field = propertyTarget.DeclaringType?.GetField($"<{propertyTarget.Name}>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                field?.SetValue(defaultObject,
                    propertyInputValue != null || overrideWithNulls ? propertyInputValue : defaultValue);
            }
        }

        return defaultObject;
    }

    private static T? BuildViaConstructor(T defaultObject, object inputObject, bool overrideWithNulls)
    {
        foreach (var ctor in typeof(T).GetConstructors().OrderByDescending(c => c.GetParameters().Length))
        {
            try
            {
                var ctorArguments = ctor
                    .GetParameters()
                    .Select(param => GetConstructorArgument(param, defaultObject, inputObject, overrideWithNulls))
                    .ToArray();

                return (T)ctor.Invoke(ctorArguments);
            }
            catch (System.Exception exception)
            {
                Console.WriteLine("Failed to build entity fixture: {0}", exception);
            }
        }

        return default;
    }

    private static object? GetConstructorArgument(ParameterInfo ctorParam, T defaultObject, object inputObject,
                                                  bool overrideWithNulls)
    {
        var inputProp = inputObject.GetType()
            .GetProperty(ctorParam.Name!, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (inputProp != null)
        {
            var inputValue = inputProp.GetValue(inputObject);
            if (inputValue != null || overrideWithNulls) return inputValue;
        }

        var classProperty = typeof(T).GetProperty(ctorParam.Name!,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (classProperty != null)
        {
            return classProperty.GetValue(defaultObject);
        }

        var classField = typeof(T).GetField(ctorParam.Name!,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        // Fallback to defaultObject property
        if (classField != null)
        {
            return classField.GetValue(defaultObject);
        }

        // If nothing found, use default for type
        return ctorParam.HasDefaultValue ? ctorParam.DefaultValue : GetDefault(ctorParam.ParameterType);
    }

    private static object? GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

    protected T BuildWithValuesWhenUndefined(
        Dictionary<string, dynamic?> pairs,
        object? inputObject = null,
        bool overrideWithNulls = false)
    {
        var output = Build(inputObject, overrideWithNulls);

        foreach (var pair in pairs)
        {
            var destInfo = _fixtureType.GetProperty(pair.Key);
            if (destInfo != null &&
                (inputObject == null || inputObject.GetType().GetProperties().All(p => p.Name != pair.Key)))
            {
                destInfo.SetValue(output, pair.Value);
            }
        }

        return output;
    }

    private readonly Type _fixtureType = typeof(T);
}
