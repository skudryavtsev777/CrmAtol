using System.ComponentModel.DataAnnotations;

namespace CrmAtol.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Наименование обязательно")]
        [Display(Name = "Наименование")]
        public string Name { get; set; }

        [Display(Name = "ИНН")]
        [RegularExpression(@"^\d{10}$|^\d{12}$", ErrorMessage = "ИНН должен содержать 10 или 12 цифр")]
        public string Inn { get; set; }

        [Display(Name = "Телефон")]
        [Phone]
        public string Phone { get; set; }

        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Адрес")]
        public string Address { get; set; }
    }
}
