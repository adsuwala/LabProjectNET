using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LabApp.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        [Precision(18, 2)]
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        [Precision(18, 2)]
        public decimal UnitPrice { get; set; }
        [Precision(18, 2)]
        public decimal? PromoPrice { get; set; }
        public int Quantity { get; set; }
        [Precision(18, 2)]
        public decimal LineTotal { get; set; }

        public Order? Order { get; set; }
    }
}
