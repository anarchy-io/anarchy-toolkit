using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Testing.Helper.Exceptions;

/// <summary>
/// Thrown when every value of an enum was excluded from a random pick.
/// </summary>
public class NoEnumValuesAvailableException(Type enumType)
    : InvalidStateException($"No values available for enum {enumType.Name}.");
