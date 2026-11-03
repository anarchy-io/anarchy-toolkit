###
# Generic
SHELL := /bin/bash
.DEFAULT_GOAL := help

###
# Colors
CL_RESET := \033[0m
CL_RED := \033[31m
CL_GREEN := \033[32m
CL_YELLOW := \033[33m
CL_BLUE := \033[34m
CL_MAGENTA := \033[35m
CL_CYAN := \033[36m
CL_INVERTED := \033[37m
BG_RED := \033[41m\033[1;97m
BG_GREEN := \033[42m\033[1;97m
BG_YELLOW := \033[43m\033[1;30m
BG_BLUE := \033[44m\033[1;97m
BG_MAGENTA := \033[45m\033[1;97m
BG_CYAN := \033[46m\033[1;30m

###
# Global Flags
ENV ?= dev
NUKE_RELOAD_ENV ?= true

###
# Constants
PROJECT_ROOT := $(dir $(MAKEFILE_PATH))
PACKAGES_OUT_DIR := "$(PROJECT_ROOT)artifacts/packages"

###
# GIT
GIT_HEAD_SHA := $(shell git rev-parse --short HEAD 2>/dev/null)

.PHONY: info
info: #? Prints out host-relevant information
	@./scripts/utils/print-title.sh 'blue' 'Environment Info'
	@echo -e "${CL_GREEN}Current Env Rules:${CL_RESET} '$(ENV)'"
	@echo -e "${CL_GREEN}Project Root:${CL_RESET} '$(PROJECT_ROOT)'"
	@./scripts/utils/print-title.sh 'blue' 'DOTNET'
	@echo -e "\n${CL_GREEN}Dotnet active version:${CL_RESET} '$$(dotnet --version)'"
	@echo -e "${CL_GREEN}Dotnet available SDKs:${CL_RESET}\n$$(dotnet --list-sdks)\n"
	@echo -e "${CL_GREEN}Dotnet available runtimes:${CL_RESET}\n$$(dotnet --list-runtimes)\n"
	@echo -e "${CL_CYAN}$$(dotnet nuget list source)${CL_RESET}"
