using NutrientInsight.Api.Models;

namespace NutrientInsight.Api.Services;

public static class ExportUtilities
{
    public static string ClassifyStatus(decimal value, decimal? refLow, decimal? refHigh)
    {
        if (!refLow.HasValue && !refHigh.HasValue)
            return "Unknown";
        
        if (refLow.HasValue && value < refLow.Value)
            return "Low";
        
        if (refHigh.HasValue && value > refHigh.Value)
            return "High";
        
        return "In Range";
    }

    public static string RangePosition(decimal value, decimal? refLow, decimal? refHigh)
    {
        if (!refLow.HasValue || !refHigh.HasValue)
            return "Unknown";

        if (value < refLow.Value)
            return "Outside (Below)";
        
        if (value > refHigh.Value)
            return "Outside (Above)";

        var range = refHigh.Value - refLow.Value;
        var position = value - refLow.Value;
        var ratio = position / range;

        if (ratio <= 0.33m)
            return "Lower";
        else if (ratio >= 0.67m)
            return "Upper";
        else
            return "Mid";
    }

    public static Dictionary<string, List<LabMarker>> GroupMarkers(ICollection<LabMarker> markers)
    {
        var groups = new Dictionary<string, List<LabMarker>>();

        var lipidMarkers = new[] { "Total Cholesterol", "LDL", "HDL", "Triglycerides", "VLDL" };
        var glucoseMarkers = new[] { "Glucose", "Hemoglobin A1c", "A1C", "Fasting Glucose", "Insulin" };
        var thyroidMarkers = new[] { "TSH", "Free T4", "Free T3", "T4", "T3" };
        var electrolyteMarkers = new[] { "Sodium", "Potassium", "Chloride", "CO2", "Carbon Dioxide" };
        var liverMarkers = new[] { "ALT", "AST", "ALP", "Bilirubin", "Alkaline Phosphatase" };
        var cbcMarkers = new[] { "WBC", "RBC", "Hemoglobin", "Hematocrit", "Platelets", "White Blood", "Red Blood" };

        groups["Lipid Panel"] = markers.Where(m => lipidMarkers.Any(lm => m.MarkerName.Contains(lm, StringComparison.OrdinalIgnoreCase))).ToList();
        groups["Glucose Markers"] = markers.Where(m => glucoseMarkers.Any(gm => m.MarkerName.Contains(gm, StringComparison.OrdinalIgnoreCase))).ToList();
        groups["Thyroid"] = markers.Where(m => thyroidMarkers.Any(tm => m.MarkerName.Contains(tm, StringComparison.OrdinalIgnoreCase))).ToList();
        groups["Electrolytes"] = markers.Where(m => electrolyteMarkers.Any(em => m.MarkerName.Contains(em, StringComparison.OrdinalIgnoreCase))).ToList();
        groups["Liver"] = markers.Where(m => liverMarkers.Any(lm => m.MarkerName.Contains(lm, StringComparison.OrdinalIgnoreCase))).ToList();
        groups["Blood Counts"] = markers.Where(m => cbcMarkers.Any(cm => m.MarkerName.Contains(cm, StringComparison.OrdinalIgnoreCase))).ToList();

        return groups.Where(g => g.Value.Any()).ToDictionary(g => g.Key, g => g.Value);
    }

    public static List<MarkerTrend> ComputeTrends(LabReport currentReport, LabReport? previousReport)
    {
        if (previousReport == null)
            return new List<MarkerTrend>();

        var trends = new List<MarkerTrend>();

        foreach (var current in currentReport.LabMarkers)
        {
            var previous = previousReport.LabMarkers.FirstOrDefault(m => 
                m.MarkerName.Equals(current.MarkerName, StringComparison.OrdinalIgnoreCase));

            if (previous != null)
            {
                var delta = current.Value - previous.Value;
                var direction = delta > 0 ? "↑" : delta < 0 ? "↓" : "→";

                trends.Add(new MarkerTrend
                {
                    MarkerName = current.MarkerName,
                    PreviousValue = previous.Value,
                    CurrentValue = current.Value,
                    Delta = delta,
                    Direction = direction,
                    Unit = current.Unit
                });
            }
        }

        return trends;
    }
}

public class MarkerTrend
{
    public string MarkerName { get; set; } = string.Empty;
    public decimal PreviousValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal Delta { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}