using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class PrivateSession
{
    public int SessionId { get; set; }

    public int MemberId { get; set; }

    public string? Location { get; set; }

    public int? Status { get; set; }

    public string? Note { get; set; }

    public DateTime? CreateTime { get; set; }

    public int TimeId { get; set; }

    public virtual CoachTime? Time { get; set; } = null!;
}
