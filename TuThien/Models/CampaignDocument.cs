using System;
using System.Collections.Generic;

namespace TuThien.Models;

public partial class CampaignDocument
{
    public int DocumentId { get; set; }

    public int CampaignId { get; set; }

    public string FileUrl { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public string? Description { get; set; }

    public virtual Campaign Campaign { get; set; } = null!;
}
