using System.ComponentModel.DataAnnotations;

namespace race_day.DTOs;

public class CreateEventDto
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string EventType { get; set; } = "Running"; // e.g., Running, Cycling, Trail

    [Required]
    public DateOnly EventDate { get; set; }
 
    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public string Location { get; set; } = string.Empty;

    [Required]
    public string Province { get; set; } = "Gauteng";

    public string? Description { get; set; }
}

public class UpdateEventDto
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    public string Location { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public string Status { get; set; } = "Upcoming"; // "Upcoming", "Active", "Completed", "Cancelled"
}

public class CreateCategoryDto
{
    [Required]
    [StringLength(50)]
    public string CategoryName { get; set; } = string.Empty; // e.g., "10km Run", "21km Half Marathon"

    [Required]
    [Range(0.1, 500.0)]
    public decimal DistanceKm { get; set; }

    [Required]
    [Range(0.0, 10000.0)]
    public decimal EntryFee { get; set; }

    [Required]
    [Range(1, 50000)]
    public int MaxParticipants { get; set; }

    [Required]
    public TimeSpan CutoffTime { get; set; }
}