#!/usr/bin/env bash
set -e

CL_SUBTITLE='\033[43m\033[1;30m'
CL_DEFAULT='\033[0m'
CL_HEADER='\033[1;34m'
CL_DIM='\033[2m'

HOOK_TYPE="$1"
HOOKS_DIR=".git/hooks"

if [[ -z "$HOOK_TYPE" ]]; then
	echo -e "\n${CL_SUBTITLE} ENABLED GIT HOOKS ${CL_DEFAULT}\n"

	found=0
	for hook in "$HOOKS_DIR"/*; do
		if [[ -f "$hook" && -x "$hook" && "$hook" != *.sample ]]; then
			found=1
			echo -e "\n${CL_HEADER}🔹 $(basename "$hook")${CL_DEFAULT}"
			echo -e "${CL_DIM}--------------------------------------${CL_DEFAULT}"
			cat "$hook"
		fi
	done

	if [[ $found -eq 0 ]]; then
		echo "No active hooks found in $HOOKS_DIR."
	fi
else
	HOOK_PATH="$HOOKS_DIR/$HOOK_TYPE"
	echo -e "\n${CL_SUBTITLE} GIT HOOK: $HOOK_TYPE ${CL_DEFAULT}\n"
	if [[ -f "$HOOK_PATH" ]]; then
		cat "$HOOK_PATH"
	else
		echo "No '$HOOK_TYPE' hook found at $HOOK_PATH."
	fi
fi
