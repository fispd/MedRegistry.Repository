namespace MedRegistry.webapi.DTOs;

public class ScheduleDto
{
    public int ScheduleId { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string? CabinetNumber { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool? IsAvailable { get; set; }
}

public class CreateScheduleDto
{
    public int DoctorId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class UpdateScheduleDto
{
    public DateOnly? WorkDate { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool? IsAvailable { get; set; }
}

