using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public int DoctorId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool? IsAvailable { get; set; }

    public DateOnly WorkDate { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;
}
