namespace fitPass.Models.ViewModels
{
    public class CheckInRecordStatusViewModel
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = null!;
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public int Status { get; set; } // 1: 入場中, 2: 已退場
        public int RecordId { get; set; }
    }
}
