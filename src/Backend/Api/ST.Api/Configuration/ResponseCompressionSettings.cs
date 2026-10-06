namespace ST.Api.Configuration;

internal sealed class ResponseCompressionSettings
{
    public const string SectionName = "ResponseCompression";

    public bool Enabled { get; set; } = true;

    public bool EnableForHttps { get; set; } = true;
}
