using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Specialization
{
    public int SpecializationId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
