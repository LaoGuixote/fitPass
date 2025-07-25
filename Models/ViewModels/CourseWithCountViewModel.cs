namespace fitPass.Models.ViewModels
{
    public class CourseWithCountViewModel
    {
        public CourseSchedule Course { get; set; }
        public int ReservationCount { get; set; }
        public List<Account>? RegisteredMembers { get; set; }
    }
}
