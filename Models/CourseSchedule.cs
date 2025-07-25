using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class CourseSchedule
{
    public int CourseId { get; set; }

    public int CoachId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? ClassTimeDaily { get; set; }

    public int? ClassTimeDayOfWeek { get; set; }

    public string? Location { get; set; }

    public int? MaxStudent { get; set; }

    public int? StudentCount { get; set; }

    public DateOnly? ClassStartDate { get; set; }

    public DateOnly? ClassEndDate { get; set; }

    public int? Price { get; set; }

    public bool? IsCanceled { get; set; }

    public byte[]? CourseImage { get; set; }

    public virtual Coach? Coach { get; set; } = null!;

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
