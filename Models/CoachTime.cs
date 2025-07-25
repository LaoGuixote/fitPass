using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class CoachTime
{
    public int TimeId { get; set; }

    public int CoachId { get; set; }

    public DateOnly Date { get; set; }

    public int TimeSlot { get; set; }

    public int Status { get; set; }

    public virtual Coach? Coach { get; set; } = null!;

    public virtual ICollection<PrivateSession> PrivateSessions { get; set; } = new List<PrivateSession>();
}
