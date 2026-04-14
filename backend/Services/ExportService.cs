using ClosedXML.Excel;
using NutrientInsight.Api.Models;
using System.Text;

namespace NutrientInsight.Api.Services;

public class ExportService
{
    private readonly ContentService _contentService;

    public ExportService(ContentService contentService)
    {
        _contentService = contentService;
    }

    public async Task<byte[]> GenerateExcelReport(LabReport report, List<string> focusAreas, LabReport? previousReport)
    {
        using var workbook = new XLWorkbook();

        var totalMarkers = report.LabMarkers.Count;
        var outOfRange = report.LabMarkers.Count(m => m.Status == "Low" || m.Status == "High");
        var inRange = report.LabMarkers.Count(m => m.Status == "Normal");

        var summarySheet = workbook.Worksheets.Add("Summary");
        summarySheet.Cell("A1").Value = "VitalNexus Educational Wellness Report";
        summarySheet.Cell("A1").Style.Font.Bold = true;
        summarySheet.Cell("A1").Style.Font.FontSize = 16;
        
        summarySheet.Cell("A3").Value = "Report ID:";
        summarySheet.Cell("B3").Value = report.ReportPublicId;
        summarySheet.Cell("A4").Value = "Report Date:";
        summarySheet.Cell("B4").Value = report.ReportDate.ToShortDateString();

        var markersSheet = workbook.Worksheets.Add("Your Markers");
        markersSheet.Cell("A1").Value = "Marker";
        markersSheet.Cell("B1").Value = "Your Value";
        markersSheet.Cell("C1").Value = "Unit";
        markersSheet.Cell("D1").Value = "Lab Range";
        markersSheet.Cell("E1").Value = "Status";
        markersSheet.Range("A1:E1").Style.Font.Bold = true;

        int row = 2;
        foreach (var marker in report.LabMarkers)
        {
            var rangeText = marker.ReferenceLow.HasValue && marker.ReferenceHigh.HasValue
                ? $"{marker.ReferenceLow} - {marker.ReferenceHigh}"
                : "Not provided";

            markersSheet.Cell($"A{row}").Value = marker.MarkerName;
            markersSheet.Cell($"B{row}").Value = marker.Value;
            markersSheet.Cell($"C{row}").Value = marker.Unit;
            markersSheet.Cell($"D{row}").Value = rangeText;
            markersSheet.Cell($"E{row}").Value = marker.Status;
            row++;
        }
        markersSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Task.FromResult(stream.ToArray());
    }

    public async Task<byte[]> GeneratePdfReport(LabReport report, List<string> focusAreas, LabReport? previousReport)
    {
        var html = await GenerateHtmlReportAsync(report, focusAreas, previousReport);
        return Encoding.UTF8.GetBytes(html);
    }

