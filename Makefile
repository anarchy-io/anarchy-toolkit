# The Makefile and scripts are distributed by Anarchy.Toolkit.Build.Cli; this repository uses them in place.
MAKEFILE_PATH := $(abspath $(lastword $(MAKEFILE_LIST)))
SCRIPTS_DIR := src/Build.Cli/Boilerplate/scripts

include src/Build.Cli/Boilerplate/Makefile
