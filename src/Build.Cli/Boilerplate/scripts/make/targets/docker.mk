##############################
## LOCAL CONTAINER MANAGEMENT
##############################

# Requires the build project to implement IDockerBuild. The Compose file defaults to docker/docker-compose.yml.
DOCKER_TARGET_OPTIONS = --DockerComposeFile "$(DOCKER_COMPOSE_FILE_NAME)" --DockerProjectName "$(DOCKER_PROJECT_NAME)"

.PHONY: docker_info
docker_info: #? Outputs info about the host's Docker engine
	@$(SCRIPTS_DIR)/console/title.sh 'blue' 'Checking Docker Installation...'
	${MAKE} fallout SKIP_TITLE=true TARGET="DockerInfo $(DOCKER_TARGET_OPTIONS)"

.PHONY: docker_stopall
docker_stopall: #? Stops all docker containers running in the host (if any)
	@$(SCRIPTS_DIR)/console/title.sh 'red' 'Stopping all Docker containers running in the host'
	${MAKE} fallout SKIP_TITLE=true TARGET="DockerStopAll $(DOCKER_TARGET_OPTIONS)"

.PHONY: container_logs
container_logs: #? Prints the logs of running Docker containers
#+ CONTAINER|str|Allows to filter the logs of one specific container by name
#+ ARGS|str|Allows extra arguments to Docker's log command (default="")
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'DOCKER CONTAINER LOGS'
	${MAKE} fallout SKIP_TITLE=true TARGET="DockerLogs $(DOCKER_TARGET_OPTIONS)$(if $(CONTAINER), --DockerContainer $(CONTAINER),)$(if $(ARGS), --DockerArgs '$(ARGS)',)"

.PHONY: start
start: #? Starts the development environment
#+ ARGS|str|Allows extra arguments to Docker Compose's UP command (default="")
#+ WAIT_FOR|str|Space-separated containers to wait for after starting (default="")
#+ HEALTHY|bool|Waits for WAIT_FOR containers to be healthy rather than running (default=false)
	@$(SCRIPTS_DIR)/console/title.sh 'yellow' 'Starting the local development environment...'
	${MAKE} fallout SKIP_TITLE=true TARGET="DockerStart $(DOCKER_TARGET_OPTIONS)$(if $(ARGS), --DockerArgs '$(ARGS)',)$(if $(WAIT_FOR), --DockerWaitFor $(WAIT_FOR),)$(if $(filter true,$(HEALTHY)), --DockerWaitForHealthy true,)"

.PHONY: stop
stop: #? Stops the development environment
	@$(SCRIPTS_DIR)/console/title.sh 'red' 'Stopping the local development environment...'
	${MAKE} fallout SKIP_TITLE=true TARGET="DockerStop $(DOCKER_TARGET_OPTIONS)"

.PHONY: restart
restart: #? Restarts the development environment
#+ CONTAINER|str|Restarts only one specific container by name
	@$(SCRIPTS_DIR)/console/title.sh 'yellow' 'Restarting the local development environment...'
	${MAKE} fallout SKIP_TITLE=true TARGET="DockerRestart $(DOCKER_TARGET_OPTIONS)$(if $(CONTAINER), --DockerContainer $(CONTAINER),)"

.PHONY: destroy
destroy: #? Destroys the development environment
#! This removes the environment's volumes, and the data in them!
	@$(SCRIPTS_DIR)/console/title.sh 'red' 'Destroying the local development environment...'
	${MAKE} fallout SKIP_TITLE=true TARGET="DockerDestroy $(DOCKER_TARGET_OPTIONS)"

.PHONY: wait_running_container
wait_running_container: #? Waits for a container to be running
#+ CONTAINER|str|Name of the container (required)
#+ TIMEOUT|int|Seconds to wait (default=60)
	@test -n "$(CONTAINER)" || { echo 'CONTAINER is required'; exit 1; }
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Verify container is running'
	${MAKE} fallout SKIP_TITLE=true TARGET="DockerWaitRunning $(DOCKER_TARGET_OPTIONS) --DockerContainer $(CONTAINER)$(if $(TIMEOUT), --DockerTimeout $(TIMEOUT),)"

.PHONY: wait_healthy_container
wait_healthy_container: #? Waits for a container to be running, then for its health check to pass
#+ CONTAINER|str|Name of the container (required)
#+ TIMEOUT|int|Seconds to wait for it to become healthy (default=60)
#+ START_TIMEOUT|int|Seconds to wait for it to be running first (default=180)
	@test -n "$(CONTAINER)" || { echo 'CONTAINER is required'; exit 1; }
	@$(SCRIPTS_DIR)/console/title.sh 'green' 'Verify container is Healthy'
	${MAKE} fallout SKIP_TITLE=true TARGET="DockerWaitHealthy $(DOCKER_TARGET_OPTIONS) --DockerContainer $(CONTAINER)$(if $(TIMEOUT), --DockerTimeout $(TIMEOUT),)$(if $(START_TIMEOUT), --DockerStartTimeout $(START_TIMEOUT),)"
