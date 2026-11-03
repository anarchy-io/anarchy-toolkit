namespace Anarchy.Toolkit.Tests.Unit.Web.Fixture;

/// <summary>
/// Test-local exception standing in for a consumer's domain exception, which the toolkit does not know about; the
/// toolkit middleware treats it as any other unhandled exception.
/// </summary>
public class TestCustomException(string message, Exception innerException) : Exception(message, innerException);
