namespace fitPass.ViewModels
{
    public class CoachDashboardViewModel
    {
        public string Name { get; set; }
        public string? Specialty { get; set; }
        public string? Description { get; set; }
        public string? CoachPhoto { get; set; }
        public int UpcomingClassCount { get; set; }
        public int ScheduledSlotsCount { get; set; }
    }
}
