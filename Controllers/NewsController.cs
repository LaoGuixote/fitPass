using Microsoft.AspNetCore.Mvc;
using fitPass.Models;

public class NewsController : Controller
{
    private readonly GymManagementContext _context;

    public NewsController(GymManagementContext context)
    {
        _context = context;
    }

    // ✅ 消息總覽（分頁）
    public IActionResult Index(int page = 1)
    {
        int pageSize = 5;
        var visibleNews = _context.News
            .Where(n => n.IsVisible == true)
            .ToList() // ⭐ 先取回資料
            .OrderByDescending(n =>
             n.PublishTime
    );

        var paged = visibleNews
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)visibleNews.Count() / pageSize);

        return View(paged);
    }

    // ✅ 消息詳情
    public IActionResult Detail(int id)
    {
        var news = _context.News.FirstOrDefault(n => n.NewsId == id);
        if (news == null) return NotFound();
        return View(news);
    }
}
