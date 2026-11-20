# xUnit v3 test projects on Microsoft.Testing.Platform

The toolkit's test kits (`Anarchy.Toolkit.Testing*`) and test targets expect xUnit v3 test projects running on
Microsoft.Testing.Platform (MTP). Add one test project at a time, and run the suite after each: `make test_unit` (and
the other kinds) must discover and run it.

## Steps

1. **Runner.** Select the MTP runner in `global.json` (`make boilerplate ITEMS=global.json APPEND=true` adds it when
   missing):

   ```json
   { "test": { "runner": "Microsoft.Testing.Platform" } }
   ```

   With it, `dotnet test` runs MTP projects only; the toolkit's `Test*` targets run each project with the runner it
   uses. Leave `global.json` alone when the repository has test projects that are not xUnit v3.
2. **Project file.** `<OutputType>Exe</OutputType>`; reference `xunit.v3`, plus `xunit.runner.visualstudio` (3.x) and
   `Microsoft.NET.Test.Sdk` for IDEs, and the `Testing*` kit the tests need. For coverage use
   `Microsoft.Testing.Extensions.CodeCoverage`. Check with the user how the CI server should receive MTP results (for
   example TRX files from `Microsoft.Testing.Extensions.TrxReport`, `--report-trx`).
3. **Companion packages.** Use their v3 builds: `AutoFixture.Xunit3`, `Snapshooter.Xunit3` (`.snap` files and
   `__snapshots__` folders take their names from the test class and method). Skipping uses `Assert.Skip*`. The
   toolkit's testing kits need Moq 4.21 or later.
4. **Code.**
   - `ITestOutputHelper` is in `Xunit`; `IAsyncLifetime` returns `ValueTask`.
   - Pass `TestContext.Current.CancellationToken` to calls that take a token (analyzer `xUnit1051`, an error in
     Release builds with warnings as errors).
   - `MemberData` members must be static. Collection fixtures and `[assembly: CollectionBehavior]` are supported. All
     tests of one project run in one process, in parallel across classes.
5. **Naming.** Name projects by kind (`Unit`, `Integration`, `EndToEnd`, or `<Name>.Tests.<Kind>`), or declare other
   names with `TestKindAliases` in `.fallout/parameters.json`. Prefer one project per kind: projects do not run in
   parallel with each other.
6. **Verify.** `make test_unit` (and the other kinds), then filters on one project:
   `dotnet run --project tests/Unit -c Release -- -class "*OrderTest"`.
