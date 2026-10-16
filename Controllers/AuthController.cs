using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using race_day.Data;
using race_day.DTOs;
using race_day.Models;

namespace race_day.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{// Inject the DbContext into the controller
    private readonly RaceDayDbContext _context;

    public AuthController(RaceDayDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    // Changed to [FromForm] so Swagger creates individual text boxes for each field
    public async Task<IActionResult> Register([FromQuery] RegisterRequestDto dto)
    {
        if (dto.Role != "Organiser" && dto.Role != "Participant")
        {
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });
        }

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return Conflict(new { message = "Email address is already registered." });
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = passwordHash,
            Role = dto.Role,
            PhoneNumber = dto.PhoneNumber,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Register), new { id = user.UserId }, new { user.UserId, user.Email, user.Role });
    }

    [HttpPost("login")]
    // Changed to [FromForm] here as well
    public async Task<IActionResult> Login([FromQuery] LoginRequestDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Ok(new
        {
            message = "Login successful. Swagger will now automatically authenticate your requests.",
            user.Role,
            user.Email
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Successfully logged out. Your session has been cleared." });
    }
}