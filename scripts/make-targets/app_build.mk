#############################
## PROJECT COMPILATION
#############################

.PHONY: clean
clean: #? Cleans build outputs across the solution
	@./scripts/utils/print-title.sh 'red' 'Cleaning Up Solution'
	${MAKE} nuke SKIP_TITLE=true TARGET=Clean

.PHONY: build
build: #? Cleans and builds the full solution
	@./scripts/utils/print-title.sh 'yellow' 'Building Solution'
	${MAKE} nuke SKIP_TITLE=true TARGET=Build

.PHONY: compile
compile: #? Compiles the solution
	@./scripts/utils/print-title.sh 'green' 'Compiling Solution'
	${MAKE} nuke SKIP_TITLE=true TARGET=Compile

.PHONY: list_projects
list_projects: #? Lists all projects within the solution
	${MAKE} nuke SKIP_TITLE=true TARGET=ListProjects

.PHONY: run_app
run_app: #? Runs one of the runnable (OutputType=Exe) projects under src/
#+ APP_NAME|str|Name of the application to run
	@./scripts/utils/print-title.sh 'green' 'Running App: ${APP_NAME}'
	${MAKE} nuke SKIP_TITLE=true TARGET="RunApp --AppName ${APP_NAME}"

.PHONY: run_apps
run_apps: #? Runs multiple runnable (OutputType=Exe) projects under src/ in parallel
#+ APP_NAMES|str|Comma-separated names of the applications to run
	@./scripts/utils/print-title.sh 'green' 'Running Apps: ${APP_NAMES}'
	${MAKE} nuke SKIP_TITLE=true TARGET="RunApps --AppNames ${APP_NAMES}"
