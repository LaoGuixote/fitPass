namespace fitPass.ViewModels.CoursrOverview
{
    public class GroupCourse
    {
        public string Title { get; set; } = "";
        public int CourseId { get; set; } 
        public string CoachName { get; set; } = "";
        public DateOnly? ClassStartDate { get; set; }
        public DateOnly? ClassEndDate { get; set; }
        public int? ClassTimeDaily { get; set; }
        public byte[]? CourseImage { get; set; }
    }
}
