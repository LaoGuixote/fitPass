using System;
using System.Collections.Generic;

namespace fitPass.Models;

public partial class PasswordReset
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DateTime ExpiredAt { get; set; }
}
