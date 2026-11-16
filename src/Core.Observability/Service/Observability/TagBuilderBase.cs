namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

/// <summary>
/// Fluent builder for metric tags in the <c>name:value</c> format expected by <see cref="MeasurerBase"/>.
/// </summary>
/// <remarks>
/// The built array always starts with a <c>success:true</c> or <c>success:false</c> tag, followed by the added tags in
/// insertion order. Instances convert implicitly to <see cref="string"/>[] so they can be passed directly as the
/// <c>tags</c> argument of metric methods. Use the concrete <see cref="Tags"/> class to create instances.
/// </remarks>
public abstract class TagBuilderBase
{
    /// <summary>
    /// Whether the built tags describe a successful outcome; rendered as the leading <c>success:</c> tag.
    /// </summary>
    protected bool Success;

    /// <summary>
    /// The tags added so far, already formatted as <c>name:value</c>.
    /// </summary>
    protected List<string> Tags = new();

    /// <summary>
    /// Adds a tag with the given name and string value.
    /// </summary>
    /// <param name="tag">The tag name.</param>
    /// <param name="value">The tag value.</param>
    /// <returns>This builder, for chaining.</returns>
    public abstract TagBuilderBase WithTag(string tag, string value);

    /// <summary>
    /// Adds a tag with the given name and integer value.
    /// </summary>
    /// <param name="tag">The tag name.</param>
    /// <param name="value">The tag value.</param>
    /// <returns>This builder, for chaining.</returns>
    public abstract TagBuilderBase WithTag(string tag, int value);

    /// <summary>
    /// Adds a tag with the given name and long value.
    /// </summary>
    /// <param name="tag">The tag name.</param>
    /// <param name="value">The tag value.</param>
    /// <returns>This builder, for chaining.</returns>
    public abstract TagBuilderBase WithTag(string tag, long value);

    /// <summary>
    /// Add a tag with name "outcome" and the provided value.
    /// </summary>
    /// <param name="value">The outcome value.</param>
    /// <returns>This builder, for chaining.</returns>
    public TagBuilderBase WithOutcome(string value) => WithTag("outcome", value);

    /// <summary>
    /// Add a tag with name "outcome" and value "success".
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
    public TagBuilderBase WithOutcomeSuccess() => WithTag("outcome", "success");

    /// <summary>
    /// Add a tag with name "outcome" and the type of the provided exception as its value.
    /// </summary>
    /// <param name="ex">The exception whose type name (e.g. <c>TimeoutException</c>) becomes the outcome value.</param>
    /// <returns>This builder, for chaining.</returns>
    public TagBuilderBase WithOutcome(Exception ex) => WithTag("outcome", ex.GetType().Name);

    /// <summary>
    /// Builds the tag array: the <c>success:true|false</c> tag followed by every added tag.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The built tags.</returns>
    public static implicit operator string[](TagBuilderBase builder) => builder.Build();

    private string[] Build()
    {
        var ts = new string[Tags.Count + 1];
        ts[0] = $"success:{Success.ToString().ToLower()}";
        Array.Copy(Tags.ToArray(), 0, ts, 1, Tags.Count);
        return ts;
    }
}

/// <summary>
/// Default <see cref="TagBuilderBase"/> implementation. Start from <see cref="Success"/> or <see cref="Failure"/>.
/// </summary>
/// <example>
/// <code language="csharp">
/// measurer.IncrementCounter("orders.processed", Tags.Success.WithTag("region", "eu").WithOutcomeSuccess());
/// measurer.IncrementCounter("orders.processed", Tags.Failure.WithOutcome(exception));
/// </code>
/// </example>
public class Tags : TagBuilderBase
{
    /// <summary>
    /// Gets a new builder whose output starts with <c>success:true</c>.
    /// </summary>
    public static new TagBuilderBase Success => new Tags(true);

    /// <summary>
    /// Gets a new builder whose output starts with <c>success:false</c>.
    /// </summary>
    public static TagBuilderBase Failure => new Tags(false);

    private Tags(bool success) => base.Success = success;

    /// <inheritdoc />
    public override TagBuilderBase WithTag(string tag, string value)
    {
        Tags.Add($"{tag}:{value}");
        return this;
    }

    /// <inheritdoc />
    public override TagBuilderBase WithTag(string tag, int value) => WithTag(tag, value.ToString());

    /// <inheritdoc />
    public override TagBuilderBase WithTag(string tag, long value) => WithTag(tag, value.ToString());
}
