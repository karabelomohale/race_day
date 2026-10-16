using System.ComponentModel.DataAnnotations;

namespace race_day.DTOs;

public class CreateEnrolmentDto
{
    [Required]
    public int CategoryId { get; set; }

    [Required]
    [Phone]
    public string EmergencyContact { get; set; } = string.Empty;

    public string? MedicalAidNumber { get; set; }
}

public class CaptureResultDto
{
    [Required]
    public int EnrolmentId { get; set; }

    [Required]
    public TimeSpan FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int OverallPosition { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryPosition { get; set; }
}