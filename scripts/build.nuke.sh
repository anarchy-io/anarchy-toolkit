#!/usr/bin/env bash
set -euo pipefail

SCRIPTS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${SCRIPTS_DIR}/shared/colors.env"

"${SCRIPTS_DIR}/console/subtitle.sh" "Shell Information:"
bash --version 2>&1 | head -n 1

###########################################################################
# CONFIGURATION
###########################################################################

BUILD_PROJECT_FILE="$SCRIPTS_DIR/../build/_build.csproj"

# ─── Configuration ────────────────────────────────────────────────
# Reload the environment variables from the shell environment file, if it exists.
# This loads the correct dotnet SDK version, in case the installation script has updated it.
"${SCRIPTS_DIR}/console/subtitle.sh" "Reload environment variables:"

if [ "${NUKE_RELOAD_ENV:-true}" = false ]; then
    echo -e "\n${CL_YELLOW}Skipping reloading as NUKE_RELOAD_ENV is set to false...${CL_RESET}"
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

###########################################################################
# EXECUTION
###########################################################################

"${SCRIPTS_DIR}/console/subtitle.sh" "Microsoft (R) .NET SDK version:"
dotnet --version

"${SCRIPTS_DIR}/console/subtitle.sh" "Running targets in the Nuke Build Project:"
# NUKE_TELEMETRY_OPTOUT: no usage data is sent, and Nuke no longer stops on its telemetry notice to rewrite
# 'NukeTelemetryVersion' in build/_build.csproj.
ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Development} \
    DOTNET_ENVIRONMENT=${DOTNET_ENVIRONMENT:-Development} \
    NUKE_LOG_LEVEL=${NUKE_LOG_LEVEL:-Information} \
    NUKE_TELEMETRY_OPTOUT=${NUKE_TELEMETRY_OPTOUT:-1} \
    NUKE_NO_LOGO=true \
    dotnet run --project "$BUILD_PROJECT_FILE" -- "$@"
