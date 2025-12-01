namespace MedRegistry.webapi.DTOs;

public class StatisticsDto
{
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int TotalAppointments { get; set; }
    public int AppointmentsToday { get; set; }
    public int PendingAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public List<SpecializationStatsDto> DoctorsBySpecialization { get; set; } = new();
}

public class SpecializationStatsDto
{
    public string Specialization { get; set; } = string.Empty;
    public int Count { get; set; }
}

