using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class Inbody
{
    public int InBodyId { get; set; }

    public int MemberId { get; set; }

    public int? Height { get; set; }

    public int? Weight { get; set; }

    public int? BodyFat { get; set; }

    public int? Bmr { get; set; }

    public DateOnly? RecordDate { get; set; }

    public string? Note { get; set; }

    public string? GoalNote { get; set; }

    public virtual Account? Member { get; set; } = null!;
}
