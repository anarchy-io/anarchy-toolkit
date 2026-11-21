## Releases

### 2.0.1

- chore(build): Format package release notes property

### 2.0.0

- Preserve the original session failure when transaction startup, rollback or cleanup fails; propagate caller cancellation and reject concurrent sessions.
- Reject repeated transaction starts and preserve both failures when transaction and connection cleanup fail.
- Make transaction ownership explicit with `RunInJoinedSessionAsync` and `RunInJoinedOrManagedSessionAsync`; participants cannot begin, commit or roll back the caller's transaction.
- Roll back only pending transactions, explicitly roll back what a `RunInSessionAsync` callback left open, and stop reporting post-commit notification failures as commit failures.
- Check cancellation before an automatic commit and never interrupt a started commit; publish transaction lifecycle events with an uncancelled token.
- Discard the connection after a failed begin or commit, make session cleanup idempotent, and reject `BeginAsync`, `CommitAsync` and `RollbackAsync` calls from other async flows.

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

### 1.1.2

- build: migrate local automation to build tooling

### 1.1.1

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.1.0

- Added configurable maximum database connection pool size.

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced provider-agnostic relational repositories, units of work, connection configuration, event store support and health reporting.
