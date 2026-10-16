using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using race_day.Data;
using System.Security.Claims;

namespace race_day.Controllers;

public record UpdateProfileDto(string FirstName, string LastName, string? PhoneNumber);

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public UsersController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves the profile information of the currently logged-in user.
    /// </summary>
    /// <response code="200">Returns the user's profile details.</response>
    /// <response code="401">If the user is not logged in.</response>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);

        if (user == null) return NotFound(new { message = "User not found." });

        return Ok(new
        {
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role,
            user.PhoneNumber,
            user.CreatedAt
        });
    }

    /// <summary>
    /// Updates the personal profile details of the currently logged-in user.
    /// </summary>
    /// <param name="dto">Updated user profile fields.</param>
    /// <response code="200">Returns the updated user profile.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromQuery] UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);

        if (user == null) return NotFound(new { message = "User not found." });

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.PhoneNumber = dto.PhoneNumber;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role,
            user.PhoneNumber,
        });
    }
}