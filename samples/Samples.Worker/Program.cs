using Anarchy.Toolkit.Samples.Worker.Composition;
using Microsoft.Extensions.Hosting;

await using var entryPoint = new WorkerEntryPoint();

await entryPoint.App.RunAsync();
