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

#############################
## DOCUMENTATION
#############################

DOCS_DIR := $(abspath docs)

.PHONY: docs_sync
docs_sync: #? Copies the package and sample READMEs into the documentation site
	@$(DOCS_DIR)/scripts/sync-readmes.sh

.PHONY: docs_install
docs_install: #? Installs the documentation site's Ruby gems (needs Ruby and Bundler)
	@cd "$(DOCS_DIR)" && bundle install

.PHONY: docs_build
docs_build: docs_sync docs_install #? Builds the documentation site into docs/_site
	@cd "$(DOCS_DIR)" && bundle exec jekyll build

.PHONY: docs_serve
docs_serve: docs_sync docs_install #? Serves the documentation site locally with live reload
	@cd "$(DOCS_DIR)" && bundle exec jekyll serve --livereload
