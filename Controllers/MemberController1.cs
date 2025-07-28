using fitPass.Models;
using fitPass.ViewModels;
using fitPass.ViewModels.CoursrOverview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.EntityFrameworkCore;


//[Authorize]
public class MemberController : Controller
{
    private readonly GymManagementContext _context;

    public MemberController(GymManagementContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var memberId = HttpContext.Session.GetInt32("MemberId");
        if (memberId == null) return RedirectToAction("Login", "Account");

        var member = await _context.Accounts.FirstOrDefaultAsync(a => a.MemberId == memberId);
        if (member == null) return NotFound();

        var latestSub = await _context.SubscriptionLogs
            .Where(x => x.MemberId == memberId)
            .OrderByDescending(x => x.SubscriptionId)
            .FirstOrDefaultAsync();

        bool hasValidSubscription = latestSub != null && latestSub.EndDate >= DateOnly.FromDateTime(DateTime.Today);


        var courseEvents = new List<CourseEventViewModel>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        // 團課資料（Reservations + CourseSchedules）
        var groupReservations = await _context.Reservations
            .Include(r => r.Course).ThenInclude(c => c.Coach).ThenInclude(co => co.Account)
            .Where(r => r.MemberId == memberId && r.Status == 1)
            .ToListAsync();

        foreach (var r in groupReservations)
        {
            var c = r.Course;
            if (c?.ClassStartDate.HasValue == true &&
                c.ClassEndDate.HasValue &&
                c.ClassTimeDayOfWeek.HasValue &&
                c.ClassTimeDaily.HasValue)
            {
                var date = today;
                while (date <= c.ClassEndDate.Value)
                {
                    if ((int)date.DayOfWeek == c.ClassTimeDayOfWeek && date >= today)
                    {
                        courseEvents.Add(new CourseEventViewModel
                        {
                            CourseId = c.CourseId,
                            CourseTitle = c.Title,
                            CoachName = c.Coach?.Account?.Name ?? "",
                            ClassDate = date,
                            TimeSlot = c.ClassTimeDaily.Value,
                            CourseType = "Group"
                        });
                        break; // 只取最近一堂
                    }
                    date = date.AddDays(1);
                }
            }
        }
        // 補：一對一課程（來自 PrivateSessions + CoachTime）
        var oneOnOneList = await _context.PrivateSessions
            .Include(ps => ps.Time).ThenInclude(t => t.Coach).ThenInclude(c => c.Account)
            .Where(ps => ps.MemberId == memberId && ps.Status == 1)
            .ToListAsync();

        foreach (var ps in oneOnOneList)
        {
            var t = ps.Time;
            if (t != null)
            {
                courseEvents.Add(new CourseEventViewModel
                {
                    CourseTitle = "一對一課程",
                    CoachName = t.Coach?.Account?.Name ?? "",
                    ClassDate = t.Date,
                    TimeSlot = t.TimeSlot,
                    CourseType = "Private"
                });
            }
        }



        var news = await _context.News
            .Where(n => n.IsVisible == true)
            .OrderByDescending(n => n.PublishTime)
            .Take(3)
            .ToListAsync();

        var isCheckedIn = await _context.CheckInRecords
            .AnyAsync(r => r.MemberId == memberId && r.CheckInTime.Value.Date == DateTime.Today && r.CheckOutTime == null);

        var peopleNow = await _context.CheckInRecords
            .CountAsync(r => r.CheckInTime.Value.Date == DateTime.Today && r.CheckOutTime == null);

        // 🔍 檢查是否為教練
        var coach = await _context.Coaches
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.AccountId == memberId);

        // 撈教練用的統計資料（如果是教練才查）
        int coachClassCount = 0;
        int scheduledSlotsCount = 0;
        if (coach != null)
        {
            coachClassCount = await _context.CourseSchedules
                .CountAsync(c => c.CoachId == coach.CoachId && c.ClassStartDate >= DateOnly.FromDateTime(DateTime.Today));

            scheduledSlotsCount = await _context.CoachTimes
                .CountAsync(c => c.CoachId == coach.CoachId);
        }

