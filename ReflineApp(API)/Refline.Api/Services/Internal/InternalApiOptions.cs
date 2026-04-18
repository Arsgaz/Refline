namespace Refline.Api.Services.Internal;

public sealed class InternalApiOptions
{
    public const string SectionName = "InternalApi";

    public string ApiKey { get; set; } = string.Empty;
}
