namespace fitPass.Models
{
    public class MemberDashboardViewModel
    {
        public Account? Member { get; set; }
        public List<Reservation>? Reservations { get; set; }
        public List<News>? NewsList { get; set; }
        public int PeopleNow { get; set; }
        public bool IsCheckedIn { get; set; }
    }

}
