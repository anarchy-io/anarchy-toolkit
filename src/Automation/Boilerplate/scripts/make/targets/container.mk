#############################
## CONTAINER IMAGES
#############################
#@ anarchy-release

.PHONY: container_publish_app_artifact
container_publish_app_artifact: #? Publishes a runnable application to be included in a container image (needs IContainerBuild)
#+ APP_NAME|str|Runnable project to publish (required)
	@test -n "$(APP_NAME)" || { echo 'APP_NAME is required'; exit 1; }
	${MAKE} anarchy SKIP_TITLE=true TARGET="ContainerPublishAppArtifact --AppName $(APP_NAME)"

.PHONY: container_image_build
container_image_build: #? Builds the container image of a published application (needs IContainerBuild)
#+ APP_NAME|str|Runnable project of the image (required)
	@test -n "$(APP_NAME)" || { echo 'APP_NAME is required'; exit 1; }
	${MAKE} anarchy SKIP_TITLE=true TARGET="ContainerImageBuild --AppName $(APP_NAME)"

.PHONY: container_image_push
container_image_push: #? Pushes an application's container image to the configured registry (needs IContainerBuild)
#+ APP_NAME|str|Runnable project of the image (required)
	@test -n "$(APP_NAME)" || { echo 'APP_NAME is required'; exit 1; }
	${MAKE} anarchy SKIP_TITLE=true TARGET="ContainerImagePush --AppName $(APP_NAME)"
