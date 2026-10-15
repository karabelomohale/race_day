using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace race_day.Models;

public class EventEnrolment
{
    [Key]
    public int EnrolmentId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    [Required]
    public int ParticipantId { get; set; }

    [ForeignKey(nameof(ParticipantId))]
    public User? Participant { get; set; }

    public int RaceNumber { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(20)]
    public string PaymentStatus { get; set; } = "Paid";

    [Required, MaxLength(100)]
    public string EmergencyContact { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? MedicalAidNumber { get; set; }

    public Result? Result { get; set; }
}