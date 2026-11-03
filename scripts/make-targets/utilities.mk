#############################
## UTILITIES
#############################

.PHONY: nuke
nuke: #? Runs any target via the Nuke build project
#+ TARGET|str|Name of the target defined in the build project
ifneq ($(SKIP_TITLE),true)
	@./scripts/utils/print-title.sh 'blue' 'Running Nuke Target'
endif
ifndef TARGET
	@echo -e "\n${CL_RED}You must provide a TARGET=<TargetName> to execute a nuke task.${CL_RESET}\n"
	@NUKE_LOG_LEVEL=Information NUKE_NO_LOGO=true dotnet run --project ./build/_build.csproj -- --help
else
	NUKE_RELOAD_ENV=${NUKE_RELOAD_ENV} ./scripts/build.nuke.sh $(TARGET)
endif

.PHONY: nuke_help
nuke_help: #? Prints the Nuke build project's help information
	@./scripts/utils/print-title.sh 'blue' "Nuke's Help"
	NUKE_RELOAD_ENV=${NUKE_RELOAD_ENV} ./scripts/build.nuke.sh --help

.PHONY: upgrade_dependencies
upgrade_dependencies: #? Allows for upgrading all solution dependencies at once
#+ LOCK|str|Specifies whether package versions should be partially locked. Possible values: "Major" (default), "Minor" or "None".
	@./scripts/utils/print-title.sh 'blue' 'UPDATE DEPENDENCIES'
	VERSION_LOCK="${LOCK}" ./scripts/dev-env/upgrade-dependencies.sh

.PHONY: art_logo
art_logo:
	@./scripts/utils/ascii_art.sh

.PHONY: git_get_head_sha
git_get_head_sha: #? Gets the current HEAD SHA of the Git repository
	@echo ${GIT_HEAD_SHA}

.PHONY: git_hooks_enable
git_hooks_enable: #? Sets up a GIT hook
#+ OPTIONS|str|Set to "--help" to display more options
	@./scripts/utils/print-title.sh 'yellow' 'ENABLE GIT HOOK'
	@./scripts/utils/git-hooks/enable.sh $(OPTIONS)

.PHONY: git_hooks_disable
git_hooks_disable: #? Removes a configured Git Hook
#+ HOOK_TYPE|str|Name of the hook (default: pre-commit)
	@./scripts/utils/print-title.sh 'yellow' 'DISABLE GIT HOOK'
	@./scripts/utils/git-hooks/disable.sh $(or $(HOOK_TYPE),pre-commit)

.PHONY: git_hooks_show
git_hooks_show: #? Shows content of a configured Git Hook
#+ HOOK_TYPE|str|Name of the hook (default: all)
	@./scripts/utils/print-title.sh 'yellow' 'SHOW GIT HOOKS'
	@./scripts/utils/git-hooks/show.sh $(HOOK_TYPE)
