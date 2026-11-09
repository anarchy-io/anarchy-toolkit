#!/usr/bin/env bash

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
source "${SCRIPT_DIR}/../shared/colors.env"

print_title() {
    local color="$1"
    local message="$(echo "$2" | tr '[:lower:]' '[:upper:]')"

    case "$color" in
        black)   bg_color="$BG_BLACK" ;;
        red)     bg_color="$BG_RED" ;;
        green)   bg_color="$BG_GREEN" ;;
        yellow)  bg_color="$BG_YELLOW" ;;
        blue)    bg_color="$BG_BLUE" ;;
        magenta) bg_color="$BG_MAGENTA" ;;
        cyan)    bg_color="$BG_CYAN" ;;
        white)   bg_color="$BG_WHITE" ;;
        gray)    bg_color="$BG_GRAY" ;;
        *)       bg_color="$BG_DEFAULT" ;;
    esac

    local term_width=$(TERM=${TERM:-xterm} tput cols 2>/dev/null || echo 90)
    local padding=$(( (term_width - ${#message}) / 2 ))
    (( padding < 0 )) && padding=0
    printf '\n%b%*s%s%*s%b\n' "$bg_color" "$padding" "" "$message" "$padding" "" "$CL_RESET"
}

print_title "$1" "$2"
