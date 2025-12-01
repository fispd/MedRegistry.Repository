namespace MedRegistry.webapi.DTOs;

public class DoctorDto
{
    public int DoctorId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? Specialization { get; set; }
    public int? WorkExperienceYears { get; set; }
    public string? CabinetNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class CreateDoctorDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public int SpecializationId { get; set; }
    public int? WorkExperienceYears { get; set; }
    public string? CabinetNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? MedicalPolicy { get; set; }
}

