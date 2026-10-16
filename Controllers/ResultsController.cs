using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using race_day.Data;
using race_day.Models;
using System.Security.Claims;

namespace race_day.Controllers;

public record CaptureResultDto(int EnrolmentId, TimeSpan GunTime, TimeSpan ChipTime, int OverallPosition, int CategoryPosition, string Status);

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Captures official race finish times and positions for an enrolled participant (Organiser only).
    /// </summary>
    /// <param name="dto">Finish time, positions, and completion status.</param>
    /// <response code="201">Result successfully recorded.</response>
    /// <response code="403">If accessed by a Participant instead of an Organiser.</response>
    /// <response code="404">If the enrolment ID does not exist.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CaptureResult([FromQuery] CaptureResultDto dto)
    {
        var enrolment = await _context.EventEnrolments.FindAsync(dto.EnrolmentId);
        if (enrolment == null)
            return NotFound(new { message = "Enrolment record not found." });

        if (await _context.Results.AnyAsync(r => r.EnrolmentId == dto.EnrolmentId))
            return Conflict(new { message = "Result already captured for this enrolment." });

        var result = new Result
        {
            EnrolmentId = dto.EnrolmentId,
            GunTime = dto.GunTime,
            ChipTime = dto.ChipTime,
            OverallPosition = dto.OverallPosition,
            CategoryPosition = dto.CategoryPosition,
            Status = dto.Status
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEventResults), new { eventId = enrolment.CategoryId }, result);
    }

    /// <summary>
    /// Retrieves the public leaderboard and race results for a specific event.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <response code="200">List of participant results ordered by overall position.</response>
    [HttpGet("event/{eventId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        var results = await _context.Results
            .Include(r => r.Enrolment)
                .ThenInclude(e => e!.Participant)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e!.Category)
            .Where(r => r.Enrolment!.Category!.EventId == eventId)
            .OrderBy(r => r.OverallPosition)
            .Select(r => new
            {
                r.ResultId,
                r.OverallPosition,
                r.CategoryPosition,
                r.GunTime,
                r.ChipTime,
                r.Status,
                RaceNumber = r.Enrolment!.RaceNumber,
                RunnerName = r.Enrolment.Participant!.FirstName + " " + r.Enrolment.Participant.LastName,
                CategoryName = r.Enrolment.Category!.CategoryName
            })
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>
    /// Retrieves personal historical race results for the logged-in Participant.
    /// </summary>
    /// <response code="200">Returns the participant's personal race history.</response>
    [HttpGet("my-results")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyResults()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var myResults = await _context.Results
            .Include(r => r.Enrolment)
                .ThenInclude(e => e!.Category)
                    .ThenInclude(c => c!.Event)
            .Where(r => r.Enrolment!.ParticipantId == userId)
            .Select(r => new
            {
                r.ResultId,
                EventTitle = r.Enrolment!.Category!.Event!.Title,
                EventDate = r.Enrolment.Category.Event.EventDate,
                Category = r.Enrolment.Category.CategoryName,
                DistanceKm = r.Enrolment.Category.DistanceKm,
                r.GunTime,
                r.ChipTime,
                r.OverallPosition,
                r.CategoryPosition,
                r.Status
            })
            .ToListAsync();

        return Ok(myResults);
    }
}