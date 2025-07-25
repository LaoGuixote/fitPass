using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class Account
{
    public int MemberId { get; set; }

    public string Name { get; set; } = null!;

    public int? Gender { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Phone { get; set; }

    public DateOnly? Birthday { get; set; }

    public DateOnly? JoinDate { get; set; }

    public int Admin { get; set; }

    public byte[]? ProfileImage { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? LastLoginTime { get; set; }

    public int? Type { get; set; }

    public DateOnly? SubscriptionEndDate { get; set; }

    public int Point { get; set; }

    public virtual ICollection<CheckInRecord> CheckInRecords { get; set; } = new List<CheckInRecord>();

    public virtual Coach? Coach { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Inbody> Inbodies { get; set; } = new List<Inbody>();

    public virtual ICollection<PointLog> PointLogs { get; set; } = new List<PointLog>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual ICollection<SubscriptionLog> SubscriptionLogs { get; set; } = new List<SubscriptionLog>();
}
