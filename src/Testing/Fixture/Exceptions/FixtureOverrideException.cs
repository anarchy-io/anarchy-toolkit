using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Testing.Fixture.Exceptions;

/// <summary>
/// Thrown when a fixture cannot override a property with the provided value.
/// </summary>
public class FixtureOverrideException(Type fixtureType, string propertyName, ArgumentException innerException)
    : InvalidArgumentException(
        $"Failed to override {fixtureType} => {propertyName}'s value because \"{innerException.Message}\"",
        innerException);
