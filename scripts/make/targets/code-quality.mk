#############################
## CODE QUALITY
#############################

.PHONY: lint
lint: #? Runs linter analysis on the whole solution
	@./scripts/console/title.sh 'cyan' 'Running Lint Analysis'
ifeq ($(ENV),ci)
	${MAKE} nuke SKIP_TITLE=true TARGET="Lint --IsCiBuild true"
else
	${MAKE} nuke SKIP_TITLE=true TARGET="Lint"
endif

.PHONY: lint_fix
lint_fix: #? Runs the linter auto-fixer on the whole solution
#+ AUTO_STAGE|bool|Restages files during pre-commit
	@./scripts/console/title.sh 'cyan' 'Running Lint Auto-Fix'
	${MAKE} nuke SKIP_TITLE=true TARGET="LintFix$(if $(filter true,$(AUTO_STAGE)), --AutoStageLintFixes true,)"

.PHONY: test
test: #? Runs all solution tests
	@./scripts/console/title.sh 'cyan' 'Running All Tests'
	${MAKE} nuke SKIP_TITLE=true TARGET=TestAll

.PHONY: pull_request
pull_request: #? Runs all tests and validations for the Pull Request CI-pipeline
	@./scripts/console/title.sh 'cyan' 'Running Pull Request Checks'
	${MAKE} nuke SKIP_TITLE=true TARGET="PullRequest --IsCiBuild true"

.PHONY: test_unit
test_unit: #? Runs unit tests
#+ COMPILE|bool|Rebuilds the solution before running tests (DEFAULT=false)
	@./scripts/console/title.sh 'cyan' 'Running Unit Tests'
ifeq ($(COMPILE),true)
	$(MAKE) compile
endif
	${MAKE} nuke SKIP_TITLE=true TARGET="TestUnit --IsCiBuild true"

.PHONY: test_integration
test_integration: #? Runs integration tests (requires local Docker resources)
#+ COMPILE|bool|Rebuilds the solution before running tests (DEFAULT=false)
	@./scripts/console/title.sh 'cyan' 'Running Integration Tests'
ifeq ($(COMPILE),true)
	$(MAKE) compile
endif
	${MAKE} nuke SKIP_TITLE=true TARGET=TestIntegration

.PHONY: test_end_to_end
test_end_to_end: #? Runs end-to-end tests
#+ COMPILE|bool|Rebuilds the solution before running tests (DEFAULT=false)
	@./scripts/console/title.sh 'cyan' 'Running End-to-End Tests'
ifeq ($(COMPILE),true)
	$(MAKE) compile
endif
	${MAKE} nuke SKIP_TITLE=true TARGET=TestEndToEnd

.PHONY: check_layering
check_layering: #? Verifies the layered architecture: references, imports, layer folders
#+ BY_FOLDER|bool|Use layer folders under src instead of project name prefixes (DEFAULT=false)
#+ LAYERING_ROOT|str|Root folder relative to the solution (DEFAULT=src)
	@./scripts/console/title.sh 'cyan' 'Checking Layering'
	${MAKE} nuke SKIP_TITLE=true TARGET="CheckLayering --LayeringByFolder $(if $(BY_FOLDER),$(BY_FOLDER),false)$(if $(LAYERING_ROOT), --LayeringRoot '$(LAYERING_ROOT)',)"

.PHONY: dependency_map
dependency_map: #? Generates docs/dependency-map.html: project references, layers and where the code lives
	@./scripts/console/title.sh 'cyan' 'Generating Dependency Map'
	${MAKE} nuke SKIP_TITLE=true TARGET=GenerateDependencyMap
