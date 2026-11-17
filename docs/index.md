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
  **A .NET developer platform.** An integrated ecosystem for building maintainable .NET applications
  through consistent architecture, reusable infrastructure and automated development workflows. Build without limits.
intro:
  - excerpt: "Architecture. Infrastructure. Build engineering. Three pillars, each adoptable on its own, designed to work consistently together."
pillars:
  - image_path: /assets/images/features/pillars-architecture.png
    alt: "Layered architecture illustration"
    title: "Architecture"
    excerpt: "Layered application structure where dependencies only point down, DI-module composition and enforceable conventions for async, exceptions, enums, endpoints and tests."
    url: "/guides/architecture.html"
    btn_class: "btn--pillar btn--pillar-architecture"
    btn_label: "Explore the architecture"
  - image_path: /assets/images/features/pillars-infra.png
    alt: "Reusable infrastructure components illustration"
    title: "Infrastructure"
    excerpt: "Independently published packages for persistence, messaging, observability, configuration, hosting, APIs, UIs and testing: reference only what you use."
    url: "/packages/"
    btn_class: "btn--pillar btn--pillar-infrastructure"
    btn_label: "Browse the packages"
  - image_path: /assets/images/features/pillars-build.png
    alt: "Build automation pipeline illustration"
    title: "Build Ecosystem"
    excerpt: "Fallout build components and the `anarchy-toolkit` CLI: scaffolding, validation, tests, layering checks, packaging, versioning and releases."
    url: "/guides/fallout-builds.html"
    btn_class: "btn--pillar btn--pillar-build"
    btn_label: "Automate the build"
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

{% include feature_row id="pillars" %}

## Choose your entry point
{: .adoption-heading}

Adopt the whole platform, or start with the single piece you need.
{: .adoption-lead}

{% include feature_row %}

## Where to go next

- **[Overview](overview/)**: what the platform is, how its three pillars fit together and the ideas behind it.
- **[Getting Started](getting-started/)**: choose between the CLI tool, the Fallout build and individual packages.
- **[NuGet.org](https://www.nuget.org/profiles/anarchy-io)**: every published `Anarchy.Toolkit.*` package and its versions.
- **[Guides](guides/)**: the architecture, infrastructure, build and engineering-practice guides.
- **[Samples](samples/)**: runnable applications composed from the platform.
- **[API reference](api/)**: the generated reference for every public type.
