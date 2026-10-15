using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace race_day.Models;

public class Route
{
    [Key]
    public int RouteId { get; set; }

    [Required]
    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required, MaxLength(100)]
    public string RouteName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string StartingPoint { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string FinishingPoint { get; set; } = string.Empty;

    public int ElevationGainMeters { get; set; }

    [MaxLength(255)]
    public string? GpxFileUrl { get; set; }

    [Required, MaxLength(100)]
    public string WeatherLocationQuery { get; set; } = string.Empty;
}