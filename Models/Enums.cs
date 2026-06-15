namespace CrmAtol.Models
{
    public class Enums
    {
        public enum RequestPriority
        {
            Низкий,
            Средний,
            Высокий,
            Критический
        }

        public enum RequestStatus
        {
            Новая,
            Выполняется,
            Выполнена,
            Закрыта
        }

        public enum InteractionType
        {
            Звонок,
            Email,
            Встреча,
            Сообщение
        }
    }
}
