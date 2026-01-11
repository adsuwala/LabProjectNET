namespace LabApp.Models
{
    public class ProductListViewModel
    {
        public IEnumerable<Product> Products { get; set; } = Enumerable.Empty<Product>();
        public IEnumerable<string> Categories { get; set; } = Enumerable.Empty<string>();
        public string? SelectedCategory { get; set; }
        public string? SearchTerm { get; set; }
    }
}
