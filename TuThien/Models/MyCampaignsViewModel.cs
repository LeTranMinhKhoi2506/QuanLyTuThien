using System.Collections.Generic;

namespace TuThien.Models
{
    public class ContributedCampaignViewModel
    {
        public Campaign Campaign { get; set; }
        public decimal TotalDonated { get; set; }
    }

    public class MyCampaignsViewModel
    {
        public List<Campaign> OwnedCampaigns { get; set; } = new List<Campaign>();
    }
}
