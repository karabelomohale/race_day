using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using race_day.Data;
using race_day.Models;

namespace race_day.Controllers;

public record CreateEventDto(string Title, string EventType, DateTime EventDate, TimeSpan StartTime, string Location, string Province, string? Description);
public record UpdateEventDto(string Title, DateTime EventDate, string Location, string? Description, string Status);
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
        var query = _context.Events.Include(e => e.Categories).AsQueryable();

        if (!string.IsNullOrEmpty(province)) query = query.Where(e => e.Province == province);
        if (!string.IsNullOrEmpty(eventType)) query = query.Where(e => e.EventType == eventType);

        var events = await query.ToListAsync();
        return Ok(events);
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
        return Ok(ev);
    }

    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateEvent([FromForm] CreateEventDto dto) // Changed to [FromForm]
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

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
            Status = "Upcoming"
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEventById), new { id = ev.EventId }, ev);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromForm] UpdateEventDto dto) // Changed to [FromForm]
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        ev.Title = dto.Title;
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location;
        ev.Description = dto.Description;
        ev.Status = dto.Status;

        await _context.SaveChangesAsync();
        return Ok(ev);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
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
        return Ok(categories);
    }

    [HttpPost("{eventId}/categories")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddCategory(int eventId, [FromForm] CreateCategoryDto dto) // Changed to [FromForm]
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        var category = new Category
        {
            EventId = eventId,
            CategoryName = dto.CategoryName,
            DistanceKm = dto.DistanceKm,
            EntryFee = dto.EntryFee,
            MaxParticipants = dto.MaxParticipants,
            CutoffTime = dto.CutoffTime
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