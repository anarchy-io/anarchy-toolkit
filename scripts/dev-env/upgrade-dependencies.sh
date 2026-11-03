#!/usr/bin/env bash
set -euo pipefail

# ─── Configuration ────────────────────────────────────────────────
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
source "${SCRIPT_DIR}/variables.env"
# shellcheck disable=SC1090
source "$("${SCRIPT_DIR}/get-shell-env-file.sh")"
TOOL_COMMAND="${DOTNET_ROOT:-$HOME/.dotnet}/tools/dotnet-outdated"

# ─── Helpers ──────────────────────────────────────────────────────
# Returns 0 if `dotnet outdated` can actually run against the active SDK.
# A tool installed under an older SDK (e.g. net8.0) will fail to launch when
# only a newer runtime is available.
function dotnet_outdated_works() {
    $TOOL_COMMAND --version >/dev/null 2>&1
}

function reinstall_dotnet_outdated_tool() {
    echo -e "${CL_YELLOW}'${CL_RESET}dotnet-outdated-tool${CL_YELLOW}' appears incompatible with the current SDK ($(dotnet --version)); reinstalling...${CL_RESET}"
    dotnet tool uninstall --global dotnet-outdated-tool >/dev/null 2>&1 || true
    dotnet tool install --global dotnet-outdated-tool
}

echo -e "\n${BG_MAGENTA} Checking for outdated NuGet packages ${CL_RESET}\n"
dotnet list package --outdated || {
  echo -e "${CL_RED}Failed to list outdated packages.${CL_RESET}"
  echo -e "\n${CL_RED_BOLD}Aborting due to failure.${CL_RESET}"
  exit 1
}

echo -e "\n${BG_MAGENTA} Verifying tool presence ${CL_RESET}\n"
if ! dotnet tool list -g | grep -q dotnet-outdated-tool; then
  echo -e "${CL_YELLOW}The '${CL_RESET}dotnet-outdated-tool${CL_YELLOW}' is not installed.${CL_RESET}"
  read -r -p "Would you like to install it now? [y/N] " REPLY
  if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${CL_GREEN}Installing the '${CL_RESET}dotnet-outdated-tool${CL_GREEN}'...${CL_RESET}"
    dotnet tool install --global dotnet-outdated-tool
  else
        echo -e "\n⚠️ ${CL_YELLOW_BOLD}The '${CL_RESET}dotnet-outdated-tool${CL_YELLOW_BOLD}' was not installed, skipping update.${CL_RESET}"
    exit 0
  fi
else
  echo -e "✅ ${CL_GREEN}The '${CL_RESET}dotnet-outdated-tool${CL_GREEN}' is present in the system${CL_RESET}"
fi

# Even if the tool is "installed", it may have been installed against an
# older SDK runtime and now fail to launch. Validate, then reinstall if so.
if ! dotnet_outdated_works; then
  reinstall_dotnet_outdated_tool
  if ! dotnet_outdated_works; then
    echo -e "${CL_RED}'${CL_RESET}dotnet-outdated-tool${CL_RED}' still fails to run after reinstall; aborting.${CL_RESET}"
    exit 1
  fi
fi

echo -e "\n${BG_MAGENTA} Updating dependencies ${CL_RESET}\n"
read -r -p "Would you like to update ALL outdated dependencies (version lock: ${VERSION_LOCK:-Major})? [y/N] " REPLY
if [[ $REPLY =~ ^[Yy]$ ]]; then
  echo -e "${CL_CYAN}Updating all outdated dependencies...${CL_RESET}"
  $TOOL_COMMAND --upgrade --version-lock "${VERSION_LOCK:-Major}"
  echo -e "\n${CL_GREEN_BOLD}Dependencies updated successfully.${CL_RESET}"
else
  echo -e "\n${CL_YELLOW_BOLD}Update skipped by user.${CL_RESET}"
fi
