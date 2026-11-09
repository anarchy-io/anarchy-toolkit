#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

"${SCRIPT_DIR}/../console/title.sh" 'magenta' 'Development Environment Installation'
source "${SCRIPT_DIR}/../shared/colors.env"

# ─── .NET SDK ──────────────────────────────────────────────────────────────────
# Delegates all version resolution, SDK installation, and environment variable
# persistence to install-sdk.sh.  Because we `source` the script, the exports
# it performs (DOTNET_ROOT, PATH, DOTNET_EXE) are immediately visible in this
# shell session as well.
echo -e "${CL_BLUE}Installing / validating .NET SDK...${CL_RESET}"
source "${SCRIPT_DIR}/install-sdk.sh"

# ─── Application environment variable ─────────────────────────────────────────
echo -e "${CL_BLUE}Setting up environment variables: ${CL_RESET}"
source "${SCRIPT_DIR}/set-shell-variable.sh" "ASPNETCORE_ENVIRONMENT" "Development"
echo -e "${CL_RED}  → If the environment variable has changed and your IDE was already open, make sure to restart it to pick up the new value.${CL_RESET}"

# ─── Local certificates ────────────────────────────────────────────────────────
echo -e "\n${CL_YELLOW}Trusting local development certificates (might request password)...${CL_RESET}"
dotnet dev-certs https --trust

echo -e "\n${BG_GREEN} Environment Validation Completed =] ${CL_RESET}\n"
