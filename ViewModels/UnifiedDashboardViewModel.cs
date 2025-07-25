using fitPass.Models;

namespace fitPass.ViewModels
{
    public class UnifiedDashboardViewModel
    {
        public Account Member { get; set; }
        public List<CourseEventViewModel> UpcomingCourses { get; set; }

        public bool HasValidSubscription { get; set; }

        public List<News> NewsList { get; set; }
        public int PeopleNow { get; set; }
        public bool IsCheckedIn { get; set; }

        // 教練專區資料
        public bool IsCoach { get; set; }
        public int? CoachId { get; set; }
        public string? CoachPhoto { get; set; }
        public string? Specialty { get; set; }
        public string? Description { get; set; }
        public int UpcomingClassCount { get; set; }
        public int ScheduledSlotsCount { get; set; }
    }
}
