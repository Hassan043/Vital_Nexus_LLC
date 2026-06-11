namespace NutrientInsight.Api.Configuration;

public sealed class AnthropicOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-3-5-sonnet-latest";
}
