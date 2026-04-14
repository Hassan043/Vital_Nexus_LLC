using Microsoft.EntityFrameworkCore;
using NutrientInsight.Api.Data;
using NutrientInsight.Api.Models;
using NutrientInsight.Api.DTOs;
using System.Text;

namespace NutrientInsight.Api.Services;

public class LabService
{
    private readonly AppDbContext _context;

    public LabService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LabReport> CreateLabReport(Guid userId, CreateLabReportRequest request)
    {
        var report = new LabReport
        {
            Id = Guid.NewGuid(),
            ReportPublicId = GenerateReportPublicId(),
            UserId = userId,
            PetProfileId = request.PetProfileId,
            ReportDate = request.ReportDate,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var markerDto in request.Markers)
        {
            var marker = new LabMarker
            {
                Id = Guid.NewGuid(),
                LabReportId = report.Id,
                MarkerName = markerDto.MarkerName.Trim(),
                Value = markerDto.Value,
                Unit = markerDto.Unit,
                ReferenceLow = markerDto.ReferenceLow,
                ReferenceHigh = markerDto.ReferenceHigh,
                TestDate = markerDto.TestDate,
                Status = CalculateStatus(markerDto.Value, markerDto.ReferenceLow, markerDto.ReferenceHigh)
            };
            report.LabMarkers.Add(marker);
        }

        Console.WriteLine($"🔍 About to save {report.LabMarkers.Count} markers:");
        foreach (var m in report.LabMarkers)
        {
            Console.WriteLine($"  ✓ {m.MarkerName}: {m.Value} {m.Unit}");
        }

        _context.LabReports.Add(report);
        await _context.SaveChangesAsync();

        Console.WriteLine($"✅ Successfully saved report with {report.LabMarkers.Count} markers");

        return report;
    }

    private string GenerateReportPublicId()
    {
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomSuffix = GenerateBase36String(6);
        return $"VN-{datePrefix}-{randomSuffix}";
    }

    private string GenerateBase36String(int length)
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var random = new Random();
        var result = new StringBuilder(length);
        
        for (int i = 0; i < length; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }
        
        return result.ToString();
    }

    public string CalculateStatus(decimal value, decimal? referenceLow, decimal? referenceHigh)
    {
        if (!referenceLow.HasValue || !referenceHigh.HasValue)
            return "Unknown";

        if (value < referenceLow.Value)
            return "Low";
        if (value > referenceHigh.Value)
            return "High";
        return "Normal";
    }

    public async Task<List<LabReport>> GetUserReports(Guid userId)
    {
        return await _context.LabReports
            .Include(r => r.LabMarkers)
            .Include(r => r.PetProfile)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReportDate)
            .ToListAsync();
    }

    public async Task<LabReport?> GetReportById(Guid userId, Guid reportId)
    {
        return await _context.LabReports
            .Include(r => r.LabMarkers)
            .Include(r => r.PetProfile)
            .FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == userId);
    }

    public List<string> GetTopFocusAreas(LabReport report)
    {
        var focusAreas = new List<string>();
        
        foreach (var marker in report.LabMarkers.Where(m => m.Status == "Low" || m.Status == "High"))
        {
            focusAreas.Add($"{marker.MarkerName} ({marker.Status})");
        }

        if (focusAreas.Count == 0)
        {
            focusAreas.Add("General Wellness");
        }

        return focusAreas.Take(3).ToList();
    }

    public async Task<bool> DeleteReport(Guid userId, Guid reportId)
    {
        var report = await _context.LabReports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == userId);

        if (report == null)
            return false;

        _context.LabReports.Remove(report);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<MarkerTrendPoint>> GetMarkerTrends(Guid userId, string markerName)
    {
        var reports = await _context.LabReports
            .Include(r => r.LabMarkers)
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.ReportDate)
            .ToListAsync();

        return reports
            .SelectMany(r => r.LabMarkers
                .Where(m => m.MarkerName.ToLower() == markerName.ToLower())
                .Select(m => new MarkerTrendPoint
                {
                    ReportDate = r.ReportDate,
                    Value = m.Value,
                    Status = m.Status,
                    ReferenceLow = m.ReferenceLow,
                    ReferenceHigh = m.ReferenceHigh,
                    Unit = m.Unit
                }))
            .ToList();
    }

    public async Task<List<string>> GetAllMarkerNames(Guid userId)
    {
        return await _context.LabReports
            .Include(r => r.LabMarkers)
            .Where(r => r.UserId == userId)
            .SelectMany(r => r.LabMarkers.Select(m => m.MarkerName))
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();
    }
}