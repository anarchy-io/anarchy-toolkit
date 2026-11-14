# The Makefile and scripts are distributed by Anarchy.Toolkit.Build.Cli; this repository uses them in place.
MAKEFILE_PATH := $(abspath $(lastword $(MAKEFILE_LIST)))
SCRIPTS_DIR := src/Build.Cli/Boilerplate/scripts

# Repository-only targets, also listed by 'make help'.
export MAKE_EXTRA_TARGETS := scripts/make/targets/monorepo.mk

include src/Build.Cli/Boilerplate/Makefile
include $(MAKE_EXTRA_TARGETS)
