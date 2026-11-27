#!/usr/bin/env bash
# Copies the package and sample READMEs into the Jekyll site (docs/portal/packages/<Project>/ and
# docs/portal/samples/),
# rewriting links that only make sense on GitHub or NuGet so they point to the site's own pages.
# The copies are generated: they are git-ignored and refreshed before every site build.
set -euo pipefail

docs_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
repo_dir="$(cd "$docs_dir/../.." && pwd)"
repo_url="https://github.com/anarchy-io/anarchy-toolkit"
site_url="https://anarchy-io.github.io/anarchy-toolkit/"

# Package READMEs are also shown on NuGet.org, so they link to the published site with absolute URLs;
# on the site itself those links become relative so they also work in local previews.
# $1: relative path from the generated page back to the site root
rewrite_links() {
  sed -E -e "s#${site_url}#$1#g"
}

rm -rf "$docs_dir/samples"
find "$docs_dir/packages" -mindepth 1 -maxdepth 1 -type d -exec rm -rf {} +

count=0
for readme in "$repo_dir"/src/*/README.md; do
  project="$(basename "$(dirname "$readme")")"
  mkdir -p "$docs_dir/packages/$project"
  rewrite_links "../../" < "$readme" > "$docs_dir/packages/$project/README.md"
  count=$((count + 1))
done

# Gathers every package's "## Third-party dependencies" section into one page, a level-two heading per package.
third_party="$docs_dir/packages/third-party-dependencies.md"
{
  cat <<'EOF'
# Third-party dependencies

Most third-party libraries a package brings are announced by its name: `Infrastructure.Persistence.Db.Postgres` brings
a PostgreSQL driver and `Infrastructure.Telemetry.Serilog` brings Serilog. The libraries below are the *invisible*
ones: a package brings them although its name does not say so, and they reach your application as transitive
dependencies. They are listed so you can check licences, versions and conflicts before you adopt a package, and know
when one of them appears in its public API, where upgrading or replacing it would affect your code. The framework,
`Microsoft.Extensions.*` and ASP.NET Core are not listed.

This page is generated from the `## Third-party dependencies` section of each [package](README.md)'s README; packages
without third-party dependencies beyond their name are not listed.
EOF
  for readme in "$repo_dir"/src/*/README.md; do
    project="$(basename "$(dirname "$readme")")"
    section="$(awk '/^## /{inside = ($0 == "## Third-party dependencies"); next} inside' "$readme")"
    [ -n "$section" ] || continue
    printf '\n## [%s](%s/README.md)\n' "$project" "$project"
    printf '%s\n' "$section" | rewrite_links "../"
  done
} > "$third_party"

mkdir -p "$docs_dir/samples"
rewrite_links "../" < "$repo_dir/samples/README.md" \
  | sed -E \
    -e "s#\]\(\.\./docs/portal/(guides|conventions)/([a-z-]+)\.md#](../\1/\2.md#g" \
    -e "s#\]\((Samples\.[A-Za-z]+)\)#](${repo_url}/tree/main/samples/\1)#g" \
  > "$docs_dir/samples/README.md"

echo "Synced $count package READMEs, their third-party dependencies and the samples README into $docs_dir"
