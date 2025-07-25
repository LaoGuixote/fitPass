using fitPass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace fitPass.Controllers
{
    public class CourseSchedulesController : Controller
    {
        private readonly GymManagementContext _context;

        public CourseSchedulesController(GymManagementContext context)
        {
            _context = context;
        }


        // 新增這兩個 Action
        public async Task<IActionResult> GroupCourseList(int? coachId, string signUpFilter, string availableFilter)
        {
            // 直接抓所有課程（不篩選）
            var groupCourses = await _context.CourseSchedules.ToListAsync();
            var coaches = await _context.Coaches
    .Where(c => c.CoachType == 2)
    .ToListAsync();

            var accounts = await _context.Accounts.ToListAsync();

            // 教練清單 for dropdown
            var coachItems = coaches.Select(coach =>
            {
                var account = accounts.FirstOrDefault(a => a.MemberId == coach.AccountId);
                return new
                {
                    CoachId = coach.CoachId,
                    Name = account?.Name ?? "未知教練"
                };
            }).ToList();
            ViewBag.CoachList = coachItems;

            // 新增：取得目前登入者ID
            int? memberId = HttpContext.Session.GetInt32("MemberId");

            // 取得所有「已報名」課程ID
            List<int> reservedCourseIds = new List<int>();
            if (memberId.HasValue)
            {
                reservedCourseIds = _context.Reservations
                    .Where(r => r.MemberId == memberId && r.Status == 1)
                    .Select(r => r.CourseId)
                    .Distinct()
                    .ToList();
            }

            var viewModel = groupCourses.Select(course =>
            {
                var coach = coaches.FirstOrDefault(co => co.CoachId == course.CoachId);
                var account = coach != null ? accounts.FirstOrDefault(a => a.MemberId == coach.AccountId) : null;
                string coachName = account != null ? account.Name : "";
                string photoUrl = null;

                return new GroupCourseVM
                {
                    Course = course,
                    CoachName = coachName,
                    PhotoUrl = photoUrl
                };
            });
            // 篩選邏輯
            if (coachId.HasValue && coachId.Value != 0)
            {
                viewModel = viewModel.Where(vm => vm.Course.CoachId == coachId.Value);
            }
            if (!string.IsNullOrEmpty(signUpFilter) && memberId.HasValue)
            {
                if (signUpFilter == "registered")
                {
                    viewModel = viewModel.Where(vm => reservedCourseIds.Contains(vm.Course.CourseId));
                }
                else if (signUpFilter == "notRegistered")
                {
                    viewModel = viewModel.Where(vm => !reservedCourseIds.Contains(vm.Course.CourseId));
                }
            }
            // **這裡加上你的「可報名課程」篩選**
            if (availableFilter == "available")
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                viewModel = viewModel.Where(vm => vm.Course.ClassStartDate > today);
            }

            return View(viewModel.ToList());
        }
        public async Task<IActionResult> GroupCourseDetail(int id)
        {
            // 1. 找課程
            var course = await _context.CourseSchedules.FindAsync(id);
            if (course == null) return NotFound();

            // 2. 撈教練名字
            var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.CoachId == course.CoachId);
            var account = coach != null ? await _context.Accounts.FirstOrDefaultAsync(a => a.MemberId == coach.AccountId) : null;
            string coachName = account?.Name ?? "未知教練";

            // 3. 查這堂課的報名人數（status=1）
            int currentCount = await _context.Reservations
                .Where(r => r.CourseId == id && r.Status == 1)
                .Select(r => r.MemberId)
                .Distinct()
                .CountAsync();

            // 4. 查課堂上限
            int maxCount = course.MaxStudent ?? 0;  // 防止 null

            // 5. 丟給 View
            ViewBag.CoachName = coachName;
            ViewBag.CurrentCount = currentCount;
            ViewBag.MaxCount = maxCount;

            // 【新增】課程區間
            string dateRange = "";
            if (course.ClassStartDate.HasValue && course.ClassEndDate.HasValue)
                dateRange = $"{course.ClassStartDate.Value:yyyy/MM/dd} ~ {course.ClassEndDate.Value:yyyy/MM/dd}";
            ViewBag.DateRange = dateRange;

            // 【新增】課程時間（如：每週二 15:00-16:00）
            string[] weekNames = { "日", "一", "二", "三", "四", "五", "六" };
            string timeString = "";
            if (course.ClassTimeDayOfWeek.HasValue && course.ClassTimeDaily.HasValue)
            {
                int weekIdx = course.ClassTimeDayOfWeek.Value;
                int slot = course.ClassTimeDaily.Value;
                string week = weekNames[weekIdx];
                string startTime = (6 + slot - 1).ToString("D2") + ":00";
                string endTime = (6 + slot).ToString("D2") + ":00";
                timeString = $"每週{week} {startTime} - {endTime}";
            }
            ViewBag.TimeString = timeString;

            // 【新增】地點
            ViewBag.Location = course.Location ?? "未指定";

            // 【新增】判斷目前登入會員是否已報名
            int? memberId = HttpContext.Session.GetInt32("MemberId");
            bool hasReserved = false;
            if (memberId.HasValue)
            {
                hasReserved = await _context.Reservations.AnyAsync(r => r.MemberId == memberId && r.CourseId == id && r.Status == 1);
            }
            ViewBag.HasReserved = hasReserved;
            ViewBag.ClassTimeStart = course.ClassStartDate; // ✅ 傳給前端判斷是否過期

            return View(course);
        }

        public async Task<IActionResult> MobileSchedule()
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null) return RedirectToAction("Login", "Account");

            var myReservations = await _context.Reservations
                .Include(r => r.Course).ThenInclude(c => c.Coach).ThenInclude(co => co.Account)
                .Where(r => r.MemberId == memberId && r.Status == 1)
                .ToListAsync();

            var myCourseEvents = new List<CourseEventViewModel>();
            foreach (var r in myReservations)
            {
                var c = r.Course;
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
                                CourseTitle = c.Title,
                                ClassDate = day,
                                TimeSlot = c.ClassTimeDaily.Value,
                                CoachName = c.Coach?.Account?.Name ?? "",
                            });
                        }
                    }
                }
            }

            // --- 2. 取得「一對一」預約課程 ---
            var myOneOnOne = await _context.PrivateSessions
                .Include(ps => ps.Time)
                    .ThenInclude(ct => ct.Coach)
                        .ThenInclude(c => c.Account)
                .Where(ps => ps.MemberId == memberId && ps.Status == 1)
                .ToListAsync();

            foreach (var ps in myOneOnOne)
            {
                var ct = ps.Time; // <<== 這裡改成 .Time
                if (ct != null)
                {
                    myCourseEvents.Add(new CourseEventViewModel
                    {
                        CourseTitle = "一對一課程", // 或你想顯示的名稱
                        ClassDate = ct.Date,
                        TimeSlot = ct.TimeSlot,
                        CoachName = ct.Coach?.Account?.Name ?? "",
                        // 你要什麼資料可以再加
                    });
                }
            }

            return View(myCourseEvents);
        }

        public async Task<IActionResult> DailySchedule(DateOnly? date)
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null) return RedirectToAction("Login", "Account");

            var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);

            // ---------- 1. 團體課程 ----------
            // 查詢這個會員曾經預約過哪些課程（CourseId）
            var myCourseIds = await _context.Reservations
                .Where(r => r.MemberId == memberId && r.Status == 1)
                .Select(r => r.CourseId)
                .Distinct()
                .ToListAsync();

            // 只撈出這些課程在這一天有上課的排程
            var myCourses = await _context.CourseSchedules
                .Include(c => c.Coach).ThenInclude(co => co.Account)
                .Where(c =>
                    myCourseIds.Contains(c.CourseId)
                    && c.ClassStartDate <= targetDate
                    && c.ClassEndDate >= targetDate
                    && c.ClassTimeDayOfWeek == (int)targetDate.DayOfWeek
                )
                .ToListAsync();

            // ---------- 2. 一對一課程 ----------
            // 只抓這個會員今天有預約（且 status=1）的 1對1
            var oneOnOneList = await _context.PrivateSessions
                .Include(ps => ps.Time)
                    .ThenInclude(ct => ct.Coach)
                        .ThenInclude(co => co.Account)
                .Where(ps => ps.MemberId == memberId
                    && ps.Status == 1
                    && ps.Time.Date == targetDate)
                .ToListAsync();

            // ---------- 3. 組合 ViewModel ----------
            var myCourseEvents = new List<CourseEventViewModel>();

            // 團體課程
            myCourseEvents.AddRange(
                myCourses.Select(c => new CourseEventViewModel
                {
                    CourseTitle = c.Title,
                    ClassDate = targetDate,
                    TimeSlot = c.ClassTimeDaily ?? 0,
                    CoachName = c.Coach?.Account?.Name ?? "",
                    CourseId = c.CourseId,
                    
                })
            );

            // 一對一課程
            foreach (var ps in oneOnOneList)
            {
                var ct = ps.Time;
                if (ct != null)
                {
                    myCourseEvents.Add(new CourseEventViewModel
                    {
                        CourseTitle = "一對一課程",
                        ClassDate = ct.Date,
                        TimeSlot = ct.TimeSlot,
                        CoachName = ct.Coach?.Account?.Name ?? "",
                        // 下面可依需要新增其他欄位
                       
                    });
                }
            }

            // ---------- 4. 處理 weekDates 供切換 ----------
            var weekStart = targetDate.AddDays(-(int)targetDate.DayOfWeek);
            var weekDates = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).ToList();
            ViewBag.WeekDates = weekDates;
            ViewBag.Date = targetDate;

            return View(myCourseEvents);
        }




        [HttpGet]
        public IActionResult GetAllCourseEvents(DateTime? start, DateTime? end)
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null) return Unauthorized();

            var events = new List<object>();

            // ---------- 團體課程：依天分組 ----------
            var groupCourses = _context.Reservations
                .Include(r => r.Course)
                .Where(r => r.MemberId == memberId && r.Status == 1 && r.Course != null)
                .Select(r => r.Course)
                .Distinct()
                .ToList();

            // 用字典分天
            var groupDict = new Dictionary<string, List<object>>();
            foreach (var c in groupCourses)
            {
                string coachName = "";
                if (c.CoachId != null)
                {
                    var coach = _context.Coaches.FirstOrDefault(x => x.CoachId == c.CoachId);
                    if (coach != null)
                    {
                        var account = _context.Accounts.FirstOrDefault(a => a.MemberId == coach.AccountId && a.Admin == 2);
                        if (account != null)
                            coachName = account.Name;
                    }
                }

                if (c.ClassStartDate.HasValue && c.ClassEndDate.HasValue && c.ClassTimeDayOfWeek.HasValue && c.ClassTimeDaily.HasValue)
                {
                    var startDate = c.ClassStartDate.Value;
                    var endDate = c.ClassEndDate.Value;
                    for (var day = startDate; day <= endDate; day = day.AddDays(1))
                    {
                        if ((int)day.DayOfWeek == c.ClassTimeDayOfWeek)
                        {
                            string key = day.ToString("yyyy-MM-dd");
                            var detail = new
                            {
                                title = c.Title,
                                time = $"{6 + c.ClassTimeDaily.Value - 1}:00 - {6 + c.ClassTimeDaily.Value}:00",
                                coachName = coachName,
                                location = c.Location ?? ""
                            };
                            if (!groupDict.ContainsKey(key))
                                groupDict[key] = new List<object>();
                            groupDict[key].Add(detail);
                        }
                    }
                }
            }
            // 統一一天只產生一個點
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

            // ---------- 一對一課程：依天分組 ----------
            var privateSessionList = (
                from ps in _context.PrivateSessions
                join ct in _context.CoachTimes on ps.TimeId equals ct.TimeId
                join coach in _context.Coaches on ct.CoachId equals coach.CoachId
                join account in _context.Accounts on coach.AccountId equals account.MemberId
                where ps.MemberId == memberId
                    && ps.Status == 1
                    && ct.Status == 1
                    && account.Admin == 2
                select new
                {
                    ps.SessionId,
                    ct.Date,
                    ct.TimeSlot,
                    CoachName = account.Name
                }
            ).ToList();

            var oneOnOneDict = new Dictionary<string, List<object>>();
            foreach (var ps in privateSessionList)
            {
                var baseDate = ps.Date.ToDateTime(TimeOnly.MinValue);
                string key = baseDate.ToString("yyyy-MM-dd");
                int hour = 6 + (ps.TimeSlot - 1);
                var detail = new
                {
                    coachName = ps.CoachName,
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

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            // 1. 找到課程
            var course = await _context.CourseSchedules.FirstOrDefaultAsync(c => c.CourseId == id);
            if (course == null)
                return NotFound();

            // 2. 撈所有報名這門課的 Reservation（狀態=1=已報名）
            var reservations = await _context.Reservations
                .Where(r => r.CourseId == id && r.Status == 1)
                .ToListAsync();

            // 3. 根據 MemberId 去 Accounts 撈出名字
            var memberIds = reservations.Select(r => r.MemberId).Distinct().ToList();
            var students = await _context.Accounts
                .Where(a => memberIds.Contains(a.MemberId))
                .ToListAsync();

            // 4. 傳到 ViewBag 或組一個 ViewModel
            ViewBag.Students = students;   // 這裡 students 會是 List<Account>，Account 裡面有 Name

            ViewBag.CourseTitle = course.Title;

            return View();
        }

        [HttpPost]
        public IActionResult AjaxSignUp(int courseId)
        {
            var memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null)
                return Json(new { success = false, message = "未登入，請先登入會員" });

            var course = _context.CourseSchedules.FirstOrDefault(c => c.CourseId == courseId);
            if (course == null)
                return Json(new { success = false, message = "查無此課程" });

            var member = _context.Accounts.FirstOrDefault(a => a.MemberId == memberId);
            if (member == null)
                return Json(new { success = false, message = "查無會員" });

            int price = course.Price ?? 0;

            // 是否已報名過
            var reservation = _context.Reservations.FirstOrDefault(r => r.MemberId == memberId && r.CourseId == courseId);

            // 已報名（Status=1）
            if (reservation != null && reservation.Status == 1)
                return Json(new { success = false, message = "你已經報名過此課程" });

            // 人數判斷（避免超收，建議加）
            int currentCount = _context.Reservations.Count(r => r.CourseId == courseId && r.Status == 1);
            int maxCount = course.MaxStudent ?? int.MaxValue;
            if (currentCount >= maxCount)
                return Json(new { success = false, message = "課程人數已滿，無法再報名" });

            // 點數判斷
            if (member.Point < price)
                return Json(new { success = false, message = "點數不足，請先儲值" });

            // 狀況1：沒有任何紀錄，新增
            if (reservation == null)
            {
                reservation = new Reservation
                {
                    MemberId = memberId.Value,
                    CourseId = courseId,
                    Status = 1,
                    IsNoShow = false,
                    Note = null
                };
                _context.Reservations.Add(reservation);
            }
            else if (reservation.Status == 2) // 狀況2：之前取消過，要改成已報名
            {
                reservation.Status = 1;
            }

            // 扣點
            _context.PointLogs.Add(new PointLog
            {
                MemberId = member.MemberId,
                AlterationTime = DateTime.Now,
                OriginalPoint = member.Point,
                FinallPoint = member.Point - price,
                Detail = $"報名 {course.Title} 課程 扣 {price} 點"
            });
            member.Point -= price;

            _context.SaveChanges();
            return Json(new { success = true, message = $"報名成功！已扣除 {price} 點。" });

        }

        [HttpPost]
