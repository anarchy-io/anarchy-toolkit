ifndef MAKEFILE_PATH
MAKEFILE_PATH := $(abspath $(lastword $(MAKEFILE_LIST)))
endif
# Where the scripts live, relative to the project root. An including Makefile or the command line may set it; a
# value inherited from another project's make (which exports it) is ignored.
ifneq ($(filter undefined environment,$(origin SCRIPTS_DIR)),)
SCRIPTS_DIR := scripts
endif
# Project-specific target files listed by 'make help'; likewise only from an including Makefile or the command line.
ifneq ($(filter undefined environment,$(origin MAKE_EXTRA_TARGETS)),)
MAKE_EXTRA_TARGETS :=
endif
export MAKE_EXTRA_TARGETS

# Include one by one to keep CLI autocompletion support
include $(SCRIPTS_DIR)/make/targets/globals.mk
include $(SCRIPTS_DIR)/make/targets/application.mk
include $(SCRIPTS_DIR)/make/targets/code-quality.mk
include $(SCRIPTS_DIR)/make/targets/docker.mk
include $(SCRIPTS_DIR)/make/targets/installation.mk
include $(SCRIPTS_DIR)/make/targets/packaging.mk
include $(SCRIPTS_DIR)/make/targets/utilities.mk

.PHONY: help
help: art_logo #? Prints the available make targets
	@$(MAKE) help_dynamic

.PHONY: help_dynamic
help_dynamic:
	@$(SCRIPTS_DIR)/make/help.sh

%:
	@echo -e "\n${BG_RED} UNKNOWN TARGET '$@' ${CL_RESET}\n"
	@echo -e "Try running '${CL_YELLOW}make ${CL_CYAN}help${CL_RESET}' to see available commands\n"
