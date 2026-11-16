using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Testing.Fixture.Exceptions;

/// <summary>
/// Thrown when a fixture cannot override a property with the provided value.
/// </summary>
/// <remarks>Raised by <see cref="Anarchy.Toolkit.Testing.Fixture.EntityFixtureBase{TEntity}"/> when reflection-based property overriding fails.</remarks>
/// <param name="fixtureType">The type whose property could not be overridden.</param>
/// <param name="propertyName">The name of the property being overridden.</param>
/// <param name="innerException">The underlying error raised while setting the value.</param>
public class FixtureOverrideException(Type fixtureType, string propertyName, ArgumentException innerException)
    : InvalidArgumentException(
        $"Failed to override {fixtureType} => {propertyName}'s value because \"{innerException.Message}\"",
        innerException);
