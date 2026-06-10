namespace NutrientInsight.Api.Configuration;

public sealed class AppOptions
{
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    public ConnectionStrings ConnectionStrings { get; set; } = new();
    public JwtOptions Jwt { get; set; } = new();
    public SmtpOptions Smtp { get; set; } = new();
    public AnthropicOptions Anthropic { get; set; } = new();
}
