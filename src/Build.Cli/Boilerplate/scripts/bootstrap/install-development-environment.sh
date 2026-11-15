#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

"${SCRIPT_DIR}/../console/title.sh" 'magenta' 'Development Environment Installation'
source "${SCRIPT_DIR}/../shared/colors.env"

# ─── .NET SDK ──────────────────────────────────────────────────────────────────
# Delegates all version resolution, SDK installation, and environment variable
# persistence to install-sdk.sh.  Because we `source` the script, the exports
# it performs (DOTNET_ROOT and PATH) are immediately visible in this shell
# session as well. DOTNET_EXE is deliberately not persisted: `dotnet` is found
# on PATH, and a saved absolute path goes stale when an SDK is removed.
source "${SCRIPT_DIR}/install-sdk.sh"

# ─── Application environment variable ─────────────────────────────────────────
"${SCRIPT_DIR}/../console/subtitle.sh" 'Environment variables:'
source "${SCRIPT_DIR}/set-shell-variable.sh" "ASPNETCORE_ENVIRONMENT" "Development"
echo -e "${CL_YELLOW}  → If the environment variable has changed and your IDE was already open, make sure to restart it to pick up the new value.${CL_RESET}"

# ─── Local certificates ────────────────────────────────────────────────────────
"${SCRIPT_DIR}/../console/subtitle.sh" 'Local development certificates:'
echo -e "${CL_YELLOW}Trusting local development certificates (might request password)...${CL_RESET}"
dotnet dev-certs https --trust

"${SCRIPT_DIR}/../console/title.sh" 'green' 'Environment Validation Completed =]'
echo
