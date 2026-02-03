using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;

namespace TuThien.ViewComponents
{
    public class SidebarViewComponent : ViewComponent
    {
        private readonly TuThienContext _context;
        public SidebarViewComponent(TuThienContext context)
        {
            _context = context;
        }
        
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }
    }
}