        var viewModel = new UnifiedDashboardViewModel
        {
            Member = member,
            UpcomingCourses = courseEvents.OrderBy(e => e.ClassDateTime).Take(3).ToList(),
            NewsList = news,
            PeopleNow = peopleNow,
            IsCheckedIn = isCheckedIn,
            HasValidSubscription = hasValidSubscription,

            // 教練資料（可為 null）
            IsCoach = coach != null,
            CoachId = coach?.CoachId,
            CoachPhoto = coach?.Photo != null ? $"data:image/jpeg;base64,{Convert.ToBase64String(coach.Photo)}" : null,
            Specialty = coach?.Specialty,
            Description = coach?.Description,
            UpcomingClassCount = coachClassCount,
            ScheduledSlotsCount = scheduledSlotsCount
        };

        ViewData["MemberName"] = member.Name;
        ViewData["PeopleNow"] = viewModel.PeopleNow;

        bool isProfileIncomplete = string.IsNullOrEmpty(member.Phone) || member.Birthday == null;

        ViewBag.IsProfileIncomplete = isProfileIncomplete;
        ViewBag.MemberName = member.Name;

        ViewBag.recommend = _context.CourseSchedules.Where(x => x.ClassStartDate > DateOnly.FromDateTime(DateTime.Now)).ToList();

