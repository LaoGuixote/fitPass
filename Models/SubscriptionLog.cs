using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class SubscriptionLog
{
    public int SubscriptionId { get; set; }

    public int MemberId { get; set; }

    public DateTime SubscribedTime { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string SubscriptionType { get; set; } = null!;

    public virtual Account? Member { get; set; } = null!;
}
