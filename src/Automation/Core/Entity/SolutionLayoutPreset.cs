namespace Anarchy.Toolkit.Automation.Core.Entity;

/// <summary>Named starting layouts for bootstrapped or scaffolded solutions.</summary>
public enum SolutionLayoutPreset
{
    /// <summary>Shared src/Library layers and the standard application directories.</summary>
    Monorepo,
    /// <summary>Layer libraries directly under src and one application directory.</summary>
    SingleApp
}
