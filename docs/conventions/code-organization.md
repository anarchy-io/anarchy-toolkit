# Code Organization

Organize projects around shared layers, deployment, distribution, or other meaningful compilation boundaries. Inside a
project, group code by responsibility rather than creating a project for every class or feature.

![Architectural blueprint lines on a dark background with red-orange highlights, wide banner](../assets/images/headers/guides.png)

> **Recommendation, not a requirement.** Adopt these conventions where they help your solution. Existing toolkit
> packages are being migrated selectively, so their current namespaces and folders may still use earlier conventions.
{: .notice--info}

## Why

- **Find the code.** Responsibility-oriented names make the purpose of a class clear without relying on a generic
  `Service` or `Entity` namespace. See [layered architecture](../decisions/0001-layered-architecture.md).
- **Share deliberately.** A monorepo's applications can build on a common library without turning each application
  feature into a separately distributed package.
- **Keep orchestration visible.** UseCases describe workflows; aggregates coordinate resources when consistency or
  persistence requires it. Neither replaces a repository's responsibility for queries and individual writes.

## Projects and folders

A project root contains a `.csproj`. A folder inside that root is ordinary source organization, even if it is named
after a layer. In a monorepo, prefer shared projects under `src/Library/Core`, `Infrastructure`, `Configuration`, and,
when needed, `Web`; deployable APIs, jobs, and UIs live in their own projects. A job typically references shared
Configuration; an API or UI may reference shared Web. The reference direction is
`[Web →] Configuration → Infrastructure → Core`.

Prefer placing Anarchy and other third-party dependencies in the appropriate shared library, exposing registrations
through DI modules or wrapping them where useful. Applications then share a foundation while retaining their own
features. The trade-off is that an application may carry unused transitive assemblies or larger artifacts. The solution
developer decides whether sharing or keeping a dependency application-local is more appropriate.

For a single application, prefer layer projects directly under `src/` and an application project above them; shared
Web remains optional. An application project can contain lower-layer folders to extend shared functionality without
creating more projects. These folders do not change the project's own layer or relax dependency direction. See
[Layering](layering.md) and [Your First Solution](../guides/greenfield-solution.md).

## Names and responsibilities

- Prefer plural namespace segments and singular type names. Keep physical folders aligned with namespaces, except for
  extension classes as described below. Keep meaningful suffixes such as `Entity`, `Dto`, `Model`, and `Repository`
  when they distinguish responsibilities; do not apply them mechanically.
- Name services for what they do, such as `Publisher`, `Validator`, or `Dispatcher`. A repository keeps its
  `Repository` suffix. Avoid generic names that obscure responsibility.
- Put general-purpose helpers under `Helpers/`, grouped by subject. Pure helpers should be deterministic and not
  depend on application state or infrastructure. Technology-specific and test helpers can use dependencies appropriate
  to their layer; a helper must not become a substitute for a domain service.
- Put extension classes under the project-root `Extensions/` folder, organized by their subject. Name the class with
  the `Extensions` suffix and use the subject's namespace, without adding an `Extensions` namespace segment solely for
  the physical folder.
- Define technology-neutral contracts in Core. Organize adapters by resource and then technology, for example
  `Repositories/Orders/Postgres/` and `Repositories/Orders/DynamoDb/`; the composition root selects the implementation.

## UseCases

Place business workflows under a project-root `UseCases/` folder. A UseCase should read as the sequence of operations
needed to accomplish a business task: load the resource, apply changes, validate, persist, and return a result as
appropriate. It normally has one public execution method and does not need an interface by default. Extract reusable
technical operations or independent rules into dependencies instead of obscuring the workflow with private helpers.
Not every operation needs a UseCase.

## Aggregates

Aggregates are services under `Services/Aggregates/`. Use one when related entities must be assembled, changed, and
persisted as a logical resource. For example, `CustomerAggregate` might coordinate `CustomerRepository`,
`AddressRepository`, and `ContactInfoRepository`. Repositories still own queries and individual persistence access;
an aggregate coordinates multi-entity writes and consistency where needed.

Make transaction ownership explicit: an aggregate may own a transaction, participate in a caller-owned transaction,
or use a savepoint in an existing transaction where supported. It must not commit or dispose a transaction owned by its
caller. When domain events and an outbox are involved, coordinate their persistence atomically with the resource changes.

## Tests

Keep unit, integration, and end-to-end suites distinct. A shared `tests/Common/` project can contain fixtures, mocks,
and test helpers. Unit and integration test file locations and namespaces mirror their system-under-test counterparts.
See [Testing](testing.md) for test levels and toolkit test kits.

## Opting out or adapting

Use the structure and naming that suit the solution; record deliberate differences in its architecture decisions or
`AGENTS.md`. A smaller service may not benefit from explicit UseCases or aggregates, and some dependencies belong in
one application rather than a shared library. You give up some consistency between projects and must explain where new
code belongs, but the toolkit packages do not require these folders or names.
