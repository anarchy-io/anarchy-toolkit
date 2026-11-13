#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
PROJECT_ROOT="${PROJECT_ROOT:-$(cd "${SCRIPT_DIR}/../.." && pwd)}"
[[ -f "${PROJECT_ROOT}/global.json" ]] || PROJECT_ROOT="$PWD"
DOTNET_DIRECTORY="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"
DOTNET_GLOBAL_FILE="${PROJECT_ROOT}/global.json"
printf -v DOTNET_ROOT_EXPORT '%q' "$DOTNET_DIRECTORY"

# Let the .NET host apply global.json's full roll-forward policy rather than
# trying to compare SDK versions ourselves (and relying on GNU sort on macOS).
sdk_works() {
    (cd "$PROJECT_ROOT" && "$1" --version >/dev/null 2>&1)
}

if command -v dotnet >/dev/null 2>&1 && sdk_works "$(command -v dotnet)"; then
    echo "A compatible .NET SDK is already installed: $(cd "$PROJECT_ROOT" && dotnet --version)"
elif [[ -x "$DOTNET_DIRECTORY/dotnet" ]] && sdk_works "$DOTNET_DIRECTORY/dotnet"; then
    echo "A compatible .NET SDK is already installed in $DOTNET_DIRECTORY."
    source "${SCRIPT_DIR}/set-shell-variable.sh" "DOTNET_ROOT" "$DOTNET_ROOT_EXPORT" "true"
    source "${SCRIPT_DIR}/set-shell-variable.sh" "PATH" '$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH' "true"
else
    DOTNET_VERSION=$(perl -nle 'print $1 if m{"version"\s*:\s*"([^"]+)"}' "$DOTNET_GLOBAL_FILE")
    DOTNET_ROLL_FORWARD=$(perl -nle 'print $1 if m{"rollForward"\s*:\s*"([^"]+)"}' "$DOTNET_GLOBAL_FILE")
    DOTNET_INSTALL_FILE="${PROJECT_ROOT}/.fallout/temp/dotnet-install.sh"
    mkdir -p "$(dirname "$DOTNET_INSTALL_FILE")" "$DOTNET_DIRECTORY"
    curl -Lsfo "$DOTNET_INSTALL_FILE" "https://dot.net/v1/dotnet-install.sh"
    chmod +x "$DOTNET_INSTALL_FILE"

    if [[ -z "$DOTNET_VERSION" ]]; then
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --channel STS --no-path
    elif [[ "$DOTNET_ROLL_FORWARD" == disable ]]; then
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --version "$DOTNET_VERSION" --no-path
    else
        DOTNET_CHANNEL=$(printf '%s' "$DOTNET_VERSION" | cut -d. -f1,2)
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --channel "$DOTNET_CHANNEL" --quality ga --no-path
    fi

    if ! sdk_works "$DOTNET_DIRECTORY/dotnet"; then
        echo "The installed SDK does not satisfy $DOTNET_GLOBAL_FILE." >&2
        return 1 2>/dev/null || exit 1
    fi
    source "${SCRIPT_DIR}/set-shell-variable.sh" "DOTNET_ROOT" "$DOTNET_ROOT_EXPORT" "true"
    source "${SCRIPT_DIR}/set-shell-variable.sh" "PATH" '$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH' "true"
fi

echo "Microsoft (R) .NET SDK version $(cd "$PROJECT_ROOT" && dotnet --version)"
