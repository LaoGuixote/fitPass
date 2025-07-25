using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class Coach
{
    public int CoachId { get; set; }

    public int AccountId { get; set; }

    public string? Specialty { get; set; }

    public string? Description { get; set; }

    public byte[]? Photo { get; set; }

    public int? CoachType { get; set; }

    public virtual Account? Account { get; set; } = null!;

    public virtual ICollection<CoachTime> CoachTimes { get; set; } = new List<CoachTime>();

    public virtual ICollection<CourseSchedule> CourseSchedules { get; set; } = new List<CourseSchedule>();
}
