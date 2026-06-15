using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using static CrmAtol.Models.Enums;

namespace CrmAtol.Models
{
    public class Request
    {
        public int Id { get; set; }

        [Display(Name = "Номер")]
        public string Number { get; set; }

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Выберите клиента")]
        [Display(Name = "Клиент")]
        public int ClientId { get; set; }
        public Client Client { get; set; }

        [Required(ErrorMessage = "Тема обязательна")]
        [Display(Name = "Тема")]
        [StringLength(250)]
        public string Subject { get; set; }

        [Display(Name = "Описание")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Приоритет")]
        public RequestPriority Priority { get; set; } = RequestPriority.Средний;

        [Display(Name = "Статус")]
        public RequestStatus Status { get; set; } = RequestStatus.Новая;

        [Display(Name = "Исполнитель")]
        public string? AssignedToUserId { get; set; }
        public IdentityUser AssignedToUser { get; set; }

        [Display(Name = "Срок реакции")]
        public DateTime DeadlineReaction { get; set; }

        [Display(Name = "Срок решения")]
        public DateTime DeadlineResolution { get; set; }

        [Display(Name = "Дата закрытия")]
        public DateTime? ClosedAt { get; set; }

        [Display(Name = "Результат")]
        [DataType(DataType.MultilineText)]
        public string? Resolution { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
