using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutrientInsight.Api.Services;
using System.Security.Claims;

namespace NutrientInsight.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadsController : ControllerBase
{
    private readonly PdfParserService _pdfParserService;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(PdfParserService pdfParserService, ILogger<UploadsController> logger)
    {
        _pdfParserService = pdfParserService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }

    [HttpPost("parse-lab-report")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ParseLabReport(IFormFile file)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("Upload request received from user. File: {FileName}, Size: {Size} bytes", 
                file?.FileName ?? "null", file?.Length ?? 0);
            
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file uploaded or file is empty");
                return BadRequest(new { error = "No file uploaded" });
            }

            if (file.ContentType != "application/pdf")
            {
                _logger.LogWarning("Invalid content type: {ContentType}", file.ContentType);
                return BadRequest(new { error = "Only PDF files are supported" });
            }

            if (file.Length > 10 * 1024 * 1024)
            {
                _logger.LogWarning("File too large: {Size} bytes", file.Length);
                return BadRequest(new { error = "File size must be less than 10MB" });
            }

            _logger.LogInformation("Starting PDF parsing...");
            
            using var stream = file.OpenReadStream();
            var result = await _pdfParserService.ParseLabReport(stream);

            _logger.LogInformation(
                "Lab report parsed successfully. ReportDate: {Date}, Markers: {Count}", 
                result.ReportDate?.ToString("yyyy-MM-dd") ?? "null",
                result.Markers.Count
            );

            return Ok(new
            {
                reportDate = result.ReportDate?.ToString("yyyy-MM-dd"),
                markers = result.Markers.Select(m => new
                {
                    key = m.Key,
                    markerName = m.MarkerName,
                    value = m.Value,
                    referenceLow = m.ReferenceLow,
                    referenceHigh = m.ReferenceHigh
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse lab report: {Message}", ex.Message);
            return BadRequest(new { error = $"Failed to parse lab report: {ex.Message}" });
        }
    }
}