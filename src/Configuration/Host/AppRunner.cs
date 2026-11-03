namespace Anarchy.Toolkit.Configuration.Host;

public record AppRunner(AppRunner.Types Type)
{
    public enum Types
    {
        Lambda,
        Ec2Worker
    }
}
