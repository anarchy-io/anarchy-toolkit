#############################
## UTILITIES
#############################
#@ anarchy-maintenance

# With ENV=ci, every target runs as a CI build: its dotnet commands use diagnostic verbosity (see IDefaultBuild).
ANARCHY_CI_FLAGS = $(if $(and $(filter ci,$(ENV)),$(if $(findstring --IsCiBuild,$(TARGET)),,ci)), --IsCiBuild true,)

.PHONY: anarchy fallout nuke
anarchy fallout nuke: #? Runs any target via the build project (nuke and fallout are compatibility aliases)
#+ TARGET|str|Name of the target defined in the build project
#+ ENV|str|Set to "ci" to run it as a CI build (--IsCiBuild true)
#+ ANARCHY_RELOAD_ENV|bool|Reload the shell environment before running the build (default=true)
#+ ANARCHY_LOG_LEVEL|str|Build log level (default=Information)
ifneq ($(SKIP_TITLE),true)
	@$(SCRIPTS_DIR)/console/title.sh 'blue' 'Running Build Target'
endif
ifndef TARGET
	@echo -e "\n${CL_RED}You must provide a TARGET=<TargetName> to execute a build task.${CL_RESET}\n"
	ANARCHY_RELOAD_ENV="$(ANARCHY_RELOAD_ENV)" $(SCRIPTS_DIR)/build.anarchy.sh --help
else
	ANARCHY_RELOAD_ENV="$(ANARCHY_RELOAD_ENV)" $(SCRIPTS_DIR)/build.anarchy.sh $(TARGET)$(ANARCHY_CI_FLAGS)
endif

.PHONY: anarchy_help fallout_help nuke_help
anarchy_help fallout_help nuke_help: #? Prints the build project's help information (fallout_help and nuke_help are compatibility aliases)
#+ ANARCHY_RELOAD_ENV|bool|Reload the shell environment before running the build (default=true)
#+ ANARCHY_LOG_LEVEL|str|Build log level (default=Information)
	@$(SCRIPTS_DIR)/console/title.sh 'blue' "Build Help"
	ANARCHY_RELOAD_ENV="$(ANARCHY_RELOAD_ENV)" $(SCRIPTS_DIR)/build.anarchy.sh --help

.PHONY: upgrade_dependencies
upgrade_dependencies: #? Allows for upgrading all solution dependencies at once
#+ LOCK|str|Specifies whether package versions should be partially locked. Possible values: "Major" (default), "Minor" or "None".
#+ INSTALL_TOOL|bool|Allow installing or reinstalling dotnet-outdated without prompting (DEFAULT=false)
#+ UPGRADE|bool|Allow upgrading without prompting (DEFAULT=false)
	@$(SCRIPTS_DIR)/console/title.sh 'blue' 'UPGRADE DEPENDENCIES'
	${MAKE} anarchy SKIP_TITLE=true TARGET="UpgradeDependencies$(if $(LOCK), --DependencyVersionLock $(LOCK),)$(if $(filter true,$(INSTALL_TOOL)), --DependencyInstallTool true,)$(if $(filter true,$(UPGRADE)), --DependencyUpgrade true,)"

.PHONY: art_logo
art_logo:
	@$(SCRIPTS_DIR)/console/banner.sh

.PHONY: git_get_head_sha
git_get_head_sha: #? Gets the current HEAD SHA of the Git repository
	@echo ${GIT_HEAD_SHA}

.PHONY: git_hooks_enable
git_hooks_enable: #? Sets up a GIT hook
#+ OPTIONS|str|Set to "--help" for options, including --no-skip-* overrides for saved hook preferences
	@$(SCRIPTS_DIR)/console/title.sh 'yellow' 'ENABLE GIT HOOK'
	@set -e; args=(EnableGitHook); set -- $(OPTIONS); \
	while [ "$$#" -gt 0 ]; do \
		case "$$1" in \
			--hook-type) [ "$$#" -ge 2 ] || { echo 'Missing hook type' >&2; exit 1; }; args+=(--HookType "$$2"); shift 2 ;; \
			--skip-lint-fix) args+=(--HookSkipLintFix true); shift ;; \
			--skip-test-static) args+=(--HookSkipTestStatic true); shift ;; \
			--skip-test-runtime) args+=(--HookSkipTestRuntime true); shift ;; \
			--no-skip-lint-fix) args+=(--HookSkipLintFix false); shift ;; \
			--no-skip-test-static) args+=(--HookSkipTestStatic false); shift ;; \
			--no-skip-test-runtime) args+=(--HookSkipTestRuntime false); shift ;; \
			--dry-run) args+=(--HookDryRun true); shift ;; \
			--force) args+=(--HookForce true); shift ;; \
			--help) echo 'Usage: make git_hooks_enable OPTIONS="[--hook-type TYPE] [--skip-lint-fix|--no-skip-lint-fix] [--skip-test-static|--no-skip-test-static] [--skip-test-runtime|--no-skip-test-runtime] [--dry-run] [--force]"'; exit 0 ;; \
			*) echo "Unknown option: $$1" >&2; exit 1 ;; \
		esac; \
	done; \
	ANARCHY_RELOAD_ENV="$(ANARCHY_RELOAD_ENV)" $(SCRIPTS_DIR)/build.anarchy.sh "$${args[@]}"

