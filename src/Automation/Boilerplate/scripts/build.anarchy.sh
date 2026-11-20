#!/usr/bin/env bash
set -euo pipefail

SCRIPTS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${SCRIPTS_DIR}/shared/colors.env"

"${SCRIPTS_DIR}/console/subtitle.sh" "Shell Information:"
bash --version 2>&1 | head -n 1

###########################################################################
# CONFIGURATION
###########################################################################

# The project root: PROJECT_ROOT (exported by the Makefile), else the scripts' parent if it holds the build, else the working directory.
PROJECT_ROOT="${PROJECT_ROOT:-$(cd "$SCRIPTS_DIR/.." && pwd)}"
[[ -f "$PROJECT_ROOT/build/_build.csproj" ]] || PROJECT_ROOT="$PWD"
BUILD_PROJECT_FILE="$PROJECT_ROOT/build/_build.csproj"

# ─── Configuration ────────────────────────────────────────────────
# Reload the environment variables from the shell environment file, if it exists.
# This loads the correct dotnet SDK version, in case the installation script has updated it.
"${SCRIPTS_DIR}/console/subtitle.sh" "Reload environment variables:"

if [ "${ANARCHY_RELOAD_ENV:-true}" = false ]; then
    echo -e "\n${CL_YELLOW}Skipping reloading as ANARCHY_RELOAD_ENV is set to false...${CL_RESET}"
else
    ENV_FILENAME="$("$SCRIPTS_DIR/bootstrap/shell-profile-path.sh")"

    if [[ -f "$ENV_FILENAME" ]]; then
        echo -e "\n${CL_GREEN}Sourcing environment variables from '${ENV_FILENAME}'...${CL_RESET}"
        # shellcheck disable=SC1090
        source "$ENV_FILENAME"
    else
        echo -e "\n${CL_RED}Environment file '${ENV_FILENAME}' does not exist, skipping...${CL_RESET}"
    fi
fi

# Fallout runs DOTNET_EXE when set; a path left over from a removed SDK (e.g. Homebrew's) would fail every target.
if [[ -n "${DOTNET_EXE:-}" && ! -x "$DOTNET_EXE" ]]; then
    echo -e "\n${CL_YELLOW}DOTNET_EXE points to '${DOTNET_EXE}', which is not an executable; ignoring it and using 'dotnet' from PATH.${CL_RESET}"
    unset DOTNET_EXE
fi

###########################################################################
# EXECUTION
###########################################################################

"${SCRIPTS_DIR}/console/subtitle.sh" "Microsoft (R) .NET SDK version:"
dotnet --version

"${SCRIPTS_DIR}/console/subtitle.sh" "Running targets in the build project:"
# Translate the Anarchy wrapper's log level to the Fallout/NUKE engine settings.
ANARCHY_EFFECTIVE_LOG_LEVEL="${ANARCHY_LOG_LEVEL:-${FALLOUT_LOG_LEVEL:-Information}}"
ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Development} \
    DOTNET_ENVIRONMENT=${DOTNET_ENVIRONMENT:-Development} \
    FALLOUT_LOG_LEVEL="$ANARCHY_EFFECTIVE_LOG_LEVEL" \
    NUKE_LOG_LEVEL="$ANARCHY_EFFECTIVE_LOG_LEVEL" \
    FALLOUT_NO_LOGO=true \
    NUKE_NO_LOGO=true \
    dotnet run --project "$BUILD_PROJECT_FILE" -- "$@"
