using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class News
{
    public int NewsId { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public string? Category { get; set; }

    public DateTime? PublishTime { get; set; }

    public bool? IsVisible { get; set; }

    public int? Level { get; set; }

    public byte[]? Banner { get; set; }

    public byte[]? Insideimg { get; set; }

    public DateTime? Showtime { get; set; }
}
