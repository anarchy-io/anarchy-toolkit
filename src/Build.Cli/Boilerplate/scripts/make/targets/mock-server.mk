##############################
## MOCK SERVER
##############################

# Requires the build project to implement IMockServerBuild and the solution to hold a mock server host: a runnable
# project whose name ends in MockServer (or MOCK_PROJECT) calling MockServerEntryPoint.RunAsync(args).
MOCK_SERVER_OPTIONS = $(if $(MOCK_PROJECT), --MockServerProject '$(MOCK_PROJECT)',)$(if $(PORT), --MockServerPort $(PORT),)$(if $(EXPECTATIONS), --MockServerExpectations $(EXPECTATIONS),)$(if $(RECORD), --MockServerRecord '$(RECORD)',)$(if $(VERBOSITY), --MockServerVerbosity $(VERBOSITY),)$(if $(ARGS), --MockServerArgs '$(ARGS)',)

.PHONY: mock_server
mock_server: #? Runs the mock server in the foreground until Ctrl+C
#+ MOCK_PROJECT|str|Mock server host project (default: the runnable project whose name ends in MockServer)
#+ PORT|int|Port (default: 6000, or the next free one)
#+ EXPECTATIONS|str|Space-separated expectation files, directories or globs
#+ RECORD|str|Record file (default: artifacts/mock-server/requests.jsonl)
#+ VERBOSITY|str|quiet, minimal, normal, detailed or diagnostic (default: normal)
#+ ARGS|str|Extra mock server arguments, e.g. '--watch'
	@$(SCRIPTS_DIR)/console/title.sh 'green' 'Running the mock server'
	${MAKE} fallout SKIP_TITLE=true TARGET="MockServerRun$(MOCK_SERVER_OPTIONS)"

.PHONY: mock_server_start
mock_server_start: #? Starts the mock server in the background
#+ MOCK_PROJECT|str|Mock server host project (default: the runnable project whose name ends in MockServer)
#+ PORT|int|Port (default: 6000, or the next free one)
#+ EXPECTATIONS|str|Space-separated expectation files, directories or globs
#+ RECORD|str|Record file (default: artifacts/mock-server/requests.jsonl)
#+ VERBOSITY|str|quiet, minimal, normal, detailed or diagnostic (default: normal)
#+ ARGS|str|Extra mock server arguments, e.g. '--watch'
	@$(SCRIPTS_DIR)/console/title.sh 'green' 'Starting the mock server'
	${MAKE} fallout SKIP_TITLE=true TARGET="MockServerStart$(MOCK_SERVER_OPTIONS)"

.PHONY: mock_server_stop
mock_server_stop: #? Stops the background mock server
	@$(SCRIPTS_DIR)/console/title.sh 'red' 'Stopping the mock server'
	${MAKE} fallout SKIP_TITLE=true TARGET=MockServerStop

.PHONY: test_integration_mocked
test_integration_mocked: #? Runs integration tests with the mock server (MOCKSERVER_URL)
#+ EXPECTATIONS|str|Space-separated expectation files, directories or globs
#+ VERBOSITY|str|quiet, minimal, normal, detailed or diagnostic (default: normal)
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Running Integration Tests with the mock server'
	${MAKE} fallout SKIP_TITLE=true TARGET="TestIntegrationWithMockServer$(MOCK_SERVER_OPTIONS)"

.PHONY: test_end_to_end_mocked
test_end_to_end_mocked: #? Runs end-to-end tests with the mock server (MOCKSERVER_URL)
#+ EXPECTATIONS|str|Space-separated expectation files, directories or globs
#+ VERBOSITY|str|quiet, minimal, normal, detailed or diagnostic (default: normal)
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Running End-to-End Tests with the mock server'
	${MAKE} fallout SKIP_TITLE=true TARGET="TestEndToEndWithMockServer$(MOCK_SERVER_OPTIONS)"
