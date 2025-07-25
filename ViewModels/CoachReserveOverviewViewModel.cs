namespace fitPass.ViewModels
{
    public class CoachReserveOverviewViewModel
    {
        public string MemberName { get; set; }
        public DateOnly Date { get; set; }
        public int TimeSlot { get; set; }
        public int Status { get; set; }
        public string TimeRange => $"{(5 + TimeSlot):00}:00~{(6 + TimeSlot):00}:00";
    }
}
