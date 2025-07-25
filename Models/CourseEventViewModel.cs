namespace fitPass.Models
{
    public class CourseEventViewModel
    {
        public string CourseTitle { get; set; }
        public DateOnly ClassDate { get; set; }
        public int TimeSlot { get; set; }
        public string CoachName { get; set; }

        public int ClassTimeDaily { get; set; }    // 可選
        public string Title { get; set; }          // 可選
        public int CourseId { get; set; }

        // 新增欄位 ✅
        public string CourseType { get; set; } = "";

        // 自動轉換為 DateTime，方便 Razor 用來排序與顯示時間
        public DateTime ClassDateTime => ClassDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(8 + (TimeSlot - 1))));
    }
}
