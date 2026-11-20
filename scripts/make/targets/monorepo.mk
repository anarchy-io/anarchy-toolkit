#############################
## MONOREPO
#############################
#@ anarchy-monorepo

TOOL_PROJECT := $(abspath src/Automation.Build.Cli/Automation.Build.Cli.csproj)

.PHONY: run_dotnet_tool
run_dotnet_tool: #? Runs the anarchy-toolkit tool from source, to test it locally
#+ TOOL_ARGS|str|Arguments for the tool (e.g. 'bootstrap MyApp --output /tmp/MyApp'); none shows its help
#+ TOOL_DIR|str|Directory to run it in, where scaffold looks for a solution (defaults to the current one)
	@$(SCRIPTS_DIR)/console/title.sh 'green' 'Running anarchy-toolkit'
	@cd "$(or $(TOOL_DIR),$(CURDIR))" && dotnet run --project "$(TOOL_PROJECT)" -- $(TOOL_ARGS)

.PHONY: check_package_hierarchy
check_package_hierarchy: #? Verifies the toolkit's package hierarchy: same-layer references only to name ancestors or allowed pairs
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Checking Package Hierarchy'
	${MAKE} anarchy SKIP_TITLE=true TARGET=CheckPackageHierarchy

#############################
## DOCUMENTATION
#############################

DOCS_DIR := $(abspath docs)

# Install the gems in the user's gem directory instead of the system Ruby's (e.g. /opt/homebrew),
# which often isn't writable. Bundler appends ruby/<version>, matching `gem env user_gemhome`.
# Exported so `bundle exec` in the targets below resolves the gems from the same place.
export BUNDLE_PATH ?= $(HOME)/.local/share/gem

.PHONY: docs_sync
docs_sync: #? Copies the package and sample READMEs into the documentation site
	@$(DOCS_DIR)/scripts/sync-readmes.sh

.PHONY: skills_docs_sync
skills_docs_sync: #? Copies the conventions, decisions and onboarding guides into the distributed skills' references
#+ CHECK|bool|Only check that the skill is up to date, failing otherwise (as CI does)
	@$(DOCS_DIR)/scripts/sync-skill-docs.sh $(if $(filter true,$(CHECK)),--check,)

.PHONY: docs_install
docs_install: #? Installs the documentation site's Ruby gems (needs Ruby and Bundler)
	@cd "$(DOCS_DIR)" && bundle install

.PHONY: docs_build
docs_build: docs_sync docs_install #? Builds the documentation site into docs/_site
	@cd "$(DOCS_DIR)" && bundle exec jekyll build

.PHONY: docs_serve
docs_serve: docs_sync docs_install #? Serves the documentation site locally with live reload
	@cd "$(DOCS_DIR)" && bundle exec jekyll serve --livereload

.PHONY: docs_api_build
docs_api_build: #? Builds the DocFX API reference into artifacts/docs-api
	${MAKE} anarchy SKIP_TITLE=true TARGET=GenerateApiReference

DOCS_API_OUTPUT := $(abspath artifacts/docs-api)

.PHONY: docs_api_serve
docs_api_serve: $(if $(filter true,$(NO_BUILD)),,docs_api_build) #? Serves the DocFX API reference locally on http://localhost:8080
#+ NO_BUILD|bool|Serve the existing build as is, refreshing only its stylesheets (template and tokens), to check style changes quickly
	@if [ "$(NO_BUILD)" = "true" ]; then \
		test -d "$(DOCS_API_OUTPUT)/public" || { echo "No API reference in $(DOCS_API_OUTPUT): run without NO_BUILD first" >&2; exit 1; }; \
		cp docs-api/template/public/main.css docs/assets/css/tokens.css "$(DOCS_API_OUTPUT)/public/"; \
	fi
	@cd "$(DOCS_API_OUTPUT)" && python3 -m http.server 8080
