namespace GESTION_LTIPN.Models
{
    public class DashboardViewModel
    {
        // Overall Statistics
        public int TotalBookings { get; set; }
        public int TotalVoyages { get; set; }
        public int TotalSocieties { get; set; }
        public int TotalCamions { get; set; }
        public int TotalUsers { get; set; }

        // Booking Statistics
        public int PendingBookings { get; set; }
        public int ValidatedBookings { get; set; }
        public Dictionary<string, int> BookingsByStatus { get; set; } = new();
        public Dictionary<string, int> BookingsByTypeVoyage { get; set; } = new();
        public List<RecentBooking> RecentBookings { get; set; } = new();

        // Voyage Statistics
        public int PlannedVoyages { get; set; }
        public int InProgressVoyages { get; set; }
        public int CompletedVoyages { get; set; }
        public Dictionary<string, int> VoyagesByStatus { get; set; } = new();
        public Dictionary<string, int> VoyagesByDepartureCity { get; set; } = new();
        public Dictionary<string, int> VoyagesByDepartureType { get; set; } = new();
        public List<RecentVoyage> RecentVoyages { get; set; } = new();

        // Society Statistics
        public int ActiveSocieties { get; set; }
        public List<TopSociety> TopSocieties { get; set; } = new();

        // Camion Statistics
        public int ActiveCamions { get; set; }
        public int AvailableCamions { get; set; }
        public int CamionsInUse { get; set; }
        public Dictionary<string, int> CamionsByType { get; set; } = new();

        // Monthly Trends
        public Dictionary<string, int> BookingsByMonth { get; set; } = new();
        public Dictionary<string, int> VoyagesByMonth { get; set; } = new();
    }

    public class RecentBooking
    {
        public string? BookingReference { get; set; }
        public string? SocietyName { get; set; }
        public string? TypeVoyage { get; set; }
        public int NbrLTC { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? BookingStatus { get; set; }
    }

    public class RecentVoyage
    {
        public int VoyageId { get; set; }
        public int VoyageNumber { get; set; }
        public string? BookingReference { get; set; }
        public string? DepartureCity { get; set; }
        public DateTime? DepartureDate { get; set; }
        public string? VoyageStatus { get; set; }
    }

    public class TopSociety
    {
        public string? SocietyName { get; set; }
        public int BookingCount { get; set; }
        public int VoyageCount { get; set; }
    }
}
