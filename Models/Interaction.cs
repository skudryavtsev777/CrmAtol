using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using static CrmAtol.Models.Enums;

namespace CrmAtol.Models
{
    public class Interaction
    {
        public int Id { get; set; }

        [Display(Name = "Заявка")]
        public int RequestId { get; set; }
        public Request Request { get; set; }

        [Display(Name = "Дата/время")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Тип")]
        public InteractionType Type { get; set; }

        [Required(ErrorMessage = "Содержание обязательно")]
        [Display(Name = "Содержание")]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; }

        [Display(Name = "Автор")]
        public string AuthorUserId { get; set; }
        public IdentityUser AuthorUser { get; set; }
    }
}
