###
# Generic
SHELL := /bin/bash
.DEFAULT_GOAL := help

###
# Colors
CL_RESET := $(shell source scripts/shared/colors.env; printf '%s' "$$CL_RESET")
CL_RED := $(shell source scripts/shared/colors.env; printf '%s' "$$CL_RED")
CL_GREEN := $(shell source scripts/shared/colors.env; printf '%s' "$$CL_GREEN")
CL_YELLOW := $(shell source scripts/shared/colors.env; printf '%s' "$$CL_YELLOW")
CL_BLUE := $(shell source scripts/shared/colors.env; printf '%s' "$$CL_BLUE")
CL_MAGENTA := $(shell source scripts/shared/colors.env; printf '%s' "$$CL_MAGENTA")
CL_CYAN := $(shell source scripts/shared/colors.env; printf '%s' "$$CL_CYAN")
CL_INVERTED := $(shell source scripts/shared/colors.env; printf '%s' "$$CL_INVERTED")
BG_RED := $(shell source scripts/shared/colors.env; printf '%s' "$$BG_RED")
BG_GREEN := $(shell source scripts/shared/colors.env; printf '%s' "$$BG_GREEN")
BG_YELLOW := $(shell source scripts/shared/colors.env; printf '%s' "$$BG_YELLOW")
BG_BLUE := $(shell source scripts/shared/colors.env; printf '%s' "$$BG_BLUE")
BG_MAGENTA := $(shell source scripts/shared/colors.env; printf '%s' "$$BG_MAGENTA")
BG_CYAN := $(shell source scripts/shared/colors.env; printf '%s' "$$BG_CYAN")

###
# Global Flags
ENV ?= dev
NUKE_RELOAD_ENV ?= true

###
# Constants
empty :=
space := $(empty) $(empty)
PROJECT_ROOT := $(dir $(MAKEFILE_PATH))
PROJECT_NAME := $(notdir $(patsubst %/,%,$(subst $(space),-,$(PROJECT_ROOT))))
PACKAGES_OUT_DIR := "$(PROJECT_ROOT)artifacts/packages"

###
# Docker Config
DOCKER_ROOT=${PROJECT_ROOT}docker
DOCKER_COMPOSE_FILE_NAME=${DOCKER_ROOT}/docker-compose.yml

# Detect Docker Compose version and assign to DOCKER_COMPOSE variable
DOCKER_VERSION := $(shell docker version --format '{{.Server.Version}}' 2>/dev/null)
DOCKER_COMPOSE_VERSION := $(shell docker compose version 2>/dev/null | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' || echo "")

# Default to legacy docker-compose if no Compose V2 is found
ifeq ($(DOCKER_COMPOSE_VERSION),)
    DOCKER_COMPOSE := docker-compose
	DOCKER_COMPOSE_VERSION := $(shell docker-compose version 2>/dev/null || echo "unknown")
else
    DOCKER_COMPOSE := docker compose
endif

DOCKER_COMPOSE_COMMAND=DOCKER_ROOT="${DOCKER_ROOT}" ${DOCKER_COMPOSE} -f "${DOCKER_COMPOSE_FILE_NAME}" --project-name "${PROJECT_NAME}"
DOCKER_COMPOSE_EXEC := ${DOCKER_COMPOSE_COMMAND} exec
DOCKER_DEV_CONSOLE := ${DOCKER_COMPOSE_EXEC} -e DEV_LOG_LEVEL=${LOGLEVEL} "${PROJECT_NAME}-dev-console"

###
# GIT
GIT_HEAD_SHA := $(shell git rev-parse --short HEAD 2>/dev/null)

.PHONY: info
info: art_logo #? Prints out host-relevant information
	@./scripts/console/title.sh 'blue' 'Environment Info'
	@echo -e "${CL_GREEN}Current Env Rules:${CL_RESET} '$(ENV)'"
	@echo -e "${CL_GREEN}Project Root:${CL_RESET} '$(PROJECT_ROOT)'"
	@echo -e "${CL_GREEN}Project Name:${CL_RESET} '$(PROJECT_NAME)'"
	@echo -e "${CL_GREEN}Commands Auto-Prefix:${CL_RESET} '$(START_COMMAND)'"
	@echo -e "${CL_GREEN}Run commands in Dev Console docker container:${CL_RESET} '$(DOCKER)'"
	@echo -e "\n${CL_GREEN}Exported Paths (\$$PATH env var):${CL_RESET}"
	@printf '%s\n' "$$PATH" | tr ':' '\n'
	@./scripts/console/title.sh 'blue' 'DOTNET'
	@echo -e "\n${CL_GREEN}Dotnet active version:${CL_RESET} '$$(dotnet --version)'"
	@echo -e "${CL_GREEN}Dotnet available SDKs:${CL_RESET}\n$$(dotnet --list-sdks)\n"
	@echo -e "${CL_GREEN}Dotnet available runtimes:${CL_RESET}\n$$(dotnet --list-runtimes)\n"
	@echo -e "${CL_CYAN}$$(dotnet nuget list source)${CL_RESET}"
	@./scripts/console/title.sh 'blue' 'PYTHON'
	@echo -e "\n${CL_GREEN}Bin:${CL_RESET} $$(python --version)"
	@echo -e "${CL_GREEN}Pip:${CL_RESET} $$(pip --version)"
	@./scripts/console/title.sh 'blue' 'JAVA'
	@echo -e "\n${CL_GREEN}Available Versions:${CL_RESET}\n$$(java --version)"
