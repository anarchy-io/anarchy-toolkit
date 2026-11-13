#############################
## CODE QUALITY
#############################

.PHONY: lint
lint: #? Runs linter analysis on the whole solution
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Running Lint Analysis'
ifeq ($(ENV),ci)
	${MAKE} fallout SKIP_TITLE=true TARGET="Lint --IsCiBuild true"
else
	${MAKE} fallout SKIP_TITLE=true TARGET="Lint"
endif

.PHONY: lint_fix
lint_fix: #? Runs the linter auto-fixer on the whole solution
#+ AUTO_STAGE|bool|Restages files during pre-commit
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Running Lint Auto-Fix'
	${MAKE} fallout SKIP_TITLE=true TARGET="LintFix$(if $(filter true,$(AUTO_STAGE)), --AutoStageLintFixes true,)"

.PHONY: test
test: #? Runs all solution tests
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Running All Tests'
	${MAKE} fallout SKIP_TITLE=true TARGET=TestAll

.PHONY: pull_request
pull_request: #? Runs all tests and validations for the Pull Request CI-pipeline
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Running Pull Request Checks'
	${MAKE} fallout SKIP_TITLE=true TARGET="PullRequest --IsCiBuild true"

.PHONY: test_unit
test_unit: #? Runs unit tests
#+ COMPILE|bool|Rebuilds the solution before running tests (DEFAULT=false)
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Running Unit Tests'
ifeq ($(COMPILE),true)
	$(MAKE) compile
endif
	${MAKE} fallout SKIP_TITLE=true TARGET="TestUnit --IsCiBuild true"

.PHONY: test_integration
test_integration: #? Runs integration tests (requires local Docker resources)
#+ COMPILE|bool|Rebuilds the solution before running tests (DEFAULT=false)
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Running Integration Tests'
ifeq ($(COMPILE),true)
	$(MAKE) compile
endif
	${MAKE} fallout SKIP_TITLE=true TARGET=TestIntegration

.PHONY: test_end_to_end
test_end_to_end: #? Runs end-to-end tests
#+ COMPILE|bool|Rebuilds the solution before running tests (DEFAULT=false)
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Running End-to-End Tests'
ifeq ($(COMPILE),true)
	$(MAKE) compile
endif
	${MAKE} fallout SKIP_TITLE=true TARGET=TestEndToEnd

.PHONY: check_layering
check_layering: #? Verifies the layered architecture: references, imports, layer folders
#+ BY_FOLDER|bool|Use layer folders instead of project name prefixes (DEFAULT=LayeringByFolder in .fallout/parameters.json, else false)
#+ LAYERING_ROOT|str|Root folder relative to the solution (DEFAULT=LayeringRoot in .fallout/parameters.json, else src)
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Checking Layering'
	${MAKE} fallout SKIP_TITLE=true TARGET="CheckLayering$(if $(BY_FOLDER), --LayeringByFolder $(BY_FOLDER),)$(if $(LAYERING_ROOT), --LayeringRoot '$(LAYERING_ROOT)',)"

.PHONY: dependency_map
dependency_map: #? Generates docs/dependency-map.html: project references, layers and where the code lives
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Generating Dependency Map'
	${MAKE} fallout SKIP_TITLE=true TARGET=GenerateDependencyMap
