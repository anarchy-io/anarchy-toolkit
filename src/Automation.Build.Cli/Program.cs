using Anarchy.Toolkit.Automation.Build.Cli.Service;

return ToolApplication.CreateDefault().Run(args, interactive: !Console.IsInputRedirected);
