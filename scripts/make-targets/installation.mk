#############################
## ENVIRONMENT BUILDING
#############################

.PHONY: install
install: art_logo #? Restores dependencies and compiles the solution
#+ ENV|str|Defaults to "dev", can be overriden to "ci"
#+ WITH_HOOKS|bool|Enable local GIT hooks (DEFAULT=false)
	@./scripts/utils/print-title.sh 'green' 'Installing Environment'
	@$(MAKE) install_dependencies compile
ifeq ($(WITH_HOOKS),true)
	@$(MAKE) git_hooks_enable OPTIONS="--skip-test-runtime"
endif

.PHONY: install_dependencies
install_dependencies: #? Installs the dependency packages
	@./scripts/utils/print-title.sh 'cyan' 'Installing Dependencies'
	${MAKE} nuke SKIP_TITLE=true TARGET=Restore

.PHONY: rebuild
rebuild: #? Cleans and reinstalls the local development environment
	@$(MAKE) clean
	@$(MAKE) install

.PHONY: appsettings
appsettings: #? Generates the untracked appsettings.Development.json files from their appsettings.json.template
#+ RESET|bool|Ask again for every template key, offering the current value as default (DEFAULT=false)
	@./scripts/utils/print-title.sh 'cyan' 'Generating Development AppSettings'
ifeq ($(RESET),true)
	${MAKE} nuke SKIP_TITLE=true TARGET="GenerateAppSettings --reset-app-settings"
else
	${MAKE} nuke SKIP_TITLE=true TARGET=GenerateAppSettings
endif
