using VitalNexus.AiAnalysis.Worker.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddDapr();
builder.Services.AddAiAnalysisWorker(builder.Configuration);

// Application Insights baseline. No-ops locally until APPLICATIONINSIGHTS_CONNECTION_STRING
// (or ApplicationInsights:ConnectionString) is configured in Azure.
// IMPORTANT: never log PHI, raw lab values, AI prompts/responses, or clinical notes.
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

app.UseCloudEvents();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapSubscribeHandler();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = builder.Configuration.GetSection(WorkerOptions.SectionName)["ServiceName"]
        ?? "VitalNexus.AiAnalysis.Worker",
    environment = app.Environment.EnvironmentName,
    utc = DateTime.UtcNow.ToString("O"),
}));

app.Run();
