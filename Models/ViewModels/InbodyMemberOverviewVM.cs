namespace fitPass.Models.ViewModels
{
    public class InbodyMemberOverviewVM
    {
        public int MemberId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int InbodyCount { get; set; }
        public DateOnly? LatestRecordDate { get; set; }
    }
}
