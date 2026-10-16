using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using race_day.Data;
using race_day.Models;

namespace race_day.Controllers;

public record CreateEnrolmentDto(int CategoryId, string EmergencyContact, string? MedicalAidNumber);
// This DTO is used to receive enrolment data from the client.
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EnrolmentsController : ControllerBase
{// Inject the DbContext into the controller
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEnrolment([FromQuery] CreateEnrolmentDto dto) 
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var category = await _context.Categories.FindAsync(dto.CategoryId);
        if (category == null) return NotFound(new { message = "Category not found." });

        var alreadyEnrolled = await _context.EventEnrolments
            .AnyAsync(e => e.CategoryId == dto.CategoryId && e.ParticipantId == userId);

        if (alreadyEnrolled) return Conflict(new { message = "You are already enrolled in this category." });

        var newRaceNumber = await _context.EventEnrolments.CountAsync() + 1000;

        var enrolment = new EventEnrolment
        {
            CategoryId = dto.CategoryId,
            ParticipantId = userId,
            RaceNumber = newRaceNumber,
            EmergencyContact = dto.EmergencyContact,
            MedicalAidNumber = dto.MedicalAidNumber,
            PaymentStatus = "Pending"
        };

        _context.EventEnrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyEnrolments), new { id = enrolment.EnrolmentId }, enrolment);
    }

    [HttpGet("my-enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var enrolments = await _context.EventEnrolments
            .Include(e => e.Category)
                .ThenInclude(c => c!.Event)
            .Where(e => e.ParticipantId == userId)
            .ToListAsync();

        return Ok(enrolments);
    }
}