namespace Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;

public static class AppEnvironments
{
    /// <summary>
    /// Specifies the Development environment.
    /// </summary>
    /// <remarks>The development environment can enable features that shouldn't be exposed in production. Because of the performance cost, scope validation and dependency validation only happens in development.</remarks>
    public const string Development = "Development";

    /// <summary>
    /// Specifies the Testing environment.
    /// </summary>
    /// <remarks>The testing environment can be used to by developers validate app changes before changing the environment to acceptance.</remarks>
    public const string Testing = "Testing";

    /// <summary>
    /// Specifies the Acceptance environment.
    /// </summary>
    /// <remarks>The acceptance environment can be used to by stakeholders validate app changes before changing the environment to production.</remarks>
    public const string Acceptance = "Acceptance";

    /// <summary>
    /// Specifies the Production environment.
    /// </summary>
    /// <remarks>The production environment should be configured to maximize security, performance, and application robustness.</remarks>
    public const string Production = "Production";

    public static readonly IReadOnlyCollection<string> AllAvailable = [
        Development,
        Testing,
        Acceptance,
        Production
    ];
}
