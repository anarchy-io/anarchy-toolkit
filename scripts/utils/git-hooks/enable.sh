#!/usr/bin/env bash
set -e

CL_SUBTITLE='\033[45m\033[1;97m'
CL_RESET='\033[0m'
CL_PREVIEW='\033[36m'
CL_ERROR='\033[0;31m'
CL_SUCCESS="\033[0;32m"

HOOK_TYPE="pre-commit"
SKIP_FLAGS=""
DRY_RUN=0

print_help() {
	CL_HEADER='\033[1;36m'
    CL_OPTION='\033[1;33m'

	echo -e ""
	echo -e "${CL_HEADER}Usage:${CL_RESET}"
	echo -e "  make git_hooks_enable OPTIONS=\"[--hook-type <type>] [--dry-run] [--skip-<task>]...\""
	echo -e "  ${CL_OPTION}OR${CL_RESET}"
	echo -e "  ./scripts/utils/git-hooks/enable.sh [--hook-type <type>] [--dry-run] [--skip-<task>]..."
	echo ""

	echo -e "${CL_HEADER}Options:${CL_RESET}"
	echo -e "  ${CL_OPTION}--hook-type <type>${CL_RESET}       Hook type to configure (default: pre-commit)"
	echo -e "  ${CL_OPTION}--skip-lint-fix${CL_RESET}          Skip automatic lint fixing"
	echo -e "  ${CL_OPTION}--skip-test-static${CL_RESET}       Skip static analysis (lint + unit tests)"
	echo -e "  ${CL_OPTION}--skip-test-runtime${CL_RESET}      Skip runtime analysis (integration + acceptance tests)"
	echo -e "  ${CL_OPTION}--dry-run${CL_RESET}                Preview the generated hook without writing"
	echo -e "  ${CL_OPTION}--help${CL_RESET}                   Show this help message"
	echo ""

	echo -e "${CL_HEADER}Examples:${CL_RESET}"
	echo -e "  make git_hooks_enable OPTIONS=\"--skip-test-runtime\""
	echo -e "  ./scripts/utils/git-hooks/enable.sh --hook-type pre-push --dry-run"
}

# Parse CLI flags
while [[ $# -gt 0 ]]; do
	case "$1" in
		--hook-type)
			HOOK_TYPE="$2"
			shift 2
			;;
		--skip-*)
			SKIP_FLAGS="$SKIP_FLAGS $1"
			shift
			;;
		--dry-run)
			DRY_RUN=1
			shift
			;;
		--help)
			print_help
			exit 0
			;;
		*)
			echo -e "${CL_ERROR}Unknown option: $1${CL_RESET}"
			print_help
			exit 1
			;;
	esac
done

echo -e "\n${CL_SUBTITLE} Setting up Git hook: ${HOOK_TYPE} ${CL_RESET}"

HOOK_TASKS=""

# Helper: check if a skip flag is present
should_skip() {
	local flag="--skip-$1"
	[[ "$SKIP_FLAGS" == *"$flag"* ]]
}

# Build list of tasks (Make targets)
if should_skip "lint-fix"; then
	echo "❌  Skipping lint auto-fixer..."
else
	echo "📋 Including lint auto-fixer..."
	HOOK_TASKS+=" lint_fix AUTO_STAGE=true"
fi

if should_skip "test-static"; then
	echo "❌  Skipping static analysis..."
else
	echo "📋 Including static analysis..."
	HOOK_TASKS+=" lint test_unit"
fi

if should_skip "test-runtime"; then
	echo "❌  Skipping runtime analysis..."
else
	echo "📋 Including runtime analysis..."
	HOOK_TASKS+=" test_integration test_acceptance"
fi

# Trim whitespace
HOOK_TASKS="$(xargs <<< "$HOOK_TASKS")"

# 🛑 Exit if no tasks included
if [[ -z "$HOOK_TASKS" ]]; then
	echo -e "\n${CL_ERROR}🛑 All tasks were skipped — no hook will be created.${CL_RESET}"
	exit 0
fi

# Construct hook content
HOOK_CONTENT=$(cat <<-EOF
#!/usr/bin/env bash
set -e

PROJECT_ROOT="\$(git rev-parse --show-toplevel)"

# Skip during Rebases
if [ -d "\$PROJECT_ROOT/.git/rebase-merge" ] || [ -d "\$PROJECT_ROOT/.git/rebase-apply" ]; then
    echo -e "\n${CL_SUBTITLE} Skipping Git Hook '$HOOK_TYPE' tasks during rebase... ${CL_RESET}"
    exit 0
fi

echo -e "\n${CL_SUBTITLE} Running Git Hook '$HOOK_TYPE' tasks... ${CL_RESET}"
make -C "\$PROJECT_ROOT" -f Makefile $HOOK_TASKS
EOF
)

# 🧪 Dry run
if [[ "$DRY_RUN" == "1" ]]; then
	echo -e "\n${CL_PREVIEW}--- [Dry Run] Hook Content for '${HOOK_TYPE}' ---${CL_RESET}"
	echo "$HOOK_CONTENT"
	echo -e "${CL_PREVIEW}-----------------------------------------------${CL_RESET}"
	exit 0
fi

# ✅ Write the hook
HOOKS_DIR=".git/hooks"
mkdir -pv "$HOOKS_DIR"
HOOK_PATH="$HOOKS_DIR/$HOOK_TYPE"
echo "$HOOK_CONTENT" > "$HOOK_PATH"
chmod +x "$HOOK_PATH"

echo -e "\n${CL_SUCCESS}Git hook created at $HOOK_PATH${CL_RESET}"
