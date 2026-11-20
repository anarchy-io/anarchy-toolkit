using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using Anarchy.Toolkit.Automation.Core.Service;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.EndToEnd.Automation.Build.Component;

public class BoilerplateTest
{
    [Fact]
    public void PackagedMakeTargets_DelegateConvertedCommandsAndKeepHookOptions()
    {
        if (OperatingSystem.IsWindows()) return;

        var root = Path.Combine(Path.GetTempPath(), "anarchy-make-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            BoilerplateTasks.Create(root, null, new BoilerplateOptions {
                Items = "Makefile,scripts"
            });

            var log = Path.Combine(root, "calls");
            var environment = new Dictionary<string, string> {
                ["MAKE_TEST_LOG"] = log,
                ["ANARCHY_RELOAD_ENV"] = "false"
            };

            File.WriteAllText(Path.Combine(root, "scripts", "build.anarchy.sh"),
                "#!/usr/bin/env bash\nprintf '<%s>\\n' \"$@\" >> \"$MAKE_TEST_LOG\"\n");

            File.WriteAllText(Path.Combine(root, "scripts", "bootstrap", "install-development-environment.sh"),
                "#!/usr/bin/env bash\nprintf '<bootstrap>\\n' >> \"$MAKE_TEST_LOG\"\n");

            string RunMake(params string[] args)
            {
                File.WriteAllText(log, "");
                var result = LocalProcessTasks.Run(root, "make", args, environment);
                result.ExitCode.Should()
                    .Be(0, result.Output + result.Error);

                return File.ReadAllText(log);
            }

            RunMake("lint_fix", "AUTO_STAGE=true")
                .Should()
                .Contain("<LintFix>\n<--AutoStageLintFixes>\n<true>");

            RunMake("upgrade_dependencies", "LOCK=Minor", "INSTALL_TOOL=true", "UPGRADE=true")
                .Should()
                .Contain(
                    "<UpgradeDependencies>\n<--DependencyVersionLock>\n<Minor>\n<--DependencyInstallTool>\n<true>\n<--DependencyUpgrade>\n<true>");

            RunMake("git_hooks_enable",
                    "OPTIONS=--hook-type pre-push --skip-lint-fix --skip-test-static --skip-test-runtime --dry-run --force")
                .Should()
                .Contain(
                    "<EnableGitHook>\n<--HookType>\n<pre-push>\n<--HookSkipLintFix>\n<true>\n<--HookSkipTestStatic>\n<true>\n<--HookSkipTestRuntime>\n<true>\n<--HookDryRun>\n<true>\n<--HookForce>\n<true>");

            RunMake("git_hooks_show", "HOOK_TYPE=pre-push")
                .Should()
                .Contain("<ShowGitHooks>\n<--HookType>\n<pre-push>");

            RunMake("git_hooks_disable")
                .Should()
                .Contain("<DisableGitHook>\n<--HookType>\n<pre-commit>");

            RunMake("git_hooks_enable", "OPTIONS=--help")
                .Should()
                .BeEmpty();

            foreach (var name in new[] {
                         "anarchy",
                         "fallout",
                         "nuke"
                     })
            {
                RunMake(name, "TARGET=Pack --Configuration Release")
                    .Should()
                    .Be("<Pack>\n<--Configuration>\n<Release>\n");

                RunMake(name, "TARGET=Pack", "ENV=ci")
                    .Should()
                    .Be("<Pack>\n<--IsCiBuild>\n<true>\n");

                RunMake(name, "TARGET=Pack --IsCiBuild false", "ENV=ci")
                    .Should()
                    .Be("<Pack>\n<--IsCiBuild>\n<false>\n");
            }

            foreach (var name in new[] {
                         "anarchy_help",
                         "fallout_help",
                         "nuke_help"
                     })
                RunMake(name)
                    .Should()
                    .Be("<--help>\n");

            var help = LocalProcessTasks.Run(root, "make", ["help", "COMMAND=install"], environment);
            help.ExitCode.Should()
                .Be(0, help.Output + help.Error);

            help.Output.Should()
                .Contain("INSTALLATION")
                .And.Contain("Sets up the development environment");

            RunMake("install", "WITH_HOOKS=true")
                .Should()
                .Contain("<bootstrap>\n<Restore>\n<Compile>\n<EnableGitHook>\n<--HookSkipTestRuntime>\n<true>");

            RunMake("install", "ENV=ci")
                .Should()
                .Contain("<Restore>\n<--IsCiBuild>\n<true>\n<Compile>\n<--IsCiBuild>\n<true>")
                .And.NotContain("<bootstrap>");

            RunMake("validate_release", "RELEASE_BASE=main")
                .Should()
                .Contain("<ValidateLocalRelease>\n<--ReleaseBase>\n<main>");

            RunMake("container_publish_app_artifact", "APP_NAME=Api")
                .Should()
                .Contain("<ContainerPublishAppArtifact>\n<--AppName>\n<Api>");

            RunMake("container_image_build", "APP_NAME=Api")
                .Should()
                .Contain("<ContainerImageBuild>\n<--AppName>\n<Api>");

