namespace ST.Infrastructure.Configuration;

public sealed class MessagingSettings
{
    public const string SectionName = "Messaging";

    public bool Enabled { get; set; } = true;
    public string Transport { get; set; } = "RabbitMq";
    public RabbitMqSettings RabbitMq { get; set; } = new();
    public AzureServiceBusSettings AzureServiceBus { get; set; } = new();
}
