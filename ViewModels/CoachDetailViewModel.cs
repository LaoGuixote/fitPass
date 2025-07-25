using System;

namespace fitPass.ViewModels
{
    public class CoachDetailViewModel
    {
        public int CoachId { get; set; }
        public string? Name { get; set; }
        public string? Specialty { get; set; }
        public string? Description { get; set; }
        public string? CoachPhoto { get; set; }
        public int CoachType { get; set; }
    }
}
