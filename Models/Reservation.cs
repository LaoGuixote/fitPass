using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class Reservation
{
    public int ReservationId { get; set; }

    public int MemberId { get; set; }

    public int CourseId { get; set; }

    public int? Status { get; set; }

    public DateOnly? ReservationDate { get; set; }

    public int? ReservationTime { get; set; }

    public DateTime? CreateTime { get; set; }

    public bool? IsNoShow { get; set; }

    public string? Note { get; set; }

    public virtual CourseSchedule? Course { get; set; } = null!;

    public virtual Account? Member { get; set; } = null!;
}
