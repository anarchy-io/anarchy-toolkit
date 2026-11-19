# xUnit v2 + VSTest to xUnit v3 + Microsoft.Testing.Platform

The toolkit's test kits (`Anarchy.Toolkit.Testing*`) and test targets expect xUnit v3 test projects running on
Microsoft.Testing.Platform (MTP). Move one test project per change, and run the suite before and after each move: the
number of tests and their results must not change.

## Steps

1. **Runner.** Add the MTP runner to `global.json` (`make boilerplate ITEMS=global.json APPEND=true` adds it when
   missing):

   ```json
   { "test": { "runner": "Microsoft.Testing.Platform" } }
   ```

   With it, `dotnet test` runs MTP projects only. If some projects must stay on VSTest for a while, leave
   `global.json` alone and run those projects with `dotnet test <project>` and the migrated ones with
   `dotnet run --project <project>`; the toolkit's `Test*` targets run each project with the runner it uses.
2. **Project file.** `<OutputType>Exe</OutputType>`; replace `xunit` with `xunit.v3` and keep
   `xunit.runner.visualstudio` (3.x) plus `Microsoft.NET.Test.Sdk` for IDEs. Remove VSTest-only packages such as
   `coverlet.collector` (use `Microsoft.Testing.Extensions.CodeCoverage`) and VSTest adapters such as
   `TeamCity.VSTest.TestAdapter`; check with the user how the CI server should receive MTP results (for example TRX
   files from `Microsoft.Testing.Extensions.TrxReport`).
3. **Companion packages.** Use their v3 builds: `AutoFixture.Xunit3` (instead of `AutoFixture.Xunit2`),
   `Snapshooter.Xunit3` (instead of `Snapshooter.Xunit`; same `.snap` files and `__snapshots__` folders, names come
   from the test class and method, so renaming those renames the snapshot), `Xunit.SkippableFact` is no longer needed
   (`Assert.Skip*`). The toolkit's testing kits need Moq 4.21 or later.
4. **Code.**
   - `ITestOutputHelper` moved to `Xunit` (it was in `Xunit.Abstractions`); `IAsyncLifetime` returns `ValueTask`.
   - Pass `TestContext.Current.CancellationToken` to calls that take a token (analyzer `xUnit1051`, an error in
     Release builds with warnings as errors).
   - `[Theory]` data with non-serializable values works without changes; `MemberData` must be static.
   - Collection fixtures and `[assembly: CollectionBehavior]` work as before. All tests of one project run in one
     process, in parallel across classes.
5. **Naming.** Name projects by kind (`Unit`, `Integration`, `EndToEnd`, or `<Name>.Tests.<Kind>`), or map existing
   names with `TestKindAliases` in `.fallout/parameters.json`. Prefer one project per kind: projects do not run in
   parallel with each other.
6. **Verify.** `make test_unit` (and the other kinds), then filters on one project:
   `dotnet run --project tests/Unit -c Release -- -class "*OrderTest"`.
