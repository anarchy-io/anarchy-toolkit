#!/usr/bin/env bash

print_title() {
    local color="$1"
    local message="$(echo "$2" | tr '[:lower:]' '[:upper:]')"

    case "$color" in
        black)   bg_color_code="40";  fg_color_code="97";; # Bright white text on black background
        red)     bg_color_code="41";  fg_color_code="97";; # Bright white text on red background
        green)   bg_color_code="42";  fg_color_code="97";; # Bright white text on green background
        yellow)  bg_color_code="43";  fg_color_code="30";; # Black text on yellow background
        blue)    bg_color_code="44";  fg_color_code="97";; # Bright white text on blue background
        magenta) bg_color_code="45";  fg_color_code="97";; # Bright white text on magenta background
        cyan)    bg_color_code="46";  fg_color_code="30";; # Black text on cyan background
        white)   bg_color_code="47";  fg_color_code="30";; # Black text on white background
        gray)    bg_color_code="100"; fg_color_code="97";; # Bright white text on gray background
        *)       bg_color_code="49";  fg_color_code="97";; # Default to default background color, bright white text
    esac

    local color_reset="\033[0m"
    local term_width=$(TERM=${TERM:-xterm} tput cols)
    local padding=$(( (term_width - ${#message}) / 2 ))
    printf "\n\033[${bg_color_code}m\033[1;${fg_color_code}m%*s%s%*s${color_reset}\n" $padding "" "$message" $padding ""
}

print_title "$1" "$2"
