using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutrientInsight.Api.Services;
using System.Security.Claims;

namespace NutrientInsight.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportsController : ControllerBase
{
    private readonly LabService _labService;
    private readonly ExportService _exportService;
    private readonly ContentService _contentService;

    public ExportsController(LabService labService, ExportService exportService, ContentService contentService)
    {
        _labService = labService;
        _exportService = exportService;
        _contentService = contentService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }

    [HttpGet("excel/{reportId}")]
    public async Task<IActionResult> ExportExcel(Guid reportId)
    {
        var userId = GetUserId();
        var report = await _labService.GetReportById(userId, reportId);

        if (report == null)
            return NotFound(new { error = "Report not found" });

        var focusAreas = report.LabMarkers
            .Where(m => m.Status == "Low" || m.Status == "High")
            .Select(m => m.MarkerName)
            .ToList();

        var userReports = await _labService.GetUserReports(userId);
        var previousReport = userReports
            .Where(r => r.Id != report.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        var excelBytes = await _exportService.GenerateExcelReport(report, focusAreas, previousReport);
        var filename = _exportService.GetExportFilename(report, "xlsx");

        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
    }

    [HttpGet("pdf/{reportId}")]
    public async Task<IActionResult> ExportPdf(Guid reportId)
    {
        var userId = GetUserId();
        var report = await _labService.GetReportById(userId, reportId);

        if (report == null)
            return NotFound(new { error = "Report not found" });

        var focusAreas = report.LabMarkers
            .Where(m => m.Status == "Low" || m.Status == "High")
            .Select(m => m.MarkerName)
            .ToList();

        var userReports = await _labService.GetUserReports(userId);
        var previousReport = userReports
            .Where(r => r.Id != report.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        var htmlBytes = await _exportService.GeneratePdfReport(report, focusAreas, previousReport);
        var filename = _exportService.GetExportFilename(report, "html");

        return File(htmlBytes, "text/html", filename);
    }
}