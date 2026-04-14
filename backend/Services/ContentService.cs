using System.Text.Json;
using System.Text.RegularExpressions;
using NutrientInsight.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Retry;

namespace NutrientInsight.Api.Services;

public class ContentService
{
    private readonly ILogger<ContentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private List<MarkerDefinition>? _cachedDefinitions;
    private readonly SemaphoreSlim _dynamicFileLock = new(1, 1);
    private readonly Dictionary<string, SemaphoreSlim> _markerLocks = new();
    private readonly AsyncRetryPolicy _retryPolicy;

    public ContentService(ILogger<ContentService> logger, IConfiguration configuration, IMemoryCache cache)
    {
        _logger = logger;
        _configuration = configuration;
        _cache = cache;

        // Polly retry policy: exponential backoff, 3 retries
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} after {Delay}s due to: {Exception}", 
                        retryCount, timeSpan.TotalSeconds, exception.Message);
                });
    }

    public List<MarkerDefinition> GetMarkerDefinitions()
    {
        if (_cachedDefinitions != null)
            return _cachedDefinitions;

        try
        {
            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "markers.json");
            
            _logger.LogInformation("Attempting to load markers from: {Path}", jsonPath);
            
            if (!File.Exists(jsonPath))
            {
                _logger.LogWarning("markers.json not found at {Path}, using default definitions", jsonPath);
                return GetDefaultMarkerDefinitions();
            }

            var jsonContent = File.ReadAllText(jsonPath);
            _logger.LogInformation("Read {Length} characters from JSON file", jsonContent.Length);
            
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            
            var wrapper = JsonSerializer.Deserialize<MarkerWrapper>(jsonContent, options);
            _cachedDefinitions = wrapper?.Markers;

            if (_cachedDefinitions == null || _cachedDefinitions.Count == 0)
            {
                _logger.LogWarning("No markers found in JSON, using defaults");
                return GetDefaultMarkerDefinitions();
            }

            _logger.LogInformation("✅ Successfully loaded {Count} marker definitions from JSON", _cachedDefinitions.Count);
            return _cachedDefinitions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load marker definitions, using defaults");
            return GetDefaultMarkerDefinitions();
        }
    }

    public async Task<MarkerDefinition?> GetOrGenerateMarkerDefinitionAsync(string markerName)
    {
        // 1. Check static JSON first
        var definitions = GetMarkerDefinitions();
        var def = definitions.FirstOrDefault(d => 
            d.Key.Equals(markerName, StringComparison.OrdinalIgnoreCase) ||
            d.DisplayName.Equals(markerName, StringComparison.OrdinalIgnoreCase) ||
            markerName.Contains(d.Key, StringComparison.OrdinalIgnoreCase) ||
            d.Key.Contains(markerName, StringComparison.OrdinalIgnoreCase) ||
            d.DisplayName.Contains(markerName, StringComparison.OrdinalIgnoreCase));

        if (def != null)
            return def;

        // 2. Check dynamic JSON (previously generated)
        var dynamicDef = await LoadFromDynamicJsonAsync(markerName);
        if (dynamicDef != null)
            return dynamicDef;

        // 3. Check memory cache
        if (_cache.TryGetValue($"marker_{markerName}", out MarkerDefinition? cached))
        {
            _logger.LogInformation("Retrieved {Marker} from memory cache", markerName);
            return cached;
        }

        // 4. Generate via AI (with per-marker lock to prevent duplicate calls)
        var markerLock = GetMarkerLock(markerName);
        await markerLock.WaitAsync();
        try
        {
            // Double-check cache after acquiring lock
            if (_cache.TryGetValue($"marker_{markerName}", out MarkerDefinition? cachedAfterLock))
                return cachedAfterLock;

            _logger.LogInformation("🤖 Generating definition for unknown marker: {Marker}", markerName);
            var generated = await GenerateMarkerDefinitionAsync(markerName);
            
            // Cache in memory
            _cache.Set($"marker_{markerName}", generated, TimeSpan.FromHours(24));
            
            // Persist to dynamic JSON
            await SaveToDynamicJsonAsync(generated);
            
            return generated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate definition for {Marker}, using fallback", markerName);
            return GetFallbackDefinition(markerName);
        }
        finally
        {
            markerLock.Release();
        }
    }

    private SemaphoreSlim GetMarkerLock(string markerName)
    {
        lock (_markerLocks)
        {
            if (!_markerLocks.ContainsKey(markerName))
                _markerLocks[markerName] = new SemaphoreSlim(1, 1);
            return _markerLocks[markerName];
        }
    }

    private async Task<MarkerDefinition?> LoadFromDynamicJsonAsync(string markerName)
    {
        try
        {
            var dynamicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "markerDictionary.dynamic.json");
            
            if (!File.Exists(dynamicPath))
                return null;

            var jsonContent = await File.ReadAllTextAsync(dynamicPath);
            var wrapper = JsonSerializer.Deserialize<MarkerWrapper>(jsonContent, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return wrapper?.Markers?.FirstOrDefault(d =>
                d.Key.Equals(markerName, StringComparison.OrdinalIgnoreCase) ||
                d.DisplayName.Equals(markerName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load from dynamic JSON");
            return null;
        }
    }

    private async Task SaveToDynamicJsonAsync(MarkerDefinition definition)
    {
        await _dynamicFileLock.WaitAsync();
        try
        {
            var dynamicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "markerDictionary.dynamic.json");
            
            MarkerWrapper wrapper;
            if (File.Exists(dynamicPath))
            {
                var existing = await File.ReadAllTextAsync(dynamicPath);
                wrapper = JsonSerializer.Deserialize<MarkerWrapper>(existing, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                }) ?? new MarkerWrapper { Markers = new List<MarkerDefinition>() };
            }
            else
            {
                wrapper = new MarkerWrapper { Markers = new List<MarkerDefinition>() };
            }

            // Remove any existing definition with same key
            wrapper.Markers?.RemoveAll(m => m.Key.Equals(definition.Key, StringComparison.OrdinalIgnoreCase));
            
            // Add new definition
            wrapper.Markers ??= new List<MarkerDefinition>();
            wrapper.Markers.Add(definition);

            var json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(dynamicPath, json);
            _logger.LogInformation("💾 Persisted {Marker} to dynamic JSON", definition.DisplayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist marker to dynamic JSON");
        }
        finally
        {
            _dynamicFileLock.Release();
        }
    }

    private async Task<MarkerDefinition> GenerateMarkerDefinitionAsync(string markerName)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
                     ?? _configuration["Anthropic:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("Anthropic API key not found");
            return GetFallbackDefinition(markerName);
        }

        var prompt = $@"You are a medical content expert creating educational (non-diagnostic) lab marker definitions.

For the lab marker ""{markerName}"", provide:

1. What This Marker Measures (1-2 simple sentences, 5th-8th grade reading level)
2. Why It Is Commonly Tested (1 sentence)
3. Educational Context If Below Range (2-3 sentences, safe language only)
4. Educational Context If Above Range (2-3 sentences, safe language only)
5. Related Markers (comma-separated list of 3-5 markers often reviewed together)

CRITICAL SAFETY RULES:
- NO disease names (no ""diabetes"", ""hypothyroidism"", ""cancer"", ""disease"", etc.)
- NO risk language (no ""increases risk of"", ""causes"", ""leads to"", etc.)
- NO medication recommendations (no drug names, no ""take"", ""prescribe"")
- NO supplement dosing (no mg, IU, mcg amounts)
- NO diagnostic language (no ""you have"", ""diagnosis"", ""confirms"", ""indicates you have"")
- Use ONLY: ""may be associated with"", ""often discussed"", ""commonly reviewed"", ""sometimes seen""
- Always include: ""Your clinician can provide personalized context""

Respond ONLY with valid JSON in this exact format (no markdown, no explanation):
{{
  ""whatItMeasures"": ""..."",
  ""whyTested"": ""..."",
  ""belowContext"": ""..."",
  ""aboveContext"": ""..."",
  ""relatedMarkers"": [""Marker1"", ""Marker2"", ""Marker3""]
}}";

        try
        {
            // Use HttpClient directly - more reliable than SDK
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var requestBody = new
            {
                model = "claude-3-5-sonnet-20241022",
                max_tokens = 1024,
                temperature = 0.3,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

var response = await _retryPolicy.ExecuteAsync(async () =>
{
    var result = await httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
    if (!result.IsSuccessStatusCode)
    {
        var errorBody = await result.Content.ReadAsStringAsync();
        _logger.LogError("Anthropic API error ({Status}): {Error}", result.StatusCode, errorBody);
    }
    result.EnsureSuccessStatusCode();
    return result;
});

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            var messageContent = responseObj
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";

            // Extract JSON (strip markdown if present)
            var jsonStart = messageContent.IndexOf('{');
            var jsonEnd = messageContent.LastIndexOf('}') + 1;
            if (jsonStart == -1 || jsonEnd <= jsonStart)
                throw new Exception("No JSON found in response");

            var jsonContent = messageContent.Substring(jsonStart, jsonEnd - jsonStart);

            var parsed = JsonSerializer.Deserialize<GeneratedMarkerContent>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null)
                throw new Exception("Failed to parse JSON response");

            var definition = new MarkerDefinition
            {
                Key = markerName.ToLower().Replace(" ", "_").Replace("(", "").Replace(")", ""),
                DisplayName = markerName,
                WhatItMeasures = parsed.WhatItMeasures,
                WhyTested = parsed.WhyTested,
                BelowContext = parsed.BelowContext,
                AboveContext = parsed.AboveContext,
                RelatedMarkers = parsed.RelatedMarkers.ToArray()
            };

            // Safety validation
            if (!IsSafeDefinition(definition))
            {
                _logger.LogWarning("🚨 Generated unsafe content for {Marker}, using fallback", markerName);
                return GetFallbackDefinition(markerName);
            }

            _logger.LogInformation("✅ Generated safe definition for {Marker}", markerName);
            return definition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate marker definition via Anthropic");
            return GetFallbackDefinition(markerName);
        }
    }

    private bool IsSafeDefinition(MarkerDefinition def)
    {
        var unsafePatterns = new[]
        {
            @"\bdiabetes\b",
            @"\bhypothyroidism\b",
            @"\bhyperthyroidism\b",
            @"\bcancer\b",
            @"\bdisease\b",
            @"\byou have\b",
            @"\bdiagnosis\b",
            @"\bdiagnose\b",
            @"\btreatment\b",
            @"\btreat\b",
            @"\bcure\b",
            @"\bprevent\b",
            @"\bprevention\b",
            @"\brisk of\b",
            @"\bcauses\b",
            @"\bleads to\b",
            @"\bresults in\b",
            @"\btake \d+\s*(mg|mcg|iu)\b",
            @"\bsupplement with\b",
            @"\bprescribe\b",
            @"\bmedication\b"
        };

        var allText = $"{def.WhatItMeasures} {def.WhyTested} {def.BelowContext} {def.AboveContext}".ToLower();

        foreach (var pattern in unsafePatterns)
        {
            if (Regex.IsMatch(allText, pattern, RegexOptions.IgnoreCase))
            {
                _logger.LogWarning("Found unsafe pattern: {Pattern} in definition for {Marker}", pattern, def.DisplayName);
                return false;
            }
        }

        return true;
    }

    private MarkerDefinition GetFallbackDefinition(string markerName)
    {
        return new MarkerDefinition
        {
            Key = markerName.ToLower().Replace(" ", "_").Replace("(", "").Replace(")", ""),
            DisplayName = markerName,
            WhatItMeasures = "This marker is commonly included in laboratory testing.",
            WhyTested = "Your clinician can explain why this test was ordered for you.",
            BelowContext = "Lower levels may be associated with various factors. Discuss with your clinician for personalized context.",
            AboveContext = "Higher levels may be associated with various factors. Discuss with your clinician for personalized context.",
            RelatedMarkers = Array.Empty<string>()
        };
    }

    private List<MarkerDefinition> GetDefaultMarkerDefinitions()
    {
        return new List<MarkerDefinition>
        {
            new MarkerDefinition
            {
                Key = "wbc",
                DisplayName = "White Blood Cells (WBC)",
                WhatItMeasures = "These cells help your body fight infections and heal from injuries.",
                WhyTested = "Commonly checked to understand immune system activity.",
                BelowContext = "Lower levels may reflect various factors. Your clinician can help determine what's appropriate for you.",
                AboveContext = "Higher levels may reflect various factors. Your clinician can help determine what's appropriate for you.",
                RelatedMarkers = new[] { "RBC", "Hemoglobin", "Hematocrit", "Platelets" }
            }
        };
    }

    public string GetMarkerStatus(decimal value, decimal? refLow, decimal? refHigh)
    {
        if (refLow.HasValue && value < refLow.Value)
            return "Low";
        if (refHigh.HasValue && value > refHigh.Value)
            return "High";
        return "Normal";
    }
}

public class MarkerWrapper
{
    public List<MarkerDefinition>? Markers { get; set; }
}

public class MarkerDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string WhatItMeasures { get; set; } = string.Empty;
    public string WhyTested { get; set; } = string.Empty;
    public string BelowContext { get; set; } = string.Empty;
    public string AboveContext { get; set; } = string.Empty;
    public string[] RelatedMarkers { get; set; } = Array.Empty<string>();
}

public class GeneratedMarkerContent
{
    public string WhatItMeasures { get; set; } = "";
    public string WhyTested { get; set; } = "";
    public string BelowContext { get; set; } = "";
    public string AboveContext { get; set; } = "";
    public List<string> RelatedMarkers { get; set; } = new();
}