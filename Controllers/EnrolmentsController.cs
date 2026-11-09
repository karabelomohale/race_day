using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using race_day.Data;
using race_day.Models;

namespace race_day.Controllers;

// Added EventId so the API knows which event the participant is enrolling in when CategoryId is null
public record CreateEnrolmentDto(int? EventId, int? CategoryId, string EmergencyContact, string? MedicalAidNumber);

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateEnrolment([FromBody] CreateEnrolmentDto dto)
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        if (roleClaim != "Participant") return StatusCode(403, "Access Denied. Only Participants can enrol.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("nameid");
        if (userIdClaim == null) return Unauthorized("Token is missing the User ID.");

        var userId = int.Parse(userIdClaim);

        // Automatically clean up any broken NULL-category rows in the database so UQ_Category_Participant never crashes
        var brokenRows = await _context.EventEnrolments.Where(e => e.CategoryId == null).ToListAsync();
        if (brokenRows.Any())
        {
            _context.EventEnrolments.RemoveRange(brokenRows);
            await _context.SaveChangesAsync();
        }

        int? resolvedCategoryId = (dto.CategoryId.HasValue && dto.CategoryId.Value > 0) ? dto.CategoryId.Value : null;

        // If the frontend didn't send a CategoryId, find or auto-create a "General Entry" category for this EventId
        if (!resolvedCategoryId.HasValue && dto.EventId.HasValue && dto.EventId.Value > 0)
        {
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.EventId == dto.EventId.Value);

            if (existingCategory != null)
            {
                resolvedCategoryId = existingCategory.CategoryId;
            }
            else
            {
                var newDefaultCategory = new Category
                {
                    EventId = dto.EventId.Value,
                    CategoryName = "General Entry",
                    DistanceKm = 5,
                    EntryFee = 0,
                    MaxParticipants = 1000,
                    CutoffTime = TimeSpan.FromHours(12)
                };
                _context.Categories.Add(newDefaultCategory);
                await _context.SaveChangesAsync();
                resolvedCategoryId = newDefaultCategory.CategoryId;
            }
        }

        if (!resolvedCategoryId.HasValue)
        {
            return BadRequest(new { message = "Could not resolve an event category for this enrolment." });
        }

        var category = await _context.Categories.FindAsync(resolvedCategoryId.Value);
        if (category == null) return NotFound(new { message = "Category not found." });

        // Check if participant is already enrolled in this event/category
        var alreadyEnrolled = await _context.EventEnrolments
            .Include(e => e.Category)
            .AnyAsync(e => e.ParticipantId == userId &&
                          (e.CategoryId == resolvedCategoryId.Value || (dto.EventId.HasValue && e.Category != null && e.Category.EventId == dto.EventId.Value)));

        if (alreadyEnrolled) return Conflict(new { message = "You are already enrolled in this event." });

        var newRaceNumber = await _context.EventEnrolments.CountAsync() + 1000;

        var enrolment = new EventEnrolment
        {
            CategoryId = resolvedCategoryId.Value, // Always saves a valid CategoryId linked to the Event!
            ParticipantId = userId,
            RaceNumber = newRaceNumber,
            EmergencyContact = dto.EmergencyContact,
            MedicalAidNumber = dto.MedicalAidNumber,
            PaymentStatus = "Pending"
        };

        _context.EventEnrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return Ok(enrolment);
    }

    [HttpGet("my-enrolments")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        if (roleClaim != "Participant") return StatusCode(403, "Access Denied.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("nameid");
        if (userIdClaim == null) return Unauthorized();

        var userId = int.Parse(userIdClaim);

        // Automatically remove any orphaned NULL-category rows if they still exist
        var brokenRows = await _context.EventEnrolments
            .Where(e => e.ParticipantId == userId && e.CategoryId == null)
            .ToListAsync();

        if (brokenRows.Any())
        {
            _context.EventEnrolments.RemoveRange(brokenRows);
            await _context.SaveChangesAsync();
        }

        var enrolments = await _context.EventEnrolments
            .Include(e => e.Category)
                .ThenInclude(c => c!.Event)
            .Where(e => e.ParticipantId == userId && e.Category != null && e.Category.Event != null)
            .Select(e => new
            {
                Id = e.EnrolmentId,
                Title = e.Category!.Event!.Title,
                EventDate = e.Category.Event.EventDate,
                CategoryName = e.Category.CategoryName,
                Status = e.PaymentStatus
            })
            .ToListAsync();

        return Ok(enrolments);
    }
}