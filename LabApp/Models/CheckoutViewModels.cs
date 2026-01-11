using System.ComponentModel.DataAnnotations;

namespace LabApp.Models
{
    public class CheckoutInputModel
    {
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Imie i nazwisko")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [RegularExpression(@"\d{9}", ErrorMessage = "Numer telefonu powinien zawierac 9 cyfr.")]
        [Display(Name = "Numer telefonu")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Ulica i numer")]
        public string Street { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Kod pocztowy")]
        [RegularExpression(@"\d{2}-\d{3}", ErrorMessage = "Kod pocztowy w formacie 00-000.")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Miasto")]
        public string City { get; set; } = string.Empty;

        [Display(Name = "Akceptuje regulamin")]
        public bool AcceptTerms { get; set; }

        [Display(Name = "Zakladam konto")]
        public bool CreateAccount { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Haslo")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Powtorz haslo")]
        public string? ConfirmPassword { get; set; }
    }

    public class CartPageViewModel
    {
        public List<CartItem> Items { get; set; } = new();
        public CheckoutInputModel Checkout { get; set; } = new();
        public bool EmailReadOnly { get; set; }
        public decimal Total => Items.Sum(i => i.Total);
    }
}
