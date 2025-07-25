using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class CheckInRecord
{
    public int RecordId { get; set; }

    public int MemberId { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public string? Status { get; set; }

    public string? Device { get; set; }

    public int? CheckType { get; set; }

    public virtual Account Member { get; set; } = null!;
}
