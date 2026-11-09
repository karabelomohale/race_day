using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using race_day.Data;
using race_day.Models;

namespace race_day.Controllers;

public record CreateEventDto(string Title, string EventType, DateTime EventDate, TimeSpan StartTime, string Location, string Province, string? Description, string? ImageUrl);
public record UpdateEventDto(string Title, DateTime EventDate, string Location, string? Description, string Status, string? ImageUrl);
public record CreateCategoryDto(string CategoryName, decimal DistanceKm, decimal EntryFee, int MaxParticipants, TimeSpan CutoffTime);

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllEvents([FromQuery] string? province, [FromQuery] string? eventType)
    {
        var query = _context.Events
            .Include(e => e.Categories)
            .AsQueryable();

        if (!string.IsNullOrEmpty(province)) query = query.Where(e => e.Province == province);
        if (!string.IsNullOrEmpty(eventType)) query = query.Where(e => e.EventType == eventType);

        var events = await query.ToListAsync();

        // Count enrolled participants per EventId via the Category relationship
        var enrolmentCounts = await _context.EventEnrolments
            .Where(ee => ee.Category != null)
            .GroupBy(ee => ee.Category!.EventId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        var result = events.Select(e => new {
            e.EventId,
            e.OrganiserId,
            e.Title,
            e.EventType,
            e.EventDate,
            e.StartTime,
            e.Location,
            e.Province,
            e.Description,
            e.Status,
            e.CreatedAt,
            e.ImageUrl,
            ParticipantCount = enrolmentCounts.ContainsKey(e.EventId) ? enrolmentCounts[e.EventId] : 0,
            e.Categories
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventById(int id)
    {
        var ev = await _context.Events
            .Include(e => e.Categories)
            .Include(e => e.Routes)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null) return NotFound(new { message = "Event not found." });

        // Auto-create a default category for older events (like "yeh yeh yeh") if none exists yet
        if (ev.Categories == null || !ev.Categories.Any())
        {
            var fallbackCategory = new Category
            {
                EventId = ev.EventId,
                CategoryName = "General Entry",
                DistanceKm = 5,
                EntryFee = 0,
                MaxParticipants = 1000,
                CutoffTime = TimeSpan.FromHours(12)
            };
            _context.Categories.Add(fallbackCategory);
            await _context.SaveChangesAsync();
            ev.Categories = new List<Category> { fallbackCategory };
        }

        // Count enrolled participants for this specific event via Category.EventId
        var participantCount = await _context.EventEnrolments
            .CountAsync(ee => ee.Category != null && ee.Category.EventId == id);

        var result = new
        {
            ev.EventId,
            ev.OrganiserId,
            ev.Title,
            ev.EventType,
            ev.EventDate,
            ev.StartTime,
            ev.Location,
            ev.Province,
            ev.Description,
            ev.Status,
            ev.CreatedAt,
            ev.ImageUrl,
            ParticipantCount = participantCount,
            ev.Categories,
            ev.Routes
        };

        return Ok(result);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        if (roleClaim != "Organiser") return StatusCode(403, "Access Denied. Only Organisers can create events.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("nameid");
        if (userIdClaim == null) return Unauthorized("Token is missing the User ID. Please log out and log in again.");

        var ev = new Event
        {
            OrganiserId = int.Parse(userIdClaim),
            Title = dto.Title,
            EventType = dto.EventType,
            EventDate = dto.EventDate,
            StartTime = dto.StartTime,
            Location = dto.Location,
            Province = dto.Province,
            Description = dto.Description,
            Status = "Upcoming",
            ImageUrl = dto.ImageUrl
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        // Automatically create a default category so every new event is immediately ready for enrolment
        var defaultCategory = new Category
        {
            EventId = ev.EventId,
            CategoryName = "General Entry",
            DistanceKm = 5,
            EntryFee = 0,
            MaxParticipants = 1000,
            CutoffTime = TimeSpan.FromHours(12)
        };

        _context.Categories.Add(defaultCategory);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEventById), new { id = ev.EventId }, ev);
    }

    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        if (roleClaim != "Organiser") return StatusCode(403, "Access Denied. Only Organisers can edit events.");

        var ev = await _context.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        ev.Title = dto.Title;
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location;
        ev.Description = dto.Description;
        ev.Status = dto.Status;
        ev.ImageUrl = dto.ImageUrl;

        await _context.SaveChangesAsync();
        return Ok(ev);
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        if (roleClaim != "Organiser") return StatusCode(403, "Access Denied. Only Organisers can delete events.");

        var ev = await _context.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{eventId}/categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoriesByEvent(int eventId)
    {
        var categories = await _context.Categories.Where(c => c.EventId == eventId).ToListAsync();

        // If an existing event has no categories yet, auto-create "General Entry" right here so Participant enrolment never gets a 0/NULL CategoryId
        if (!categories.Any())
        {
            var evExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (evExists)
            {
                var defaultCat = new Category
                {
                    EventId = eventId,
                    CategoryName = "General Entry",
                    DistanceKm = 5,
                    EntryFee = 0,
                    MaxParticipants = 1000,
                    CutoffTime = TimeSpan.FromHours(12)
                };
                _context.Categories.Add(defaultCat);
                await _context.SaveChangesAsync();
                categories.Add(defaultCat);
            }
        }

        return Ok(categories);
    }

    [HttpPost("{eventId}/categories")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddCategory(int eventId, [FromBody] CreateCategoryDto dto)
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        if (roleClaim != "Organiser") return StatusCode(403, "Access Denied. Only Organisers can add categories.");

        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        var category = new Category
        {
            EventId = eventId,
            CategoryName = dto.CategoryName,
            DistanceKm = dto.DistanceKm,
            EntryFee = dto.EntryFee,
            MaxParticipants = dto.MaxParticipants,
            CutoffTime = TimeSpan.FromHours(12)
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return Created($"/api/events/{eventId}/categories/{category.CategoryId}", category);
    }

    [HttpGet("{eventId}/route")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventRoute(int eventId)
    {
        var route = await _context.Routes.FirstOrDefaultAsync(r => r.EventId == eventId);
        if (route == null) return NotFound(new { message = "Route information not found." });
        return Ok(route);
    }
}