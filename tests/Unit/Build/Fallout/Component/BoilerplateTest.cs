using Anarchy.Toolkit.Build.Fallout.Component.Tasks;
using AwesomeAssertions;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Tests.Unit.Build.Fallout.Component;

public class BoilerplateTest
{
    [Fact]
    public void Create_GivenBuildProject_CreatesUsableScaffold()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "build"));
            Directory.CreateDirectory(Path.Combine(root, ".fallout"));
            File.WriteAllText(Path.Combine(root, ".fallout", "parameters.json"), "{\"Configuration\":\"Debug\"}");
            File.WriteAllText(Path.Combine(root, "build", "_build.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

            BoilerplateTasks.Create(root, "MyApp");

            File.Exists(Path.Combine(root, "MyApp.sln")).Should().BeTrue();
            File.ReadAllText(Path.Combine(root, "MyApp.sln")).Should().Contain("Core.csproj").And.Contain("_build.csproj");
            File.Exists(Path.Combine(root, "Makefile")).Should().BeTrue();
            File.Exists(Path.Combine(root, "scripts", "make", "targets", "code-quality.mk")).Should().BeTrue();
            File.Exists(Path.Combine(root, "scripts", "console", "banner-compact.sh")).Should().BeTrue();
            File.Exists(Path.Combine(root, "scripts", "console", "banner-wide.sh")).Should().BeTrue();
            File.Exists(Path.Combine(root, "tests", ".gitkeep")).Should().BeTrue();
            File.ReadAllText(Path.Combine(root, ".gitignore")).Should().Contain(".fallout/temp");
            File.ReadAllText(Path.Combine(root, ".editorconfig")).Should().Contain("root = true");
            File.ReadAllText(Path.Combine(root, "global.json")).Should().Contain("10.0.0");
            File.ReadAllText(Path.Combine(root, "nuget.config")).Should().Contain("api.nuget.org");
            File.ReadAllText(Path.Combine(root, "Directory.Build.props")).Should().Contain("**\\Queries\\**\\*.sql");
            var parameters = JsonNode.Parse(File.ReadAllText(Path.Combine(root, ".fallout", "parameters.json")))!;
            parameters["Solution"]!.GetValue<string>().Should().Be("MyApp.sln");
            parameters["Configuration"]!.GetValue<string>().Should().Be("Debug");
            if (!OperatingSystem.IsWindows())
                File.GetUnixFileMode(Path.Combine(root, "scripts", "build.fallout.sh")).Should().HaveFlag(UnixFileMode.UserExecute);

            var repeat = () => BoilerplateTasks.Create(root, "MyApp");
            repeat.Should().Throw<InvalidOperationException>().WithMessage("*overwrite*");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenConflictingFile_LeavesExistingFilesUntouched()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "Makefile"), "mine");
            var create = () => BoilerplateTasks.Create(root, "MyApp");
            create.Should().Throw<InvalidOperationException>().WithMessage("*overwrite*");
            File.ReadAllText(Path.Combine(root, "Makefile")).Should().Be("mine");
            Directory.Exists(Path.Combine(root, "scripts")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenConfiguredSolution_DoesNotReplaceParameters()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "build"));
            Directory.CreateDirectory(Path.Combine(root, ".fallout"));
            File.WriteAllText(Path.Combine(root, "build", "_build.csproj"), "<Project />");
            var parameters = Path.Combine(root, ".fallout", "parameters.json");
            File.WriteAllText(parameters, "{\"Solution\":\"existing.sln\"}");
            var create = () => BoilerplateTasks.Create(root, "MyApp");
            create.Should().Throw<InvalidOperationException>().WithMessage("*already defines a solution*");
            File.ReadAllText(parameters).Should().Be("{\"Solution\":\"existing.sln\"}");
            File.Exists(Path.Combine(root, "Makefile")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenExistingSettings_PreservesOrAppendsWithoutDuplicating()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            var files = new Dictionary<string, string> {
                [".gitignore"] = "my-ignore",
                [".editorconfig"] = "root = false\n[*]\nindent_size = 8",
                ["global.json"] = "my-sdk",
                ["NuGet.Config"] = "my-feeds",
                ["Directory.Build.props"] = "my-props"
            };
            foreach (var (name, content) in files) File.WriteAllText(Path.Combine(root, name), content);
            var options = new BoilerplateOptions { Items = ".gitignore,.editorconfig,global.json,nuget.config,Directory.Build.props" };

            BoilerplateTasks.Create(root, null, options);
            foreach (var (name, content) in files) File.ReadAllText(Path.Combine(root, name)).Should().Be(content);

            options.AppendExisting = true;
            BoilerplateTasks.Create(root, null, options);
            var gitignore = File.ReadAllText(Path.Combine(root, ".gitignore"));
            var editorconfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));
            gitignore.Should().StartWith("my-ignore\n").And.Contain(".fallout/temp");
            editorconfig.Should().StartWith(files[".editorconfig"] + "\n").And.Contain("resharper_sort_usings");
            foreach (var name in new[] { "global.json", "NuGet.Config", "Directory.Build.props" })
                File.ReadAllText(Path.Combine(root, name)).Should().Be(files[name]);
            Directory.EnumerateFiles(root).Select(Path.GetFileName).Should().NotContain("nuget.config");

            BoilerplateTasks.Create(root, null, options);
            File.ReadAllText(Path.Combine(root, ".gitignore")).Should().Be(gitignore);
            File.ReadAllText(Path.Combine(root, ".editorconfig")).Should().Be(editorconfig);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenSelection_HandlesOnlyRequestedItems()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "existing.sln"), "existing");
            var options = new BoilerplateOptions {
                Items = "scripts/console,.gitignore,Directory.Build.props",
                Exclude = "scripts/console/banner-wide.sh,.gitignore"
            };
            BoilerplateTasks.Create(root, null, options);
            File.Exists(Path.Combine(root, "scripts", "console", "title.sh")).Should().BeTrue();
            File.Exists(Path.Combine(root, "scripts", "console", "banner-wide.sh")).Should().BeFalse();
            File.Exists(Path.Combine(root, ".gitignore")).Should().BeFalse();
            File.Exists(Path.Combine(root, "Directory.Build.props")).Should().BeTrue();
            File.Exists(Path.Combine(root, "Makefile")).Should().BeFalse();
            File.ReadAllText(Path.Combine(root, "existing.sln")).Should().Be("existing");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void PackagedMakeTargets_DelegateConvertedCommandsAndKeepHookOptions()
    {
        if (OperatingSystem.IsWindows()) return;
        var root = Path.Combine(Path.GetTempPath(), "anarchy-make-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            BoilerplateTasks.Create(root, null, new BoilerplateOptions { Items = "Makefile,scripts" });
            var log = Path.Combine(root, "calls");
            var environment = new Dictionary<string, string> { ["MAKE_TEST_LOG"] = log, ["FALLOUT_RELOAD_ENV"] = "false" };
            File.WriteAllText(Path.Combine(root, "scripts", "build.fallout.sh"), "#!/usr/bin/env bash\nprintf '<%s>\\n' \"$@\" >> \"$MAKE_TEST_LOG\"\n");
            File.WriteAllText(Path.Combine(root, "scripts", "bootstrap", "install-development-environment.sh"), "#!/usr/bin/env bash\nprintf '<bootstrap>\\n' >> \"$MAKE_TEST_LOG\"\n");
            string RunMake(params string[] args)
            {
                File.WriteAllText(log, "");
                var result = LocalProcessTasks.Run(root, "make", args, environment);
                result.ExitCode.Should().Be(0, result.Output + result.Error);
                return File.ReadAllText(log);
            }
            RunMake("lint_fix", "AUTO_STAGE=true").Should().Contain("<LintFix>\n<--AutoStageLintFixes>\n<true>");
            RunMake("upgrade_dependencies", "LOCK=Minor", "INSTALL_TOOL=true", "UPGRADE=true")
                .Should().Contain("<UpgradeDependencies>\n<--DependencyVersionLock>\n<Minor>\n<--DependencyInstallTool>\n<true>\n<--DependencyUpgrade>\n<true>");
            RunMake("git_hooks_enable", "OPTIONS=--hook-type pre-push --skip-lint-fix --skip-test-static --skip-test-runtime --dry-run --force")
                .Should().Contain("<EnableGitHook>\n<--HookType>\n<pre-push>\n<--HookSkipLintFix>\n<true>\n<--HookSkipTestStatic>\n<true>\n<--HookSkipTestRuntime>\n<true>\n<--HookDryRun>\n<true>\n<--HookForce>\n<true>");
            RunMake("git_hooks_show", "HOOK_TYPE=pre-push").Should().Contain("<ShowGitHooks>\n<--HookType>\n<pre-push>");
            RunMake("git_hooks_disable").Should().Contain("<DisableGitHook>\n<--HookType>\n<pre-commit>");
            RunMake("git_hooks_enable", "OPTIONS=--help").Should().BeEmpty();
            RunMake("fallout", "TARGET=Pack").Should().Be(RunMake("nuke", "TARGET=Pack")).And.Contain("<Pack>");
            RunMake("fallout_help").Should().Be(RunMake("nuke_help")).And.Contain("<--help>");
            var help = LocalProcessTasks.Run(root, "make", ["help", "COMMAND=install"], environment);
            help.ExitCode.Should().Be(0, help.Output + help.Error);
            help.Output.Should().Contain("INSTALLATION").And.Contain("Sets up the development environment");
            RunMake("install", "WITH_HOOKS=true").Should().Contain("<bootstrap>\n<Restore>\n<Compile>\n<EnableGitHook>\n<--HookSkipTestRuntime>\n<true>");
            RunMake("install", "ENV=ci").Should().Contain("<Restore>\n<Compile>").And.NotContain("<bootstrap>");
            var invalid = LocalProcessTasks.Run(root, "make", ["git_hooks_enable", "OPTIONS=--unknown"], environment);
            invalid.ExitCode.Should().NotBe(0);
            invalid.Error.Should().Contain("Unknown option: --unknown");
            var missingType = LocalProcessTasks.Run(root, "make", ["git_hooks_enable", "OPTIONS=--hook-type"], environment);
            missingType.ExitCode.Should().NotBe(0);
            missingType.Error.Should().Contain("Missing hook type");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void PackagedMakeHelp_ListsAndFiltersEveryGroupedTargetWithItsOptions()
    {
        if (OperatingSystem.IsWindows()) return;
        var root = Path.Combine(Path.GetTempPath(), "anarchy-make-help-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            BoilerplateTasks.Create(root, null, new BoilerplateOptions { Items = "Makefile,scripts" });
            File.WriteAllText(Path.Combine(root, "scripts", "make", "targets", "aliases.mk"), """
                alpha_beta gamma_delta omega_zeta: #? A grouped target
                #! A shared warning
                #+ SHARED|str|A shared option

                """);

            (string Output, string[] Targets) RunHelp(params string[] args)
            {
                var result = LocalProcessTasks.Run(root, "make", ["help_dynamic", .. args]);
                result.ExitCode.Should().Be(0, result.Output + result.Error);
                var output = System.Text.RegularExpressions.Regex.Replace(result.Output, "\\x1b\\[[0-9;]*m", "");
                var targets = System.Text.RegularExpressions.Regex.Matches(output, @"(?m)^  (\S+)\s+")
                    .Select(match => match.Groups[1].Value).ToArray();
                return (output, targets);
            }

            var all = RunHelp();
            all.Targets.Should().Contain(["fallout", "nuke", "fallout_help", "nuke_help", "alpha_beta", "gamma_delta", "omega_zeta"]);
            all.Output.Split("A shared warning").Length.Should().Be(4);
            all.Output.Split("A shared option").Length.Should().Be(4);

            foreach (var name in new[] { "alpha_beta", "gamma_delta", "omega_zeta" })
            {
                var help = RunHelp("COMMAND=" + name);
                help.Targets.Should().Equal(name);
                help.Output.Should().Contain("A grouped target").And.Contain("A shared warning").And.Contain("A shared option");
            }

            foreach (var name in new[] { "fallout", "nuke", "fallout_help", "nuke_help" })
            {
                var help = RunHelp("COMMAND=" + name);
                help.Targets.Should().Equal(name);
                if (name is "fallout" or "nuke") help.Output.Should().Contain("TARGET");
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void PackagedInstall_BootstrapsSdkBeforeInvokingFallout()
    {
        if (OperatingSystem.IsWindows()) return;
        var root = Path.Combine(Path.GetTempPath(), "anarchy-install-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            BoilerplateTasks.Create(root, null, new BoilerplateOptions { Items = "Makefile,scripts,global.json" });
            var home = Path.Combine(root, "home");
            var bin = Path.Combine(root, "fake-bin");
            var log = Path.Combine(root, "dotnet.log");
            Directory.CreateDirectory(home);
            Directory.CreateDirectory(bin);
            void Executable(string path, string content)
            {
                File.WriteAllText(path, content);
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            Executable(Path.Combine(bin, "dotnet"), "#!/bin/bash\nexit 1\n");
            Executable(Path.Combine(bin, "curl"), """
                #!/bin/bash
                set -eu
                while [ "$1" != "-Lsfo" ]; do shift; done
                cat > "$2" <<'INSTALLER'
                #!/bin/bash
                set -eu
                while [ "$1" != "--install-dir" ]; do shift; done
                mkdir -p "$2"
                cat > "$2/dotnet" <<'DOTNET'
                #!/bin/bash
                if [ "${1:-}" = --version ]; then echo 10.0.302; else printf '%s\n' "$*" >> "$FAKE_DOTNET_LOG"; fi
                DOTNET
                chmod +x "$2/dotnet"
                INSTALLER

                """);
            var environment = new Dictionary<string, string> {
                ["HOME"] = home,
                ["SHELL"] = "/bin/bash",
                ["PATH"] = bin + Path.PathSeparator + System.Environment.GetEnvironmentVariable("PATH"),
                ["DOTNET_INSTALL_DIR"] = Path.Combine(home, ".dotnet"),
                ["DOTNET_ROOT"] = "",
                ["FALLOUT_RELOAD_ENV"] = "true",
                ["FAKE_DOTNET_LOG"] = log
            };
            var result = LocalProcessTasks.Run(root, "make", ["install", "WITH_HOOKS=true"], environment);
            result.ExitCode.Should().Be(0, result.Output + result.Error);
            var calls = File.ReadAllText(log);
            calls.Should().Contain("dev-certs https --trust").And.Contain("-- Restore").And.Contain("-- Compile")
                .And.Contain("-- EnableGitHook --HookSkipTestRuntime true");
            File.ReadAllText(Path.Combine(home, ".bash_profile")).Should().Contain("DOTNET_ROOT").And.Contain("ASPNETCORE_ENVIRONMENT");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenUnknownItem_FailsBeforeWritingAnything()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            var create = () => BoilerplateTasks.Create(root, null, new BoilerplateOptions { Items = ".gitignore,not-an-item" });
            create.Should().Throw<ArgumentException>().WithMessage("*not-an-item*");
            Directory.EnumerateFileSystemEntries(root).Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Create_GivenSolutionOnly_AddsBuildWithoutCoreOrOtherFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-boilerplate-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "build"));
            File.WriteAllText(Path.Combine(root, "build", "_build.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
            BoilerplateTasks.Create(root, "OnlyBuild", new BoilerplateOptions { Items = "solution" });
            File.ReadAllText(Path.Combine(root, "OnlyBuild.sln")).Should().Contain("_build.csproj").And.NotContain("Core.csproj");
            Directory.Exists(Path.Combine(root, "src")).Should().BeFalse();
            File.Exists(Path.Combine(root, "global.json")).Should().BeFalse();
            File.Exists(Path.Combine(root, "Directory.Build.props")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
