namespace ST.Infrastructure.Configuration;

public sealed class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public ushort Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "guest";
}
