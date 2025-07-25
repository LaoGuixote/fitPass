using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using fitPass.Models;

namespace fitPass.Controllers
{
    public class SubscribeController : Controller
    {
        private readonly GymManagementContext _context;

        public SubscribeController(GymManagementContext context)
        {
            _context = context;
        }

        // GET: Subscribe
        public  IActionResult Index()
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null)
                return RedirectToAction("Login", "Account");

            ViewBag.time = new DateOnly();
            if (_context.SubscriptionLogs.FirstOrDefault(a => a.MemberId == memberId) != null)
            {
                var member = _context.SubscriptionLogs.OrderByDescending(a => a.SubscriptionId).FirstOrDefault(a => a.MemberId == memberId);
                ViewBag.time = member!.EndDate;
            }
            return View();
        }

        [HttpPost]
        public IActionResult Index(string type)
        {
            int day = 0;
            int point = 0;
            string plan = "";
            switch (type)
            {
                case "1":
                    day = 30;
                    point = 900;
                    plan = "月繳";
                    break;
                case "2":
                    day = 120;
                    point = 2500;
                    plan = "季繳";
                    break;
                case "3":
                    day = 365;
                    point = 9000;
                    plan = "年繳";
                    break;
                default:
                    return RedirectToAction("Subscribe", "Member");
            }

            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null)
                return RedirectToAction("Login", "Account");

            var member = _context.Accounts.FirstOrDefault(a => a.MemberId == memberId);
            if (member!.Point - point > 0)
            {
                var lastest = _context.SubscriptionLogs.OrderByDescending(a => a.SubscriptionId).FirstOrDefault(a => a.MemberId == memberId);
                if (lastest == null || lastest.EndDate < DateOnly.FromDateTime(DateTime.Now))
                {
                    _context.SubscriptionLogs.Add(new SubscriptionLog
                    {
                        MemberId = memberId.Value,
                        SubscribedTime = DateTime.Now,
                        StartDate = DateOnly.FromDateTime(DateTime.Now),
                        EndDate = DateOnly.FromDateTime(DateTime.Now).AddDays(day),
                        SubscriptionType = plan
                    });
                }
                else
                {
                    _context.SubscriptionLogs.Add(new SubscriptionLog
                    {
                        MemberId = memberId.Value,
                        SubscribedTime = DateTime.Now,
                        StartDate = lastest.EndDate,
                        EndDate = lastest.EndDate.AddDays(day),
                        SubscriptionType = plan
                    });
                }

                _context.PointLogs.Add(new PointLog
                {
                    MemberId = member!.MemberId,
                    AlterationTime = DateTime.Now,
                    OriginalPoint = member.Point,
                    FinallPoint = member.Point - point,
                    Detail = $"訂閱 {plan}會籍 扣 {point} 點"
                });
                member.Point -= point;

                _context.SaveChanges();
                TempData["result"] = 1;
            }
            else
            {
                TempData["result"] = 0;
            }

            return RedirectToAction("Index");
        }

        public IActionResult Record()
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null)
                return RedirectToAction("Login", "Account");

            var record = _context.SubscriptionLogs.Where(a => a.MemberId == memberId).OrderByDescending(a => a.SubscriptionId).ToList();
            return View(record);
        }

        private bool SubscriptionLogExists(int id)
        {
            return _context.SubscriptionLogs.Any(e => e.SubscriptionId == id);
        }
    }
}
