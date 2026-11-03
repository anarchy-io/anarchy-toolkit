namespace Anarchy.Toolkit.Hosting.Workers.Entity.Service.Health
{
    internal class BuildMetadata
    {
        public string? BuildMachine { get; set; }
        public string? BuiltBy { get; set; }
        public DateTimeOffset? BuiltWhen { get; set; }
        public string? GitSha1 { get; set; }
        public string? BuildNumber { get; set; }
    }
}
