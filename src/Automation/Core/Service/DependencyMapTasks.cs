using Anarchy.Toolkit.Automation.Core.Entity;
using System.Globalization;
using System.Net;
using System.Text;

namespace Anarchy.Toolkit.Automation.Core.Service;

/// <summary>
/// Collects a <see cref="DependencyMap"/> from a solution and renders it as a self-contained HTML page (see
/// <c>IDependencyMapBuild</c>). Only the Mermaid graph needs a script, loaded from a CDN by the viewer's
/// browser, which also uses it to draw a group's own graph when its ring in the onion is clicked.
/// </summary>
public static class DependencyMapTasks
{
    private const string MermaidScript = "https://cdn.jsdelivr.net/npm/mermaid@11.4.1/dist/mermaid.esm.min.mjs";
    private const string ElkScript = "https://cdn.jsdelivr.net/npm/@mermaid-js/layout-elk@0.1.7/dist/mermaid-layout-elk.esm.min.mjs";

    /// <summary>
    /// Categorical colors (light, dark), assigned to groups in display order. They open with Anarchy Orange and
    /// avoid Anarchy Red, which marks layering violations (see the Anarchy I/O design tokens in the documentation).
    /// </summary>
    private static readonly (string Light, string Dark)[] Palette =
    [
        ("#FF751F", "#FF8A3D"), ("#2F6FD0", "#5B8FE0"), ("#1A9E6E", "#2DBA88"), ("#C98500", "#E0A020"),
        ("#7A4FD0", "#9B7BE6"), ("#C2417A", "#DD6A9C"), ("#0F8A9E", "#2BB0C4"), ("#5C5C66", "#A1A1AA"),
    ];

    /// <summary>
    /// Groups past the palette share this neutral color; the table and legend still name them.
    /// </summary>
    private static readonly (string Light, string Dark) OverflowColor = ("#85858F", "#85858F");

    /// <summary>
    /// The default group of a project (see <c>IDependencyMapBuild.GetDependencyMapGroup</c>): null (not mapped) under
    /// <c>build/</c>, <c>Tests</c> under <c>tests/</c>, <c>Samples</c> under <c>samples/</c>, otherwise its name prefix
    /// if that is one of <paramref name="layers"/> or <c>Testing</c>, else <c>Other</c>.
    /// </summary>
    /// <param name="projectName">The project name.</param>
    /// <param name="relativePath">The project file's path relative to the solution directory.</param>
    /// <param name="layers">The layers, innermost first.</param>
    /// <returns>The group, or <see langword="null"/> when the project is not mapped.</returns>
    public static string? GetDefaultGroup(string projectName, string relativePath, IReadOnlyList<string> layers)
    {
        var path = relativePath.Replace('\\', '/');
        if (path.StartsWith("build/", StringComparison.OrdinalIgnoreCase)) return null;
        if (path.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)) return "Tests";
        if (path.StartsWith("samples/", StringComparison.OrdinalIgnoreCase)) return "Samples";

        var prefix = LayeringTasks.PrefixOf(projectName);

