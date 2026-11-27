using Amazon.Lambda.Core;

namespace Anarchy.Toolkit.Testing.Aws.Fixture.Amazon.Lambda;

/// <summary>
/// A static, test-only <see cref="ILambdaContext"/> implementation to pass to Lambda handlers under test.
/// </summary>
/// <remarks>
/// Returns fixed values (function <c>TestFunction</c>, version <c>1</c>, 128 MB, 5 minutes remaining) and a new
/// <see cref="AwsRequestId"/> on every access. <see cref="ClientContext"/>, <see cref="Identity"/> and
/// <see cref="Logger"/> return <see langword="null"/>, so code under test must not rely on them.
/// </remarks>
public class LambdaContextFixture : ILambdaContext
{
    /// <inheritdoc/>
    /// <value>A new random GUID on each access.</value>
    public string AwsRequestId => Guid.NewGuid().ToString();
    /// <inheritdoc/>
    /// <value>Always <see langword="null"/>.</value>
    public IClientContext ClientContext => null!;
    /// <inheritdoc/>
    public string FunctionName => "TestFunction";
    /// <inheritdoc/>
    public string FunctionVersion => "1";
    /// <inheritdoc/>
    /// <value>Always <see langword="null"/>.</value>
    public ICognitoIdentity Identity => null!;
    /// <inheritdoc/>
    public string InvokedFunctionArn => "arn:aws:lambda:local:test";
    /// <inheritdoc/>
    /// <value>Always <see langword="null"/>.</value>
    public ILambdaLogger Logger {
        get { return null!; }
    }
    /// <inheritdoc/>
    public string LogGroupName => "test-log-group";
    /// <inheritdoc/>
    public string LogStreamName => "test-log-stream";
    /// <inheritdoc/>
    public int MemoryLimitInMB => 128;
    /// <inheritdoc/>
    /// <value>Always 5 minutes.</value>
    public TimeSpan RemainingTime => TimeSpan.FromMinutes(5);
}
