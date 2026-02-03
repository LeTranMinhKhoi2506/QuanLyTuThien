using System;
using System.Collections.Generic;

namespace TuThien.Models;

public partial class UserProfile
{
    public int ProfileId { get; set; }

    public int? UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Address { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public virtual User? User { get; set; }
}
