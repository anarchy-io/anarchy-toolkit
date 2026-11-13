#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
source "${SCRIPT_DIR}/../shared/colors.env"
# printf below does not expand backslash escapes inside arguments.
for color in CL_YELLOW CL_CYAN CL_RED CL_GREEN CL_BLUE CL_RESET CL_MAGENTA; do
    printf -v "$color" '%b' "${!color}"
done
COMMAND="${COMMAND:-}"

# ─── Helper: wrap text with fixed offset for continuation lines ──────────────
soft_wrap() {
    local text="$1"
    local offset_current="$2"
    local offset_new_lines="${3:-$offset_current}"
    #local max_width=$(TERM=${TERM:-xterm} tput cols || echo 90)
    local max_width=90
    local line="" word=""
    local result=""
    local available=$((max_width - offset_current))

    for word in $text; do
        if [ $((${#line} + ${#word} + 1)) -gt $available ]; then
            result+="$line"$'\n'"$(printf '%*s' "$offset_new_lines" '')"
            line="$word"
        else
            if [ -z "$line" ]; then
                line="$word"
            else
                line="$line $word"
            fi
        fi
    done

    result+="$line"
    printf "%s" "$result"
}

# ─── Printing Helpers ─────────────────────────────────────────────────────────
print_file_section() {
    local filename="$1"
    echo -e "${CL_GREEN}${1}:${CL_RESET}"
}

print_target() {
    local name="$1" desc="$2"
    printf "  %s%-20s%s %s\n" "$CL_CYAN" "$name" "$CL_RESET" "$(soft_wrap "$desc" "20" "23")"
}

print_warning() {
    local text="$1"
    printf "    %-26s%s%s\n" "$CL_RED" "$(soft_wrap "$text" "26" "23")" "$CL_RESET"
}

print_variable() {
    local name="$1" type="$2" desc="$3"
    local formatted_type="$CL_RESET($CL_YELLOW$type$CL_RESET)"
    printf "    ↳ %s%-11s %-23s %s%s%s\n" \
        "$CL_MAGENTA" "$name" \
        "$formatted_type" \
        "$CL_BLUE" "$(soft_wrap "$desc" "23" "27")" "$CL_RESET"
}

# ─── Main ─────────────────────────────────────────────────────────────────────
echo -e "To run a task execute '${CL_YELLOW}make${CL_RESET} ${CL_CYAN}[task_name]${CL_MAGENTA} [...FLAG=value]${CL_YELLOW}'${CL_RESET}\n"

if [[ -z "$COMMAND" ]]; then
    echo -e "${CL_YELLOW}The available tasks are:${CL_RESET}\n"
fi

for file in "${SCRIPT_DIR}"/targets/*.mk; do
    filename=$(basename "$file" .mk | tr 'a-z' 'A-Z' | tr '_-' '  ')
    print_file_section "$filename"

    awk -v COMMAND="${COMMAND}" '
    function print_targets(    i) {
        for (i = 1; i <= count; i++) {
            if (COMMAND == "" || COMMAND == names[i]) {
                print "TARGET|" names[i] "|" desc
                if (notes != "") printf "%s", notes
                print "END"
            }
        }
    }

    /^[a-zA-Z0-9_.-]+([ \t]+[a-zA-Z0-9_.-]+)*:.*#\?/ {
        print_targets()
        line=$0
        sub(/#\? /, "\x1F", line) # use a unit separator
        split(line, parts, "\x1F")
        split(parts[1], nameparts, ":")
        count = split(nameparts[1], names, /[ \t]+/)
        desc = parts[2]
        notes = ""
        next
    }

    count && /^#! / { sub(/^#! /, ""); notes = notes "WARN|" $0 "\n"; next; }

    count && /^#\+ / { sub(/^#\+ /, ""); notes = notes "VAR|" $0 "\n"; next; }

    END { print_targets() }
    ' "$file" | while IFS='|' read -r kind f1 f2 f3; do
        case "$kind" in
            TARGET) print_target "$f1" "$f2" ;;
            WARN)   print_warning "$f1" ;;
            VAR)    print_variable "$f1" "$f2" "$f3" ;;
            END)    if [[ -z "$COMMAND" ]]; then echo ""; fi;;
        esac
    done
done

if [[ -z "$COMMAND" ]]; then
    echo -e "\nTo filter the help, use:\n    ${CL_YELLOW}make${CL_RESET} ${CL_CYAN}help${CL_MAGENTA} COMMAND=${CL_RESET}<command_name>"
fi
