using fitPass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace FitPass.Controllers
{
    public class ReservationController : Controller
    {
        private readonly GymManagementContext _context;
        public ReservationController(GymManagementContext context)
        {
            _context = context;
        }

        // 顯示月曆可預約的日期
        public IActionResult ReserveCalendar(int coachId)
        {
            var coach = _context.Coaches
                .Include(c => c.Account)
                .FirstOrDefault(c => c.CoachId == coachId);
            ViewBag.CoachId = coachId;
            return View(coach);
        }

        //取得教練有哪些日子可預約
        [HttpGet]
        public IActionResult GetAvailableDates(int coachId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var days = _context.CoachTimes
                .Where(c => c.CoachId == coachId && c.Status == 0 && c.Date >= today)
                .GroupBy(c => c.Date)
                .Select(g => new {
                    title = "可預約",
                    start = g.Key.ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-dd")
                })
                .ToList();

            return Json(days);
        }

        //點日期顯示當日可預約時段
        [HttpGet]
        public IActionResult GetAvailableTimeSlots(int coachId, DateOnly date)
        {
            var slots = _context.CoachTimes
                .Where(c => c.CoachId == coachId && c.Date == date && c.Status == 0)
                .OrderBy(c => c.TimeSlot)
                .Select(c => new { c.TimeId, c.TimeSlot })
                .ToList();
            return Json(slots);
        }

        //點日期顯示當日可預約時段
        //public IActionResult SelectTime(int coachId, DateOnly date)
        //{
        //    var timeSlots = _context.CoachTimes
        //        .Where(c => c.CoachId == coachId && c.Date == date && c.Status == 0)
        //        .OrderBy(c => c.TimeSlot)
        //        .ToList();

        //    ViewBag.CoachId = coachId;
        //    ViewBag.Date = date;
        //    return View(timeSlots);
        //}

        //送出預約
        [HttpPost]
        public IActionResult SubmitReservation(int timeId)
        {
            int? memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null)
                return Json(new { success = false, message = "尚未登入" });

            var time = _context.CoachTimes.FirstOrDefault(c => c.TimeId == timeId && c.Status == 0);
            if (time == null)
                return Json(new { success = false, message = "此時段無法預約" });

            //防止重複預約
            var exists = _context.PrivateSessions.Any(p => p.TimeId == timeId);
            if (exists)
                return Json(new { success = false, message = "此時段已被預約，請選其他時段" });

            int cost = 299;

            var member = _context.Accounts.FirstOrDefault(mp => mp.MemberId == memberId);

            if (member.Point < cost)
            {
                return Json(new { success = false, message = "點數不足，請先儲值" });
            }

            member.Point -= cost;

            time.Status = 1;

            var session = new PrivateSession
            {
                TimeId = time.TimeId,
                MemberId = memberId.Value,
                CreateTime = DateTime.Now
            };

            _context.PrivateSessions.Add(session);
            _context.SaveChanges();

            return Json(new { success = true, message = "預約成功！" });
        }
    }



}
