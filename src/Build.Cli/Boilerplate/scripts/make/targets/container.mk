#############################
## CONTAINER IMAGES
#############################
#@ anarchy-release

.PHONY: container_publish
container_publish: #? Publishes a runnable application to be included in a container image (needs IContainerBuild)
#+ APP_NAME|str|Runnable project to publish (required)
	@test -n "$(APP_NAME)" || { echo 'APP_NAME is required'; exit 1; }
	${MAKE} fallout SKIP_TITLE=true TARGET="EcrPublishAppArtifact --AppName $(APP_NAME)"

.PHONY: container_build
container_build: #? Builds the container image of a published application (needs IContainerBuild)
#+ APP_NAME|str|Runnable project of the image (required)
	@test -n "$(APP_NAME)" || { echo 'APP_NAME is required'; exit 1; }
	${MAKE} fallout SKIP_TITLE=true TARGET="EcrImageBuild --AppName $(APP_NAME)"

.PHONY: container_push
container_push: #? Pushes an application's container image to the configured registry (needs IContainerBuild)
#+ APP_NAME|str|Runnable project of the image (required)
	@test -n "$(APP_NAME)" || { echo 'APP_NAME is required'; exit 1; }
	${MAKE} fallout SKIP_TITLE=true TARGET="EcrImagePush --AppName $(APP_NAME)"
