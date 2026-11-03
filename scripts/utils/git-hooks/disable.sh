#!/usr/bin/env bash
set -e

CL_SUBTITLE='\033[43m\033[1;30m'
CL_DEFAULT='\033[0m'
CL_SUCCESS='\033[0;32m'
CL_ERROR='\033[0;31m'

HOOK_TYPE="$1"
HOOKS_DIR=".git/hooks"
HOOK_PATH="$HOOKS_DIR/$HOOK_TYPE"

if [[ -z "$HOOK_TYPE" ]]; then
	echo -e "${CL_ERROR}❌ No hook type provided. Usage: ./disable-hook.sh <hook-type>${CL_DEFAULT}"
	exit 1
fi

echo -e "\n${CL_SUBTITLE} DISABLING GIT HOOK: $HOOK_TYPE ${CL_DEFAULT}\n"

if [[ -f "$HOOK_PATH" ]]; then
	rm -f "$HOOK_PATH"
	echo -e "${CL_SUCCESS}✅  Hook '$HOOK_TYPE' removed.${CL_DEFAULT}"
else
	echo -e "${CL_ERROR}🛑  No '$HOOK_TYPE' hook found at $HOOK_PATH.${CL_DEFAULT}"
fi
