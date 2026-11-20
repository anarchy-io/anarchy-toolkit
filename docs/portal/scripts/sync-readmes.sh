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

mkdir -p "$docs_dir/samples"
rewrite_links "../" < "$repo_dir/samples/README.md" \
  | sed -E \
    -e "s#\]\(\.\./docs/portal/(guides|conventions)/([a-z-]+)\.md#](../\1/\2.md#g" \
    -e "s#\]\((Samples\.[A-Za-z]+)\)#](${repo_url}/tree/main/samples/\1)#g" \
  > "$docs_dir/samples/README.md"

echo "Synced $count package READMEs and the samples README into $docs_dir"
