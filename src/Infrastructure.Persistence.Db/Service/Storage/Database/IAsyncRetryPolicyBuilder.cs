using Polly;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;

public interface IAsyncRetryPolicyBuilder<T>
{
    ResiliencePipeline Build();
}
