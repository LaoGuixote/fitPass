using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class PointLog
{
    public int PointLogId { get; set; }

    public int MemberId { get; set; }

    public DateTime AlterationTime { get; set; }

    public int OriginalPoint { get; set; }

    public int FinallPoint { get; set; }

    public string? Detail { get; set; }

    public virtual Account? Member { get; set; } = null!;
}
