namespace NutrientInsight.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public ICollection<LabReport> LabReports { get; set; } = new List<LabReport>();
    public ICollection<PetProfile> PetProfiles { get; set; } = new List<PetProfile>();
    public BodyTendencyProfile? BodyTendencyProfile { get; set; }
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}

public class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public User User { get; set; } = null!;
}

public class PetProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public int? Age { get; set; }
    public decimal? Weight { get; set; }
    
    public User User { get; set; } = null!;
    public ICollection<LabReport> LabReports { get; set; } = new List<LabReport>();
}

public class LabReport
{
    public Guid Id { get; set; }
    public string ReportPublicId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid? PetProfileId { get; set; }
    public DateTime ReportDate { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public User User { get; set; } = null!;
    public PetProfile? PetProfile { get; set; }
    public ICollection<LabMarker> LabMarkers { get; set; } = new List<LabMarker>();
}

public class LabMarker
{
    public Guid Id { get; set; }
    public Guid LabReportId { get; set; }
    public string MarkerName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? ReferenceLow { get; set; }
    public decimal? ReferenceHigh { get; set; }
    public string Status { get; set; } = "Unknown";
    public DateTime TestDate { get; set; }
    
    public LabReport LabReport { get; set; } = null!;
}

public class BodyTendencyProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public decimal? Waist { get; set; }
    public int GainFat { get; set; }
    public int BuildMuscle { get; set; }
    public int NaturallyLean { get; set; }
    public string ActivityLevel { get; set; } = string.Empty;
    public string Tendency { get; set; } = string.Empty;
    
    public User User { get; set; } = null!;
}

public class ExercisePlan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Goal { get; set; } = string.Empty;
    public string FitnessLevel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string PlanJson { get; set; } = string.Empty;
}