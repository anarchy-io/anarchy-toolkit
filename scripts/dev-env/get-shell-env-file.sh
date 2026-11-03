#!/usr/bin/env bash
set -euo pipefail

current_shell=$(basename "${SHELL:-}")
target_file=""

case "$current_shell" in
    zsh)
        target_file="$HOME/.zshenv"
        ;;
    bash)
        if [[ -f "$HOME/.bashenv" ]]; then
            target_file="$HOME/.bashenv"
        else
            target_file="$HOME/.bash_profile"
        fi
        ;;
    sh)
        target_file="$HOME/.profile"
        ;;
    *)
        echo "Unsupported shell '$current_shell'. Please set your env file manually." >&2
        exit 1
        ;;
esac

if [[ ! -f "$target_file" ]]; then
    touch "$target_file"
    echo "Created new shell environment file: $target_file" >&2
fi

# Stdout must contain only the resolved path so callers can safely use command substitution.
printf '%s\n' "$target_file"