.PHONY: config_show config_set config_unset config_configure
config_show: #? Shows saved Anarchy preferences separately from effective defaults
	${MAKE} anarchy SKIP_TITLE=true TARGET=ShowAnarchyPreferences

config_set: export ANARCHY_PREFERENCE_KEY = $(value KEY)
config_set: export ANARCHY_PREFERENCE_VALUE = $(value VALUE)
config_set: #? Saves one typed JSON preference override
#+ KEY|str|Preference path, e.g. docker.waitForHealthy (required)
#+ VALUE|str|JSON value, e.g. false, ["db"], or "a string" (required)
	@set -e; test -n "$${ANARCHY_PREFERENCE_KEY:-}" || { echo 'KEY is required' >&2; exit 1; }; test -n "$${ANARCHY_PREFERENCE_VALUE:-}" || { echo 'VALUE must be a JSON value' >&2; exit 1; }; args=(SetAnarchyPreference --PreferenceKey "$${ANARCHY_PREFERENCE_KEY}" --PreferenceValue "$${ANARCHY_PREFERENCE_VALUE}"); ANARCHY_RELOAD_ENV="$(ANARCHY_RELOAD_ENV)" $(SCRIPTS_DIR)/build.anarchy.sh "$${args[@]}"

config_unset: export ANARCHY_PREFERENCE_KEY = $(value KEY)
config_unset: #? Removes one saved Anarchy preference override
#+ KEY|str|Preference path, e.g. docker.waitForHealthy (required)
	@set -e; test -n "$${ANARCHY_PREFERENCE_KEY:-}" || { echo 'KEY is required' >&2; exit 1; }; args=(UnsetAnarchyPreference --PreferenceKey "$${ANARCHY_PREFERENCE_KEY}"); ANARCHY_RELOAD_ENV="$(ANARCHY_RELOAD_ENV)" $(SCRIPTS_DIR)/build.anarchy.sh "$${args[@]}"

config_configure: #? Interactively edits optional Anarchy preference overrides
	ANARCHY_RELOAD_ENV="$(ANARCHY_RELOAD_ENV)" $(SCRIPTS_DIR)/build.anarchy.sh ConfigureAnarchyPreferences

.PHONY: git_hooks_disable
git_hooks_disable: #? Removes a configured Git Hook
#+ HOOK_TYPE|str|Name of the hook (default: pre-commit)
	@$(SCRIPTS_DIR)/console/title.sh 'yellow' 'DISABLE GIT HOOK'
	${MAKE} anarchy SKIP_TITLE=true TARGET="DisableGitHook --HookType $(or $(HOOK_TYPE),pre-commit)"

.PHONY: git_hooks_show
git_hooks_show: #? Shows content of a configured Git Hook
#+ HOOK_TYPE|str|Name of the hook (default: all)
	@$(SCRIPTS_DIR)/console/title.sh 'yellow' 'SHOW GIT HOOKS'
	${MAKE} anarchy SKIP_TITLE=true TARGET="ShowGitHooks$(if $(HOOK_TYPE), --HookType $(HOOK_TYPE),)"

.PHONY: boilerplate
boilerplate: #? Installs selected boilerplate items (Makefile, scripts, settings files...) without overwriting files
#+ ITEMS|str|Comma-separated items, e.g. Makefile,scripts (DEFAULT=all but the agent skills)
#+ EXCLUDE|str|Comma-separated items to leave out
#+ APPEND|bool|Sync the packaged .gitignore and .editorconfig into a managed block of existing ones (DEFAULT=false)
#+ DRY_RUN|bool|Only report what would be created, kept, updated or conflict (DEFAULT=false)
	${MAKE} anarchy SKIP_TITLE=true TARGET="SetupBoilerplate$(if $(ITEMS), --BoilerplateItems $(ITEMS),)$(if $(EXCLUDE), --BoilerplateExclude $(EXCLUDE),)$(if $(filter true,$(APPEND)), --AppendExisting true,)$(if $(filter true,$(DRY_RUN)), --BoilerplateDryRun true,)"

.PHONY: boilerplate_diff
boilerplate_diff: #? Shows how the solution's boilerplate files differ from the packaged ones, without writing
#+ ITEMS|str|Comma-separated packaged paths or folders, e.g. Makefile,scripts (DEFAULT=all but the agent skills)
	${MAKE} anarchy SKIP_TITLE=true TARGET="DiffBoilerplate$(if $(ITEMS), --BoilerplateItems $(ITEMS),)"

.PHONY: api_reference
api_reference: #? Generates the DocFX API reference website from the compiled packages (needs IApiReferenceBuild)
	${MAKE} anarchy SKIP_TITLE=true TARGET=GenerateApiReference
