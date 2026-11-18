# Adopting Anarchy Toolkit: instructions for coding agents

You are a coding agent asked to adopt [Anarchy Toolkit](https://github.com/anarchy-io/anarchy-toolkit) in the current
directory, or to migrate a repository that already uses it to the latest version. This page only gets the toolkit's
own instructions onto the machine: they ship with the CLI as the `anarchy-adopt` skill, so they match the version you
install and work without network access afterwards.

1. Check the prerequisites and report them to the user:

   ```bash
   dotnet --list-sdks   # needs a 10.0 SDK (https://dotnet.microsoft.com/download)
   make --version       # GNU Make (macOS, Linux or WSL)
   git status --short   # keep the user's uncommitted changes intact
   ```

2. Install or update the CLI, then install the agent skills into the repository (they go to `.agents/skills` and never
   overwrite existing files; ask the user first):

   ```bash
   dotnet tool update --global Anarchy.Toolkit.Build.Cli.Tool
   anarchy-toolkit skills install
   ```

   If the user declines, read the same instructions without installing them:
   `anarchy-toolkit boilerplate show .agents/skills/anarchy-adopt/SKILL.md`.

3. Load the `anarchy-adopt` skill (`.agents/skills/anarchy-adopt/SKILL.md`) and follow it.
