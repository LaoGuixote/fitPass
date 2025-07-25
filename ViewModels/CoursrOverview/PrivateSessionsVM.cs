namespace fitPass.ViewModels.CoursrOverview
{
    public class PrivateSessionsVM
    {
        public string CoachName { get; set; } = "";
        public DateOnly Date { get; set; }
        public int TimeSlot { get; set; }
        public int TimeId { get; set; }
        public int Status { get; set; } 
        public int SessionId { get; set; }
    }
}