            RunMake("container_image_push", "APP_NAME=Api")
                .Should()
                .Contain("<ContainerImagePush>\n<--AppName>\n<Api>");

            RunMake("boilerplate", "ITEMS=Makefile,scripts", "APPEND=true")
                .Should()
                .Contain("<SetupBoilerplate>\n<--BoilerplateItems>\n<Makefile,scripts>\n<--AppendExisting>\n<true>");

            RunMake("skills_sync", "PREFIX=shop")
                .Should()
                .Contain("<SyncAgentSkills>\n<--AgentSkillsPrefix>\n<shop>");

            RunMake("install_skills")
                .Should()
                .Contain("<SetupBoilerplate>\n<--BoilerplateItems>\n<skills>");

            RunMake("lint")
                .Should()
                .Be("<Lint>\n");

            RunMake("lint", "ENV=ci")
                .Should()
                .Be("<Lint>\n<--IsCiBuild>\n<true>\n");

            RunMake("compile", "ENV=ci")
                .Should()
                .Be("<Compile>\n<--IsCiBuild>\n<true>\n");

            RunMake("pull_request", "ENV=ci")
                .Should()
                .Be("<PullRequest>\n<--IsCiBuild>\n<true>\n");

            var invalid = LocalProcessTasks.Run(root, "make", ["git_hooks_enable", "OPTIONS=--unknown"], environment);
            invalid.ExitCode.Should()
                .NotBe(0);

            invalid.Error.Should()
                .Contain("Unknown option: --unknown");

            var missingType =
                LocalProcessTasks.Run(root, "make", ["git_hooks_enable", "OPTIONS=--hook-type"], environment);

            missingType.ExitCode.Should()
                .NotBe(0);

            missingType.Error.Should()
                .Contain("Missing hook type");
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
            BoilerplateTasks.Create(root, null, new BoilerplateOptions {
                Items = "Makefile,scripts"
            });

            File.WriteAllText(Path.Combine(root, "scripts", "make", "targets", "aliases.mk"), """
                alpha_beta gamma_delta omega_zeta: #? A grouped target
                #! A shared warning
                #+ SHARED|str|A shared option

                """);

            (string Output, string[] Targets) RunHelp(params string[] args)
            {
                var result = LocalProcessTasks.Run(root, "make", ["help_dynamic", .. args]);
                result.ExitCode.Should()
                    .Be(0, result.Output + result.Error);

                var output = System.Text.RegularExpressions.Regex.Replace(result.Output, "\\x1b\\[[0-9;]*m", "");
                var targets = System.Text.RegularExpressions.Regex.Matches(output, @"(?m)^  (\S+)\s+")
                    .Select(match => match.Groups[1].Value)
                    .ToArray();

                return (output, targets);
            }

            var all = RunHelp();
            all.Targets.Should()
                .Contain([
                    "anarchy", "fallout", "nuke", "anarchy_help", "fallout_help", "nuke_help", "alpha_beta",
                    "gamma_delta", "omega_zeta"
                ]);

            all.Output.Split("A shared warning")
                .Length.Should()
                .Be(4);

            all.Output.Split("A shared option")
                .Length.Should()
                .Be(4);

            foreach (var name in new[] {
                         "alpha_beta",
                         "gamma_delta",
                         "omega_zeta"
                     })
            {
                var help = RunHelp("COMMAND=" + name);
                help.Targets.Should()
                    .Equal(name);

                help.Output.Should()
                    .Contain("A grouped target")
                    .And.Contain("A shared warning")
                    .And.Contain("A shared option");
            }

            foreach (var name in new[] {
                         "anarchy",
                         "fallout",
                         "nuke",
                         "anarchy_help",
                         "fallout_help",
                         "nuke_help"
                     })
            {
                var help = RunHelp("COMMAND=" + name);
                help.Targets.Should()
                    .Equal(name);

                if (name is "anarchy" or "fallout" or "nuke")
                    help.Output.Should()
                        .Contain("TARGET");
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
            BoilerplateTasks.Create(root, null, new BoilerplateOptions {
                Items = "Makefile,scripts,global.json"
            });

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
                ["PATH"] = bin + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH"),
                ["DOTNET_INSTALL_DIR"] = Path.Combine(home, ".dotnet"),
                ["DOTNET_ROOT"] = "",
                ["ANARCHY_RELOAD_ENV"] = "true",
                ["FAKE_DOTNET_LOG"] = log
            };

            var result = LocalProcessTasks.Run(root, "make", ["install", "WITH_HOOKS=true"], environment);
            result.ExitCode.Should()
                .Be(0, result.Output + result.Error);

            var calls = File.ReadAllText(log);
            calls.Should()
                .Contain("dev-certs https --trust")
                .And.Contain("-- Restore")
                .And.Contain("-- Compile")
                .And.Contain("-- EnableGitHook --HookSkipTestRuntime true");

            File.ReadAllText(Path.Combine(home, ".bash_profile"))
                .Should()
                .Contain("DOTNET_ROOT")
                .And.Contain("ASPNETCORE_ENVIRONMENT");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
