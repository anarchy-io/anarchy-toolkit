## Releases

### 3.0.0

- **Breaking:** extension classes take the `Extensions` suffix under `Extensions/<extended namespace>/`, helpers live in `Helpers/<Subject>/`, and their namespaces follow those folders: update `using` directives.
- **Breaking:** depends on the new major versions of `Infrastructure.Persistence.Db`.

### 2.0.0

- Update dependencies for the relational event-persistence split and event contract changes.

### 1.0.1

- chore(build): Format package release notes property

### 1.0.0

- New package identity, renamed from `Anarchy.Toolkit.Infrastructure.Persistence.Mysql`; versions below are historical releases under that package ID.
- Preserve unit-of-work failures and use provider-neutral application telemetry.

### Historical releases under `Anarchy.Toolkit.Infrastructure.Persistence.Mysql`

### 1.2.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata

### 1.2.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1

### 1.1.5

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.1.4

- refactor(src): clean up code style across library packages
- chore(deps): update AWS SDK, Lambda ASP.NET Core server and MySqlConnector packages

### 1.1.2

- build: migrate local automation to build tooling

### 1.1.1

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.1.0

- Applied the configurable maximum pool size to MySQL connection strings.

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced the MySQL relational persistence provider, connection handling and provider-specific health reports.
