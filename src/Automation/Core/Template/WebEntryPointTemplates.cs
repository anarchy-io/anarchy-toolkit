namespace Anarchy.Toolkit.Automation.Core.Template;

/// <summary>Project-root process and optional Lambda entry points for web applications.</summary>
internal static class WebEntryPointTemplates
{
    public static IReadOnlyDictionary<string, string> Render(ApplicationContext context)
    {
        var ns = context.Namespace;
        var startup = context.StartupName;
        var ports = ApplicationTemplates.PortsOf(context);
        var dispatch = context.App.Container ? """
                    if (string.Equals(Environment.GetEnvironmentVariable("RUNTIME_HOST"), "container", StringComparison.OrdinalIgnoreCase))
                    {
                        EntryPointContainer.Main(args);
                        return;
                    }

            """ : "";
        var files = new Dictionary<string, string> {
            ["EntryPointLocal.cs"] = $$"""
                using {{ns}}.Composition;
                using Anarchy.Toolkit.Runtime.Web;

                namespace {{ns}};

                public static class EntryPointLocal
                {
                    public static void Main(string[] args)
                    {
                {{dispatch}}        WebAppEntryPoint.RunWebHost<{{startup}}>(args, {{ports.Http}}, {{ports.Https}});
                    }
                }

                """
        };
        if (context.App.Container)
            files["EntryPointContainer.cs"] = $$"""
                using {{ns}}.Composition;
                using Anarchy.Toolkit.Runtime.Web;

                namespace {{ns}};

                public static class EntryPointContainer
                {
                    public static void Main(string[] args) => WebAppEntryPoint.RunContainerWebHost<{{startup}}>(args);
                }

                """;
        if (context.App.ApiLambda)
            files["EntryPointLambda.cs"] = $$"""
                using {{ns}}.Composition;
                using Anarchy.Toolkit.Runtime.Web.Aws;

                namespace {{ns}};

                /// <summary>API Gateway handler: <c>{{ns}}::{{ns}}.EntryPointLambda::FunctionHandlerAsync</c>.</summary>
                public sealed class EntryPointLambda : AwsWebAppEntryPointBase<{{startup}}>;

                """;
        return files;
    }
}
