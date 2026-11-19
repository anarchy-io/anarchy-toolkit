#!/usr/bin/env bash
# Copies documentation pages into the distributed agent skills, so agents in solutions built on the toolkit can read
# them offline, at the version of the installed toolkit:
#   docs/conventions, docs/decisions                       -> anarchy-conventions/references/{conventions,decisions}
#   docs/guides/{adopting-existing-solution,migration-recipes,pitfalls,bringing-your-company-stack}.md
#                                                          -> anarchy-adopt/references/guides
#   docs/guides/{mental-model,greenfield-solution}.md      -> anarchy/references/guides
# Links between pages copied into the same skill stay relative; every other site link becomes an absolute URL to the
# published site. --check compares instead of writing, and fails when a skill is out of date (run in CI).
set -euo pipefail

docs_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
repo_dir="$(cd "$docs_dir/.." && pwd)"
skills_dir="$repo_dir/src/Automation/Boilerplate/.agents/skills"
site_url="https://anarchy-io.github.io/anarchy-toolkit/"
check=false
[[ "${1:-}" == "--check" ]] && check=true

# "<skill>|<section>|<pages>": the pages (all when empty) of docs/<section> copied to <skill>/references/<section>.
bundles=(
  "anarchy-conventions|conventions|"
  "anarchy-conventions|decisions|"
  "anarchy-adopt|guides|adopting-existing-solution migration-recipes pitfalls bringing-your-company-stack"
  "anarchy|guides|mental-model greenfield-solution"
)

# $1: the page's section; $2: the sections bundled in the same skill; $3: the guides bundled in the same skill.
# Kramdown attribute lines only style the site and are dropped. Relative links are first made site-relative
# (sibling pages gain their section), then kept relative when the target is bundled alongside, or else published:
# .md pages are .html there, README.md pages are their folder.
convert() {
  local section="$1" kept_sections="$2" kept_guides="$3"
  local script=(-e '/^\{:.*\}$/d'
    -e "s#\]\(([A-Za-z0-9_-]+(/README)?\.md)#](../${section}/\1#g"
    -e "s#\]\(\.\./README\.md#](../overview/README.md#g")
  for kept in $kept_sections; do
    script+=(-e "s#\]\(\.\./${kept}/#](@@/${kept}/#g")
  done
  for guide in $kept_guides; do
    script+=(-e "s#\]\(\.\./guides/${guide}\.md#](@@/guides/${guide}.md#g")
  done
  script+=(
    -e "s#\]\(\.\./([^)\#]+/)?README\.md(\#[^)]*)?\)#](${site_url}\1\2)#g"
    -e "s#\]\(\.\./([^)\#]+)\.md(\#[^)]*)?\)#](${site_url}\1.html\2)#g"
    -e "s#\]\(\.\./([^)]*)\)#](${site_url}\1)#g"
    -e "s#\]\(@@/${section}/#](#g"
    -e "s#\]\(@@/#](../#g")
  sed -E "${script[@]}"
}

generated="$(mktemp -d)"
trap 'rm -rf "$generated"' EXIT
for bundle in "${bundles[@]}"; do
  IFS='|' read -r skill section pages <<< "$bundle"
  kept_sections=""
  kept_guides=""
  for other in "${bundles[@]}"; do
    IFS='|' read -r other_skill other_section other_pages <<< "$other"
    [[ "$other_skill" != "$skill" ]] && continue
    if [[ -z "$other_pages" ]]; then kept_sections+=" $other_section"; else kept_guides+=" $other_pages"; fi
  done
  [[ -z "$pages" ]] && pages="$(cd "$docs_dir/$section" && ls *.md | sed 's/\.md$//')"
  target="$generated/$skill/references/$section"
  mkdir -p "$target"
  for page in $pages; do
    convert "$section" "$kept_sections" "$kept_guides" < "$docs_dir/$section/$page.md" > "$target/$page.md"
  done
done

status=0
for skill_path in "$generated"/*; do
  skill="$(basename "$skill_path")"
  for section_path in "$skill_path"/references/*; do
    section="$(basename "$section_path")"
    destination="$skills_dir/$skill/references/$section"
    if $check; then
      if ! diff -r "$section_path" "$destination" > /dev/null 2>&1; then
        diff -r "$section_path" "$destination" || true
        status=1
      fi
    else
      rm -rf "$destination"
      mkdir -p "$destination"
      cp -R "$section_path"/. "$destination"/
      echo "Copied docs/$section into $skill/references/$section"
    fi
  done
done

if $check; then
  if [[ $status -ne 0 ]]; then
    echo "The skills are out of date with the docs: run 'make skills_docs_sync' and commit the result." >&2
    exit 1
  fi
  echo "The skills' references match the docs."
fi
