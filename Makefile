MAKEFILE_PATH := $(abspath $(lastword $(MAKEFILE_LIST)))

# Include one by one to keep CLI autocompletion support
include scripts/make-targets/globals.mk
include scripts/make-targets/app_build.mk
include scripts/make-targets/code_quality.mk
include scripts/make-targets/installation.mk
include scripts/make-targets/packaging.mk
include scripts/make-targets/utilities.mk

.PHONY: help
help: art_logo #? Prints the available make targets
	@$(MAKE) help_dynamic

.PHONY: help_dynamic
help_dynamic:
	@./scripts/utils/makefile-help.sh

%:
	@echo -e "\n${BG_RED} UNKNOWN TARGET '$@' ${CL_RESET}\n"
	@echo -e "Try running '${CL_YELLOW}make ${CL_CYAN}help${CL_RESET}' to see available commands\n"
