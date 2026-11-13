using Anarchy.Toolkit.Build.Cli.Tool.Service;

return ToolApplication.CreateDefault().Run(args, interactive: !Console.IsInputRedirected);