    private async Task<string> GenerateHtmlReportAsync(LabReport report, List<string> focusAreas, LabReport? previousReport)
    {
        var groups = ExportUtilities.GroupMarkers(report.LabMarkers);
        var groupEducation = GroupRelationships.GetGroupEducation();
        
        var totalMarkers = report.LabMarkers.Count;
        var outOfRange = report.LabMarkers.Count(m => m.Status == "Low" || m.Status == "High");
        var inRange = report.LabMarkers.Count(m => m.Status == "Normal");
        
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head>");
        html.AppendLine("<meta charset='utf-8'>");
        html.AppendLine("<title>VitalNexus Educational Wellness Report</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: -apple-system, system-ui, sans-serif; max-width: 900px; margin: 40px auto; padding: 0 20px; line-height: 1.7; color: #1a1a1a; font-size: 16px; }");
        html.AppendLine("h1 { font-size: 28px; color: #2563eb; margin-bottom: 8px; }");
        html.AppendLine("h2 { font-size: 22px; color: #1e40af; margin-top: 48px; margin-bottom: 16px; border-bottom: 2px solid #dbeafe; padding-bottom: 8px; }");
        html.AppendLine("h3 { font-size: 18px; color: #374151; margin-top: 24px; margin-bottom: 12px; }");
        html.AppendLine(".header { margin-bottom: 32px; }");
        html.AppendLine(".meta { color: #6b7280; font-size: 14px; margin-top: 4px; }");
        html.AppendLine(".disclaimer { background: #fef3c7; border-left: 4px solid #f59e0b; padding: 16px; margin: 24px 0; border-radius: 4px; }");
        html.AppendLine(".marker-box { background: #f9fafb; padding: 24px; margin: 20px 0; border-left: 4px solid #2563eb; border-radius: 4px; }");
        html.AppendLine(".marker-box h3 { margin-top: 0; color: #1f2937; }");
        html.AppendLine(".marker-detail { margin: 12px 0; color: #374151; }");
        html.AppendLine(".marker-detail strong { color: #1f2937; }");
        html.AppendLine(".status-high { color: #dc2626; font-weight: 600; }");
        html.AppendLine(".status-low { color: #ea580c; font-weight: 600; }");
        html.AppendLine(".status-normal { color: #059669; font-weight: 600; }");
        html.AppendLine(".group-box { background: #eff6ff; padding: 24px; margin: 24px 0; border-radius: 4px; border-left: 4px solid #3b82f6; }");
        html.AppendLine("ul { margin: 12px 0; padding-left: 24px; }");
        html.AppendLine("ul li { margin: 8px 0; }");
        html.AppendLine(".pattern-summary { background: #f0fdf4; padding: 20px; margin: 20px 0; border-radius: 4px; border-left: 4px solid #10b981; }");
        html.AppendLine(".footer { margin-top: 60px; padding-top: 20px; border-top: 2px solid #e5e7eb; font-size: 13px; color: #6b7280; }");
        html.AppendLine(".ai-generated { background: #f0f9ff; border-left: 4px solid #0284c7; padding: 12px; margin: 12px 0; border-radius: 4px; font-size: 14px; }");
        html.AppendLine("</style>");
        html.AppendLine("</head><body>");

        // 1) Header
        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>VitalNexus Educational Wellness Report</h1>");
        html.AppendLine($"<div class='meta'><strong>Report ID:</strong> {report.ReportPublicId} | <strong>Report Date:</strong> {report.ReportDate.ToShortDateString()}</div>");
        if (report.PetProfile != null)
        {
            html.AppendLine($"<div class='meta'><strong>Subject:</strong> {report.PetProfile.Name} ({report.PetProfile.Species})</div>");
        }
        html.AppendLine("</div>");

        // 2) Educational Disclaimer
        html.AppendLine("<div class='disclaimer'>");
        html.AppendLine("<strong>📚 Educational Use Only</strong><br>");
        html.AppendLine("This report is for educational purposes only. It is not medical advice, diagnosis, or treatment. Always discuss your results with a licensed clinician.");
        html.AppendLine("</div>");

        // 3) Section A: Your Individual Marker Results
        html.AppendLine("<h2>Section A: Your Individual Marker Results</h2>");
        
        foreach (var marker in report.LabMarkers)
        {
            // AI-POWERED: Get or generate marker definition
            var def = await _contentService.GetOrGenerateMarkerDefinitionAsync(marker.MarkerName);

            var statusClass = marker.Status == "Low" ? "status-low" :
                             marker.Status == "High" ? "status-high" : "status-normal";

            var statusText = marker.Status == "Low" ? "Below Range" :
                            marker.Status == "High" ? "Above Range" : "Within Range";

            if (!marker.ReferenceLow.HasValue && !marker.ReferenceHigh.HasValue)
                statusText = "Range Not Provided";

            var rangeText = marker.ReferenceLow.HasValue && marker.ReferenceHigh.HasValue
                ? $"{marker.ReferenceLow} – {marker.ReferenceHigh}"
                : "Not provided";

            html.AppendLine("<div class='marker-box'>");
            html.AppendLine($"<h3>{def?.DisplayName ?? marker.MarkerName}</h3>");
            html.AppendLine($"<div class='marker-detail'><strong>Your Value:</strong> {marker.Value} {marker.Unit}</div>");
            html.AppendLine($"<div class='marker-detail'><strong>Lab Range:</strong> {rangeText}</div>");
            html.AppendLine($"<div class='marker-detail'><strong>Status:</strong> <span class='{statusClass}'>{statusText}</span></div>");

            if (def != null)
            {
                html.AppendLine($"<div class='marker-detail'><strong>What This Marker Measures (Plain English):</strong><br>{def.WhatItMeasures}</div>");
                html.AppendLine($"<div class='marker-detail'><strong>Why It Is Commonly Tested:</strong><br>{def.WhyTested}</div>");

                if (marker.Status == "Low" && !string.IsNullOrEmpty(def.BelowContext))
                {
                    html.AppendLine($"<div class='marker-detail'><strong>If Below Range (Educational Context):</strong><br>{def.BelowContext}</div>");
                }
                else if (marker.Status == "High" && !string.IsNullOrEmpty(def.AboveContext))
                {
                    html.AppendLine($"<div class='marker-detail'><strong>If Above Range (Educational Context):</strong><br>{def.AboveContext}</div>");
                }

                if (def.RelatedMarkers.Length > 0)
                {
                    var presentRelated = def.RelatedMarkers
                        .Where(rm => report.LabMarkers.Any(m => m.MarkerName.Contains(rm, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                    
                    if (presentRelated.Any())
                    {
                        html.AppendLine($"<div class='marker-detail'><strong>Markers Often Reviewed With:</strong> {string.Join(", ", presentRelated)}</div>");
                    }
                }

                // Add disclaimer for AI-generated content
                html.AppendLine("<div class='ai-generated'>");
                html.AppendLine("📚 <strong>Educational Use Only.</strong> This is not medical advice. Discuss all results with a licensed clinician.");
                html.AppendLine("</div>");
            }

            html.AppendLine("</div>");
        }

        // 4) Section B: How These Markers Relate to Each Other
        var relevantGroups = groups.Where(g => g.Value.Count >= 2 && groupEducation.ContainsKey(g.Key)).ToList();
        
        if (relevantGroups.Any())
        {
            html.AppendLine("<h2>Section B: How These Markers Relate to Each Other</h2>");

            foreach (var group in relevantGroups)
            {
                var edu = groupEducation[group.Key];
                
                html.AppendLine("<div class='group-box'>");
                html.AppendLine($"<h3>{group.Key}</h3>");
                
                html.AppendLine("<p><strong>Markers Present:</strong></p>");
                html.AppendLine("<ul>");
                foreach (var m in group.Value)
                {
                    var statusClass = m.Status == "Low" ? "status-low" :
                                     m.Status == "High" ? "status-high" : "status-normal";
                    html.AppendLine($"<li>{m.MarkerName}: <span class='{statusClass}'>{m.Status}</span></li>");
                }
                html.AppendLine("</ul>");

                html.AppendLine($"<p><strong>Why They Are Reviewed Together:</strong><br>{edu.WhyReviewedTogether}</p>");

                var anyOutOfRange = group.Value.Any(m => m.Status != "Normal");
                if (anyOutOfRange)
                {
                    html.AppendLine("<p><strong>What This Pattern May Reflect (Educational Context Only):</strong><br>");
                    html.AppendLine("This combination is sometimes discussed in relation to lifestyle patterns, dietary habits, and general wellness. ");
                    html.AppendLine("Your clinician can provide personalized context based on your individual situation.</p>");
                }

                html.AppendLine("</div>");
            }
        }

        // 5) Section C: What This Overall Pattern May Suggest
        html.AppendLine("<h2>Section C: What This Overall Pattern May Suggest</h2>");
        html.AppendLine("<div class='pattern-summary'>");
        html.AppendLine("<ul>");
        
        if (inRange > outOfRange)
        {
            html.AppendLine("<li>Most markers are within lab reference ranges.</li>");
        }
        
        if (outOfRange > 0)
        {
            html.AppendLine($"<li>{outOfRange} marker(s) fall outside the range and may be worth discussing with your clinician.</li>");
        }

        if (relevantGroups.Any())
        {
            html.AppendLine("<li>Some related markers appear together, which can provide additional context when reviewed by a clinician.</li>");
        }

        html.AppendLine("<li>Lifestyle factors such as diet, exercise, sleep, hydration, and stress can influence many lab markers.</li>");
        html.AppendLine("<li>Lab values can vary based on testing conditions, time of day, and individual circumstances.</li>");
        html.AppendLine("</ul>");
        html.AppendLine("</div>");

        // 6) Section D: General Wellness Strategies Often Discussed
        html.AppendLine("<h2>Section D: General Wellness Strategies Often Discussed</h2>");
        html.AppendLine("<p style='color:#6b7280; margin-bottom:20px;'>These are general patterns often discussed in wellness contexts. Not personalized recommendations.</p>");

        html.AppendLine("<h3>Nutrition Patterns Often Discussed</h3>");
        html.AppendLine("<ul>");
        html.AppendLine("<li>Whole food patterns with variety</li>");
        html.AppendLine("<li>Fiber-rich vegetables and fruits</li>");
        html.AppendLine("<li>Adequate protein from various sources</li>");
        html.AppendLine("<li>Healthy fats from nuts, seeds, fish, and olive oil</li>");
        html.AppendLine("<li>Adequate hydration throughout the day</li>");
        html.AppendLine("</ul>");

        html.AppendLine("<h3>Movement Patterns Often Discussed</h3>");
        html.AppendLine("<ul>");
        html.AppendLine("<li>Regular aerobic activity most days</li>");
        html.AppendLine("<li>Resistance training for muscle and bone health</li>");
        html.AppendLine("<li>Movement throughout the day, not just structured exercise</li>");
        html.AppendLine("<li>Activities that are enjoyable and sustainable</li>");
        html.AppendLine("</ul>");

        html.AppendLine("<h3>Sleep & Recovery</h3>");
        html.AppendLine("<ul>");
        html.AppendLine("<li>Consistent sleep schedule</li>");
        html.AppendLine("<li>Adequate sleep duration for recovery</li>");
        html.AppendLine("<li>Sleep environment optimization</li>");
        html.AppendLine("</ul>");

        html.AppendLine("<h3>Stress & Consistency</h3>");
        html.AppendLine("<ul>");
        html.AppendLine("<li>Stress management practices</li>");
        html.AppendLine("<li>Social connection and support</li>");
        html.AppendLine("<li>Small, sustainable changes over time</li>");
        html.AppendLine("<li>Regular follow-up and monitoring</li>");
        html.AppendLine("</ul>");

        // 7) Section E: Questions to Ask Your Clinician
        html.AppendLine("<h2>Section E: Questions to Ask Your Clinician</h2>");
        html.AppendLine("<ul>");
        html.AppendLine("<li>Which of these markers are most important to focus on in my situation?</li>");
        html.AppendLine("<li>Should any of these markers be rechecked? If so, when?</li>");
        html.AppendLine("<li>Could factors like hydration, fasting status, or time of day have influenced these results?</li>");
        html.AppendLine("<li>Are there specific lifestyle changes that might be helpful for me to consider?</li>");
        html.AppendLine("<li>How do these results compare to my previous tests?</li>");
        html.AppendLine("<li>Are there any additional tests that might provide useful context?</li>");
        html.AppendLine("<li>What should I monitor between now and my next checkup?</li>");
        html.AppendLine("</ul>");

        // 8) Footer
        html.AppendLine("<div class='footer'>");
        html.AppendLine("<p><strong>Generated by VitalNexus — Educational Wellness Platform</strong></p>");
        html.AppendLine("<p>Lab values can vary based on hydration, fasting status, time of day, recent activity, and laboratory methodology.</p>");
        html.AppendLine("<p>This report does not diagnose, treat, or provide medical advice. Always consult with qualified healthcare professionals.</p>");
        html.AppendLine($"<p>Report generated: {DateTime.Now:MMMM dd, yyyy}</p>");
        html.AppendLine("</div>");

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    public string GetExportFilename(LabReport report, string extension)
    {
        return $"WellnessReport_{DateTime.Now:yyyy-MM-dd}.{extension}";
    }
}