#############################
## ENVIRONMENT BUILDING
#############################

.PHONY: install
install: art_logo #? Sets up the development environment, restores dependencies and compiles
#+ ENV|str|Defaults to "dev"; "ci" skips local environment setup
#+ WITH_HOOKS|bool|Enable local GIT hooks (DEFAULT=false)
	@./scripts/console/title.sh 'green' 'Installing Environment'
ifneq ($(ENV),ci)
	@$(MAKE) install_dev_env
endif
	@$(MAKE) install_dependencies compile
ifeq ($(WITH_HOOKS),true)
	@$(MAKE) git_hooks_enable OPTIONS="--skip-test-runtime"
endif

.PHONY: install_dev_env
install_dev_env: #? Installs the SDK and configures the local development environment
	@./scripts/bootstrap/install-development-environment.sh

.PHONY: install_sdk
install_sdk: #? Installs or validates the .NET SDK required by global.json
	@./scripts/bootstrap/install-sdk.sh

.PHONY: install_dependencies
install_dependencies: #? Installs the dependency packages
	@./scripts/console/title.sh 'cyan' 'Installing Dependencies'
	${MAKE} nuke SKIP_TITLE=true TARGET=Restore

.PHONY: rebuild
rebuild: #? Cleans and reinstalls the local development environment
	@$(MAKE) clean
	@$(MAKE) install

.PHONY: appsettings
appsettings: #? Generates the untracked appsettings.Development.json files from their appsettings.json.template
#+ RESET|bool|Ask again for every template key, offering the current value as default (DEFAULT=false)
	@./scripts/console/title.sh 'cyan' 'Generating Development AppSettings'
ifeq ($(RESET),true)
	${MAKE} nuke SKIP_TITLE=true TARGET="GenerateAppSettings --reset-app-settings"
else
	${MAKE} nuke SKIP_TITLE=true TARGET=GenerateAppSettings
endif
