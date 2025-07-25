namespace fitPass.Models.ViewModels
{
    public class CoachPrivateScheduleViewModel
    {
        public int CoachId { get; set; }
        public string CoachName { get; set; } = string.Empty;
        public string? Specialty { get; set; }
        public byte[]? Photo { get; set; }

        public List<CoachTimeInfo> CoachTimes { get; set; } = new();
    }

    public class CoachTimeInfo
    {
        public DateOnly Date { get; set; }
        public int TimeSlot { get; set; }
        public bool IsReserved { get; set; }
        public string? MemberName { get; set; }
    }
}
