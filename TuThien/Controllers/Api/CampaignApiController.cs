using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;

namespace TuThien.Controllers.Api;

[Route("api/campaigns")]
[ApiController]
public class CampaignApiController : ControllerBase
{
    private readonly TuThienContext _context;
    private readonly ILogger<CampaignApiController> _logger;

    public CampaignApiController(TuThienContext context, ILogger<CampaignApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Search campaigns by title
    /// GET: /api/campaigns/search?q=keyword&limit=10
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Ok(new
                {
                    success = false,
                    message = "T? khóa tìm ki?m ph?i có ít nh?t 2 ký t?",
                    campaigns = Array.Empty<object>(),
                    totalCount = 0
                });
            }

            var searchTerm = q.Trim().ToLower();

            // Tìm ki?m chi?n d?ch (?u tiên active campaigns)
            var query = _context.Campaigns
                .Include(c => c.Category)
                .Where(c => c.Title.ToLower().Contains(searchTerm) ||
                           (c.Description != null && c.Description.ToLower().Contains(searchTerm)))
                .OrderByDescending(c => c.Status == "active" ? 1 : 0)
                .ThenByDescending(c => c.CreatedAt);

            var totalCount = await query.CountAsync();
            var campaigns = await query.Take(Math.Min(limit, 20)).ToListAsync();

            var results = campaigns.Select(c => new
            {
                campaignId = c.CampaignId,
                title = c.Title,
                description = c.Description,
                status = c.Status,
                currentAmount = c.CurrentAmount ?? 0,
                targetAmount = c.TargetAmount,
                thumbnailUrl = c.ThumbnailUrl,
                categoryName = c.Category?.Name,
                createdAt = c.CreatedAt
            }).ToList();

            return Ok(new
            {
                success = true,
                campaigns = results,
                totalCount = totalCount,
                query = q
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching campaigns with query: {Query}", q);
            return StatusCode(500, new
            {
                success = false,
                message = "Có l?i x?y ra khi tìm ki?m",
                campaigns = Array.Empty<object>(),
                totalCount = 0
            });
        }
    }
}
