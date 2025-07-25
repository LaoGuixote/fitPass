using fitPass.Models;
using fitPass.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace fitPass.Controllers
{
    public class CoachController : Controller
    {
        private readonly GymManagementContext _context;

        public CoachController(GymManagementContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            int? memberId = HttpContext.Session.GetInt32("MemberId");

            if (memberId == null)
                return RedirectToAction("Login", "Account");

            var coach = await _context.Coaches
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.AccountId == memberId.Value);

            if (coach == null)
                return RedirectToAction("Login", "Account");

            var today = DateOnly.FromDateTime(DateTime.Today);

            var upcomingClassCount = await _context.CourseSchedules
                .CountAsync(c => c.CoachId == coach.CoachId && c.ClassStartDate >= today);

            var scheduledSlotsCount = await _context.CoachTimes
                .CountAsync(c => c.CoachId == coach.CoachId);

            var vm = new CoachDashboardViewModel
            {
                Name = coach.Account.Name,
                Specialty = coach.Specialty,
                Description = coach.Description,
                CoachPhoto = coach.Photo != null
                    ? $"data:image/jpeg;base64,{Convert.ToBase64String(coach.Photo)}"
                    : null,
                UpcomingClassCount = upcomingClassCount,
                ScheduledSlotsCount = scheduledSlotsCount
            };

            return View(vm);  // 對應 Views/Coach/Index.cshtml
        }
        //教練總覽
        public async Task<IActionResult> CoachOverview()
        {
            var coachData = await _context.Coaches
                .Include(c => c.Account)
                .Select(c => new CoachOverviewViewModel
                {
                    CoachId = c.CoachId,
                    Name = c.Account.Name,
                    Specialty = c.Specialty,
                    CoachType = c.CoachType,
                    CoachPhoto = c.Photo != null ? "data:image/jpeg;base64," + Convert.ToBase64String(c.Photo) : null
                })
                .ToListAsync();

            var coachType = new CoachTypeOverviewViewModel
            {
                PrivateCoach = coachData.Where(ct => ct.CoachType == 1).ToList(),
                GroupCoach = coachData.Where(ct => ct.CoachType == 2 ).ToList()
            };

            return View(coachType);
        }

        //教練個人詳細頁面
        public async Task<IActionResult> CoachDetail(int id)
        {
            var coach = await _context.Coaches
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.CoachId == id);

            if (coach == null)
                return NotFound();

            var coachDetail = new CoachDetailViewModel
            {
                CoachId = coach.CoachId,
                Name = coach.Account.Name,
                Specialty = coach.Specialty,
                Description = coach.Description,
                CoachType = (int)coach.CoachType,
                CoachPhoto = coach.Photo != null
                    ? $"data:image/jpeg;base64,{Convert.ToBase64String(coach.Photo)}"
                    : null
            };

            return View(coachDetail);
        }

        //教練班表
        public IActionResult CoachSchedule()
        {
            var coachVerify = HttpContext.Session.GetInt32("Admin");
            var memberId = HttpContext.Session.GetInt32("MemberId");

            if (coachVerify != 2 || coachVerify == null)
            {
                return RedirectToAction("Index", "Member");
            }
            var coach = _context.Coaches.FirstOrDefault(c => c.AccountId == memberId);
            if (coach == null) return NotFound("教練資料不存在");
            ViewBag.CoachId = coach.CoachId;
            return View();
        }

        [HttpPost]
        public IActionResult SubmitSchedule(DateOnly date, List<int> timeSlots)
        {
            var coachVerify = HttpContext.Session.GetInt32("Admin");
            var memberId = HttpContext.Session.GetInt32("MemberId");

            if (coachVerify != 2 || coachVerify == null)
            {
                return RedirectToAction("Index", "Member");
            }

            var coach = _context.Coaches.FirstOrDefault(c => c.AccountId == memberId);
            if (coach == null) return NotFound();

            var existingTimeSlots = _context.CoachTimes
                .Where(ct => ct.CoachId == coach.CoachId && ct.Date == date)
                .Select(ct => ct.TimeSlot)
                .ToHashSet();

            foreach(var slot in timeSlots)
            {
                if (!existingTimeSlots.Contains(slot))
                {
                    _context.CoachTimes.Add(new CoachTime
                    {
                        CoachId = coach.CoachId,
                        Date = date,
                        TimeSlot = slot,
                        Status = 0
                    });
                }
            }

            _context.SaveChanges();
            return Content("OK");
        }

        [HttpGet]
        public IActionResult GetScheduleCount()
        {
            var coachVerify = HttpContext.Session.GetInt32("Admin");
            var memberId = HttpContext.Session.GetInt32("MemberId");

            if (coachVerify != 2 || coachVerify == null)
            {
                return RedirectToAction("Index", "Member");
            }

            var coach = _context.Coaches.FirstOrDefault(c => c.AccountId == memberId);
            if (coach == null) return NotFound();

            var result = _context.CoachTimes
                .Where(ct => ct.CoachId == coach.CoachId)
                .GroupBy(ct => ct.Date)
                .Select(g => new {
                    title = $"已排 {g.Count()} 段",
                    start = g.Key.ToString("yyyy-MM-dd"), // FullCalendar 要用這個欄位當日期
                    color = "#0d6efd"
                })
                .ToList();

            return Json(result);
        }

        //查詢被選取時段
        [HttpGet]
        public IActionResult GetTimeSlots(DateOnly date)
        {
            var coachVerify = HttpContext.Session.GetInt32("Admin");
            var memberId = HttpContext.Session.GetInt32("MemberId");

            if (coachVerify != 2 || coachVerify == null)
            {
                return RedirectToAction("Index", "Member");
            }

            var coach = _context.Coaches.FirstOrDefault(c => c.AccountId == memberId);
            if (coach == null) return NotFound();

            var slots = _context.CoachTimes
                .Where(ct => ct.CoachId == coach.CoachId && ct.Date == date)
                .Select(ct => ct.TimeSlot)
                .ToList();

            return Json(slots);
        }

        //===========================================================

        public async Task<IActionResult> MobileSchedule()
        {

            // 1. 取得目前登入者 AccountId（MemberId）
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null) return RedirectToAction("Login", "Account");

            // 2. 找這個 AccountId 對應的 CoachId
            var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.AccountId == memberId);
            if (coach == null) return Content("你不是教練帳號！");

            int coachId = coach.CoachId;

            // 3. 撈出這個教練所有開設的課程
            var myCourses = await _context.CourseSchedules
                .Include(c => c.Coach).ThenInclude(co => co.Account)
                .Where(c => c.CoachId == coachId)
                .ToListAsync();

            // 4. 展開成 CourseEventViewModel 給前端 FullCalendar 用
            var myCourseEvents = new List<CourseEventViewModel>();
            foreach (var c in myCourses)
            {
                if (c.ClassStartDate.HasValue && c.ClassEndDate.HasValue && c.ClassTimeDayOfWeek.HasValue && c.ClassTimeDaily.HasValue)
                {
                    var start = c.ClassStartDate.Value;
                    var end = c.ClassEndDate.Value;
                    for (var day = start; day <= end; day = day.AddDays(1))
                    {
                        if ((int)day.DayOfWeek == c.ClassTimeDayOfWeek)
                        {
                            myCourseEvents.Add(new CourseEventViewModel
                            {
                                CourseId = c.CourseId,
                                CourseTitle = c.Title,
                                ClassDate = day,
                                TimeSlot = c.ClassTimeDaily.Value,
                                CoachName = c.Coach?.Account?.Name ?? "",
                                // ...如需更多欄位可擴充
                            });
                        }
                    }
                }
            }

            // 5. 回傳 View，給 Views/Coach/MobileSchedule.cshtml 用
            return View(myCourseEvents);
        }

        [HttpGet]
        public IActionResult GetAllCoachCourseEvents(DateTime? start, DateTime? end)
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null) return Unauthorized();

            var coach = _context.Coaches.FirstOrDefault(c => c.AccountId == memberId);
            if (coach == null) return Unauthorized();

            int coachId = coach.CoachId;
            var events = new List<object>();

            // ---------- 團體課程：依天分組 ----------
            var groupDict = new Dictionary<string, List<object>>();
            var myCourses = _context.CourseSchedules
                .Include(c => c.Coach).ThenInclude(co => co.Account)
                .Where(c => c.CoachId == coachId)
                .ToList();

            foreach (var c in myCourses)
            {
                string coachName = c.Coach?.Account?.Name ?? "";

                if (c.ClassStartDate.HasValue && c.ClassEndDate.HasValue && c.ClassTimeDayOfWeek.HasValue && c.ClassTimeDaily.HasValue)
                {
                    var startDate = c.ClassStartDate.Value;
                    var endDate = c.ClassEndDate.Value;
                    for (var day = startDate; day <= endDate; day = day.AddDays(1))
                    {
                        if ((int)day.DayOfWeek == c.ClassTimeDayOfWeek)
                        {
                            string key = day.ToString("yyyy-MM-dd");
                            int hour = 6 + c.ClassTimeDaily.Value - 1;
                            var currentCount = _context.Reservations.Count(r => r.CourseId == c.CourseId && r.Status == 1);
                            var maxCount = c.MaxStudent ?? 0;
                            var detail = new
                            {
                                courseId = c.CourseId,  // 加這行
                                title = c.Title,
                                time = $"{hour}:00 - {hour + 1}:00",
                                coachName = coachName,
                                location = c.Location ?? "",
                                currentCount = currentCount,
                                maxCount = maxCount
                            };
                            if (!groupDict.ContainsKey(key))
                                groupDict[key] = new List<object>();
                            groupDict[key].Add(detail);
                        }
                    }
                }
            }
            foreach (var kvp in groupDict)
            {
                var day = DateTime.Parse(kvp.Key);
                var eventStart = day.Date.AddHours(6);
                var eventEnd = eventStart.AddHours(1);

                events.Add(new
                {
                    id = "group_" + kvp.Key,
                    title = "",
                    start = eventStart.ToString("s"),
                    end = eventEnd.ToString("s"),
                    color = "green",
                    type = "group",
                    allCourses = kvp.Value
                });
            }

            // ---------- 一對一課程（以教練為主角）：依天分組 ----------
            // 只抓教練自己是指派的 CoachId（不是用 memberId！）
            var oneOnOneQuery = (
                from ps in _context.PrivateSessions
                join ct in _context.CoachTimes on ps.TimeId equals ct.TimeId
                join coachEntity in _context.Coaches on ct.CoachId equals coachEntity.CoachId
                join account in _context.Accounts on ps.MemberId equals account.MemberId  // 這裡要撈學員名字
                where ct.CoachId == coachId
                    && ps.Status == 1
                    && ct.Status == 1
                select new
                {
                    ps.SessionId,
                    ct.Date,
                    ct.TimeSlot,
                    MemberName = account.Name
                }


            ).ToList();

            var oneOnOneDict = new Dictionary<string, List<object>>();
            foreach (var ps in oneOnOneQuery)
            {
                var baseDate = ps.Date.ToDateTime(TimeOnly.MinValue);
                string key = baseDate.ToString("yyyy-MM-dd");
                int hour = 6 + (ps.TimeSlot - 1);
                var detail = new
                {
                    memberName = ps.MemberName,
                    time = $"{hour}:00 - {hour + 1}:00"
                };
                if (!oneOnOneDict.ContainsKey(key))
                    oneOnOneDict[key] = new List<object>();
                oneOnOneDict[key].Add(detail);
            }
            foreach (var kvp in oneOnOneDict)
            {
                var day = DateTime.Parse(kvp.Key);
                var eventStart = day.Date.AddHours(6);
                var eventEnd = eventStart.AddHours(1);

                events.Add(new
                {
                    id = "oneonone_" + kvp.Key,
                    title = "",
                    start = eventStart.ToString("s"),
                    end = eventEnd.ToString("s"),
                    color = "orange",
                    type = "oneonone",
                    allCourses = kvp.Value
                });
            }

            return Json(events);
        }



        public async Task<IActionResult> DailySchedule(DateOnly? date)
        {
            // 1. 取得目前登入的教練身分
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null) return RedirectToAction("Login", "Account");

            // 找 AccountId 對應的 CoachId
            var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.AccountId == memberId);
            if (coach == null) return Content("你不是教練帳號！");

            int coachId = coach.CoachId;
            var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);

            // 2. 撈出這天這位教練的所有課程
            var myCourses = await _context.CourseSchedules
                .Include(c => c.Coach).ThenInclude(co => co.Account)
                .Where(c =>
                    c.CoachId == coachId
                    && c.ClassStartDate <= targetDate
                    && c.ClassEndDate >= targetDate
                    && c.ClassTimeDayOfWeek == (int)targetDate.DayOfWeek
                )
                .ToListAsync();

            // 3. 組前端要用的 ViewModel
            var myCourseEvents = myCourses.Select(c => new CourseEventViewModel
            {
                CourseTitle = c.Title,
                ClassDate = targetDate,
                TimeSlot = c.ClassTimeDaily ?? 0,
                CoachName = c.Coach?.Account?.Name ?? "",
                CourseId = c.CourseId
            }).ToList();

            // 4. 處理週日期
            var weekStart = targetDate.AddDays(-(int)targetDate.DayOfWeek + 1);
            var weekDates = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).ToList();
            ViewBag.WeekDates = weekDates;
            ViewBag.Date = targetDate;

            return View("DailySchedule", myCourseEvents); // 指定找 Coach 資料夾下的 View
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            // 查課程
            var course = await _context.CourseSchedules.FirstOrDefaultAsync(c => c.CourseId == id);
            if (course == null)
                return NotFound();

            // 查有報名這堂課(狀態1)的 Reservation
            var reservations = await _context.Reservations
                .Where(r => r.CourseId == id && r.Status == 1)
                .ToListAsync();

            // 查學生 MemberId
            var memberIds = reservations.Select(r => r.MemberId).Distinct().ToList();

            // 查學生姓名
            var students = await _context.Accounts
                .Where(a => memberIds.Contains(a.MemberId))
                .ToListAsync();

            // 傳資料到 View
            ViewBag.CourseTitle = course.Title;
            ViewBag.Students = students;

            return View();
        }

    }
}