#!/usr/bin/env bash

########################################################################
#       __      __  ___     __  ___         __     _                   #
#      / /     /  |/  /    /  |/  /__  ____/ /__  (_)________  _____   #
#     / /     / /|_/ /    / /|_/ / _ \/ __  / _ \/ / ___/ __ \/ ___/   #
#    / /____ / /  / /    / /  / /  __/ /_/ /  __/ / /  / /_/ (__  )    #
#   /_____(_)_/  /_(_)  /_/  /_/\___/\__,_/\___/_/_/   \____/____/     #
#                                                                      #
#   Leandro M. Medeiros                                                #
#   leandro.m.medeiros@me.com                                          #
########################################################################

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${SCRIPT_DIR}/../shared/colors.env"

if [[ $(TERM=${TERM:-xterm} tput cols 2>/dev/null || echo 90) -gt 115 ]]; then
    "${SCRIPT_DIR}/banner-wide.sh"
elif [[ $(TERM=${TERM:-xterm} tput cols 2>/dev/null || echo 90) -gt 65 ]]; then
    "${SCRIPT_DIR}/banner-compact.sh"
else
    echo -e "\n${CL_GRAY_BOLD} ANARCHY I/${CL_RED_BOLD}O ${CL_YELLOW_BOLD}TOOLKIT${CL_RESET}\n"
fi

exit 0;
