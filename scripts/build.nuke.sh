#!/usr/bin/env bash
set -euo pipefail

# Color codes
CL_SUBTITLE='\033[45m\033[1;97m'
CL_DEFAULT='\033[0m' # No Color
CL_RED='\033[0;31m'
CL_GREEN='\033[0;32m'
CL_YELLOW='\033[0;33m'

echo -e "\n${CL_SUBTITLE} Shell Information: ${CL_DEFAULT}"
bash --version 2>&1 | head -n 1

###########################################################################
# CONFIGURATION
###########################################################################

SCRIPT_DIR=$(cd "$( dirname "$( dirname "${BASH_SOURCE[0]}" )" )" && pwd)
BUILD_PROJECT_FILE="$SCRIPT_DIR/build/_build.csproj"

# ─── Configuration ────────────────────────────────────────────────
# Reload the environment variables from the shell environment file, if it exists.
# This loads the correct dotnet SDK version, in case the installation script has updated it.
echo -e "\n${CL_SUBTITLE} Reload environment variables: ${CL_DEFAULT}"

if [ "${NUKE_RELOAD_ENV:-true}" = false ]; then
    echo -e "\n${CL_YELLOW}Skipping reloading as NUKE_RELOAD_ENV is set to false...${CL_DEFAULT}"
else
    ENV_FILENAME="$(./scripts/dev-env/get-shell-env-file.sh)"

    if [[ -f "$ENV_FILENAME" ]]; then
        echo -e "\n${CL_GREEN}Sourcing environment variables from '${ENV_FILENAME}'...${CL_DEFAULT}"
        # shellcheck disable=SC1090
        source "$ENV_FILENAME"
    else
        echo -e "\n${CL_RED}Environment file '${ENV_FILENAME}' does not exist, skipping...${CL_DEFAULT}"
    fi
fi

###########################################################################
# EXECUTION
###########################################################################

echo -e "\n${CL_SUBTITLE} Microsoft (R) .NET SDK version: ${CL_DEFAULT}\n$(dotnet --version)"

echo -e "\n${CL_SUBTITLE} Running targets in the Nuke Build Project: ${CL_DEFAULT}\n"
ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Development} \
    DOTNET_ENVIRONMENT=${DOTNET_ENVIRONMENT:-Development} \
    NUKE_LOG_LEVEL=Information \
    NUKE_NO_LOGO=true \
    dotnet run --project "$BUILD_PROJECT_FILE" -- "$@"
