using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int MemberId { get; set; }

    public string Subject { get; set; } = null!;

    public string Message { get; set; } = null!;

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<FeedbackComment> FeedbackComments { get; set; } = new List<FeedbackComment>();

    public virtual Account? Member { get; set; } = null!;
}
