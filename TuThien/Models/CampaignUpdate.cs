using System;
using System.Collections.Generic;

namespace TuThien.Models;

public partial class CampaignUpdate
{
    public int UpdateId { get; set; }

    public int CampaignId { get; set; }

    public int AuthorId { get; set; }

    public string? Title { get; set; }

    public string Content { get; set; } = null!;

    public string? Type { get; set; }

    public string? ImageUrls { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Author { get; set; } = null!;

    public virtual Campaign Campaign { get; set; } = null!;
}
