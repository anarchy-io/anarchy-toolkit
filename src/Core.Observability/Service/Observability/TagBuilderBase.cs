namespace Anarchy.Toolkit.Core.Observability.Service.Observability
{
    public abstract class TagBuilderBase
    {
        protected bool Success;
        protected List<string> Tags = new();

        public abstract TagBuilderBase WithTag(string tag, string value);
        public abstract TagBuilderBase WithTag(string tag, int value);
        public abstract TagBuilderBase WithTag(string tag, long value);

        /// <summary>
        /// Add a tag with name "outcome" and the provided value.
        /// </summary>
        public TagBuilderBase WithOutcome(string value) => WithTag("outcome", value);

        /// <summary>
        /// Add a tag with name "outcome" and value "success".
        /// </summary>
        public TagBuilderBase WithOutcomeSuccess() => WithTag("outcome", "success");

        /// <summary>
        /// Add a tag with name "outcome" and the type of the provided exception as its value.
        /// </summary>
        public TagBuilderBase WithOutcome(Exception ex) => WithTag("outcome", ex.GetType().Name);

        public static implicit operator string[](TagBuilderBase builder) => builder.Build();

        private string[] Build()
        {
            var ts = new string[Tags.Count + 1];
            ts[0] = $"success:{Success.ToString().ToLower()}";
            Array.Copy(Tags.ToArray(), 0, ts, 1, Tags.Count);
            return ts;
        }
    }

    public class Tags : TagBuilderBase
    {
        public static new TagBuilderBase Success => new Tags(true);
        public static TagBuilderBase Failure => new Tags(false);

        private Tags(bool success) => base.Success = success;

        public override TagBuilderBase WithTag(string tag, string value)
        {
            Tags.Add($"{tag}:{value}");
            return this;
        }

        public override TagBuilderBase WithTag(string tag, int value) => WithTag(tag, value.ToString());
        public override TagBuilderBase WithTag(string tag, long value) => WithTag(tag, value.ToString());
    }
}
