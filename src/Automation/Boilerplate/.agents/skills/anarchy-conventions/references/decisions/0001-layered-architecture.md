# 0001. Layered architecture, checked by the build

## Context

Shared .NET libraries drift towards a web of references: a "core" project picks up a database driver, a host references a test helper, and changing one package forces releases of unrelated ones. Reviews catch some of it, late.

## Decision

Projects belong to ordered layers taken from their name prefix or, in folder mode, their folder. A new solution starts with `Core < Infrastructure < Bootstrap < Runtime < Apps`:

- `Bootstrap` (formerly `Configuration`) composes and validates settings and DI modules.
- `Runtime` holds everything that runs an application: hosting, workers and the web packages (`Runtime.Web`, `Runtime.Web.Ui`, ...). There is no separate `Web` layer: web is one kind of runtime, so its packages are `Runtime.Web.*` sub-packages and APIs, UIs and jobs no longer sit on different layers.
- `Apps` holds the applications (APIs, jobs, UIs) and has no library of its own. In folder mode they live in `src/Apps/Apis`, `src/Apps/Jobs` and `src/Apps/Ui` next to `src/Library/<Layer>`; in prefix mode they are `src/Apps.<Kind>.<Name>` next to `src/Library.<Layer>`, the `Library.` segment being skipped when reading a library's layer.
- Build automation is not a layer of an adopter's solution: it is customized in the `build/_build.csproj` project (entry file `build/Build.cs`). The toolkit's own repository keeps an `Automation` layer on top of `Runtime` for its build packages.

Each solution uses one mode and may name its own layers. Optional overrides can be saved in the root `.anarchy-toolkit.json`; without them, the in-code folder baseline applies. A project only uses its own layer or lower ones; project references marked `ReferenceOutputAssembly="false"` only order the build and are not dependencies. `ILayeringBuild` (`make check_layering`) fails on upward project references, upward namespace imports and misplaced layer folders, and `make dependency_map` draws the result. See [ADR 0012](0012-optional-automation-preferences.md) for persistence and default compatibility.

## Consequences

- Business code in `Core` has no I/O dependencies and is tested without containers.
- Package dependencies form a chain, so release order and impact are predictable.
- A file's place follows from its layer and responsibility ([Layering convention](../conventions/layering.md)).
- Some code is split across packages that would otherwise be one (contracts in `Core.*`, adapters in `Infrastructure.*`).

## Alternatives considered

The chosen style is close to what is sometimes called an [onion architecture](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/): the domain at the centre, every dependency pointing inward. It differs in also ordering the outer rings (runtime, applications, build tooling) instead of grouping them as one infrastructure ring.

- **Clean/[hexagonal architecture](https://alistair.cockburn.us/hexagonal-architecture/) by project role (Domain, Application, Adapters).** Similar intent; the layer chain was preferred because it also orders the runtime and the applications.
- **Separate `Hosting` and `Web` layers (the original chain).** Replaced: it put APIs and jobs on different layers for no dependency reason, and forced shared web code into its own library project.
- **Bootstrapping an `Automation` library in every solution.** Replaced: adopters customize their automation in the build project, so the library stayed empty.
- **Review-only rules.** Rejected: unenforced rules erode (see the [Automated value](README.md#values)).