public IActionResult AjaxCancelSignUp(int courseId)
{
    var memberId = HttpContext.Session.GetInt32("MemberId");
    if (memberId == null)
        return Json(new { success = false, message = "未登入，請先登入會員" });

    var reservation = _context.Reservations.FirstOrDefault(r => r.MemberId == memberId && r.CourseId == courseId && r.Status == 1);
    if (reservation == null)
        return Json(new { success = false, message = "你沒有報名過該課程" });

    if (reservation.Status == 2)
        return Json(new { success = false, message = "你已經取消過，請重新報名後再取消" });

    // 找課程
    var course = _context.CourseSchedules.FirstOrDefault(c => c.CourseId == courseId);
    if (course == null)
        return Json(new { success = false, message = "查無此課程" });

    int price = course.Price ?? 0;

    // 找會員
    var member = _context.Accounts.FirstOrDefault(a => a.MemberId == memberId);
    if (member == null)
        return Json(new { success = false, message = "查無會員" });

    // 取出起訖日期
    var startDate = course.ClassStartDate;
    var endDate = course.ClassEndDate;

    if (!startDate.HasValue || !endDate.HasValue)
        return Json(new { success = false, message = "課程時間設定錯誤，無法退費" });

    // 計算總堂數（每週一堂課）
    int totalWeeks = (int)((endDate.Value.ToDateTime(TimeOnly.MinValue) - startDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays / 7) + 1;

    // 每堂課金額
    decimal pricePerClass = (totalWeeks > 0) ? ((decimal)price / totalWeeks) : price;

    // 計算剩餘堂數（含今天未上課的堂數才可退費）
    int remainWeeks = 0;
    var today = DateOnly.FromDateTime(DateTime.Today);

    for (var d = startDate.Value; d <= endDate.Value; d = d.AddDays(7))
    {
        if (d >= today)
            remainWeeks++;
    }

    // 算退還點數
    int refund = (int)Math.Round(pricePerClass * remainWeeks);

    // 執行取消與退點
    reservation.Status = 2;
    int refundToUser = refund > 0 ? refund : 0;

    if (refundToUser > 0)
    {
        _context.PointLogs.Add(new PointLog
        {
            MemberId = member.MemberId,
            AlterationTime = DateTime.Now,
            OriginalPoint = member.Point,
            FinallPoint = member.Point + refundToUser,
            Detail = $"取消 {course.Title} 課程 退回 {refundToUser} 點"
        });
        member.Point += refundToUser;
    }

    _context.SaveChanges();

    return Json(new
    {
        success = true,
        message = refundToUser > 0
            ? $"你已成功取消報名，退回 {refundToUser} 點"
            : "你已成功取消報名（已上課不退費）"
    });
}

    }


}
