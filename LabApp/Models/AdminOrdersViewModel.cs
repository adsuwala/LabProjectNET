namespace LabApp.Models
{
    public class AdminOrdersViewModel
    {
        public IEnumerable<Order> Orders { get; set; } = Enumerable.Empty<Order>();
        public string? SearchTerm { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }
}
