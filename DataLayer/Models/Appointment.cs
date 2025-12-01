using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public DateTime AppointmentStart { get; set; }

    public DateTime AppointmentEnd { get; set; }

    public string? Status { get; set; }

    public string? Purpose { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    public virtual Patient Patient { get; set; } = null!;
}
