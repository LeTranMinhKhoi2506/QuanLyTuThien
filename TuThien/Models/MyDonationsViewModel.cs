using System.Collections.Generic;

namespace TuThien.Models
{
    public class MyDonationsViewModel
    {
        public List<ContributedCampaignViewModel> ContributedCampaigns { get; set; } = new List<ContributedCampaignViewModel>();
        public List<Donation> DonationHistory { get; set; } = new List<Donation>();
        
        // Simple pagination for history tab
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
