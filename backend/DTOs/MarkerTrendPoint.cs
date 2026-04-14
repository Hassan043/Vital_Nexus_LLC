namespace NutrientInsight.Api.DTOs;

public class MarkerTrendPoint
{
    public DateTime ReportDate { get; set; }
    public decimal Value { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? ReferenceLow { get; set; }
    public decimal? ReferenceHigh { get; set; }
    public string Unit { get; set; } = string.Empty;
}