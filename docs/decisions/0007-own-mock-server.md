# 0007. An in-process mock server instead of a MockServer container

## Context

Integration and end-to-end tests stood in for HTTP upstreams with a [MockServer](https://www.mock-server.com/) Docker container: a JVM to start, Docker needed on every agent, and unmatched calls checked through its verify API from the build.

## Decision

`Anarchy.Toolkit.Web.MockServer` is a .NET host that loads MockServer-format JSON expectations and can be extended with middlewares in your own host. `Testing.MockServer` asserts recorded requests (`ShouldAllHaveBeenMatched`), `IMockServerBuild` starts it around the tests, and `MockServerFixture` spawns it from an IDE.

## Consequences

- No Docker needed for tests that only mock HTTP.
- Unmatched-request checks live in the tests, not in the build.
- Features it lacks (a built-in proxy/record mode) are added as middlewares; see [Recording a real upstream](../guides/mock-server.md#recording-a-real-upstream).

## Alternatives considered

- **A MockServer container.** Nothing in the toolkit requires the toolkit mock server, so a container remains an option.
- **[WireMock.Net](https://github.com/WireMock-Net/WireMock.Net).** Capable, but a different expectation format from MockServer's.
