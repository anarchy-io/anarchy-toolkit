# The Makefile and scripts are distributed by Anarchy.Toolkit.Automation; this repository uses them in place.
# Literal paths and plain assignments keep shell completion working: it reads the Makefiles without running make.
MAKEFILE_PATH := $(abspath $(lastword $(MAKEFILE_LIST)))
SCRIPTS_ROOT := src/Automation/Boilerplate/

# Repository-only targets, also listed by 'make help'.
MAKE_EXTRA_TARGETS := scripts/make/targets/monorepo.mk

include src/Automation/Boilerplate/Makefile
include scripts/make/targets/monorepo.mk
