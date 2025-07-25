using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class Attach
{
    public int AttachId { get; set; }

    public int FeedbackId { get; set; }

    public byte[] Photo { get; set; } = null!;

    public virtual Feedback Feedback { get; set; } = null!;
}
