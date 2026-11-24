##############################
## LOCAL CONTAINER MANAGEMENT
##############################
#@ anarchy-environment

# Generated Make defaults must not mask saved preferences. Explicit adopter values
# (command-line, environment, or including Makefile) remain build overrides.
DOCKER_TARGET_OPTIONS = $(if $(filter undefined,$(origin DOCKER_COMPOSE_FILE_NAME)),,--DockerComposeFile '$(DOCKER_COMPOSE_FILE_NAME)')$(if $(filter undefined,$(origin DOCKER_PROJECT_NAME)),, --DockerProjectName '$(DOCKER_PROJECT_NAME)')
DOCKER_START_OPTIONS = $(DOCKER_TARGET_OPTIONS)$(if $(ARGS), --DockerArgs '$(ARGS)',)$(if $(filter undefined,$(origin WAIT_FOR)),, --DockerWaitFor $(WAIT_FOR))$(if $(filter undefined,$(origin HEALTHY)),, --DockerWaitForHealthy $(HEALTHY))
FLYWAY_TARGET_OPTIONS = $(if $(FLYWAY_IMAGE), --FlywayImage '$(FLYWAY_IMAGE)')$(if $(FLYWAY_CONFIG_FILES), --FlywayConfigFiles $(FLYWAY_CONFIG_FILES))$(if $(FLYWAY_LOCATIONS), --FlywayLocations $(FLYWAY_LOCATIONS))$(if $(FLYWAY_NETWORK), --FlywayNetwork '$(FLYWAY_NETWORK)')

.PHONY: docker_info
docker_info: #? Outputs info about the host's Docker engine
	@$(SCRIPTS_DIR)/console/title.sh 'blue' 'Checking Docker Installation...'
	${MAKE} anarchy SKIP_TITLE=true TARGET="DockerInfo $(DOCKER_TARGET_OPTIONS)"

.PHONY: docker_stopall
docker_stopall: #? Stops all docker containers running in the host (if any)
	@$(SCRIPTS_DIR)/console/title.sh 'red' 'Stopping all Docker containers running in the host'
	${MAKE} anarchy SKIP_TITLE=true TARGET="DockerStopAll $(DOCKER_TARGET_OPTIONS)"

.PHONY: container_logs
container_logs: #? Prints the logs of running Docker containers
#+ CONTAINER|str|Allows to filter the logs of one specific container by name
#+ ARGS|str|Allows extra arguments to Docker's log command (default="")
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'DOCKER CONTAINER LOGS'
	${MAKE} anarchy SKIP_TITLE=true TARGET="DockerLogs $(DOCKER_TARGET_OPTIONS)$(if $(CONTAINER), --DockerContainer $(CONTAINER),)$(if $(ARGS), --DockerArgs '$(ARGS)',)"

.PHONY: start
start: #? Starts the development environment
#+ ARGS|str|Allows extra arguments to Docker Compose's UP command (default="")
#+ WAIT_FOR|str|Space-separated containers to wait for after starting (default: saved docker.waitFor, otherwise none; set WAIT_FOR= to clear)
#+ HEALTHY|bool|Waits for WAIT_FOR containers to be healthy rather than running (default: saved docker.waitForHealthy, otherwise false)
	@$(SCRIPTS_DIR)/console/title.sh 'yellow' 'Starting the local development environment...'
	${MAKE} anarchy SKIP_TITLE=true TARGET="DockerStart $(DOCKER_START_OPTIONS)"

.PHONY: stop
stop: #? Stops the development environment
	@$(SCRIPTS_DIR)/console/title.sh 'red' 'Stopping the local development environment...'
	${MAKE} anarchy SKIP_TITLE=true TARGET="DockerStop $(DOCKER_TARGET_OPTIONS)"

.PHONY: restart
restart: #? Restarts the development environment
#+ CONTAINER|str|Restarts only one specific container by name
	@$(SCRIPTS_DIR)/console/title.sh 'yellow' 'Restarting the local development environment...'
	${MAKE} anarchy SKIP_TITLE=true TARGET="DockerRestart $(DOCKER_TARGET_OPTIONS)$(if $(CONTAINER), --DockerContainer $(CONTAINER),)"

