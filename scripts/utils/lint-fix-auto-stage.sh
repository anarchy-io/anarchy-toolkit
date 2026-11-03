#!/usr/bin/env bash
set -euo pipefail

CL_GREEN='\033[0;32m'
CL_RED='\033[0;31m'
CL_RESET='\033[0m'
CL_SUBTITLE='\033[45m\033[1;97m'
CL_YELLOW='\033[1;33m'
CL_CYAN='\033[1;36m'

echo -e "\n${CL_SUBTITLE} With Settings ${CL_RESET}"
echo -e "${CL_CYAN}→ Auto-stage fixes: ${CL_RESET}${AUTO_STAGE:-false}\n"

# Capture working directory state BEFORE linting
BEFORE_HASH="$(git diff --name-only)"

# Run the linter
./scripts/build.nuke.sh LintFix

# Capture working directory state AFTER linting
AFTER_HASH="$(git diff --name-only)"

# Compare hashes to detect real changes from lint
if [[ "$BEFORE_HASH" != "$AFTER_HASH" ]]; then
  CHANGED_FILES="$(git diff --name-only)"
  CHANGED_COUNT="$(echo "$CHANGED_FILES" | wc -l)"
  STAGED_FILES="$(git diff --name-only --cached)"

  if [[ -n "$STAGED_FILES" ]]; then
    if [[ "${AUTO_STAGE:-false}" == "true" ]]; then
      echo -e "\n${CL_RED}✔ Auto-staging ${CHANGED_COUNT} modified file(s)...${CL_RESET}\n"
      echo "$STAGED_FILES" | xargs git add
    else
      echo -e "\n${CL_YELLOW} 👀 The auto-linter has modified ${CHANGED_COUNT} file(s). Please review and re-stage as needed.${CL_RESET}\n"
    fi
  else
    echo -e "\n${CL_YELLOW} ∅ Nothing was staged before linting. No files re-staged.${CL_RESET}\n"
  fi
else
  echo -e "\n${CL_GREEN} ✅ No changes after auto-linting${CL_RESET}\n"
fi
