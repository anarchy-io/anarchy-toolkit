#!/usr/bin/env bash
set -euo pipefail

# ─── Configuration ────────────────────────────────────────────────
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
source "${SCRIPT_DIR}/variables.env"

VAR_NAME=${1:-}
VAR_VALUE=${2:-}
OVERRIDE=${3:-false} # optional, default false
TARGET_FILE="$("${SCRIPT_DIR}/get-shell-env-file.sh")"
MANAGED_COMMENT="# Added by Anarchy Toolkit"

usage() {
    echo -e "\n${CL_RED}Usage: $0 <variable_name> <variable_value> [override]${CL_RESET}"
}

validate_args() {
    if [ -z "$VAR_NAME" ] || [ -z "$VAR_VALUE" ]; then
        usage
        return 1
    fi
}

ensure_target_file() {
    if [ ! -f "$TARGET_FILE" ]; then
        touch "$TARGET_FILE"
    fi
}

desired_export_line() {
    printf 'export %s=%s %s' "$VAR_NAME" "$VAR_VALUE" "$MANAGED_COMMENT"
}

current_export_line() {
    if [ "$VAR_NAME" = "PATH" ]; then
        grep -m1 "^export PATH=.*${MANAGED_COMMENT}$" "$TARGET_FILE" 2>/dev/null || true
    else
        grep -m1 "^export ${VAR_NAME}=" "$TARGET_FILE" 2>/dev/null || true
    fi
}

append_export_line() {
    local line
    line="$(desired_export_line)"

    # Keep a leading visual separator before managed exports, but only when file has content.
    if [ -s "$TARGET_FILE" ]; then
        printf '\n%s\n' "$line" >> "$TARGET_FILE"
    else
        printf '%s\n' "$line" >> "$TARGET_FILE"
    fi

    echo -e "\n${CL_GREEN}Added '${CL_RESET}${VAR_NAME}${CL_GREEN}' to '${CL_RESET}${TARGET_FILE}${CL_GREEN}'${CL_RESET}"
}

replace_export_value() {
    local tmp_file
    local line
    tmp_file="$(mktemp)"
    line="$(desired_export_line)"

    awk -v var_name="$VAR_NAME" -v replacement="$line" -v managed_comment="$MANAGED_COMMENT" '
        BEGIN { replaced = 0 }
        $0 ~ ("^export " var_name "=") && (var_name != "PATH" || index($0, managed_comment) > 0) {
            if (replaced == 0) {
                print replacement
                replaced = 1
            }
            next
        }
        { print }
    ' "$TARGET_FILE" > "$tmp_file"

    mv "$tmp_file" "$TARGET_FILE"
    echo -e "\n${CL_GREEN}Updated '${CL_RESET}${VAR_NAME}${CL_GREEN}' in '${CL_RESET}${TARGET_FILE}${CL_GREEN}'${CL_RESET}"
}

sync_current_process() {
    # shellcheck disable=SC1090
    source "$TARGET_FILE" 2>/dev/null || true

    echo -e "\n${CL_BLUE}Verifying if '${CL_RESET}${VAR_NAME}${CL_BLUE}' was correctly set in this process:${CL_RESET}"
    echo "${!VAR_NAME}"

    # Scripts cannot mutate the parent shell unless sourced.
    if [ "${BASH_SOURCE[0]}" = "$0" ]; then
        echo -e "\n${CL_YELLOW}To refresh your current shell session, run: ${CL_RESET}source '${TARGET_FILE}'"
    fi
}

main() {
    local current_line
    local desired_line

    validate_args
    ensure_target_file

    # shellcheck disable=SC1090
    source "$TARGET_FILE" 2>/dev/null || true

    current_line="$(current_export_line)"
    desired_line="$(desired_export_line)"

    if [ -n "$current_line" ] && [ "$current_line" = "$desired_line" ]; then
        echo -e "\n${CL_YELLOW}Existing value matches incoming value; no changes needed.${CL_RESET}"
        sync_current_process
        echo -e "\n${CL_GREEN_BOLD}Done!${CL_RESET}\n"
        return 0
    fi

    if [ -n "$current_line" ]; then
        if [ "$OVERRIDE" = "true" ]; then
            echo -e "\n${CL_YELLOW}Override enabled, replacing value for '${CL_RESET}${VAR_NAME}${CL_YELLOW}'.${CL_RESET}"
            replace_export_value
        else
            echo -e "\n${CL_YELLOW}Variable '${CL_RESET}${VAR_NAME}${CL_YELLOW}' already exists and override is disabled. Skipping...${CL_RESET}"
            sync_current_process
            echo -e "\n${CL_GREEN_BOLD}Done!${CL_RESET}\n"
            return 0
        fi
    else
        echo -e "\n${CL_YELLOW}Variable '${CL_RESET}${VAR_NAME}${CL_YELLOW}' is not set in '${CL_RESET}${TARGET_FILE}${CL_YELLOW}'.${CL_RESET}"
        append_export_line
    fi

    sync_current_process
    echo -e "\n${CL_GREEN_BOLD}Done!${CL_RESET}\n"
}

main "$@"