.PHONY: destroy
destroy: #? Destroys the development environment
#! This removes the environment's volumes, and the data in them!
	@$(SCRIPTS_DIR)/console/title.sh 'red' 'Destroying the local development environment...'
	${MAKE} anarchy SKIP_TITLE=true TARGET="DockerDestroy $(DOCKER_TARGET_OPTIONS)"

.PHONY: wait_running_container
wait_running_container: #? Waits for a container to be running
#+ CONTAINER|str|Name of the container (required)
#+ TIMEOUT|int|Seconds to wait (default=60)
	@test -n "$(CONTAINER)" || { echo 'CONTAINER is required'; exit 1; }
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Verify container is running'
	${MAKE} anarchy SKIP_TITLE=true TARGET="DockerWaitRunning $(DOCKER_TARGET_OPTIONS) --DockerContainer $(CONTAINER)$(if $(TIMEOUT), --DockerTimeout $(TIMEOUT),)"

.PHONY: wait_healthy_container
wait_healthy_container: #? Waits for a container to be running, then for its health check to pass
#+ CONTAINER|str|Name of the container (required)
#+ TIMEOUT|int|Seconds to wait for it to become healthy (default=60)
#+ START_TIMEOUT|int|Seconds to wait for it to be running first (default=180)
	@test -n "$(CONTAINER)" || { echo 'CONTAINER is required'; exit 1; }
	@$(SCRIPTS_DIR)/console/title.sh 'green' 'Verify container is Healthy'
	${MAKE} anarchy SKIP_TITLE=true TARGET="DockerWaitHealthy $(DOCKER_TARGET_OPTIONS) --DockerContainer $(CONTAINER)$(if $(TIMEOUT), --DockerTimeout $(TIMEOUT),)$(if $(START_TIMEOUT), --DockerStartTimeout $(START_TIMEOUT),)"

.PHONY: prepare_test_environment
prepare_test_environment: #? Starts and waits for the opted-in local test environment, then migrates if Flyway is enabled
#+ ARGS|str|Extra Docker Compose up arguments
#+ WAIT_FOR|str|Space-separated containers to wait for (default: saved docker.waitFor, otherwise none)
#+ HEALTHY|bool|Waits for the containers to be healthy rather than running (default: saved docker.waitForHealthy, otherwise false)
#+ FLYWAY_IMAGE|str|Flyway CLI image with tag or digest (default: redgate/flyway:11.8.2, if enabled)
#+ FLYWAY_CONFIG_FILES|str|Space-separated configuration files to mount, in loading order (required for Flyway unless configured)
#+ FLYWAY_LOCATIONS|str|Space-separated local migration directories to mount
#+ FLYWAY_NETWORK|str|Docker network for the Flyway container to join
	@$(SCRIPTS_DIR)/console/title.sh 'yellow' 'Preparing the local test environment...'
	${MAKE} anarchy SKIP_TITLE=true TARGET="PrepareTestEnvironment $(DOCKER_START_OPTIONS)$(FLYWAY_TARGET_OPTIONS)"

.PHONY: flyway_migrate
flyway_migrate: #? Applies pending migrations in a one-shot Flyway container
#+ FLYWAY_IMAGE|str|Flyway CLI image with tag or digest (default: redgate/flyway:11.8.2)
#+ FLYWAY_CONFIG_FILES|str|Space-separated configuration files to mount, in loading order (required unless configured)
#+ FLYWAY_LOCATIONS|str|Space-separated local migration directories to mount
#+ FLYWAY_NETWORK|str|Docker network to join (default: Docker's default network)
	@$(SCRIPTS_DIR)/console/title.sh 'yellow' 'Applying Flyway migrations...'
	${MAKE} anarchy SKIP_TITLE=true TARGET="FlywayMigrate$(FLYWAY_TARGET_OPTIONS)"

.PHONY: flyway_info
flyway_info: #? Shows migration status in a one-shot Flyway container without starting Compose
#+ FLYWAY_IMAGE|str|Flyway CLI image with tag or digest (default: redgate/flyway:11.8.2)
#+ FLYWAY_CONFIG_FILES|str|Space-separated configuration files to mount, in loading order (required unless configured)
#+ FLYWAY_LOCATIONS|str|Space-separated local migration directories to mount
#+ FLYWAY_NETWORK|str|Docker network to join (default: Docker's default network)
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Showing Flyway migration status...'
	${MAKE} anarchy SKIP_TITLE=true TARGET="FlywayInfo$(FLYWAY_TARGET_OPTIONS)"
