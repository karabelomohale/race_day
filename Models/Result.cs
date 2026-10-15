using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace race_day.Models;

public class Result
{
    [Key]
    public int ResultId { get; set; }

    [Required]
    public int EnrolmentId { get; set; }

    [ForeignKey(nameof(EnrolmentId))]
    public EventEnrolment? Enrolment { get; set; }

    public TimeSpan? GunTime { get; set; }
    public TimeSpan? ChipTime { get; set; }

    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Finished"; // Finished, DNF, DNS, Disqualified

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}