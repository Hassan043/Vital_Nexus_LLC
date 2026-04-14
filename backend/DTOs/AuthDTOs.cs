namespace NutrientInsight.Api.DTOs;

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Email, Guid UserId);

public record LabMarkerDto(
    string MarkerName,
    decimal Value,
    string Unit,
    decimal? ReferenceLow,
    decimal? ReferenceHigh,
    DateTime TestDate
);

public record CreateLabReportRequest(
    Guid? PetProfileId,
    DateTime ReportDate,
    List<LabMarkerDto> Markers
);

public record CreatePetProfileRequest(
    string Name,
    string Species,
    string? Breed,
    int? Age,
    decimal? Weight
);

public record BodyTendencyRequest(
    decimal Height,
    decimal Weight,
    decimal? Waist,
    int GainFat,
    int BuildMuscle,
    int NaturallyLean,
    string ActivityLevel
);

public record ExercisePlanRequest(
    string Goal,
    string FitnessLevel
);
