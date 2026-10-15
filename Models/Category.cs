using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace race_day.Models;

public class Category
{
    [Key]
    public int CategoryId { get; set; }

    [Required]
    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,2)")]
    public decimal DistanceKm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal EntryFee { get; set; }

    public int MaxParticipants { get; set; }

    public TimeSpan CutoffTime { get; set; }

    public ICollection<EventEnrolment> Enrolments { get; set; } = new List<EventEnrolment>();
}