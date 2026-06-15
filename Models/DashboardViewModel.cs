namespace CrmAtol.Models
{
    public class DashboardViewModel
    {
        public int TotalRequests { get; set; }
        public int OpenRequests { get; set; }
        public int ClosedRequests { get; set; }
        public int OverdueRequests { get; set; }

        public List<StatusCount> RequestsByStatus { get; set; }
        public List<PriorityCount> RequestsByPriority { get; set; }
        public List<EmployeeLoad> EmployeeLoad { get; set; }
    }

    public class StatusCount
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class PriorityCount
    {
        public string Priority { get; set; }
        public int Count { get; set; }
    }

    public class EmployeeLoad
    {
        public string UserName { get; set; }
        public int Count { get; set; }
    }
}