        return View(viewModel);
    }


    [HttpGet]
    public IActionResult Edit()
    {
        var memberId = HttpContext.Session.GetInt32("MemberId");
        var member = _context.Accounts.FirstOrDefault(a => a.MemberId == memberId);
        return View(member);
    }

    [HttpPost]
    public IActionResult Edit(Account updated, [FromForm(Name = "Phone")] List<string> selectedInterestsList)
    {
        var memberId = HttpContext.Session.GetInt32("MemberId");
        var member = _context.Accounts.FirstOrDefault(a => a.MemberId == memberId);
        if (member == null) return NotFound();

        member.Name = updated.Name;
        member.Email = updated.Email;
        member.Gender = updated.Gender;
        member.Birthday = updated.Birthday;

        if (selectedInterestsList != null && selectedInterestsList.Any())
        {
            member.Phone = string.Join(",", selectedInterestsList);
        }
        else
        {
            member.Phone = ""; // 如果沒有選取任何興趣，將 Phone 欄位設為空字串
        }

        _context.SaveChanges();
        TempData["Msg"] = "會員資料已更新";
        return RedirectToAction("Index");
    }
    [HttpPost]
    public IActionResult CheckInOut()
    {
        var memberId = HttpContext.Session.GetInt32("MemberId");
        if (memberId == null) return Unauthorized();

        var today = DateTime.Today;

        var record = _context.CheckInRecords
            .FirstOrDefault(r => r.MemberId == memberId && r.CheckInTime.Value.Date == today && r.CheckOutTime == null);

        string currentStatus;

        if (record == null)
        {
            // 入場
            _context.CheckInRecords.Add(new CheckInRecord
            {
                MemberId = memberId.Value,
                CheckInTime = DateTime.Now,
                Status = "正常入場",
                Device = "WebQRCode",
                CheckType = 1
            });
            currentStatus = "已入場";
        }
        else
        {
            // 出場
            record.CheckOutTime = DateTime.Now;
            record.Status = "正常退場";
            currentStatus = "已退場";
        }

        _context.SaveChanges();

        int peopleNow = _context.CheckInRecords
            .Count(r => r.CheckInTime.Value.Date == today && r.CheckOutTime == null);

        return Json(new { success = true, peopleNow, currentStatus });
    }

    [HttpGet]
    public async Task<IActionResult> MemberCourseOverview(string groupFilter, string privateFilter, string keyword, string privateCoach, DateOnly? privateDate)
    {
        var memberId = HttpContext.Session.GetInt32("MemberId");
        if (memberId == null)
            return RedirectToAction("Login", "Member");

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        var groupCoursesQuery = _context.Reservations
        .Include(r => r.Course)
        .ThenInclude(c => c.Coach)
        .ThenInclude(a => a.Account)
        .Where(r => r.MemberId == memberId && r.Course != null);

        if (!string.IsNullOrEmpty(keyword))
        {
            groupCoursesQuery = groupCoursesQuery
                .Where(r =>
                    r.Course.Title.Contains(keyword) ||
                    r.Course.Coach.Account.Name.Contains(keyword));
        }

        if (string.IsNullOrWhiteSpace(groupFilter))
        {
            groupFilter = "active";
        }

        if (groupFilter == "expired")
        {
            groupCoursesQuery = groupCoursesQuery
                .Where(r => r.Course.ClassEndDate <= today);
        }
        else if (groupFilter == "active")
        {
            groupCoursesQuery = groupCoursesQuery
                .Where(r => r.Course.ClassEndDate > today);
        }

        var groupCourses = await groupCoursesQuery
       .Select(r => new GroupCourse
       {
           Title = r.Course.Title,
           CourseId = r.CourseId,
           CoachName = r.Course.Coach.Account.Name,
           ClassStartDate = r.Course.ClassStartDate,
           ClassEndDate = r.Course.ClassEndDate,
           ClassTimeDaily = r.Course.ClassTimeDaily,
           CourseImage = r.Course.CourseImage
       })
       .OrderBy(r => r.CourseId)
       .ToListAsync();

        var privateQuery = _context.PrivateSessions
        .Include(ps => ps.Time)
        .ThenInclude(c => c.Coach)
        .ThenInclude(a => a.Account)
        .Where(p => p.MemberId == memberId && p.Status == 1);

        if (!string.IsNullOrEmpty(privateCoach))
        {
            privateQuery = privateQuery.Where(p => p.Time.Coach.Account.Name.Contains(privateCoach));
        }

        if (string.IsNullOrWhiteSpace(privateFilter))
        {
            privateFilter = "active";
        }

        if (privateDate.HasValue)
        {
            privateQuery = privateQuery
                .Where(p => p.Time.Date == privateDate.Value);
        }
        else if (privateFilter == "expired")
        {
            privateQuery = privateQuery
                .Where(p => p.Time.Date <= today);
        }
        else if (privateFilter == "active")
        {
            privateQuery = privateQuery
                .Where(p => p.Time.Date > today);
        }

        var privateSessions = await privateQuery
            .Select(p => new PrivateSessionsVM
            {
                SessionId = p.SessionId,
                Status = p.Status ?? 0,
                CoachName = p.Time.Coach.Account.Name,
                Date = p.Time.Date,
                TimeSlot = p.Time.TimeSlot,
                TimeId = p.TimeId
            })
            .OrderBy(p => p.Date)
            .ToListAsync();

        var courseOverview = new CourseOverview
        {
            GroupCourses = groupCourses,
            PrivateCourses = privateSessions
        };

        return View(courseOverview);
    }

    [HttpPost]
    public IActionResult CancelPrivate(int id)
    {
        var memberId = HttpContext.Session.GetInt32("MemberId");
        if (memberId == null)
        {
            return Json(new { succes = false, message = "請先登入" });
        }

        var member = _context.Accounts.FirstOrDefault(a => a.MemberId == memberId);
        if (member == null)
        {
            return Json(new { succes = false, message = "找不到會員資料" });
        }

        var session = _context.PrivateSessions.FirstOrDefault(p => p.SessionId == id);
        if (session == null)
        {
            return Json(new { succes = false, message = "找不到該預約紀錄" });
        }

        session.Status = 2;
        int refundPrice = 299;
        member.Point += refundPrice;

        var coachTime = _context.CoachTimes.FirstOrDefault(t => t.TimeId == session.TimeId);
        if (coachTime != null)
            coachTime.Status = 0;

        _context.SaveChanges();

        return Json(new
        {
            succes = true,
            message = "預約已成功取消"
        });
    }

}
