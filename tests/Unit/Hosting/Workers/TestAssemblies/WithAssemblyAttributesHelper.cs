using Anarchy.Toolkit.Core.Health.Entity.Attribute;
using System.Reflection;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestAssemblies;

/// <summary>
/// Replaces the legacy <c>TestAssemblies.WithAssemblyAttributes</c> helper assembly (which only carried
/// <c>[assembly: ApplicationRunbookUri("https://some.com/runbook")]</c>). The attribute cannot be applied to this
/// test assembly because other tests expect its runbook URI to be <c>null</c>, so an <see cref="Assembly"/> double
/// is used instead.
/// </summary>
public static class WithAssemblyAttributesHelper
{
    public static Assembly Assembly { get; } = new AssemblyWithAttributes(
        name: "TestAssemblies.WithAssemblyAttributes",
        inner: typeof(object).Assembly,
        new ApplicationRunbookUriAttribute("https://some.com/runbook")
    );

    private sealed class AssemblyWithAttributes : Assembly
    {
        private readonly string _name;
        private readonly Assembly _inner;
        private readonly Attribute[] _attributes;

        public AssemblyWithAttributes(string name, Assembly inner, params Attribute[] attributes)
        {
            _name = name;
            _inner = inner;
            _attributes = attributes;
        }

        public override string FullName => GetName().FullName;

        public override AssemblyName GetName() => new(_name) { Version = new Version(1, 0, 0, 0) };

        public override AssemblyName GetName(bool copiedName) => GetName();

        public override object[] GetCustomAttributes(bool inherit) => _attributes.ToArray<object>();

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            var matches = _attributes.Where(attributeType.IsInstanceOfType).ToArray();
            var result = Array.CreateInstance(attributeType, matches.Length);
            Array.Copy(matches, result, matches.Length);

            return (object[])result;
        }

        public override bool IsDefined(Type attributeType, bool inherit) =>
            _attributes.Any(attributeType.IsInstanceOfType);

        public override Stream? GetManifestResourceStream(string name) => _inner.GetManifestResourceStream(name);

        public override string[] GetManifestResourceNames() => _inner.GetManifestResourceNames();
    }
}
