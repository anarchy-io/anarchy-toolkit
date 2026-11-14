#############################
## MONOREPO
#############################

TOOL_PROJECT := $(abspath src/Build.Cli.Tool/Build.Cli.Tool.csproj)

.PHONY: run_dotnet_tool
run_dotnet_tool: #? Runs the anarchy-toolkit tool from source, to test it locally
#+ TOOL_ARGS|str|Arguments for the tool (e.g. 'bootstrap MyApp --output /tmp/MyApp'); none shows its help
#+ TOOL_DIR|str|Directory to run it in, where scaffold looks for a solution (defaults to the current one)
	@$(SCRIPTS_DIR)/console/title.sh 'green' 'Running anarchy-toolkit'
	@cd "$(or $(TOOL_DIR),$(CURDIR))" && dotnet run --project "$(TOOL_PROJECT)" -- $(TOOL_ARGS)
