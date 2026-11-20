###
# Generic
SHELL := /bin/bash
.DEFAULT_GOAL := help

###
# Colors
# One shell for all colors: every make invocation (including recursive ones) parses this file.
$(foreach color,$(shell source $(SCRIPTS_DIR)/shared/colors.env; printf 'CL_RESET=%s CL_RED=%s CL_GREEN=%s CL_YELLOW=%s CL_BLUE=%s CL_MAGENTA=%s CL_CYAN=%s CL_INVERTED=%s BG_RED=%s BG_GREEN=%s BG_YELLOW=%s BG_BLUE=%s BG_MAGENTA=%s BG_CYAN=%s' "$$CL_RESET" "$$CL_RED" "$$CL_GREEN" "$$CL_YELLOW" "$$CL_BLUE" "$$CL_MAGENTA" "$$CL_CYAN" "$$CL_INVERTED" "$$BG_RED" "$$BG_GREEN" "$$BG_YELLOW" "$$BG_BLUE" "$$BG_MAGENTA" "$$BG_CYAN"),$(eval $(color)))

###
# Global Flags
ENV ?= dev

###
# Constants
empty :=
space := $(empty) $(empty)
PROJECT_ROOT := $(dir $(MAKEFILE_PATH))
export PROJECT_ROOT
export SCRIPTS_DIR
PROJECT_NAME := $(notdir $(patsubst %/,%,$(subst $(space),-,$(PROJECT_ROOT))))
PACKAGES_OUT_DIR := "$(PROJECT_ROOT)artifacts/packages"

###
# Docker Config
DOCKER_ROOT=${PROJECT_ROOT}docker
DOCKER_COMPOSE_FILE_NAME_DEFAULT := ${DOCKER_ROOT}/docker-compose.yml
# Compose project names must be lower case.
DOCKER_PROJECT_NAME_DEFAULT := $(shell printf '%s' '$(PROJECT_NAME)' | tr '[:upper:]' '[:lower:]')
# Keep generated fallbacks internal, so adopter-defined public variables remain
# detectable whether assigned before or after this Makefile is included.
DOCKER_COMPOSE_FILE_NAME_EFFECTIVE = $(if $(filter undefined,$(origin DOCKER_COMPOSE_FILE_NAME)),$(DOCKER_COMPOSE_FILE_NAME_DEFAULT),$(DOCKER_COMPOSE_FILE_NAME))
DOCKER_PROJECT_NAME_EFFECTIVE = $(if $(filter undefined,$(origin DOCKER_PROJECT_NAME)),$(DOCKER_PROJECT_NAME_DEFAULT),$(DOCKER_PROJECT_NAME))

# Detected lazily, once, and only by targets that use Docker Compose: probing Docker on every make invocation is slow.
# Defaults to legacy docker-compose if no Compose V2 is found.
DOCKER_COMPOSE = $(eval DOCKER_COMPOSE := $(if $(shell docker compose version 2>/dev/null),docker compose,docker-compose))$(DOCKER_COMPOSE)

DOCKER_COMPOSE_COMMAND=DOCKER_ROOT="${DOCKER_ROOT}" ${DOCKER_COMPOSE} -f "${DOCKER_COMPOSE_FILE_NAME_EFFECTIVE}" --project-name "${DOCKER_PROJECT_NAME_EFFECTIVE}"
DOCKER_COMPOSE_EXEC = ${DOCKER_COMPOSE_COMMAND} exec
DOCKER_DEV_CONSOLE = ${DOCKER_COMPOSE_EXEC} -e DEV_LOG_LEVEL=${LOGLEVEL} "${DOCKER_PROJECT_NAME_EFFECTIVE}-dev-console"

###
# GIT
GIT_HEAD_SHA = $(shell git rev-parse --short HEAD 2>/dev/null)

.PHONY: info
info: art_logo #? Prints out host-relevant information
#@ anarchy-develop
	@$(SCRIPTS_DIR)/console/title.sh 'blue' 'Environment Info'
	@echo -e "${CL_GREEN}Current Env Rules:${CL_RESET} '$(ENV)'"
	@echo -e "${CL_GREEN}Project Root:${CL_RESET} '$(PROJECT_ROOT)'"
	@echo -e "${CL_GREEN}Project Name:${CL_RESET} '$(PROJECT_NAME)'"
	@echo -e "${CL_GREEN}Commands Auto-Prefix:${CL_RESET} '$(START_COMMAND)'"
	@echo -e "${CL_GREEN}Run commands in Dev Console docker container:${CL_RESET} '$(DOCKER)'"
	@echo -e "\n${CL_GREEN}Exported Paths (\$$PATH env var):${CL_RESET}"
	@printf '%s\n' "$$PATH" | tr ':' '\n'
	@$(SCRIPTS_DIR)/console/title.sh 'blue' 'DOTNET'
	@echo -e "\n${CL_GREEN}Dotnet active version:${CL_RESET} '$$(dotnet --version)'"
	@echo -e "${CL_GREEN}Dotnet available SDKs:${CL_RESET}\n$$(dotnet --list-sdks)\n"
	@echo -e "${CL_GREEN}Dotnet available runtimes:${CL_RESET}\n$$(dotnet --list-runtimes)\n"
	@echo -e "${CL_CYAN}$$(dotnet nuget list source)${CL_RESET}"
	@$(SCRIPTS_DIR)/console/title.sh 'blue' 'PYTHON'
	@echo -e "\n${CL_GREEN}Bin:${CL_RESET} $$(python --version)"
	@echo -e "${CL_GREEN}Pip:${CL_RESET} $$(pip --version)"
	@$(SCRIPTS_DIR)/console/title.sh 'blue' 'JAVA'
	@echo -e "\n${CL_GREEN}Available Versions:${CL_RESET}\n$$(java --version)"
