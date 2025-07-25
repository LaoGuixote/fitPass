using fitPass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace fitPass.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly GymManagementContext _context;

        public FeedbackController(GymManagementContext context)
        {
            _context = context;
        }

        // GET: Feedback
        public async Task<IActionResult> Index()
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null)
                return RedirectToAction("Login", "Account");
            var feedback = await _context.Feedbacks.Where(a => a.MemberId == memberId).Include(i => i.FeedbackComments).OrderByDescending(a => a.CreatedAt).ToListAsync();
            return View(feedback);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // 移除 async/await，因為只處理單一 Comment 且是直接返回 JSON
        public async Task<JsonResult> AddComment(int feedbackId, string commentText)
        {
            if (string.IsNullOrWhiteSpace(commentText))
            {
                // 返回錯誤 JSON
                return Json(new { success = false, message = "回應內容不能為空。" });
            }

            var feedback = await _context.Feedbacks.FindAsync(feedbackId);
            if (feedback == null)
            {
                // 返回錯誤 JSON
                return Json(new { success = false, message = "找不到指定的意見。" });
            }

            // 這裡您需要根據實際邏輯判斷是否為管理員回覆
            // 範例：假設目前登入的使用者不是管理員 (此處為使用者發布留言)
            // 您可以根據 HttpContext.Session 或其他驗證方式判斷 isAdminReply

            var comment = new FeedbackComment
            {
                FeedbackId = feedbackId,
                CommentText = commentText,
                CreatedAt = DateTime.Now,
                Admin = false // 設定 Admin 屬性
            };

            _context.FeedbackComments.Add(comment);
            await _context.SaveChangesAsync();

            // 返回成功的 JSON 數據，包含新創建的評論資訊
            // 注意：序列化選項可以幫助處理循環引用或日期格式
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve, // 處理循環引用，如果有的話
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase // 將 C# 屬性名轉換為 camelCase (JavaScript 常用)
            };

            return Json(new
            {
                success = true,
                message = "回應已成功發布！",
                comment = new // 傳回新回應的必要數據
                {
                    CommentId = comment.CommentId,
                    CommentText = comment.CommentText,
                    CreatedAt = comment.CreatedAt.ToString("yyyy/MM/dd HH:mm:ss"), // 格式化日期時間
                    Admin = comment.Admin,
                    FeedbackId = comment.FeedbackId
                }
            }, options); // 傳遞序列化選項
        }


        // GET: Feedback/Create
        public IActionResult Create()
        {
            ViewData["MemberId"] = new SelectList(_context.Accounts, "MemberId", "MemberId");
            return View();
        }

        // POST: Feedback/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Feedback feedback)
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null)
                return RedirectToAction("Login", "Account");
            feedback.MemberId = memberId.Value;
            feedback.Status = 1;
            feedback.CreatedAt = DateTime.Now;
            _context.Add(feedback);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool FeedbackExists(int id)
        {
            return _context.Feedbacks.Any(e => e.FeedbackId == id);
        }
    }
}