        return layers.Contains(prefix) || prefix == "Testing" ? prefix : "Other";
    }

    /// <summary>
    /// The default group of a project in a solution layout (see <c>IDependencyMapBuild.GetDependencyMapGroup</c>):
    /// null (not mapped) under <c>build/</c>, <c>Tests</c> under <c>tests/</c>, <c>Samples</c> under <c>samples/</c>,
    /// otherwise its layer (see <see cref="LayeringTasks.GetLayer(string, string, SolutionLayout)"/>) if that is one
    /// of the layout's layers or <c>Testing</c>, else <c>Other</c>.
    /// </summary>
    /// <param name="projectName">The project name.</param>
    /// <param name="relativePath">The project file's path relative to the solution directory.</param>
    /// <param name="layout">The solution's layout.</param>
    /// <returns>The group, or <see langword="null"/> when the project is not mapped.</returns>
    public static string? GetDefaultGroup(string projectName, string relativePath, SolutionLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var path = relativePath.Replace('\\', '/');
        if (path.StartsWith("build/", StringComparison.OrdinalIgnoreCase)) return null;
        if (path.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)) return "Tests";
        if (path.StartsWith("samples/", StringComparison.OrdinalIgnoreCase)) return "Samples";

        var layer = LayeringTasks.GetLayer(projectName, relativePath, layout);

        return layer is not null && (layout.Layers.Contains(layer) || layer == LayeringTasks.Testing) ? layer : "Other";
    }

    /// <summary>
    /// Maps the projects with a group (see <paramref name="groupOf"/>) and the references between them.
    /// </summary>
    /// <param name="title">The page heading, usually the solution name.</param>
    /// <param name="solutionProjects">The solution's projects.</param>
    /// <param name="groupOf">Gives a project's group, or <see langword="null"/> to leave it out (e.g. <see cref="GetDefaultGroup(string, string, SolutionLayout)"/>).</param>
    /// <param name="layers">The layers, innermost first.</param>
    /// <returns>The map, with source sizes counted from disk (see <see cref="CountSource"/>).</returns>
    public static DependencyMap Collect(string title, IEnumerable<SolutionProjectInfo> solutionProjects, Func<SolutionProjectInfo, string?> groupOf, IReadOnlyList<string> layers)
    {
        var projects = solutionProjects
            .Select(project => (Project: project, Group: groupOf(project)))
            .Where(x => x.Group is not null)
            .ToArray();

        var namesByPath = projects.ToDictionary(
            x => Path.GetFullPath(x.Project.Path),
            x => x.Project.Name,
            StringComparer.OrdinalIgnoreCase);

        var references = projects
            .SelectMany(x => x.Project.ProjectReferences
                .Select(include => Path.GetFullPath(Path.Combine(x.Project.Directory,
                    include.Replace('\\', Path.DirectorySeparatorChar))))
                .Where(namesByPath.ContainsKey)
                .Select(path => new DependencyMapReference(x.Project.Name, namesByPath[path])))
            .Distinct()
            .ToArray();

        var mapped = projects
            .Select(x => {
                var (files, lines) = CountSource(x.Project.Directory);
                return new DependencyMapProject(x.Project.Name, x.Group!, files, lines);
            })
            .ToArray();

        return new DependencyMap(title, layers, mapped, references);
    }

    /// <summary>
    /// Counts the <c>.cs</c> files under a project directory and their non-blank lines, ignoring <c>bin</c>/<c>obj</c>.
    /// </summary>
    /// <param name="directory">The project directory.</param>
    /// <returns>The number of files and of non-blank lines.</returns>
    public static (int Files, int Lines) CountSource(string directory)
    {
        var files = Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(file => Path.GetRelativePath(directory, file).Split(Path.DirectorySeparatorChar)[0] is not ("bin" or "obj"))
            .ToArray();

        return (files.Length, files.Sum(file => File.ReadLines(file).Count(line => !string.IsNullOrWhiteSpace(line))));
    }

    /// <summary>
    /// Renders the map as a static HTML page. The output is deterministic: same map, same bytes.
    /// </summary>
    /// <param name="map">The map to render.</param>
    /// <returns>The HTML page.</returns>
    public static string RenderHtml(DependencyMap map)
    {
        var groups = map.Groups;
        var projects = map.Projects.OrderBy(p => groups.ToList().IndexOf(p.Group)).ThenBy(p => p.Name, StringComparer.Ordinal).ToArray();
        var references = map.References.OrderBy(r => r.From, StringComparer.Ordinal).ThenBy(r => r.To, StringComparer.Ordinal).ToArray();
        var violations = references.Where(map.IsViolation).ToArray();
        var drawn = WithoutTransitive(references).Concat(violations).Distinct()
            .OrderBy(r => r.From, StringComparer.Ordinal).ThenBy(r => r.To, StringComparer.Ordinal)
            .ToArray();
        var title = Encode(map.Title);

        var html = new StringBuilder();
        html.Append($$"""
            <!doctype html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{title}} dependency map</title>
            <script>
              // Applied before the page renders, so a saved theme never flashes the other one.
              try { const saved = localStorage.getItem("anarchy-dependency-map-theme"); if (saved) document.documentElement.dataset.theme = saved; } catch {}
            </script>
            <style>
            {{Styles(groups.Count)}}
            </style>
            </head>
            <body>
            <main>
            <header>
              <div class="title-row">
                <h1>{{title}} dependency map</h1>
                <button type="button" class="theme-toggle" aria-label="Switch between light and dark mode"></button>
              </div>
              <p class="muted">Generated by Anarchy automation's <code>GenerateDependencyMap</code> target, powered by
              <a href="https://fallout.build/">Fallout Build</a>. Dependencies only point
              down the layers: {{Encode(string.Join(" < ", map.Layers))}}.</p>
              <ul class="stats">
                <li><strong>{{projects.Length}}</strong> projects</li>
                <li><strong>{{references.Length}}</strong> references</li>
                <li class="{{(violations.Length > 0 ? "bad" : "")}}"><strong>{{violations.Length}}</strong> layering violations</li>
              </ul>
            </header>

            <section>
              <h2>Layers</h2>
              <p class="muted">Click a layer, or a group outside the chain, to zoom in on how its projects reference each other.</p>
              <div class="explorer" data-view="overview">
                <div class="overview">
                  {{Onion(map, groups)}}
                  {{OutsideButtons(map)}}
                </div>
                <div class="detail">
                  <div class="detail-head"><button type="button" class="back">← All layers</button><h3></h3></div>
                  <div class="detail-body"></div>
                </div>
              </div>
              {{Legend(groups)}}
              {{GroupTable(map, groups)}}
            </section>

            <section>
              <h2>Project references</h2>
              <p class="muted">Each project is outlined in its group's color (see the legend above); arrows point to the projects it
              references. References already implied by others
              (A → C when A → B → C) are left out{{(violations.Length > 0 ? "; references that break the layering are always drawn, red and dashed" : "")}}.</p>
              <div class="graph"><pre class="mermaid" id="all-references">
            {{Encode(Mermaid(map, groups, projects, drawn))}}
              </pre></div>
            </section>

            """);

        foreach (var group in groups)
        {
            html.Append(GroupDetail(map, groups, group));
        }

        if (violations.Length > 0)
        {
            html.Append("<section>\n  <h2>Layering violations</h2>\n  <ul>\n");
            foreach (var violation in violations)
            {
                html.Append($"    <li><code>{Encode(violation.From)}</code> → <code>{Encode(violation.To)}</code></li>\n");
            }
            html.Append("  </ul>\n</section>\n");
        }

        html.Append($$"""
            </main>
            <script type="module">
              import mermaid from "{{MermaidScript}}";
              import elkLayouts from "{{ElkScript}}";
              mermaid.registerLayoutLoaders(elkLayouts);
              const root = document.documentElement;
              const themeKey = "anarchy-dependency-map-theme";
              const isDark = () => root.dataset.theme === "dark"
                || (root.dataset.theme !== "light" && matchMedia("(prefers-color-scheme: dark)").matches);

              function configure() {
                const css = getComputedStyle(root);
                const token = name => css.getPropertyValue(name).trim();
                mermaid.initialize({
                  startOnLoad: false,
                  layout: "elk",
                  elk: { mergeEdges: false, nodePlacementStrategy: "BRANDES_KOEPF" },
                  theme: "base",
                  darkMode: isDark(),
                  fontFamily: token("--font-sans"),
                  themeVariables: {
                    background: token("--surface-2"),
                    primaryColor: token("--surface"),
                    primaryTextColor: token("--text-primary"),
                    primaryBorderColor: token("--border"),
                    lineColor: token("--text-secondary"),
                    clusterBkg: token("--surface"),
                    clusterBorder: token("--border"),
                    edgeLabelBackground: token("--surface")
                  },
                  flowchart: { useMaxWidth: false }
                });
              }

              // Mermaid replaces each graph's source with its drawing: keep the source to draw it again in the other theme.
              function render(nodes) {
                for (const node of nodes) {
                  if (node.dataset.source === undefined) node.dataset.source = node.textContent;
                  else { node.textContent = node.dataset.source; node.removeAttribute("data-processed"); }
                }
                return mermaid.run({ nodes });
              }

              const toggle = document.querySelector(".theme-toggle");
              const label = () => { toggle.textContent = isDark() ? "☀ Light mode" : "☾ Dark mode"; };
              toggle.addEventListener("click", () => {
                root.dataset.theme = isDark() ? "light" : "dark";
                try { localStorage.setItem(themeKey, root.dataset.theme); } catch {}
                label();
                configure();
                render([...document.querySelectorAll("pre.mermaid[data-source]")]);
              });
              label();
              configure();

              // Zooming in: the clicked ring is scaled up to fill the onion while it fades out, then the group's
              // own graph and table (from its <template>) replace it.
              const explorer = document.querySelector(".explorer");
              const onion = explorer.querySelector(".onion");
              const detail = explorer.querySelector(".detail");
              const reduceMotion = matchMedia("(prefers-reduced-motion: reduce)").matches;

              async function open(group, ring) {
                const template = [...document.querySelectorAll("template[data-group]")].find(t => t.dataset.group === group);
                if (!template) return;

                if (ring && !reduceMotion) {
                  onion.style.transform = `scale(${ring.dataset.zoom})`;
                  onion.classList.add("zooming");
                  await new Promise(resolve => setTimeout(resolve, 350));
                }

                const body = detail.querySelector(".detail-body");
                body.replaceChildren(template.content.cloneNode(true));
                detail.querySelector("h3").textContent = group;
                explorer.dataset.view = "detail";
                await render([...body.querySelectorAll("pre.mermaid")]);
                detail.querySelector(".back").focus();
                history.replaceState(null, "", "#" + encodeURIComponent(group));
              }

              function close() {
                if (explorer.dataset.view !== "detail") return;
                explorer.dataset.view = "overview";
                onion.classList.remove("zooming");
                onion.style.transform = "";
                history.replaceState(null, "", location.href.split("#")[0]);
              }

              explorer.querySelector(".overview").addEventListener("click", event => {
                const target = event.target.closest("[data-group]");
                if (target) open(target.dataset.group, target.closest(".ring"));
              });
              onion.addEventListener("keydown", event => {
                if ((event.key === "Enter" || event.key === " ") && event.target.matches(".ring")) {
                  event.preventDefault();
                  open(event.target.dataset.group, event.target);
                }
              });
              detail.querySelector(".back").addEventListener("click", close);
              document.addEventListener("keydown", event => { if (event.key === "Escape") close(); });

              if (location.hash) open(decodeURIComponent(location.hash.slice(1)));

              // Not awaited, so the layers respond to clicks while the full graph is still being laid out.
              render([document.getElementById("all-references")]);
            </script>
            </body>
            </html>

            """);

        return html.ToString().ReplaceLineEndings("\n");
    }

    private static string Styles(int groupCount)
    {
        string Series(Func<(string Light, string Dark), string> pick) => string.Concat(
            Enumerable.Range(0, groupCount).Select(i => $"  --series-{i}: {pick(ColorOf(i))};\n"));

        // The Anarchy I/O design tokens (docs/portal/assets/css/tokens.css), inlined so the page stays stand-alone: the
        // brand typefaces are used when installed, with system fallbacks, and nothing is loaded from the documentation.
        const string light = """
              --surface: #FFFFFF;
              --surface-2: #F5F5F7;
              --text-primary: #111114;
              --text-secondary: #5C5C66;
              --border: #DCDCE2;
              --link: #C81818;
              --bad: #C81818;
            """;
        const string dark = """
              --surface: #111114;
              --surface-2: #1C1C21;
              --text-primary: #F5F5F7;
              --text-secondary: #A1A1AA;
              --border: #2E2E36;
              --link: #FF751F;
              --bad: #FF5A4A;
            """;
        return $$"""
            :root {
              color-scheme: light;
              --font-sans: "Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", "Helvetica Neue", Arial, sans-serif;
              --font-mono: "JetBrains Mono", ui-monospace, Menlo, Consolas, monospace;
              --gradient: linear-gradient(110deg, #F02020 0%, #FF751F 100%);
              --radius: 8px;
            {{light}}
            {{Series(c => c.Light)}}}
            @media (prefers-color-scheme: dark) {
              :root:not([data-theme="light"]) {
                color-scheme: dark;
            {{dark}}
            {{Series(c => c.Dark)}}  }
            }
            :root[data-theme="dark"] {
              color-scheme: dark;
            {{dark}}
            {{Series(c => c.Dark)}}}
            * { box-sizing: border-box; }
            body { margin: 0; background: var(--surface); color: var(--text-primary);
              font: 15px/1.6 var(--font-sans); -webkit-font-smoothing: antialiased; }
            main { max-width: 1280px; margin: 0 auto; padding: 32px 24px 64px; }
            header { padding-bottom: 20px; border-bottom: 3px solid transparent; border-image: var(--gradient) 1; }
            h1 { font-size: 1.9rem; font-weight: 700; letter-spacing: -0.01em; margin: 0 0 8px; }
            h2 { font-size: 1.25rem; font-weight: 700; margin: 40px 0 12px; padding-left: 12px; border-left: 3px solid #F02020; }
            a { color: var(--link); text-decoration: none; }
            a:hover { color: #FF751F; text-decoration: underline; }
            .muted, figcaption { color: var(--text-secondary); }
            code { font-family: var(--font-mono); font-size: 0.85em; background: var(--surface-2); border-radius: 4px; padding: 1px 5px; }
            .stats { display: flex; flex-wrap: wrap; gap: 8px; list-style: none; padding: 0; margin: 16px 0 0; }
            .stats li { background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius); padding: 4px 12px; }
            .stats .bad { border-color: var(--bad); }
            .stats .bad strong { color: var(--bad); }
            .explorer[data-view="overview"] .detail, .explorer[data-view="detail"] .overview { display: none; }
            .overview { overflow: hidden; }
            .onion { display: block; max-width: 100%; height: auto; transform-origin: 50% 50%;
              transition: transform 0.35s ease-in, opacity 0.35s ease-in; }
            .onion.zooming { opacity: 0; }
            .onion text { fill: var(--text-primary); font: 600 12px var(--font-sans); pointer-events: none;
              paint-order: stroke; stroke: var(--surface); stroke-width: 3px; stroke-linejoin: round; }
            .ring { cursor: pointer; outline: none; }
            .ring path { fill-opacity: 0.2; transition: fill-opacity 0.15s; }
            .ring:hover path, .ring:focus-visible path { fill-opacity: 0.45; }
            .ring:focus-visible circle { stroke-width: 4; }
            button { font: 500 0.9rem var(--font-sans); color: #FFFFFF; background: var(--gradient); border: 0;
              border-radius: var(--radius); padding: 6px 14px; cursor: pointer; }
            button:hover { background: var(--text-primary); color: var(--surface); }
            .title-row { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; }
            .theme-toggle { flex: none; background: var(--surface-2); color: var(--text-primary); border: 1px solid var(--border); }
            .theme-toggle:hover { background: var(--text-primary); color: var(--surface); }
            .detail-head { display: flex; align-items: center; gap: 16px; margin-bottom: 8px; }
            .detail-head h3 { margin: 0; font-size: 1.1rem; font-weight: 700; }
            .detail .graph { margin: 12px 0 16px; }
            @media (prefers-reduced-motion: reduce) { .onion, .ring path { transition: none; } }
            .legend { display: flex; flex-wrap: wrap; gap: 6px 18px; list-style: none; padding: 0; margin: 20px 0; font-size: 0.9rem; }
            .swatch { display: inline-block; width: 12px; height: 12px; border-radius: 3px; margin-right: 6px; vertical-align: -1px; }
            .table-wrap { overflow-x: auto; }
            table { border-collapse: collapse; width: 100%; font-size: 0.9rem; }
            th { font-weight: 600; color: var(--text-secondary); text-transform: uppercase; font-size: 0.75rem; letter-spacing: 0.04em; }
            th, td { text-align: left; padding: 8px 12px 8px 0; border-bottom: 1px solid var(--border); vertical-align: top; }
            td.num, th.num { text-align: right; font-variant-numeric: tabular-nums; }
            .graph { overflow-x: auto; background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius); padding: 16px; }
            pre.mermaid { margin: 0; font-family: var(--font-sans); }
            """;
    }

    private static string Onion(DependencyMap map, IReadOnlyList<string> groups)
    {
        const double size = 360, center = size / 2, outer = 175;
        var layers = map.Layers;
        var svg = new StringBuilder($"""<svg class="onion" viewBox="0 0 {N(size)} {N(size)}" width="{N(size)}" height="{N(size)}" role="group" aria-label="Layers from the innermost ({Encode(layers.FirstOrDefault() ?? "")}) to the outermost ({Encode(layers.LastOrDefault() ?? "")})">""");
        var labels = new StringBuilder();

        // Outermost first, so each ring's border is drawn over the inner edge of the ring around it.
        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var ro = outer * (i + 1) / layers.Count;
            var ri = outer * i / layers.Count;
            var color = $"var(--series-{groups.ToList().IndexOf(layers[i])})";
            var count = map.Projects.Count(p => p.Group == layers[i]);
            var path = Circle(center, ro) + (ri > 0 ? " " + Circle(center, ri) : "");

            svg.Append($"""<g class="ring" data-group="{Encode(layers[i])}" data-zoom="{N(outer / ro)}" role="button" tabindex="0" aria-label="Zoom into {Encode(layers[i])}: {Projects(count)}">""");
            svg.Append($"""<path d="{path}" fill-rule="evenodd" fill="{color}"><title>{Encode(layers[i])}: {Projects(count)}</title></path>""");
            svg.Append($"""<circle cx="{N(center)}" cy="{N(center)}" r="{N(ro)}" fill="none" stroke="{color}" stroke-width="2"/></g>""");

            var labelY = i == 0 ? center + 4 : center - (ro + ri) / 2 + 4;
            labels.Append($"""<text x="{N(center)}" y="{N(labelY)}" text-anchor="middle">{Encode(layers[i])} ({count})</text>""");
        }

        return svg.Append(labels).Append("</svg>").ToString();
    }

    private static string OutsideButtons(DependencyMap map)
    {
        var outside = map.Groups.Where(g => !map.IsLayer(g))
            .Select(g => $"""<button type="button" data-group="{Encode(g)}">{Encode(g)} ({map.Projects.Count(p => p.Group == g)})</button>""")
            .ToArray();

        return outside.Length == 0 ? "" : $"""<p class="muted">Outside the chain: {string.Join(" ", outside)}</p>""";
    }

    private static string Legend(IReadOnlyList<string> groups)
    {
        var items = groups.Select((g, i) => $"""<li><span class="swatch" style="background: var(--series-{i})"></span>{Encode(g)}</li>""");

        return $"""<ul class="legend">{string.Concat(items)}</ul>""";
    }

    /// <summary>
    /// The zoomed-in view of a group, kept in a <c>&lt;template&gt;</c> until its ring is clicked: the graph of the
    /// references between its own projects, and a table with what each project uses inside and outside the group.
    /// </summary>
    private static string GroupDetail(DependencyMap map, IReadOnlyList<string> groups, string group)
    {
        var members = map.Projects.Where(p => p.Group == group).OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
        var names = members.Select(p => p.Name).ToHashSet();
        var groupOf = map.Projects.ToDictionary(p => p.Name, p => p.Group);
        var inside = map.References.Where(r => names.Contains(r.From) && names.Contains(r.To))
            .OrderBy(r => r.From, StringComparer.Ordinal).ThenBy(r => r.To, StringComparer.Ordinal)
            .ToArray();
        var detail = new StringBuilder($"""<template data-group="{Encode(group)}">""");

        if (members.Length == 0)
        {
            return detail.Append("""<p class="muted">No projects.</p></template>""").Append('\n').ToString();
        }

        var drawn = WithoutTransitive(inside).OrderBy(r => r.From, StringComparer.Ordinal).ThenBy(r => r.To, StringComparer.Ordinal).ToArray();

        string List(IEnumerable<string> projects, bool withGroup = false)
        {
            var items = projects.Order(StringComparer.Ordinal)
                .Select(n => $"<code>{Encode(n)}</code>" + (withGroup ? $""" <span class="muted">({Encode(groupOf[n])})</span>""" : ""))
                .ToArray();
            return items.Length == 0 ? "—" : string.Join(", ", items);
        }

        // Only projects with a reference inside the group are drawn: Mermaid stacks unconnected nodes in one tall
        // column, so those are listed instead (the table below has them too).
        var connected = members.Where(p => inside.Any(r => r.From == p.Name || r.To == p.Name)).ToArray();
        var standalone = members.Except(connected).Select(p => p.Name).ToArray();

        detail.Append($"""<p class="muted">{Projects(members.Length)}, {inside.Length} {(inside.Length == 1 ? "reference" : "references")} between them{(drawn.Length < inside.Length ? " (the graph leaves out the ones already implied by others)" : "")}.</p>""");
        if (connected.Length > 0)
        {
            detail.Append($"""<div class="graph"><pre class="mermaid">{Encode(Mermaid(map, groups, connected, drawn))}</pre></div>""");
        }
        if (standalone.Length > 0)
        {
            detail.Append($"""<p>{(connected.Length > 0 ? "Not referencing or referenced by the others" : "None of them reference each other")}: {List(standalone)}.</p>""");
        }

        detail.Append($"""<div class="table-wrap"><table><thead><tr><th>Project</th><th class="num">Lines</th><th>References in {Encode(group)}</th><th>Referenced by in {Encode(group)}</th><th>References elsewhere</th></tr></thead><tbody>""");

        foreach (var project in members)
        {
            var uses = map.References.Where(r => r.From == project.Name).Select(r => r.To).ToArray();
            var usedBy = inside.Where(r => r.To == project.Name).Select(r => r.From);

            detail.Append($"""<tr><td><code>{Encode(project.Name)}</code></td><td class="num">{Count(project.SourceLines)}</td><td>{List(uses.Where(names.Contains))}</td><td>{List(usedBy)}</td><td>{List(uses.Where(n => !names.Contains(n)), withGroup: true)}</td></tr>""");
        }

        return detail.Append("</tbody></table></div></template>\n").ToString();
    }

    private static string GroupTable(DependencyMap map, IReadOnlyList<string> groups)
    {
        var total = Math.Max(1, map.Projects.Sum(p => p.SourceLines));
        var rows = new StringBuilder();

        foreach (var group in groups)
        {
            var members = map.Projects.Where(p => p.Group == group).OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
            var lines = members.Sum(p => p.SourceLines);
            var names = members.Length == 0 ? "—" : string.Join(", ", members.Select(p => $"<code>{Encode(p.Name)}</code>"));

            rows.Append($"""<tr><td>{Encode(group)}{(map.IsLayer(group) ? "" : " <span class=\"muted\">(outside)</span>")}</td><td class="num">{members.Length}</td><td class="num">{Count(members.Sum(p => p.SourceFiles))}</td><td class="num">{Count(lines)}</td><td class="num">{Percent((double)lines / total)}</td><td>{names}</td></tr>""");
        }

        return $"""<div class="table-wrap"><table><thead><tr><th>Group</th><th class="num">Projects</th><th class="num">Files</th><th class="num">Lines</th><th class="num">Share</th><th>Projects</th></tr></thead><tbody>{rows}</tbody></table></div>""";
    }

    private static string Mermaid(
        DependencyMap map,
        IReadOnlyList<string> groups,
        IReadOnlyList<DependencyMapProject> projects,
        IReadOnlyList<DependencyMapReference> drawn)
    {
        var ids = projects.Select((p, i) => (p.Name, Id: $"p{i}")).ToDictionary(x => x.Name, x => x.Id);
        var text = new StringBuilder("flowchart RL\n");

        // Nodes are colored by group rather than boxed in subgraphs: references cross every group, and Mermaid
        // lays boxed groups out side by side, which makes the graph several screens wide.
        foreach (var project in projects)
        {
            var g = groups.ToList().IndexOf(project.Group);
            text.Append($"  {ids[project.Name]}[\"{Label(project.Name)}\"]:::c{g}\n");
        }

        foreach (var reference in drawn)
        {
            text.Append($"  {ids[reference.From]} {(map.IsViolation(reference) ? "-.->" : "-->")} {ids[reference.To]}\n");
        }

        for (var g = 0; g < groups.Count; g++)
        {
            text.Append($"  classDef c{g} stroke:{ColorOf(g).Light},stroke-width:2px\n");
        }

        var violationIndexes = drawn.Select((r, i) => (r, i)).Where(x => map.IsViolation(x.r)).Select(x => x.i.ToString(CultureInfo.InvariantCulture)).ToArray();
        if (violationIndexes.Length > 0)
        {
            text.Append($"  linkStyle {string.Join(",", violationIndexes)} stroke:#C81818,stroke-width:2px\n");
        }

        return text.ToString();
    }

    /// <summary>
    /// Drops the references implied by others (A → C when A → B → C), which only clutter the graph: references
    /// form a DAG, so a reference is redundant when its target is reachable through another one.
    /// </summary>
    /// <param name="references">The references, forming a directed acyclic graph.</param>
    /// <returns>The references that are not implied by others.</returns>
    public static IReadOnlyList<DependencyMapReference> WithoutTransitive(IReadOnlyCollection<DependencyMapReference> references)
    {
        var targets = references.GroupBy(r => r.From).ToDictionary(g => g.Key, g => g.Select(r => r.To).ToArray());
        var reachable = new Dictionary<string, HashSet<string>>();

        HashSet<string> Reach(string project)
        {
            if (reachable.TryGetValue(project, out var known)) return known;

            var result = reachable[project] = [];
            foreach (var target in targets.GetValueOrDefault(project, []))
            {
                result.Add(target);
                result.UnionWith(Reach(target));
            }

            return result;
        }

        return references
            .Where(r => !targets[r.From].Any(other => other != r.To && Reach(other).Contains(r.To)))
            .ToArray();
    }

    private static (string Light, string Dark) ColorOf(int index) => index < Palette.Length ? Palette[index] : OverflowColor;

    /// <summary>
    /// A full circle as two arcs, so it can be combined into an even-odd ring path.
    /// </summary>
    private static string Circle(double center, double r) =>
        $"M{N(center - r)} {N(center)} a{N(r)} {N(r)} 0 1 0 {N(2 * r)} 0 a{N(r)} {N(r)} 0 1 0 {N(-2 * r)} 0 Z";

    private static string Projects(int count) => count == 1 ? "1 project" : $"{count} projects";

    private static string Percent(double share) => share.ToString("0.#%", CultureInfo.InvariantCulture);

    private static string Count(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Label(string value) => value.Replace("\"", "#quot;");

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
