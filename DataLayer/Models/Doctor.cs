using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Doctor
{
    public int DoctorId { get; set; }

    public int UserId { get; set; }

    public int SpecializationId { get; set; }

    public string? LicenseNumber { get; set; }

    public int? WorkExperienceYears { get; set; }

    public string? CabinetNumber { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual Specialization Specialization { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
