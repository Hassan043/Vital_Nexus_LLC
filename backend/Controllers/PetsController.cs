using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutrientInsight.Api.Data;
using NutrientInsight.Api.DTOs;
using NutrientInsight.Api.Models;
using System.Security.Claims;

namespace NutrientInsight.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PetsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PetsController(AppDbContext context)
    {
        _context = context;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePet([FromBody] CreatePetProfileRequest request)
    {
        var userId = GetUserId();
        
        var pet = new PetProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Species = request.Species,
            Breed = request.Breed,
            Age = request.Age,
            Weight = request.Weight
        };

        _context.PetProfiles.Add(pet);
        await _context.SaveChangesAsync();

        return Ok(pet);
    }

    [HttpGet]
    public async Task<IActionResult> GetPets()
    {
        var userId = GetUserId();
        var pets = await _context.PetProfiles
            .Where(p => p.UserId == userId)
            .ToListAsync();

        return Ok(pets);
    }
}
