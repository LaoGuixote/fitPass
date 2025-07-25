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
    public class PointController : Controller
    {
        private readonly GymManagementContext _context;

        public PointController(GymManagementContext context)
        {
            _context = context;
        }

        // GET: Point
        public  IActionResult Index()
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null)
                return RedirectToAction("Login", "Account");

            var member = _context.Accounts.FirstOrDefault(a => a.MemberId == memberId);
            ViewBag.record = _context.PointLogs.Where(a => a.MemberId == memberId).OrderByDescending(a => a.AlterationTime).Take(10).ToList();
            return View(member);
        }

        [HttpPost]
        public IActionResult Index(int point)
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null)
                return RedirectToAction("Login", "Account");

            if (point > 0)
            {
                var member = _context.Accounts.FirstOrDefault(a => a.MemberId == memberId);
                _context.PointLogs.Add(new PointLog
                {
                    MemberId = member!.MemberId,
                    AlterationTime = DateTime.Now,
                    OriginalPoint = member.Point,
                    FinallPoint = member.Point + point,
                    Detail = $"儲值 {point} 點"
                });
                member.Point += point;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        public IActionResult Records()
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null)
                return RedirectToAction("Login", "Account");

            var record = _context.PointLogs.Where(a => a.MemberId == memberId).OrderByDescending(a => a.AlterationTime).ToList();
            return View(record);
        }

        private bool AccountExists(int id)
        {
            return _context.Accounts.Any(e => e.MemberId == id);
        }
    }
}
