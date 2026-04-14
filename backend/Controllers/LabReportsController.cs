using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutrientInsight.Api.DTOs;
using NutrientInsight.Api.Services;
using System.Security.Claims;

namespace NutrientInsight.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LabReportsController : ControllerBase
{
    private readonly LabService _labService;

    public LabReportsController(LabService labService)
    {
        _labService = labService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReport([FromBody] CreateLabReportRequest request)
    {
        var userId = GetUserId();
        var report = await _labService.CreateLabReport(userId, request);
        return Ok(report);
    }

    [HttpGet]
    public async Task<IActionResult> GetReports()
    {
        var userId = GetUserId();
        var reports = await _labService.GetUserReports(userId);
        return Ok(reports);
    }

    [HttpGet("{reportId}")]
    public async Task<IActionResult> GetReport(Guid reportId)
    {
        var userId = GetUserId();
        var report = await _labService.GetReportById(userId, reportId);
        
        if (report == null)
            return NotFound();

        return Ok(report);
    }

    [HttpGet("{reportId}/focus-areas")]
    public async Task<IActionResult> GetFocusAreas(Guid reportId)
    {
        var userId = GetUserId();
        var report = await _labService.GetReportById(userId, reportId);
        
        if (report == null)
            return NotFound();

        var focusAreas = _labService.GetTopFocusAreas(report);
        return Ok(focusAreas);
    }

    [HttpDelete("{reportId}")]
    public async Task<IActionResult> DeleteReport(Guid reportId)
    {
        var userId = GetUserId();
        var success = await _labService.DeleteReport(userId, reportId);

        if (!success)
            return NotFound(new { error = "Report not found" });

        return Ok(new { message = "Report deleted successfully" });
    }

    [HttpGet("markers/all")]
    public async Task<IActionResult> GetAllMarkerNames()
    {
        var userId = GetUserId();
        var names = await _labService.GetAllMarkerNames(userId);
        return Ok(names);
    }

    [HttpGet("markers/{markerName}/trends")]
    public async Task<IActionResult> GetMarkerTrends(string markerName)
    {
        var userId = GetUserId();
        var trends = await _labService.GetMarkerTrends(userId, markerName);
        return Ok(trends);
    }
}