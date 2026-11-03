using Amazon.Lambda.Core;

namespace Anarchy.Toolkit.Testing.Fixture.Amazon.Lambda;

public class LambdaContextFixture : ILambdaContext
{
    public string AwsRequestId => Guid.NewGuid().ToString();
    public IClientContext ClientContext => null!;
    public string FunctionName => "TestFunction";
    public string FunctionVersion => "1";
    public ICognitoIdentity Identity => null!;
    public string InvokedFunctionArn => "arn:aws:lambda:local:test";
    public ILambdaLogger Logger {
        get { return null!; }
    }
    public string LogGroupName => "test-log-group";
    public string LogStreamName => "test-log-stream";
    public int MemoryLimitInMB => 128;
    public TimeSpan RemainingTime => TimeSpan.FromMinutes(5);
}
