#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
PROJECT_ROOT="${PROJECT_ROOT:-$(cd "${SCRIPT_DIR}/../.." && pwd)}"
[[ -f "${PROJECT_ROOT}/global.json" ]] || PROJECT_ROOT="$PWD"
DOTNET_DIRECTORY="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"
DOTNET_GLOBAL_FILE="${PROJECT_ROOT}/global.json"
printf -v DOTNET_ROOT_EXPORT '%q' "$DOTNET_DIRECTORY"
source "${SCRIPT_DIR}/../shared/colors.env"

# A full title when run on its own (make install_sdk); a subtitle when sourced by install-development-environment.sh.
if [[ "${BASH_SOURCE[0]}" == "$0" ]]; then
    "${SCRIPT_DIR}/../console/title.sh" 'cyan' '.NET SDK Installation'
else
    "${SCRIPT_DIR}/../console/subtitle.sh" '.NET SDK:'
fi

# Let the .NET host apply global.json's full roll-forward policy rather than
# trying to compare SDK versions ourselves (and relying on GNU sort on macOS).
sdk_works() {
    (cd "$PROJECT_ROOT" && "$1" --version >/dev/null 2>&1)
}

if command -v dotnet >/dev/null 2>&1 && sdk_works "$(command -v dotnet)"; then
    echo -e "${CL_GREEN}A compatible .NET SDK is already installed: ${CL_RESET}$(cd "$PROJECT_ROOT" && dotnet --version)"
elif [[ -x "$DOTNET_DIRECTORY/dotnet" ]] && sdk_works "$DOTNET_DIRECTORY/dotnet"; then
    echo -e "${CL_GREEN}A compatible .NET SDK is already installed in ${CL_RESET}${DOTNET_DIRECTORY}"
    source "${SCRIPT_DIR}/set-shell-variable.sh" "DOTNET_ROOT" "$DOTNET_ROOT_EXPORT" "true"
    source "${SCRIPT_DIR}/set-shell-variable.sh" "PATH" '$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH' "true"
else
    DOTNET_VERSION=$(perl -nle 'print $1 if m{"version"\s*:\s*"([^"]+)"}' "$DOTNET_GLOBAL_FILE")
    DOTNET_ROLL_FORWARD=$(perl -nle 'print $1 if m{"rollForward"\s*:\s*"([^"]+)"}' "$DOTNET_GLOBAL_FILE")
    DOTNET_INSTALL_FILE="${PROJECT_ROOT}/.fallout/temp/dotnet-install.sh"
    mkdir -p "$(dirname "$DOTNET_INSTALL_FILE")" "$DOTNET_DIRECTORY"
    curl -Lsfo "$DOTNET_INSTALL_FILE" "https://dot.net/v1/dotnet-install.sh"
    chmod +x "$DOTNET_INSTALL_FILE"

    # Pick what to download from global.json's rollForward: the exact pin when roll-forward is disabled, the
    # newest build of the pinned feature band (e.g. 10.0.1xx) when only patches may roll forward (the default),
    # and the newest GA build of the pinned major.minor otherwise. A pin below a real feature band (e.g. 10.0.0)
    # only names a lower bound, so it falls back to major.minor.
    DOTNET_PATCH=$(printf '%s' "$DOTNET_VERSION" | cut -d. -f3 | cut -d- -f1)
    if [[ -z "$DOTNET_VERSION" ]]; then
        echo -e "${CL_YELLOW}No SDK version pinned in global.json; installing the latest STS SDK into ${CL_RESET}${DOTNET_DIRECTORY}"
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --channel STS --no-path
    elif [[ "$DOTNET_ROLL_FORWARD" == disable ]]; then
        echo -e "${CL_BLUE}Installing .NET SDK ${CL_RESET}${DOTNET_VERSION}${CL_BLUE} (rollForward: disable) into ${CL_RESET}${DOTNET_DIRECTORY}"
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --version "$DOTNET_VERSION" --no-path
    elif [[ "${DOTNET_ROLL_FORWARD:-latestPatch}" =~ ^(patch|latestPatch)$ && "$DOTNET_PATCH" =~ ^[0-9]+$ && "$DOTNET_PATCH" -ge 100 ]]; then
        DOTNET_CHANNEL="$(printf '%s' "$DOTNET_VERSION" | cut -d. -f1,2).${DOTNET_PATCH:0:1}xx"
        echo -e "${CL_BLUE}Installing the latest .NET SDK in feature band ${CL_RESET}${DOTNET_CHANNEL}${CL_BLUE} (pin: ${DOTNET_VERSION}, rollForward: ${DOTNET_ROLL_FORWARD:-latestPatch}) into ${CL_RESET}${DOTNET_DIRECTORY}"
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --channel "$DOTNET_CHANNEL" --quality ga --no-path
    else
        DOTNET_CHANNEL=$(printf '%s' "$DOTNET_VERSION" | cut -d. -f1,2)
        echo -e "${CL_BLUE}Installing the latest GA .NET SDK on channel ${CL_RESET}${DOTNET_CHANNEL}${CL_BLUE} (pin: ${DOTNET_VERSION}, rollForward: ${DOTNET_ROLL_FORWARD:-latestPatch}) into ${CL_RESET}${DOTNET_DIRECTORY}"
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --channel "$DOTNET_CHANNEL" --quality ga --no-path
    fi

    if ! sdk_works "$DOTNET_DIRECTORY/dotnet"; then
        echo -e "\n${CL_RED_BOLD}The installed SDK does not satisfy ${DOTNET_GLOBAL_FILE}.${CL_RESET}" >&2
        return 1 2>/dev/null || exit 1
    fi
    source "${SCRIPT_DIR}/set-shell-variable.sh" "DOTNET_ROOT" "$DOTNET_ROOT_EXPORT" "true"
    source "${SCRIPT_DIR}/set-shell-variable.sh" "PATH" '$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH' "true"
fi

echo -e "\n${CL_GREEN_BOLD}Microsoft (R) .NET SDK version ${CL_RESET}$(cd "$PROJECT_ROOT" && dotnet --version)"
