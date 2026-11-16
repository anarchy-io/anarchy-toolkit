using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Testing.Fixture.Exceptions;
using Anarchy.Toolkit.Testing.Helper;
using System.Reflection;

namespace Anarchy.Toolkit.Testing.Fixture;

/// <summary>
/// Contract for fixtures that build test instances of <typeparamref name="T"/> (and their JSON), allowing selected
/// properties to be overridden.
/// </summary>
/// <typeparam name="T">The entity type built by the fixture.</typeparam>
public interface IEntityFixture<T>
{
    /// <summary>
    /// Builds an entity, overriding defaults with the non-null property values of <paramref name="inputEntity"/>.
    /// </summary>
    /// <param name="inputEntity">An entity whose property values override the defaults, or <see langword="null"/> for the default fixture.</param>
    /// <returns>The built entity.</returns>
    public T Build(T? inputEntity = default);
    /// <summary>
    /// Builds an entity (see <see cref="Build(object, bool)"/>) and serializes it to JSON.
    /// </summary>
    /// <param name="inputObject">An object (typically anonymous) whose properties override the defaults by name.</param>
    /// <param name="overrideWithNulls">Whether <see langword="null"/> values in <paramref name="inputObject"/> override defaults.</param>
    /// <returns>The JSON representation.</returns>
    public string ToJson(object? inputObject = null, bool overrideWithNulls = false);
    /// <summary>
    /// Builds an entity, overriding default property values with same-named properties of <paramref name="inputObject"/>.
    /// </summary>
    /// <param name="inputObject">An object (typically anonymous) whose properties override the defaults by name, or <see langword="null"/>.</param>
    /// <param name="overrideWithNulls">Whether <see langword="null"/> values in <paramref name="inputObject"/> override defaults.</param>
    /// <returns>The built entity.</returns>
    public T Build(object? inputObject, bool overrideWithNulls = false);

    /// <summary>
    /// Builds several entities, each via <see cref="Build(object, bool)"/>.
    /// </summary>
    /// <param name="elementCount">Number of entities, or <see langword="null"/> for a random count.</param>
    /// <param name="inputObject">Overrides applied to every entity.</param>
    /// <param name="overrideWithNulls">Whether <see langword="null"/> values in <paramref name="inputObject"/> override defaults.</param>
    /// <returns>The built entities.</returns>
    public IEnumerable<T> BuildList(int? elementCount = null, object? inputObject = null,
                                    bool overrideWithNulls = false);

    /// <summary>
    /// Builds several entities (see <see cref="BuildList"/>) and serializes them to a JSON array.
    /// </summary>
    /// <param name="elementCount">Number of entities, or <see langword="null"/> for a random count.</param>
    /// <param name="inputObject">Overrides applied to every entity.</param>
    /// <param name="overrideWithNulls">Whether <see langword="null"/> values in <paramref name="inputObject"/> override defaults.</param>
    /// <returns>The JSON array.</returns>
    public string ToJsonList(int? elementCount = null, object? inputObject = null, bool overrideWithNulls = false);
}

/// <summary>
/// Base class for entity fixtures: subclasses provide a default instance via <see cref="BuildDefaultFixture"/> and
/// callers override selected properties with an anonymous object.
/// </summary>
/// <remarks>
/// <para>
/// Overrides are applied by trying, in order: creating a new instance with a parameterless constructor and copying
/// properties; invoking the public constructors (most parameters first) matching parameter names case-insensitively;
/// and finally setting properties (or auto-property backing fields) on the default instance. If everything fails, the
/// unmodified default instance is returned: errors are swallowed, so a misspelled property name silently has no effect.
/// </para>
/// <para>
/// By default <see langword="null"/> values in the override object are ignored; pass <c>overrideWithNulls: true</c>
/// to apply them.
/// </para>
/// </remarks>
/// <typeparam name="T">The entity type built by the fixture.</typeparam>
/// <example>
/// <code language="csharp">
/// public class OrderFixture : EntityFixtureBase&lt;Order&gt;
/// {
///     protected override Order BuildDefaultFixture() =&gt;
///         new() { Id = TestHelper.RandomId&lt;int&gt;(), Reference = TestHelper.RandomId("Order") };
/// }
///
/// var order = new OrderFixture().Build(new { Reference = "ABC" });
/// </code>
/// </example>
public abstract class EntityFixtureBase<T> : IEntityFixture<T>
{
    /// <summary>
    /// Creates a new default instance of <typeparamref name="T"/>; called on every build, so it should return a fresh instance.
    /// </summary>
    /// <returns>The default entity.</returns>
    protected abstract T BuildDefaultFixture();

    /// <inheritdoc/>
    public T Build(T? inputEntity = default) => Build(inputEntity, false);

    /// <inheritdoc/>
    public string ToJson(object? inputObject = null, bool overrideWithNulls = false) =>
       Build(inputObject, overrideWithNulls)!.ToJson();

    /// <inheritdoc/>
    public string ToJsonList(int? elementCount = null, object? inputObject = null, bool overrideWithNulls = false) =>
       BuildList(elementCount, inputObject, overrideWithNulls).ToJson();

    /// <inheritdoc/>
    /// <remarks>When <paramref name="elementCount"/> is <see langword="null"/>, a random count between 1 and 10 is used (re-evaluated on each loop iteration).</remarks>
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

    /// <inheritdoc/>
    public T Build(object? inputObject, bool overrideWithNulls = false)
    {
        var defaultObject = BuildDefaultFixture();

        if (inputObject == null) return defaultObject;

        try
        {
            return BuildWithActivator(defaultObject, inputObject, overrideWithNulls)
                   ?? BuildViaConstructor(defaultObject, inputObject, overrideWithNulls)
                   ?? EntityFixtureBase<T>.OverrideDefaultProperties(defaultObject, inputObject, overrideWithNulls)
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

    private static T OverrideDefaultProperties(T defaultObject, object inputObject, bool overrideWithNulls)
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
            catch (Exception exception)
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

    /// <summary>
    /// Builds an entity (see <see cref="Build(object, bool)"/>) and then assigns each value in <paramref name="pairs"/>
    /// to the property of the same name, unless <paramref name="inputObject"/> already defines that property.
    /// </summary>
    /// <remarks>Useful to derive dependent defaults (e.g. values computed from other overrides).</remarks>
    /// <param name="pairs">Property names and the values to use when not explicitly overridden.</param>
    /// <param name="inputObject">Explicit overrides, or <see langword="null"/>.</param>
    /// <param name="overrideWithNulls">Whether <see langword="null"/> values in <paramref name="inputObject"/> override defaults.</param>
    /// <returns>The built entity.</returns>
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
