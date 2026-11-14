#############################
## UTILITIES
#############################

# With ENV=ci, every target runs as a CI build: its dotnet commands use diagnostic verbosity (see IDefaultBuild).
FALLOUT_CI_FLAGS = $(if $(and $(filter ci,$(ENV)),$(if $(findstring --IsCiBuild,$(TARGET)),,ci)), --IsCiBuild true,)

.PHONY: fallout nuke
fallout nuke: #? Runs any target via the build project (nuke is a compatibility alias)
#+ TARGET|str|Name of the target defined in the build project
#+ ENV|str|Set to "ci" to run it as a CI build (--IsCiBuild true)
ifneq ($(SKIP_TITLE),true)
	@$(SCRIPTS_DIR)/console/title.sh 'blue' 'Running Build Target'
endif
ifndef TARGET
	@echo -e "\n${CL_RED}You must provide a TARGET=<TargetName> to execute a build task.${CL_RESET}\n"
	@NUKE_LOG_LEVEL=Information NUKE_NO_LOGO=true dotnet run --project ./build/_build.csproj -- --help
else
	FALLOUT_RELOAD_ENV=${FALLOUT_RELOAD_ENV} $(SCRIPTS_DIR)/build.fallout.sh $(TARGET)$(FALLOUT_CI_FLAGS)
endif

.PHONY: fallout_help nuke_help
fallout_help nuke_help: #? Prints the build project's help information
	@$(SCRIPTS_DIR)/console/title.sh 'blue' "Build Help"
	FALLOUT_RELOAD_ENV=${FALLOUT_RELOAD_ENV} $(SCRIPTS_DIR)/build.fallout.sh --help

.PHONY: upgrade_dependencies
upgrade_dependencies: #? Allows for upgrading all solution dependencies at once
#+ LOCK|str|Specifies whether package versions should be partially locked. Possible values: "Major" (default), "Minor" or "None".
#+ INSTALL_TOOL|bool|Allow installing or reinstalling dotnet-outdated without prompting (DEFAULT=false)
#+ UPGRADE|bool|Allow upgrading without prompting (DEFAULT=false)
	@$(SCRIPTS_DIR)/console/title.sh 'blue' 'UPGRADE DEPENDENCIES'
	${MAKE} fallout SKIP_TITLE=true TARGET="UpgradeDependencies$(if $(LOCK), --DependencyVersionLock $(LOCK),)$(if $(filter true,$(INSTALL_TOOL)), --DependencyInstallTool true,)$(if $(filter true,$(UPGRADE)), --DependencyUpgrade true,)"

.PHONY: art_logo
art_logo:
	@$(SCRIPTS_DIR)/console/banner.sh

.PHONY: git_get_head_sha
git_get_head_sha: #? Gets the current HEAD SHA of the Git repository
	@echo ${GIT_HEAD_SHA}

.PHONY: git_hooks_enable
git_hooks_enable: #? Sets up a GIT hook
#+ OPTIONS|str|Set to "--help" to display more options
	@$(SCRIPTS_DIR)/console/title.sh 'yellow' 'ENABLE GIT HOOK'
	@set -e; args=(EnableGitHook); set -- $(OPTIONS); \
	while [ "$$#" -gt 0 ]; do \
		case "$$1" in \
			--hook-type) [ "$$#" -ge 2 ] || { echo 'Missing hook type' >&2; exit 1; }; args+=(--HookType "$$2"); shift 2 ;; \
			--skip-lint-fix) args+=(--HookSkipLintFix true); shift ;; \
			--skip-test-static) args+=(--HookSkipTestStatic true); shift ;; \
			--skip-test-runtime) args+=(--HookSkipTestRuntime true); shift ;; \
			--dry-run) args+=(--HookDryRun true); shift ;; \
			--force) args+=(--HookForce true); shift ;; \
			--help) echo 'Usage: make git_hooks_enable OPTIONS="[--hook-type TYPE] [--skip-lint-fix] [--skip-test-static] [--skip-test-runtime] [--dry-run] [--force]"'; exit 0 ;; \
			*) echo "Unknown option: $$1" >&2; exit 1 ;; \
		esac; \
	done; \
	FALLOUT_RELOAD_ENV="$(FALLOUT_RELOAD_ENV)" $(SCRIPTS_DIR)/build.fallout.sh "$${args[@]}"

.PHONY: git_hooks_disable
git_hooks_disable: #? Removes a configured Git Hook
#+ HOOK_TYPE|str|Name of the hook (default: pre-commit)
	@$(SCRIPTS_DIR)/console/title.sh 'yellow' 'DISABLE GIT HOOK'
	${MAKE} fallout SKIP_TITLE=true TARGET="DisableGitHook --HookType $(or $(HOOK_TYPE),pre-commit)"

.PHONY: git_hooks_show
git_hooks_show: #? Shows content of a configured Git Hook
#+ HOOK_TYPE|str|Name of the hook (default: all)
	@$(SCRIPTS_DIR)/console/title.sh 'yellow' 'SHOW GIT HOOKS'
	${MAKE} fallout SKIP_TITLE=true TARGET="ShowGitHooks$(if $(HOOK_TYPE), --HookType $(HOOK_TYPE),)"
