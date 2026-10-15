using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace race_day.Models;

public class Event
{
    [Key]
    public int EventId { get; set; }

    [Required]
    public int OrganiserId { get; set; }

    [ForeignKey(nameof(OrganiserId))]
    public User? Organiser { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string EventType { get; set; } = "Running"; // Running, Cycling, Walking, Multi-Sport

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Province { get; set; } = "Gauteng";

    public string? Description { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Upcoming"; // Upcoming, Ongoing, Completed, Cancelled

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Route> Routes { get; set; } = new List<Route>();
}