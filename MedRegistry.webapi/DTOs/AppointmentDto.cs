namespace MedRegistry.webapi.DTOs;

public class AppointmentDto
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string? DoctorSpecialization { get; set; }
    public string? CabinetNumber { get; set; }
    public DateTime AppointmentStart { get; set; }
    public DateTime AppointmentEnd { get; set; }
    public string? Status { get; set; }
    public string? Purpose { get; set; }
}

public class CreateAppointmentDto
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime AppointmentStart { get; set; }
    public DateTime AppointmentEnd { get; set; }
    public string? Purpose { get; set; }
}

public class UpdateAppointmentDto
{
    public DateTime? AppointmentStart { get; set; }
    public DateTime? AppointmentEnd { get; set; }
    public string? Status { get; set; }
    public string? Purpose { get; set; }
}

