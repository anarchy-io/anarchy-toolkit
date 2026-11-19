# 0001. Layered architecture, checked by the build

## Context

Shared .NET libraries drift towards a web of references: a "core" project picks up a database driver, a host references a test helper, and changing one package forces releases of unrelated ones. Reviews catch some of it, late.

## Decision

Projects belong to one of six ordered layers, `Core < Infrastructure < Configuration < Hosting < Web < Automation` in this repository, taken from their name prefix (or their folder). The build tooling retains `Build` as the default top-layer name for existing adopter solutions; layer names are configurable. A project only uses its own layer or lower ones. `ILayeringBuild` (`make check_layering`) fails on upward project references, upward namespace imports and misplaced layer folders, and `make dependency_map` draws the result.

## Consequences

- Business code in `Core` has no I/O dependencies and is tested without containers.
- Package dependencies form a chain, so release order and impact are predictable.
- A file's place follows from its layer and responsibility ([Layering convention](../conventions/layering.md)).
- Some code is split across packages that would otherwise be one (contracts in `Core.*`, adapters in `Infrastructure.*`).

## Alternatives considered

- **Clean/[hexagonal architecture](https://alistair.cockburn.us/hexagonal-architecture/) by project role (Domain, Application, Adapters).** Similar intent; the layer chain was preferred because it also orders hosting, web and build tooling.
- **Review-only rules.** Rejected: unenforced rules erode (see the [Automated value](README.md#values)).
