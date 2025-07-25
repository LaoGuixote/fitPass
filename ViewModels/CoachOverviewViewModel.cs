using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace fitPass.ViewModels
{
    public class CoachOverviewViewModel
    {
        public int CoachId { get; set; }
        public string Name { get; set; }
        public string? Specialty { get; set; }
        public string? CoachPhoto { get; set; }
        public int? CoachType { get; set; }
    }
}
