---
layout: splash
title: "Anarchy I/O Toolkit"
permalink: /
hidden: true
sidebar: false
toc: false
header:
  overlay_color: "#111114"
  overlay_filter: 0.65
  overlay_image: /assets/images/splash/home.png
  image_description: "Abstract dark background with red-orange flame light trails, minimal and angular (wide banner, 1920x600)"
  actions:
    - label: "Get started"
      url: "/getting-started/"
    - label: "Browse packages"
      url: "/packages/"
excerpt: >
  **Build without limits.** Composable .NET foundations for APIs, UIs, jobs and workers.
  The toolkit provides the foundation; you decide the destination.
intro:
  - excerpt: "One package per concern, layered so dependencies only point down, wired with a single module call, and published independently to nuget.org."
feature_row:
  - image_path: /assets/images/features/cli-tool.png
    alt: "Terminal window running a scaffolding command, flat outline illustration, red-orange accent on dark background"
    title: "Start with the CLI tool"
    excerpt: "Bootstrap a layered solution with its build, Makefile and scripts, then scaffold APIs, jobs and UIs into it."
    url: "/getting-started/cli-tool.html"
    btn_class: "btn--primary"
    btn_label: "Bootstrap a solution"
  - image_path: /assets/images/features/fallout.png
    alt: "Build pipeline of connected gears or blocks, flat outline illustration, red-orange accent on dark background"
    title: "Bring the Fallout build"
    excerpt: "Add compile, lint, test, layering, packaging and release targets to an existing repository."
    url: "/getting-started/fallout.html"
    btn_class: "btn--primary"
    btn_label: "Add the build"
  - image_path: /assets/images/features/packages.png
    alt: "Stacked modular building blocks in layers, flat outline illustration, red-orange accent on dark background"
    title: "Pick the packages"
    excerpt: "Persistence, messaging, observability, hosting, web and testing kits: reference only what you use."
    url: "/packages/"
    btn_class: "btn--primary"
    btn_label: "See the catalog"
---

{% include feature_row id="intro" type="center" %}

{% include feature_row %}

## Where to go next

- **[Overview](overview/)**: what the toolkit is, how its layers fit together and the ideas behind it.
- **[Getting Started](getting-started/)**: choose between the CLI tool, the Fallout build and individual packages.
- **[NuGet.org](https://www.nuget.org/profiles/anarchy-io)**: every published `Anarchy.Toolkit.*` package and its versions.
- **[Guides](guides/)**: the architecture, coding, observability and testing conventions.
