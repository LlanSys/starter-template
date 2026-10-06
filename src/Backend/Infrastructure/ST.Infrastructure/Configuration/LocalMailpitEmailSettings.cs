namespace ST.Infrastructure.Configuration;

public sealed class LocalMailpitEmailSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
}
