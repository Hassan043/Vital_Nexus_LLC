using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace NutrientInsight.Api.Services;

public class PdfParserService
{
    private readonly ILogger<PdfParserService> _logger;

    public PdfParserService(ILogger<PdfParserService> logger)
    {
        _logger = logger;
    }

    public async Task<ParsedLabReport> ParseLabReport(Stream pdfStream)
    {
        var result = new ParsedLabReport { Markers = new List<ParsedMarker>() };

        try
        {
            string text = ExtractTextFromPdf(pdfStream);
            result.ReportDate = ExtractReportDate(text);
            result.Markers = ParseMarkers(text);
            
            _logger.LogInformation("Extracted {Count} markers", result.Markers.Count);
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PDF parsing");
            throw new Exception("Failed to parse lab report. Please enter values manually.");
        }
    }

    private string ExtractTextFromPdf(Stream pdfStream)
    {
        using var document = PdfDocument.Open(pdfStream);
        var text = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            text.AppendLine(page.Text);
        }
        return text.ToString();
    }

    private DateTime? ExtractReportDate(string text)
    {
        var match = Regex.Match(text, @"Date Collected:\s*(\d{2}/\d{2}/\d{4})");
        if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var date))
            return date;
        return null;
    }

    private List<ParsedMarker> ParseMarkers(string text)
    {
        var markers = new List<ParsedMarker>();

        // Pattern: MarkerName 01VALUE UNIT REF_LOW-REF_HIGH (with minimal/no spaces)
        var patterns = new[]
        {
            (@"WBC\s*01(\d+\.?\d*)\s*x10E3/uL(\d+\.?\d*)-(\d+\.?\d*)", "wbc", "WBC"),
            (@"RBC\s*01(\d+\.?\d*)\s*x10E6/uL(\d+\.?\d*)-(\d+\.?\d*)", "rbc", "RBC"),
            (@"Hemoglobin\s*01(\d+\.?\d*)\s*g/dL(\d+\.?\d*)-(\d+\.?\d*)", "hemoglobin", "Hemoglobin"),
            (@"Hematocrit\s*01(\d+\.?\d*)\s*%(\d+\.?\d*)-(\d+\.?\d*)", "hematocrit", "Hematocrit"),
            (@"Platelets\s*01(\d+)\s*x10E3/uL(\d+)-(\d+)", "platelets", "Platelets"),
            (@"Glucose\s*01(\d+)\s*mg/dL(\d+)-(\d+)", "glucose_fasting", "Glucose"),
            (@"BUN\s*01(\d+)\s*mg/dL(\d+)-(\d+)", "bun", "BUN"),
            (@"Creatinine\s*01(\d+\.?\d*)\s*mg/dL(\d+\.?\d*)-(\d+\.?\d*)", "creatinine", "Creatinine"),
            (@"Sodium\s*01(\d+)\s*mmol/L(\d+)-(\d+)", "sodium", "Sodium"),
            (@"Potassium\s*01(\d+\.?\d*)\s*(?:High)?mmol/L(\d+\.?\d*)-(\d+\.?\d*)", "potassium", "Potassium"),
            (@"Chloride\s*01(\d+)\s*mmol/L(\d+)-(\d+)", "chloride", "Chloride"),
            (@"Carbon Dioxide, Total\s*01(\d+)\s*mmol/L(\d+)-(\d+)", "co2", "CO2 (Bicarbonate)"),
            (@"Calcium\s*01(\d+\.?\d*)\s*mg/dL(\d+\.?\d*)-(\d+\.?\d*)", "calcium", "Calcium"),
            (@"Protein, Total\s*01(\d+\.?\d*)\s*g/dL(\d+\.?\d*)-(\d+\.?\d*)", "total_protein", "Total Protein"),
            (@"Albumin\s*01(\d+\.?\d*)\s*g/dL(\d+\.?\d*)-(\d+\.?\d*)", "albumin", "Albumin"),
            (@"Globulin, Total(\d+\.?\d*)\s*g/dL(\d+\.?\d*)-(\d+\.?\d*)", "globulin", "Globulin"),
            (@"Bilirubin, Total\s*01(\d+\.?\d*)\s*mg/dL(\d+\.?\d*)-(\d+\.?\d*)", "bilirubin_total", "Bilirubin (Total)"),
            (@"Alkaline Phosphatase\s*01(\d+)\s*IU/L(\d+)-(\d+)", "alp", "Alkaline Phosphatase"),
            (@"AST \(SGOT\)\s*01(\d+)\s*IU/L(\d+)-(\d+)", "ast", "AST (Aspartate Aminotransferase)"),
            (@"ALT \(SGPT\)\s*01(\d+)\s*IU/L(\d+)-(\d+)", "alt", "ALT (Alanine Aminotransferase)"),
            (@"Cholesterol, Total\s*01(\d+)\s*(?:High)?mg/dL(\d+)-(\d+)", "total_cholesterol", "Total Cholesterol"),
            (@"Triglycerides\s*01(\d+)\s*mg/dL(\d+)-(\d+)", "triglycerides", "Triglycerides"),
            (@"HDL Cholesterol\s*01(\d+)\s*mg/dL>(\d+)", "hdl", "HDL Cholesterol"),
            (@"LDL Chol Calc[^\d]*(\d+)\s*mg/dL(\d+)-(\d+)", "ldl", "LDL Cholesterol"),
            (@"Hemoglobin A1c\s*01(\d+\.?\d*)\s*%(\d+\.?\d*)-(\d+\.?\d*)", "hemoglobin_a1c", "Hemoglobin A1c"),
            (@"Testosterone\s*01(\d+)\s*ng/dL(\d+)-(\d+)", "testosterone_total", "Testosterone"),
            (@"TSH\s*01(\d+\.?\d*)\s*uIU/mL(\d+\.?\d*)-(\d+\.?\d*)", "tsh", "TSH"),
            (@"Vitamin D, 25-Hydroxy\s*01(\d+\.?\d*)\s*(?:Low)?ng/mL(\d+\.?\d*)-(\d+\.?\d*)", "vitamin_d", "Vitamin D (25-Hydroxyvitamin D)"),
            (@"Magnesium\s*01(\d+\.?\d*)\s*mg/dL(\d+\.?\d*)-(\d+\.?\d*)", "magnesium", "Magnesium")
        };

        foreach (var (pattern, key, displayName) in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var value = decimal.Parse(match.Groups[1].Value);
                decimal? refLow = null;
                decimal? refHigh = null;

                if (match.Groups.Count > 2 && decimal.TryParse(match.Groups[2].Value, out var low))
                    refLow = low;
                if (match.Groups.Count > 3 && decimal.TryParse(match.Groups[3].Value, out var high))
                    refHigh = high;

                var marker = new ParsedMarker
                {
                    Key = key.Trim(),
                    MarkerName = displayName.Trim(),
                    Value = value,
                    ReferenceLow = refLow,
                    ReferenceHigh = refHigh
                };

                markers.Add(marker);
                _logger.LogInformation("Extracted {Name}: {Value} (ref: {Low}-{High})", 
                    displayName, value, refLow, refHigh);
            }
        }

        return markers;
    }
}

public class ParsedLabReport
{
    public DateTime? ReportDate { get; set; }
    public List<ParsedMarker> Markers { get; set; } = new();
}

public class ParsedMarker
{
    public string Key { get; set; } = string.Empty;
    public string MarkerName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? ReferenceLow { get; set; }
    public decimal? ReferenceHigh { get; set; }
}