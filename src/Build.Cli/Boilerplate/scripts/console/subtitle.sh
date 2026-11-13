#!/usr/bin/env bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${SCRIPT_DIR}/../shared/colors.env"

echo -e "\n${BG_MAGENTA} ${1} ${CL_RESET}\n"
