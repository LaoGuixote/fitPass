using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class FeedbackComment
{
    public int CommentId { get; set; }

    public int FeedbackId { get; set; }

    public bool Admin { get; set; }

    public string CommentText { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Feedback? Feedback { get; set; } = null!;
}
